using System.Security.Claims;
using KMG.Core.Enums;
using KMG.Core.Interfaces.Services;

namespace KMG.Api.Ai
{
    public partial class AiChatTools
    {
        private static readonly string[] PayrollViews = { "payouts", "advances", "adjustments", "insurance" };

        private IEnumerable<ToolDefinition> PeopleTools()
        {
            yield return new ToolDefinition(
                "search_employees",
                "Employees and workers: id, name, type (admin/worker), wage type, manager, phone, suspended, has login. " +
                "Wage, insurance and outstanding advances are included only for users with payroll permission.",
                Schema(
                    ("query", StringProp("Free text matched against employee name and manager name.")),
                    ("type", EnumProp("Employee type.", Enum.GetNames<EmployeeType>())),
                    ("includeSuspended", BoolProp("Include suspended employees (default false).")),
                    ("limit", IntProp("Max rows (1-30, default 15)."))),
                ViewEmployees,
                SearchEmployeesAsync);

            yield return new ToolDefinition(
                "get_payroll",
                "Payroll (sensitive). view=payouts: salary payments history (period, base, mission double-up, deductions, bonus, " +
                "advance installment, insurance, net paid). view=advances: employee advances (سلف) and remaining. " +
                "view=adjustments: deductions/bonuses. view=insurance: employee insurance and Misr Insurance amounts and payout history.",
                Schema(new[]
                {
                    ("view", EnumProp("What to return.", PayrollViews)),
                    ("employeeId", IntProp("Only this employee (payouts, advances, adjustments).")),
                    ("limit", IntProp("Max rows (1-30, default 10)."))
                }, "view"),
                ManagePayroll,
                GetPayrollAsync);
        }

        private async Task<object> SearchEmployeesAsync(ToolArgs args, ClaimsPrincipal user)
        {
            var query = args.GetString("query", MaxQueryLength);
            var type = args.GetEnum<EmployeeType>("type");
            var includeSuspended = args.GetBool("includeSuspended");
            var limit = args.GetInt("limit", 15, 1, 30);
            var showPay = await HasAbilityAsync(user, ManagePayroll);

            var matches = (await Service<IEmployeeService>().GetAllAsync())
                .Where(e => type == null || e.EmployeeType == type)
                .Where(e => includeSuspended || !e.Suspended)
                .Where(e => ArabicText.ContainsAllTokens(query, e.Name, e.ManagerName))
                .ToList();

            return new
            {
                totalMatches = matches.Count,
                returned = Math.Min(limit, matches.Count),
                payFieldsIncluded = showPay,
                employees = matches.Take(limit).Select(e => new
                {
                    id = e.Id,
                    name = e.Name,
                    type = e.EmployeeType == EmployeeType.Admin ? "إداري" : "عامل",
                    wageType = e.WageType == WageType.Monthly ? "شهري" : "يومي",
                    manager = e.ManagerName,
                    phone = e.Phone,
                    suspended = e.Suspended,
                    hasLoginAccount = e.HasLoginAccount,
                    wageAmount = showPay ? e.WageAmount : (decimal?)null,
                    insuranceAmount = showPay ? e.InsuranceAmount : (decimal?)null,
                    misrInsuranceAmount = showPay ? e.MisrInsuranceAmount : (decimal?)null,
                    remainingAdvances = showPay ? e.RemainingAdvances : (decimal?)null
                })
            };
        }

        private async Task<object> GetPayrollAsync(ToolArgs args, ClaimsPrincipal user)
        {
            var view = args.GetString("view", 20);
            if (view == null || !PayrollViews.Contains(view))
                throw new ToolArgumentException($"'view' must be one of: {string.Join(", ", PayrollViews)}.");
            var employeeId = args.GetOptionalInt("employeeId", 1, int.MaxValue);
            var limit = args.GetInt("limit", 10, 1, 30);
            var payroll = Service<IPayrollService>();

            switch (view)
            {
                case "payouts":
                {
                    var payouts = (await payroll.GetPayoutHistoryAsync(employeeId)).OrderByDescending(p => p.PaidDate).ToList();
                    return new
                    {
                        totalMatches = payouts.Count,
                        returned = Math.Min(limit, payouts.Count),
                        payouts = payouts.Take(limit).Select(p => new
                        {
                            id = p.Id,
                            employeeId = p.EmployeeId,
                            employee = p.EmployeeName,
                            periodStart = Day(p.PeriodStart),
                            periodEnd = Day(p.PeriodEnd),
                            paidDate = Day(p.PaidDate),
                            workshopDays = p.WorkshopDays,
                            missionDays = p.MissionDays,
                            baseAmount = p.BaseAmount,
                            missionDoubleUp = p.MissionDoubleUpAmount,
                            bonus = p.BonusAmount + p.HourlyBonusAmount,
                            deductions = p.DeductionsAmount + p.HourlyDeductionAmount,
                            advanceInstallment = p.AdvanceInstallmentAmount,
                            insurance = p.InsuranceAmount,
                            misrInsurance = p.MisrInsuranceAmount,
                            netPaid = p.NetPaid
                        })
                    };
                }
                case "advances":
                {
                    var advances = (await payroll.GetAdvancesAsync(employeeId)).OrderByDescending(a => a.IssueDate).ToList();
                    return new
                    {
                        totalMatches = advances.Count,
                        totalRemaining = advances.Sum(a => a.RemainingAmount),
                        returned = Math.Min(limit, advances.Count),
                        advances = advances.Take(limit).Select(a => new
                        {
                            id = a.Id,
                            employee = a.EmployeeName,
                            issueDate = Day(a.IssueDate),
                            totalAmount = a.TotalAmount,
                            installment = a.InstallmentAmount,
                            remaining = a.RemainingAmount,
                            status = a.Status,
                            notes = ArabicText.Truncate(a.Notes, 150)
                        })
                    };
                }
                case "adjustments":
                {
                    var adjustments = (await payroll.GetAdjustmentsAsync(employeeId)).OrderByDescending(a => a.Date).ToList();
                    return new
                    {
                        totalMatches = adjustments.Count,
                        returned = Math.Min(limit, adjustments.Count),
                        adjustments = adjustments.Take(limit).Select(a => new
                        {
                            id = a.Id,
                            employee = a.EmployeeName,
                            date = Day(a.Date),
                            type = a.Type == nameof(AdjustmentType.Bonus) ? "حافز" : "خصم",
                            amount = a.Amount,
                            hours = a.Hours,
                            reason = ArabicText.Truncate(a.Reason, 150),
                            appliedInPayroll = a.Applied
                        })
                    };
                }
                default:
                {
                    var insurance = Service<IInsuranceService>();
                    var misr = Service<IMisrInsuranceService>();
                    var employees = await insurance.GetEmployeeInsuranceSummaryAsync();
                    var misrEmployees = await misr.GetEmployeeMisrInsuranceSummaryAsync();
                    return new
                    {
                        insurance = new
                        {
                            monthlyTotal = employees.Sum(e => e.InsuranceAmount),
                            employees = employees.Take(limit).Select(e => new { e.EmployeeId, employee = e.EmployeeName, amount = e.InsuranceAmount }),
                            payouts = (await insurance.GetPayoutHistoryAsync()).OrderByDescending(p => p.PaidDate).Take(limit)
                                .Select(p => new { periodStart = Day(p.PeriodStart), periodEnd = Day(p.PeriodEnd), total = p.TotalAmount, paidDate = Day(p.PaidDate) })
                        },
                        misrInsurance = new
                        {
                            employeesCount = misrEmployees.Count,
                            payouts = (await misr.GetPayoutHistoryAsync()).OrderByDescending(p => p.PaidDate).Take(limit)
                                .Select(p => new { periodStart = Day(p.PeriodStart), periodEnd = Day(p.PeriodEnd), total = p.TotalAmount, paidDate = Day(p.PaidDate) })
                        }
                    };
                }
            }
        }
    }
}
