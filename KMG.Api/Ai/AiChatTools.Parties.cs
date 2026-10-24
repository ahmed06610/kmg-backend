using System.Security.Claims;
using KMG.Core.DTOs.Supplier;
using KMG.Core.Enums;
using KMG.Core.Interfaces.Services;

namespace KMG.Api.Ai
{
    public partial class AiChatTools
    {
        private const int PartyHistoryLimitMax = 25;

        private IEnumerable<ToolDefinition> PartyTools()
        {
            yield return new ToolDefinition(
                "search_clients",
                "Clients with project count, total contract value, collected and remaining. Pass id for one client's details " +
                "including all their projects (id, code, status, contract value, collected, remaining).",
                Schema(
                    ("query", StringProp("Free text matched against client name and phone.")),
                    ("id", IntProp("Client id, for full details.")),
                    ("limit", IntProp("Max rows to return (1-25, default 10)."))),
                ViewClients,
                SearchClientsAsync);

            yield return new ToolDefinition(
                "search_suppliers",
                "Suppliers with total purchases, total paid and remaining (what KMG owes them). Pass id for one supplier's " +
                "details: purchase history (material, quantity, unit price, transport, date) and payment history (cash/credit/cheques).",
                Schema(
                    ("query", StringProp("Free text matched against supplier name and phone.")),
                    ("id", IntProp("Supplier id, for full details.")),
                    ("limit", IntProp($"Max rows (list) or history rows per section (details), 1-{PartyHistoryLimitMax}, default 10."))),
                ViewSuppliers,
                SearchSuppliersAsync);
        }

        private async Task<object> SearchClientsAsync(ToolArgs args, ClaimsPrincipal user)
        {
            var id = args.GetOptionalInt("id", 1, int.MaxValue);
            var query = args.GetString("query", MaxQueryLength);
            var limit = args.GetInt("limit", 10, 1, 25);
            var clients = Service<IClientService>();
            const string collectedNote = "totalCollected includes pending (not yet cleared) cheques; get_project gives the exact breakdown per project.";

            if (id != null)
            {
                var c = await clients.GetByIdAsync(id.Value);
                if (c == null) return new { found = false, id };
                return new
                {
                    found = true,
                    id = c.Id,
                    name = c.Name,
                    phone = c.Phone,
                    address = c.Address,
                    createdAt = Day(c.CreatedAt),
                    projectsCount = c.ProjectsCount,
                    totalContractValue = c.TotalContractValue,
                    totalCollected = c.TotalCollected,
                    totalRemaining = c.TotalRemaining,
                    note = collectedNote,
                    projects = c.Projects.Take(PartyHistoryLimitMax).Select(p => new
                    {
                        id = p.Id,
                        projectCode = p.ProjectCode,
                        name = p.Name,
                        type = ProjectTypeLabel(p.ProjectType),
                        status = StatusLabel(p.Status),
                        contractValue = p.ContractValue,
                        totalCollected = p.TotalCollected,
                        remainingBalance = p.RemainingBalance
                    })
                };
            }

            var matches = (await clients.GetAllAsync())
                .Where(c => ArabicText.ContainsAllTokens(query, c.Name, c.Phone))
                .ToList();

            return new
            {
                totalMatches = matches.Count,
                returned = Math.Min(limit, matches.Count),
                note = collectedNote,
                clients = matches.Take(limit).Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    phone = c.Phone,
                    projectsCount = c.ProjectsCount,
                    totalContractValue = c.TotalContractValue,
                    totalCollected = c.TotalCollected,
                    totalRemaining = c.TotalRemaining
                })
            };
        }

        private async Task<object> SearchSuppliersAsync(ToolArgs args, ClaimsPrincipal user)
        {
            var id = args.GetOptionalInt("id", 1, int.MaxValue);
            var query = args.GetString("query", MaxQueryLength);
            var limit = args.GetInt("limit", 10, 1, PartyHistoryLimitMax);
            var suppliers = Service<ISupplierService>();

            if (id != null)
            {
                var s = await suppliers.GetByIdAsync(id.Value);
                return s == null ? new { found = false, id } : (object)BuildSupplierDetails(s, limit);
            }

            var matches = (await suppliers.GetAllAsync())
                .Where(s => ArabicText.ContainsAllTokens(query, s.Name, s.Phone))
                .ToList();

            return new
            {
                totalMatches = matches.Count,
                returned = Math.Min(limit, matches.Count),
                suppliers = matches.Take(limit).Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    phone = s.Phone,
                    totalPurchases = s.TotalPurchases,
                    totalPaid = s.TotalPaid,
                    totalRemaining = s.TotalRemaining
                })
            };
        }

        internal static object BuildSupplierDetails(SupplierDetailsDTO s, int limit)
        {
            var pendingCheques = s.Payments.Where(p => p.IsCheck && p.CheckStatus == nameof(CheckStatus.Pending)).ToList();

            return new
            {
                found = true,
                id = s.Id,
                name = s.Name,
                phone = s.Phone,
                address = s.Address,
                createdAt = Day(s.CreatedAt),
                totalPurchases = s.TotalPurchases,
                totalPaid = s.TotalPaid,
                totalRemaining = s.TotalRemaining,
                pendingCheques = new
                {
                    count = pendingCheques.Count,
                    amount = pendingCheques.Sum(p => p.Amount),
                    nextDueDate = Day(pendingCheques.Where(p => p.CheckDueDate != null).Min(p => p.CheckDueDate))
                },
                purchasesCount = s.Purchases.Count,
                purchases = s.Purchases.OrderByDescending(p => p.MovementDate).Take(limit).Select(p => new
                {
                    date = Day(p.MovementDate),
                    materialId = p.MaterialId,
                    material = p.MaterialName,
                    quantity = p.Quantity,
                    unitPrice = p.UnitPriceAtTime,
                    total = p.Quantity * p.UnitPriceAtTime,
                    transportCost = p.TransportCost,
                    notes = ArabicText.Truncate(p.Notes, 150)
                }),
                paymentsCount = s.Payments.Count,
                payments = s.Payments.OrderByDescending(p => p.PaymentDate).Take(limit).Select(p => new
                {
                    date = Day(p.PaymentDate),
                    amount = p.Amount,
                    cash = p.AmountCash,
                    credit = p.AmountCredit,
                    cheque = CheckStatusLabel(p.IsCheck, p.CheckStatus),
                    chequeDueDate = Day(p.CheckDueDate),
                    notes = ArabicText.Truncate(p.Notes, 150)
                })
            };
        }
    }
}
