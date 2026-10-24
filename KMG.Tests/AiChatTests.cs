using System.Security.Claims;
using System.Text.Json.Nodes;
using KMG.Api.Ai;
using KMG.Core.DTOs.AiChat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace KMG.Tests
{
    public class AiChatToolsTests
    {
        private static readonly ClaimsPrincipal User = new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "u1") }, "test"));

        private static AiChatTools CreateTools(params string[] allowedAbilities) =>
            CreateTools(new ServiceCollection(), allowedAbilities);

        private static AiChatTools CreateTools(IServiceCollection services, params string[] allowedAbilities) =>
            new(new FakeAuthorizationService(allowedAbilities), services.BuildServiceProvider(), NullLogger<AiChatTools>.Instance);

        [Fact]
        public void Declarations_ExposeOnlyTheReadOnlyToolSet()
        {
            var names = CreateTools().GetFunctionDeclarations().Select(d => d!["name"]!.GetValue<string>()).OrderBy(n => n, StringComparer.Ordinal);

            Assert.Equal(new[]
            {
                "get_alerts", "get_material", "get_missions", "get_overview", "get_payroll", "get_project", "get_treasury", "no_data_needed",
                "search_clients", "search_employees", "search_invoices", "search_materials", "search_projects", "search_suppliers", "search_tenders"
            }, names);
        }

        [Theory]
        [InlineData("search_materials")]
        [InlineData("get_material")]
        [InlineData("get_overview")]
        [InlineData("search_projects")]
        [InlineData("get_project")]
        [InlineData("get_missions")]
        [InlineData("search_clients")]
        [InlineData("search_suppliers")]
        [InlineData("get_treasury")]
        [InlineData("search_invoices")]
        [InlineData("search_employees")]
        [InlineData("get_payroll")]
        [InlineData("search_tenders")]
        public async Task Tool_WithoutAbility_ReturnsAccessDeniedWithoutTouchingData(string tool)
        {
            var result = await CreateTools().ExecuteAsync(tool, new JsonObject { ["id"] = 1 }, User);

            Assert.Equal("access_denied", result["error"]!.GetValue<string>());
            Assert.Null(result["result"]);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData(0)]
        [InlineData(1.5)]
        public async Task GetProject_InvalidId_ReturnsInvalidArgument(object id)
        {
            var args = new JsonObject { ["id"] = JsonValue.Create(id) };
            var result = await CreateTools("عرض المشاريع").ExecuteAsync("get_project", args, User);

            Assert.Equal("invalid_argument", result["error"]!.GetValue<string>());
        }

        [Fact]
        public async Task UnknownTool_IsRejected()
        {
            var result = await CreateTools("عرض المشاريع").ExecuteAsync("delete_project", new JsonObject { ["id"] = 1 }, User);

            Assert.Equal("unknown_tool", result["error"]!.GetValue<string>());
        }

        [Fact]
        public async Task NoDataTool_NeedsNoAbility()
        {
            var result = await CreateTools().ExecuteAsync(AiChatTools.NoDataToolName, null, User);

            Assert.True(result["result"]!["acknowledged"]!.GetValue<bool>());
        }

        [Fact]
        public async Task ToolFailure_IsReturnedAsSafeErrorInsteadOfThrowing()
        {
            var result = await CreateTools().ExecuteAsync("get_alerts", null, User);

            Assert.Equal("tool_failed", result["error"]!.GetValue<string>());
        }

        private class FakeAuthorizationService : IAuthorizationService
        {
            private readonly HashSet<string> _allowed;

            public FakeAuthorizationService(IEnumerable<string> abilities) =>
                _allowed = abilities.Select(a => $"Ability:{a}").ToHashSet();

            public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName) =>
                Task.FromResult(_allowed.Contains(policyName) ? AuthorizationResult.Success() : AuthorizationResult.Failed());

            public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements) =>
                Task.FromResult(AuthorizationResult.Failed());
        }
    }

    public class ToolArgsTests
    {
        [Theory]
        [InlineData("5", 5)]
        [InlineData(5.0, 5)]
        [InlineData(7, 7)]
        public void GetInt_AcceptsIntegerLikeValues(object raw, int expected)
        {
            var args = new ToolArgs(new JsonObject { ["limit"] = JsonValue.Create(raw) });

            Assert.Equal(expected, args.GetInt("limit", 10, 1, 25));
        }

        [Fact]
        public void GetInt_OutOfRange_Throws()
        {
            var args = new ToolArgs(new JsonObject { ["limit"] = 500 });

            Assert.Throws<ToolArgumentException>(() => args.GetInt("limit", 10, 1, 25));
        }

        [Fact]
        public void Missing_Values_UseDefaults()
        {
            var args = new ToolArgs(null);

            Assert.Equal(10, args.GetInt("limit", 10, 1, 25));
            Assert.Null(args.GetString("query", 100));
            Assert.False(args.GetBool("lowStockOnly"));
        }

        [Fact]
        public void GetString_TooLong_Throws()
        {
            var args = new ToolArgs(new JsonObject { ["query"] = new string('x', 101) });

            Assert.Throws<ToolArgumentException>(() => args.GetString("query", 100));
        }

        [Theory]
        [InlineData("Manufacturing", true)]
        [InlineData("manufacturing", true)]
        [InlineData("4", false)]
        [InlineData("DROP TABLE", false)]
        public void GetEnum_OnlyAcceptsDefinedNames(string raw, bool valid)
        {
            var args = new ToolArgs(new JsonObject { ["status"] = raw });

            if (valid)
                Assert.Equal(KMG.Core.Enums.ProjectStatus.Manufacturing, args.GetEnum<KMG.Core.Enums.ProjectStatus>("status"));
            else
                Assert.Throws<ToolArgumentException>(() => args.GetEnum<KMG.Core.Enums.ProjectStatus>("status"));
        }
    }

    public class ArabicTextTests
    {
        [Theory]
        [InlineData("ألوكوبند", "لوح الوكوبند")]
        [InlineData("واجهة", "واجهه كلادينج")]
        [InlineData("الكلادينج", "واجهة كلادينج - فرع جديد")]
        [InlineData("توشيبا كلادينج", "شركة توشيبا مصر", "واجهة كلادينج")]
        [InlineData("الألوكوبوند", "لوح ألوكوبند")]
        [InlineData("الوميتال", "بروفايل ألوميتال")]
        [InlineData(null, "anything")]
        public void ContainsAllTokens_Matches(string? query, params string[] fields) =>
            Assert.True(ArabicText.ContainsAllTokens(query, fields));

        [Fact]
        public void ContainsAllTokens_RequiresEveryToken() =>
            Assert.False(ArabicText.ContainsAllTokens("توشيبا زجاج", "شركة توشيبا مصر"));

        [Fact]
        public void ContainsAllTokens_SpellingFallbackStillRejectsUnrelatedWords() =>
            Assert.False(ArabicText.ContainsAllTokens("زجاج", "لوح ألوكوبند", "بروفايل ألوميتال"));
    }

    public class GeminiParsingTests
    {
        private static GeminiClient CreateClient() =>
            new(new HttpClient(), new ConfigurationBuilder().Build(), NullLogger<GeminiClient>.Instance);

        [Fact]
        public void ExtractCandidate_ReturnsFirstCandidate()
        {
            var candidate = CreateClient().ExtractCandidate("""{"candidates":[{"content":{"role":"model","parts":[{"text":"hi"}]},"finishReason":"STOP"}]}""");

            Assert.Equal("STOP", candidate["finishReason"]!.GetValue<string>());
        }

        [Theory]
        [InlineData("not json")]
        [InlineData("""{"candidates":[]}""")]
        [InlineData("""{"promptFeedback":{"blockReason":"SAFETY"}}""")]
        [InlineData("""{"candidates":[{"finishReason":"SAFETY"}]}""")]
        public void ExtractCandidate_UnusableResponse_ThrowsSafeException(string body) =>
            Assert.Throws<AiServiceException>(() => CreateClient().ExtractCandidate(body));

        [Fact]
        public void ParseRetryDelay_ReadsGoogleRetryInfo()
        {
            const string body = """{"error":{"code":429,"details":[{"@type":"type.googleapis.com/google.rpc.RetryInfo","retryDelay":"19.53s"}]}}""";

            Assert.Equal(TimeSpan.FromSeconds(20), GeminiClient.ParseRetryDelay(body));
            Assert.Null(GeminiClient.ParseRetryDelay("{}"));
        }

        [Fact]
        public void BuildContents_MapsAssistantToModelAndAppendsNewMessage()
        {
            var contents = GeminiChatClient.BuildContents(new AiChatRequestDTO
            {
                Message = "التاني تفاصيله إيه؟",
                History = new()
                {
                    new AiChatTurnDTO { Role = "user", Content = "هات المشاريع" },
                    new AiChatTurnDTO { Role = "assistant", Content = "1. A (#1)\n2. B (#2)" },
                    new AiChatTurnDTO { Role = "assistant", Content = "  " }
                }
            });

            Assert.Equal(new[] { "user", "model", "user" }, contents.Select(c => c!["role"]!.GetValue<string>()));
            Assert.Equal("التاني تفاصيله إيه؟", contents[2]!["parts"]![0]!["text"]!.GetValue<string>());
        }

        [Fact]
        public void ExtractText_SkipsThoughtParts()
        {
            var parts = JsonNode.Parse("""[{"text":"internal reasoning","thought":true},{"text":"الرد"}]""")!.AsArray();

            Assert.Equal("الرد", GeminiChatClient.ExtractText(parts, new JsonObject()));
        }

        [Fact]
        public void ExtractText_EmptyResponse_ThrowsSafeException()
        {
            var candidate = new JsonObject { ["finishReason"] = "MAX_TOKENS" };

            Assert.Throws<AiServiceException>(() => GeminiChatClient.ExtractText(new JsonArray(), candidate));
        }
    }
}
