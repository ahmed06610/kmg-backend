using System.Security.Claims;
using KMG.Core.DTOs.CashBox;
using KMG.Core.DTOs.Invoice;
using KMG.Core.Enums;
using KMG.Core.Interfaces.Services;

namespace KMG.Api.Ai
{
    public partial class AiChatTools
    {
        private static readonly string[] TreasuryViews = { "summary", "transactions", "custodies" };
        private static readonly string[] InvoiceSourceTypes = { "ProjectPayment", "ProjectExpense", "StockPurchase", "StockIssue", "SupplierPayment", "MiscExpense" };

        private IEnumerable<ToolDefinition> FinanceTools()
        {
            yield return new ToolDefinition(
                "get_treasury",
                "Cash box (الخزنة). view=summary: current cash/credit balance and latest transactions. view=transactions: " +
                "transaction history filtered by date range, direction (in/out), type or text, with totals for ALL matching rows " +
                "(use this for 'how much did we spend/receive in a period'). view=custodies: employee custodies (عُهد) and their settlement.",
                Schema(
                    ("view", EnumProp("What to return (default summary).", TreasuryViews)),
                    ("dateFrom", DateProp("Transactions from this date (inclusive).")),
                    ("dateTo", DateProp("Transactions up to this date (inclusive).")),
                    ("direction", EnumProp("Money in or out.", new[] { "in", "out" })),
                    ("type", EnumProp("Transaction type.", Enum.GetNames<TransactionType>())),
                    ("query", StringProp("Text matched against description, project, supplier, employee (transactions) or custody description/employee/project.")),
                    ("custodyStatus", EnumProp("Custodies filter.", Enum.GetNames<CustodyStatus>())),
                    ("limit", IntProp("Max rows (1-30, default 10)."))),
                ViewCashBox,
                GetTreasuryAsync);

            yield return new ToolDefinition(
                "search_invoices",
                "Invoices. kind=records (default): the financial documents register (project payments, project expenses, stock " +
                "purchases/issues, supplier payments, misc expenses) with amount, direction, date, project, supplier. " +
                "kind=generated: invoices issued by KMG (number, recipient, total); pass id for one generated invoice with line items.",
                Schema(
                    ("kind", EnumProp("records or generated.", new[] { "records", "generated" })),
                    ("id", IntProp("Generated invoice id, for full details with line items.")),
                    ("sourceType", EnumProp("records only: document source.", InvoiceSourceTypes)),
                    ("direction", EnumProp("records only: money in or out.", new[] { "In", "Out" })),
                    ("projectId", IntProp("records only: project filter.")),
                    ("supplierId", IntProp("records only: supplier filter.")),
                    ("dateFrom", DateProp("From date (inclusive).")),
                    ("dateTo", DateProp("To date (inclusive).")),
                    ("query", StringProp("Free text search.")),
                    ("limit", IntProp("Max rows (1-30, default 10)."))),
                ManageInvoices,
                SearchInvoicesAsync);
        }

        private async Task<object> GetTreasuryAsync(ToolArgs args, ClaimsPrincipal user)
        {
            var view = args.GetString("view", 20) ?? "summary";
            if (!TreasuryViews.Contains(view)) throw new ToolArgumentException($"'view' must be one of: {string.Join(", ", TreasuryViews)}.");
            var limit = args.GetInt("limit", 10, 1, 30);
            var query = args.GetString("query", MaxQueryLength);

            if (view == "custodies")
            {
                var status = args.GetEnum<CustodyStatus>("custodyStatus");
                var custodies = (await Service<ICustodyService>().GetAllAsync())
                    .Where(c => status == null || c.Status == status.ToString())
                    .Where(c => ArabicText.ContainsAllTokens(query, c.Description, c.EmployeeName, c.ProjectName))
                    .OrderByDescending(c => c.IssueDate)
                    .ToList();

                return new
                {
                    totalMatches = custodies.Count,
                    activeAmount = custodies.Where(c => c.Status == nameof(CustodyStatus.Active)).Sum(c => c.Amount),
                    returned = Math.Min(limit, custodies.Count),
                    custodies = custodies.Take(limit).Select(c => new
                    {
                        id = c.Id,
                        employee = c.EmployeeName,
                        project = c.ProjectName,
                        description = ArabicText.Truncate(c.Description, 150),
                        amount = c.Amount,
                        issueDate = Day(c.IssueDate),
                        status = c.Status == nameof(CustodyStatus.Active) ? "مفتوحة" : "متسوية",
                        settledAmount = c.SettledAmount,
                        settledDate = Day(c.SettledDate)
                    })
                };
            }

            if (view == "summary")
            {
                var d = await Service<ICashBoxService>().GetDetailsAsync(recentCount: Math.Min(limit, 10));
                return new
                {
                    balance = new { cash = d.TotalCash, credit = d.TotalCredit, total = d.TotalBalance },
                    latestTransactions = d.RecentTransactions.Select(TransactionRow)
                };
            }

            var direction = args.GetString("direction", 5);
            if (direction != null && direction != "in" && direction != "out")
                throw new ToolArgumentException("'direction' must be 'in' or 'out'.");
            var filter = new CashBoxTransactionFilterDTO
            {
                Page = 1,
                PageSize = limit,
                DateFrom = args.GetDate("dateFrom"),
                DateTo = args.GetDate("dateTo"),
                Type = args.GetEnum<TransactionType>("type"),
                IsIn = direction == null ? null : direction == "in",
                Search = query
            };

            var page = await Service<ICashBoxService>().GetTransactionsAsync(filter);

            return new
            {
                totalMatches = page.TotalCount,
                matchingTotals = new { cash = page.FilteredTotalCash, credit = page.FilteredTotalCredit, total = page.FilteredTotalCash + page.FilteredTotalCredit },
                note = "Amounts are signed: positive = money in, negative = money out. matchingTotals cover ALL matching rows, not only the returned ones.",
                returned = page.Items.Count,
                transactions = page.Items.Select(TransactionRow)
            };
        }

        private static object TransactionRow(CashBoxTransactionDTO t) => new
        {
            id = t.Id,
            date = Day(t.TransactionDate),
            type = t.TransactionType,
            amount = t.AmountCash + t.AmountCredit,
            cash = t.AmountCash,
            credit = t.AmountCredit,
            description = ArabicText.Truncate(t.Description, 150),
            project = t.ProjectCode == null ? null : $"{t.ProjectCode} - {t.ProjectName}",
            supplier = t.SupplierName,
            miscExpenseCategory = t.MiscExpenseCategory
        };

        private async Task<object> SearchInvoicesAsync(ToolArgs args, ClaimsPrincipal user)
        {
            var kind = args.GetString("kind", 20) ?? "records";
            if (kind != "records" && kind != "generated") throw new ToolArgumentException("'kind' must be 'records' or 'generated'.");
            var id = args.GetOptionalInt("id", 1, int.MaxValue);
            var query = args.GetString("query", MaxQueryLength);
            var dateFrom = args.GetDate("dateFrom");
            var dateTo = args.GetDate("dateTo");
            var limit = args.GetInt("limit", 10, 1, 30);
            var sourceType = args.GetString("sourceType", 30);
            if (sourceType != null && !InvoiceSourceTypes.Contains(sourceType))
                throw new ToolArgumentException($"'sourceType' must be one of: {string.Join(", ", InvoiceSourceTypes)}.");
            var direction = args.GetString("direction", 5);
            if (direction != null && direction != "In" && direction != "Out")
                throw new ToolArgumentException("'direction' must be 'In' or 'Out'.");
            var projectId = args.GetOptionalInt("projectId", 1, int.MaxValue);
            var supplierId = args.GetOptionalInt("supplierId", 1, int.MaxValue);
            var invoices = Service<IInvoiceService>();

            if (kind == "generated" || id != null)
            {
                if (id != null)
                {
                    var g = await invoices.GetGeneratedInvoiceByIdAsync(id.Value);
                    return g == null ? new { found = false, id } : (object)BuildGeneratedInvoice(g);
                }

                var generated = (await invoices.GetGeneratedInvoicesAsync())
                    .Where(g => (dateFrom == null || g.IssueDate.Date >= dateFrom.Value.Date) && (dateTo == null || g.IssueDate.Date <= dateTo.Value.Date))
                    .Where(g => ArabicText.ContainsAllTokens(query, g.InvoiceNumber, g.Title, g.RecipientName, g.ProjectName))
                    .OrderByDescending(g => g.IssueDate)
                    .ToList();

                return new
                {
                    totalMatches = generated.Count,
                    returned = Math.Min(limit, generated.Count),
                    invoices = generated.Take(limit).Select(g => new
                    {
                        id = g.Id,
                        invoiceNumber = g.InvoiceNumber,
                        title = g.Title,
                        issueDate = Day(g.IssueDate),
                        recipient = g.RecipientName,
                        project = g.ProjectName,
                        total = g.Total
                    })
                };
            }

            var page = await invoices.GetInvoicesAsync(new InvoiceFilterDTO
            {
                Page = 1,
                PageSize = limit,
                SourceType = sourceType,
                Direction = direction,
                DateFrom = dateFrom,
                DateTo = dateTo,
                ProjectId = projectId,
                SupplierId = supplierId,
                Search = query
            });

            return new
            {
                totalMatches = page.TotalCount,
                returned = page.Items.Count,
                records = page.Items.Select(i => new
                {
                    sourceType = i.SourceType,
                    sourceId = i.SourceId,
                    direction = i.Direction,
                    amount = i.Amount,
                    date = Day(i.Date),
                    title = ArabicText.Truncate(i.Title, 150),
                    notes = ArabicText.Truncate(i.Notes, 150),
                    project = i.ProjectName,
                    supplier = i.SupplierName,
                    hasAttachment = i.AttachmentUrl != null
                })
            };
        }

        internal static object BuildGeneratedInvoice(GeneratedInvoiceDTO g) => new
        {
            found = true,
            id = g.Id,
            invoiceNumber = g.InvoiceNumber,
            title = g.Title,
            issueDate = Day(g.IssueDate),
            dueDate = Day(g.DueDate),
            recipient = g.RecipientName,
            project = g.ProjectName == null ? null : new { id = g.ProjectId, name = g.ProjectName },
            notes = ArabicText.Truncate(g.Notes, 200),
            lineItemsCount = g.LineItems.Count,
            lineItems = g.LineItems.Take(30).Select(li => new
            {
                description = ArabicText.Truncate(li.Description, 150),
                quantity = li.Quantity,
                unitPrice = li.UnitPrice,
                total = li.Total
            }),
            subtotal = g.Subtotal,
            taxPercent = g.TaxPercent,
            taxAmount = g.TaxAmount,
            total = g.Total
        };
    }
}
