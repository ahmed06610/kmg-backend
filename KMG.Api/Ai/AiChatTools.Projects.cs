using System.Security.Claims;
using KMG.Core.DTOs.Mission;
using KMG.Core.Enums;
using KMG.Core.Helper;
using KMG.Core.Interfaces.Services;

namespace KMG.Api.Ai
{
    public partial class AiChatTools
    {
        private static readonly string[] ProjectSections = { "payments", "expenses", "materials", "writeOffs", "attachments", "activity", "missions" };
        private const int ProjectSectionLimit = 15;

        private IEnumerable<ToolDefinition> ProjectTools()
        {
            yield return new ToolDefinition(
                "search_projects",
                "Search/filter KMG projects. Returns compact rows with id and projectCode. With no filters returns the newest projects.",
                Schema(
                    ("query", StringProp("Free text matched against project name, project code and client name.")),
                    ("status", EnumProp("Project status.", Enum.GetNames<ProjectStatus>())),
                    ("type", EnumProp("Project type.", Enum.GetNames<ProjectType>())),
                    ("limit", IntProp("Max rows to return (1-25, default 10)."))),
                ViewProjects,
                SearchProjectsAsync);

            yield return new ToolDefinition(
                "get_project",
                "One project by numeric id: client, status, contract value, collected vs pending cheques, remaining, cost breakdown, " +
                "net profit, tender insurance and work guarantee. Add 'sections' for the project's tabs: payments (collections), " +
                "expenses, materials (stock issued/returned to the project), writeOffs, attachments (names only), activity (audit log), missions.",
                Schema(new[]
                {
                    ("id", IntProp("Project id.")),
                    ("sections", EnumListProp($"Optional detail sections (each returns up to {ProjectSectionLimit} newest rows).", ProjectSections))
                }, "id"),
                ViewProjects,
                GetProjectAsync);

            yield return new ToolDefinition(
                "get_missions",
                "Field missions (مأموريات): foreman, project, dates, custody advance, actual spent, settlement difference, labor cost, status. " +
                "Filter by projectId or status, or pass id for one mission with its workers.",
                Schema(
                    ("id", IntProp("Mission id, for one mission with workers.")),
                    ("projectId", IntProp("Only missions of this project.")),
                    ("status", EnumProp("Mission status.", Enum.GetNames<MissionStatus>())),
                    ("limit", IntProp("Max missions to return (1-25, default 10)."))),
                ManageMissions,
                GetMissionsAsync);
        }

        private async Task<object> SearchProjectsAsync(ToolArgs args, ClaimsPrincipal user)
        {
            var query = args.GetString("query", MaxQueryLength);
            var status = args.GetEnum<ProjectStatus>("status");
            var type = args.GetEnum<ProjectType>("type");
            var limit = args.GetInt("limit", 10, 1, 25);

            var matches = (await Service<IProjectService>().GetAllAsync())
                .Where(p => status == null || p.Status == status.ToString())
                .Where(p => type == null || p.ProjectType == type.ToString())
                .Where(p => ArabicText.ContainsAllTokens(query, p.Name, p.ProjectCode, p.ClientName))
                .ToList();

            return new
            {
                totalMatches = matches.Count,
                returned = Math.Min(limit, matches.Count),
                note = "totalCollected includes pending (not yet cleared) cheques; get_project gives the exact breakdown.",
                projects = matches.Take(limit).Select(p => new
                {
                    id = p.Id,
                    projectCode = p.ProjectCode,
                    name = p.Name,
                    type = ProjectTypeLabel(p.ProjectType),
                    status = StatusLabel(p.Status),
                    client = p.ClientName,
                    contractValue = p.ContractValue,
                    totalCollected = p.TotalCollected,
                    remainingBalance = p.RemainingBalance,
                    createdAt = Day(p.CreatedAt)
                })
            };
        }

        private async Task<object> GetProjectAsync(ToolArgs args, ClaimsPrincipal user)
        {
            var id = args.GetRequiredInt("id", 1, int.MaxValue);
            var sections = args.GetStringSet("sections", ProjectSections);
            var p = await Service<IProjectService>().GetByIdAsync(id);
            if (p == null) return new { found = false, id };

            var activePayments = p.Payments.Where(x => x.CheckStatus != nameof(CheckStatus.Cancelled)).ToList();
            var pendingChecks = activePayments.Where(x => x.IsCheck && x.CheckStatus == nameof(CheckStatus.Pending)).ToList();

            var result = new Dictionary<string, object?>
            {
                ["found"] = true,
                ["id"] = p.Id,
                ["projectCode"] = p.ProjectCode,
                ["name"] = p.Name,
                ["type"] = ProjectTypeLabel(p.ProjectType),
                ["status"] = StatusLabel(p.Status),
                ["client"] = new { id = p.ClientId, name = p.ClientName },
                ["description"] = ArabicText.Truncate(p.Description, MaxTextFieldLength),
                ["createdAt"] = Day(p.CreatedAt),
                ["contract"] = new
                {
                    contractValue = p.ContractValue,
                    taxPercent = p.TenderTaxPercent,
                    taxAmount = p.TenderTaxAmount,
                    contractValueWithTax = p.ContractValueWithTax,
                    writeOffs = p.TotalWriteOffs,
                    effectiveContractValue = p.EffectiveContractValue,
                    supplyProfitMarginPercent = p.SupplyProfitMargin
                },
                ["collections"] = new
                {
                    totalIncludingPendingCheques = p.TotalCollected,
                    actuallyCollected = activePayments.Sum(x => x.Amount) - pendingChecks.Sum(x => x.Amount),
                    pendingCheques = new
                    {
                        count = pendingChecks.Count,
                        amount = pendingChecks.Sum(x => x.Amount),
                        nextDueDate = Day(pendingChecks.Where(x => x.CheckDueDate != null).Min(x => x.CheckDueDate))
                    },
                    paymentsCount = activePayments.Count,
                    lastPaymentDate = activePayments.Count == 0 ? null : Day(activePayments.Max(x => x.PaymentDate)),
                    remainingBalance = p.RemainingBalance
                },
                ["costs"] = new
                {
                    materials = p.TotalMaterialsCost,
                    pettyExpenses = p.TotalPettyExpenses,
                    labor = p.TotalLaborCost,
                    custody = p.TotalCustodyCost,
                    totalCost = p.TotalCost
                },
                ["netProfit"] = p.NetProfit,
                ["tenderInsurance"] = p.TenderInsuranceAmount <= 0 ? null : new
                {
                    percent = p.TenderInsurancePercent,
                    amount = p.TenderInsuranceAmount,
                    dueDate = Day(p.InsuranceDueDate),
                    recovered = p.InsuranceRecovered
                },
                ["workGuarantee"] = p.WorkGuaranteeAmount <= 0 ? null : new
                {
                    percent = p.WorkGuaranteePercent,
                    amount = p.WorkGuaranteeAmount,
                    dueDate = Day(p.WorkGuaranteeDueDate),
                    recovered = p.WorkGuaranteeRecovered
                },
                ["activeCustodiesCount"] = p.Custodies.Count(c => c.Status == nameof(CustodyStatus.Active)),
                ["availableSections"] = new
                {
                    payments = p.Payments.Count,
                    expenses = p.Expenses.Count,
                    materialMovements = p.StockMovements.Count,
                    writeOffs = p.WriteOffs.Count,
                    attachments = p.Attachments.Count,
                    activity = p.AuditLogs.Count
                }
            };

            if (sections.Contains("payments"))
                result["payments"] = p.Payments.OrderByDescending(x => x.PaymentDate).Take(ProjectSectionLimit).Select(x => new
                {
                    date = Day(x.PaymentDate),
                    amount = x.Amount,
                    cash = x.AmountCash,
                    credit = x.AmountCredit,
                    cheque = CheckStatusLabel(x.IsCheck, x.CheckStatus),
                    chequeDueDate = Day(x.CheckDueDate),
                    notes = ArabicText.Truncate(x.Notes, 150)
                });

            if (sections.Contains("expenses"))
                result["expenses"] = p.Expenses.OrderByDescending(x => x.ExpenseDate).Take(ProjectSectionLimit).Select(x => new
                {
                    date = Day(x.ExpenseDate),
                    category = Enum.TryParse<ExpenseCategory>(x.Category, out var c) ? c.Arabic() : x.Category,
                    amount = x.Amount,
                    description = ArabicText.Truncate(x.Description, 150)
                });

            if (sections.Contains("materials"))
                result["materials"] = StockSummaryForProject(p.StockMovements);

            if (sections.Contains("writeOffs"))
                result["writeOffs"] = p.WriteOffs.OrderByDescending(x => x.WriteOffDate).Take(ProjectSectionLimit).Select(x => new
                {
                    date = Day(x.WriteOffDate),
                    amount = x.Amount,
                    reason = ArabicText.Truncate(x.Reason, 150)
                });

            if (sections.Contains("attachments"))
                result["attachments"] = p.Attachments.OrderByDescending(x => x.UploadedAt).Take(ProjectSectionLimit).Select(x => new
                {
                    fileName = x.FileName,
                    description = ArabicText.Truncate(x.Description, 150),
                    uploadedAt = Day(x.UploadedAt),
                    uploadedBy = x.UploadedByEmployeeName
                });

            if (sections.Contains("activity"))
                result["activity"] = p.AuditLogs.OrderByDescending(x => x.ActionDate).Take(ProjectSectionLimit).Select(x => new
                {
                    date = Day(x.ActionDate),
                    action = ArabicText.Truncate(x.ActionDescription, 200),
                    by = x.EmployeeName
                });

            if (sections.Contains("missions"))
            {
                result["missions"] = await HasAbilityAsync(user, ManageMissions)
                    ? (await Service<IMissionService>().GetByProjectAsync(p.Id)).Take(ProjectSectionLimit).Select(MissionRow).ToList()
                    : (object)"access_denied";
            }

            return result;
        }

        internal static object StockSummaryForProject(IReadOnlyCollection<KMG.Core.DTOs.Stock.StockMovementDTO> movements)
        {
            static decimal Sign(string type) => type == nameof(MovementType.ReturnFromProject) ? -1 : 1;

            return new
            {
                perMaterial = movements
                    .GroupBy(m => new { m.MaterialId, m.MaterialName })
                    .Select(g => new
                    {
                        materialId = g.Key.MaterialId,
                        material = g.Key.MaterialName,
                        issuedQuantity = g.Where(m => m.MovementType == nameof(MovementType.IssueToProject)).Sum(m => m.Quantity),
                        returnedQuantity = g.Where(m => m.MovementType == nameof(MovementType.ReturnFromProject)).Sum(m => m.Quantity),
                        netCost = g.Sum(m => Sign(m.MovementType) * m.Quantity * m.UnitPriceAtTime)
                    })
                    .OrderByDescending(x => x.netCost)
                    .Take(ProjectSectionLimit),
                recentMovements = movements.OrderByDescending(m => m.MovementDate).Take(ProjectSectionLimit).Select(m => new
                {
                    date = Day(m.MovementDate),
                    type = MovementTypeLabel(m.MovementType),
                    material = m.MaterialName,
                    quantity = m.Quantity,
                    unitPrice = m.UnitPriceAtTime,
                    total = m.Quantity * m.UnitPriceAtTime
                })
            };
        }

        private async Task<object> GetMissionsAsync(ToolArgs args, ClaimsPrincipal user)
        {
            var id = args.GetOptionalInt("id", 1, int.MaxValue);
            var projectId = args.GetOptionalInt("projectId", 1, int.MaxValue);
            var status = args.GetEnum<MissionStatus>("status");
            var limit = args.GetInt("limit", 10, 1, 25);
            var missions = Service<IMissionService>();

            if (id != null)
            {
                var m = await missions.GetByIdAsync(id.Value);
                if (m == null) return new { found = false, id };
                return new
                {
                    found = true,
                    mission = MissionRow(m),
                    notes = ArabicText.Truncate(m.Notes, 200),
                    workers = m.Workers.Select(w => new { employeeId = w.EmployeeId, name = w.EmployeeName, days = w.DaysCount, dailyRate = w.DailyRate })
                };
            }

            var list = projectId != null
                ? (await missions.GetByProjectAsync(projectId.Value)).Where(m => status == null || m.Status == status.ToString()).ToList()
                : await missions.GetRecentAsync(status, limit);

            return new
            {
                returned = Math.Min(limit, list.Count),
                missions = list.Take(limit).Select(MissionRow)
            };
        }

        private static object MissionRow(MissionListDTO m) => new
        {
            id = m.Id,
            projectId = m.ProjectId,
            projectCode = m.ProjectCode,
            foreman = m.ForemanName,
            startDate = Day(m.StartDate),
            endDate = Day(m.EndDate),
            status = m.Status == nameof(MissionStatus.Open) ? "مفتوحة" : m.Status == nameof(MissionStatus.Settled) ? "متسوية" : m.Status,
            custodyAdvance = m.AdvanceAmount,
            actualSpent = m.ActualSpent,
            settlementDifference = m.SettlementDifference,
            laborCost = m.TotalLaborCost
        };
    }
}
