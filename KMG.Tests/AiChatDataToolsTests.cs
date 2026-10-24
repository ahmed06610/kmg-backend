using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using KMG.Api.Ai;
using KMG.Core.DTOs.Employee;
using KMG.Core.DTOs.Stock;
using KMG.Core.DTOs.Supplier;
using KMG.Core.Enums;
using KMG.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace KMG.Tests
{
    public class AiChatDataToolsTests
    {
        private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
        private static readonly ClaimsPrincipal User = new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "u1") }, "test"));

        private static JsonNode Json(object value) => JsonSerializer.SerializeToNode(value, Web)!;

        private static StockMovementDTO Movement(int id, MovementType type, decimal qty, decimal price, string date, int? projectId = null, string? supplier = null) => new()
        {
            Id = id,
            MovementType = type.ToString(),
            MaterialId = 1,
            MaterialName = "لوح ألوكوبند",
            Quantity = qty,
            UnitPriceAtTime = price,
            MovementDate = DateTime.Parse(date),
            ProjectId = projectId,
            ProjectCode = projectId == null ? null : "KMG-2026-0001",
            SupplierId = supplier == null ? null : 1,
            SupplierName = supplier,
            CreatedByEmployeeName = "x"
        };

        private static readonly MaterialDTO Alucobond = new() { Id = 1, Name = "لوح ألوكوبند", Unit = "متر مربع", Quantity = 150, MinimumThreshold = 50, UnitPrice = 300 };

        [Fact]
        public void MaterialDetails_ReturnsPurchaseHistoryMatchingStockScreen()
        {
            var movements = new[]
            {
                Movement(5, MovementType.IssueToProject, 150, 300, "2026-10-23T23:51:21", projectId: 1),
                Movement(1, MovementType.Purchase, 300, 300, "2026-10-23T23:51:21", supplier: "مصنع الألوميتال الحديث")
            };

            var json = Json(AiChatTools.BuildMaterialDetails(Alucobond, movements, Array.Empty<StockPriceBatchDTO>(), null, 10));
            var summary = json["purchaseSummary"]!;

            Assert.Equal(1, summary["purchasesCount"]!.GetValue<int>());
            Assert.Equal(300m, summary["totalPurchasedQuantity"]!.GetValue<decimal>());
            Assert.Equal(90000m, summary["totalPurchasesValue"]!.GetValue<decimal>());
            Assert.Equal(300m, summary["weightedAverageUnitPrice"]!.GetValue<decimal>());
            Assert.Equal("2026-10-23", summary["lastPurchase"]!["date"]!.GetValue<string>());
            Assert.Equal(90000m, summary["lastPurchase"]!["total"]!.GetValue<decimal>());
            Assert.Equal("مصنع الألوميتال الحديث", summary["lastPurchase"]!["supplier"]!["name"]!.GetValue<string>());
            Assert.Equal(2, json["totalMovements"]!.GetValue<int>());
        }

        [Fact]
        public void MaterialDetails_FilterAndLimitApplyToMovementsButTotalsUseAllPurchases()
        {
            var movements = Enumerable.Range(1, 40)
                .Select(i => Movement(i, MovementType.Purchase, 10, i, $"2026-01-{(i % 28) + 1:00}"))
                .Append(Movement(99, MovementType.IssueToProject, 5, 1, "2026-02-01", projectId: 1))
                .ToList();

            var json = Json(AiChatTools.BuildMaterialDetails(Alucobond, movements, Array.Empty<StockPriceBatchDTO>(), MovementType.Purchase, 5));

            Assert.Equal(40, json["totalMovements"]!.GetValue<int>());
            Assert.Equal(5, json["movements"]!.AsArray().Count);
            Assert.Equal(40, json["purchaseSummary"]!["purchasesCount"]!.GetValue<int>());
            Assert.Equal(400m, json["purchaseSummary"]!["totalPurchasedQuantity"]!.GetValue<decimal>());
            Assert.All(json["movements"]!.AsArray(), m => Assert.Equal("شراء", m!["type"]!.GetValue<string>()));
        }

        [Fact]
        public void MaterialDetails_NoMovements_ReportsEmptyHistoryHonestly()
        {
            var json = Json(AiChatTools.BuildMaterialDetails(Alucobond, Array.Empty<StockMovementDTO>(), Array.Empty<StockPriceBatchDTO>(), null, 10));

            Assert.Equal(0, json["purchaseSummary"]!["purchasesCount"]!.GetValue<int>());
            Assert.Null(json["purchaseSummary"]!["weightedAverageUnitPrice"]);
            Assert.Null(json["purchaseSummary"]!["lastPurchase"]);
            Assert.Empty(json["movements"]!.AsArray());
        }

        [Fact]
        public void ProjectMaterials_NetsReturnsAgainstIssues()
        {
            var movements = new[]
            {
                Movement(1, MovementType.IssueToProject, 10, 5, "2026-03-01", projectId: 1),
                Movement(2, MovementType.ReturnFromProject, 2, 5, "2026-03-02", projectId: 1)
            };

            var row = Json(AiChatTools.StockSummaryForProject(movements))["perMaterial"]![0]!;

            Assert.Equal(10m, row["issuedQuantity"]!.GetValue<decimal>());
            Assert.Equal(2m, row["returnedQuantity"]!.GetValue<decimal>());
            Assert.Equal(40m, row["netCost"]!.GetValue<decimal>());
        }

        [Fact]
        public void SupplierDetails_SeparatesPendingChequesAndRespectsLimit()
        {
            var supplier = new SupplierDetailsDTO
            {
                Id = 1, Name = "مورد", TotalPurchases = 1000, TotalPaid = 400, TotalRemaining = 600,
                Payments = new()
                {
                    new SupplierPaymentDTO { Id = 1, Amount = 300, PaymentDate = DateTime.Parse("2026-01-01") },
                    new SupplierPaymentDTO { Id = 2, Amount = 100, IsCheck = true, CheckStatus = "Pending", CheckDueDate = DateTime.Parse("2026-12-01"), PaymentDate = DateTime.Parse("2026-02-01") }
                },
                Purchases = Enumerable.Range(1, 12).Select(i => new SupplierPurchaseDTO { Id = i, MaterialName = "m", Quantity = 1, UnitPriceAtTime = 10, MovementDate = DateTime.Parse("2026-01-01").AddDays(i) }).ToList()
            };

            var json = Json(AiChatTools.BuildSupplierDetails(supplier, 5));

            Assert.Equal(600m, json["totalRemaining"]!.GetValue<decimal>());
            Assert.Equal(100m, json["pendingCheques"]!["amount"]!.GetValue<decimal>());
            Assert.Equal("2026-12-01", json["pendingCheques"]!["nextDueDate"]!.GetValue<string>());
            Assert.Equal(12, json["purchasesCount"]!.GetValue<int>());
            Assert.Equal(5, json["purchases"]!.AsArray().Count);
            Assert.Equal("2026-01-13", json["purchases"]![0]!["date"]!.GetValue<string>());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Employees_PayFieldsOnlyWithPayrollPermission(bool canSeePayroll)
        {
            var services = new ServiceCollection().AddSingleton<IEmployeeService>(new FakeEmployeeService());
            var abilities = canSeePayroll ? new[] { "عرض الموظفين", "إدارة الرواتب" } : new[] { "عرض الموظفين" };
            var tools = new AiChatTools(new AllowAbilities(abilities), services.BuildServiceProvider(), NullLogger<AiChatTools>.Instance);

            var result = await tools.ExecuteAsync("search_employees", null, User);
            var employee = result["result"]!["employees"]![0]!;

            Assert.Equal("محمد", employee["name"]!.GetValue<string>());
            Assert.Null(employee["email"]);
            if (canSeePayroll) Assert.Equal(5000m, employee["wageAmount"]!.GetValue<decimal>());
            else Assert.Null(employee["wageAmount"]);
        }

        [Theory]
        [InlineData("get_treasury", "{\"view\":\"everything\"}")]
        [InlineData("get_treasury", "{\"view\":\"transactions\",\"dateFrom\":\"yesterday\"}")]
        [InlineData("get_payroll", "{}")]
        [InlineData("get_project", "{\"id\":1,\"sections\":[\"payments\",\"passwords\"]}")]
        [InlineData("search_invoices", "{\"kind\":\"all\"}")]
        public async Task InvalidArguments_AreRejectedBeforeTouchingData(string tool, string args)
        {
            var tools = new AiChatTools(new AllowAbilities("عرض الخزنة", "إدارة الرواتب", "عرض المشاريع", "إدارة الفواتير"),
                new ServiceCollection().BuildServiceProvider(), NullLogger<AiChatTools>.Instance);

            var result = await tools.ExecuteAsync(tool, JsonNode.Parse(args)!.AsObject(), User);

            Assert.Equal("invalid_argument", result["error"]!.GetValue<string>());
        }

        [Fact]
        public void ToolArgs_DateAndSectionParsing()
        {
            var args = new ToolArgs(JsonNode.Parse("{\"from\":\"2026-10-01\",\"sections\":[\"Payments\",\"expenses\"]}")!.AsObject());

            Assert.Equal(new DateTime(2026, 10, 1), args.GetDate("from"));
            Assert.Null(args.GetDate("missing"));
            Assert.Equal(new[] { "expenses", "payments" }, args.GetStringSet("sections", new[] { "payments", "expenses", "materials" }).OrderBy(s => s));
        }

        private class AllowAbilities : IAuthorizationService
        {
            private readonly HashSet<string> _allowed;
            public AllowAbilities(params string[] abilities) => _allowed = abilities.Select(a => $"Ability:{a}").ToHashSet();

            public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName) =>
                Task.FromResult(_allowed.Contains(policyName) ? AuthorizationResult.Success() : AuthorizationResult.Failed());

            public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements) =>
                Task.FromResult(AuthorizationResult.Failed());
        }

        private class FakeEmployeeService : IEmployeeService
        {
            public Task<List<EmployeeListDTO>> GetAllAsync() => Task.FromResult(new List<EmployeeListDTO>
            {
                new() { Id = 1, Name = "محمد", Email = "secret@example.com", UserName = "mohamed", WageAmount = 5000, EmployeeType = EmployeeType.Worker }
            });

            public Task<EmployeeListDTO?> GetByIdAsync(int id) => throw new NotSupportedException();
            public Task<int> CreateWorkerAsync(KMG.Core.DTOs.Employee.CreateWorkerDTO model) => throw new NotSupportedException();
            public Task<bool> UpdateWorkerAsync(KMG.Core.DTOs.Employee.UpdateWorkerDTO model) => throw new NotSupportedException();
        }
    }
}
