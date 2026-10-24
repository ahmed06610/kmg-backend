using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using KMG.Core.Enums;
using KMG.Core.Helper;
using Microsoft.AspNetCore.Authorization;

namespace KMG.Api.Ai
{
    public partial class AiChatTools
    {
        private const string ViewDashboard = "عرض لوحة التحكم";
        private const string ViewProjects = "عرض المشاريع";
        private const string ViewStock = "عرض المخزن";
        private const string ViewCashBox = "عرض الخزنة";
        private const string ViewClients = "عرض العملاء";
        private const string ViewSuppliers = "عرض الموردين";
        private const string ViewEmployees = "عرض الموظفين";
        private const string ManageInvoices = "إدارة الفواتير";
        private const string ManagePayroll = "إدارة الرواتب";
        private const string ManageMissions = "إدارة المأموريات";
        private const string ManageAi = "إدارة تكامل AI";

        public const string NoDataToolName = "no_data_needed";

        private const int MaxQueryLength = 100;
        private const int MaxTextFieldLength = 400;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IAuthorizationService _authorizationService;
        private readonly IServiceProvider _services;
        private readonly ILogger<AiChatTools> _logger;
        private readonly Dictionary<string, bool> _abilityCache = new();
        private readonly Dictionary<string, ToolDefinition> _tools;

        public AiChatTools(IAuthorizationService authorizationService, IServiceProvider services, ILogger<AiChatTools> logger)
        {
            _authorizationService = authorizationService;
            _services = services;
            _logger = logger;

            _tools = GeneralTools()
                .Concat(ProjectTools())
                .Concat(StockTools())
                .Concat(PartyTools())
                .Concat(FinanceTools())
                .Concat(PeopleTools())
                .ToDictionary(t => t.Name);
        }

        private delegate Task<object> ToolHandler(ToolArgs args, ClaimsPrincipal user);

        private record ToolDefinition(string Name, string Description, JsonObject Parameters, string? RequiredAbility, ToolHandler Handler);

        private T Service<T>() where T : notnull => _services.GetRequiredService<T>();

        public JsonArray GetFunctionDeclarations() => new(_tools.Values.Select(t => (JsonNode)new JsonObject
        {
            ["name"] = t.Name,
            ["description"] = t.Description,
            ["parameters"] = t.Parameters.DeepClone()
        }).ToArray());

        public async Task<JsonObject> ExecuteAsync(string name, JsonObject? arguments, ClaimsPrincipal user)
        {
            if (!_tools.TryGetValue(name, out var tool))
                return Error("unknown_tool", $"No tool named '{name}' exists.");

            if (tool.RequiredAbility != null && !await HasAbilityAsync(user, tool.RequiredAbility))
                return Error("access_denied", "The current user is not permitted to view this data.");

            try
            {
                var result = await tool.Handler(new ToolArgs(arguments), user);
                return new JsonObject { ["result"] = JsonSerializer.SerializeToNode(result, JsonOptions) };
            }
            catch (ToolArgumentException ex)
            {
                return Error("invalid_argument", ex.Message);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "AI tool {Tool} failed", name);
                return Error("tool_failed", "The data could not be loaded right now.");
            }
        }

        private async Task<bool> HasAbilityAsync(ClaimsPrincipal user, string ability)
        {
            if (_abilityCache.TryGetValue(ability, out var allowed)) return allowed;

            allowed = (await _authorizationService.AuthorizeAsync(user, $"Ability:{ability}")).Succeeded;
            _abilityCache[ability] = allowed;
            return allowed;
        }

        private static JsonObject Error(string code, string message) =>
            new() { ["error"] = code, ["message"] = message };

        private static string Day(DateTime date) => date.ToString("yyyy-MM-dd");

        private static string? Day(DateTime? date) => date?.ToString("yyyy-MM-dd");

        private static string StatusLabel(string status) =>
            Enum.TryParse<ProjectStatus>(status, out var s) ? s.Arabic() : status;

        private static string ProjectTypeLabel(string type) => type switch
        {
            nameof(ProjectType.ManufactureExecution) => "تصنيع وتنفيذ",
            nameof(ProjectType.Subcontracting) => "مقاولات باطن",
            nameof(ProjectType.Supply) => "توريد",
            _ => type
        };

        private static string? CheckStatusLabel(bool isCheck, string? status) => !isCheck ? null : status switch
        {
            nameof(CheckStatus.Pending) => "شيك معلق (لسه متصرفش)",
            nameof(CheckStatus.Cleared) => "شيك اتصرف",
            nameof(CheckStatus.Cancelled) => "شيك ملغي",
            _ => status
        };

        private static JsonObject Schema(params (string Name, JsonObject Property)[] properties) => Schema(properties, null);

        private static JsonObject Schema((string Name, JsonObject Property) property, string required) => Schema(new[] { property }, required);

        private static JsonObject Schema((string Name, JsonObject Property)[] properties, string? required)
        {
            var schema = new JsonObject { ["type"] = "object" };
            if (properties.Length > 0)
                schema["properties"] = new JsonObject(properties.Select(p => KeyValuePair.Create(p.Name, (JsonNode?)p.Property)));
            if (required != null)
                schema["required"] = new JsonArray(required);
            return schema;
        }

        private static JsonObject StringProp(string description) => new() { ["type"] = "string", ["description"] = description };

        private static JsonObject IntProp(string description) => new() { ["type"] = "integer", ["description"] = description };

        private static JsonObject BoolProp(string description) => new() { ["type"] = "boolean", ["description"] = description };

        private static JsonObject DateProp(string description) => new() { ["type"] = "string", ["description"] = description + " Format: yyyy-MM-dd." };

        private static JsonObject EnumProp(string description, string[] values) => new()
        {
            ["type"] = "string",
            ["description"] = description,
            ["enum"] = new JsonArray(values.Select(v => (JsonNode)v).ToArray())
        };

        private static JsonObject EnumListProp(string description, string[] values) => new()
        {
            ["type"] = "array",
            ["description"] = description,
            ["items"] = EnumProp("Section name.", values)
        };
    }
}
