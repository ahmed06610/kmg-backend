using KMG.Core.DTOs.Invoice;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using KMG.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KMG.Core.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public InvoiceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // بنجمع من خمس جداول مختلفة كل واحد فيه حالته الخاصة (مش كلهم بيعملوا حركة خزنة وقت
        // حصولهم)، فبنحوّلهم لشكل موحّد InvoiceDTO هنا فقط للعرض - مفيش جدول جديد بيتكتب فيه
        public async Task<PagedResultDTO<InvoiceDTO>> GetInvoicesAsync(InvoiceFilterDTO filter)
        {
            var all = new List<InvoiceDTO>();
            var dateFrom = filter.DateFrom?.Date;
            var dateTo = filter.DateTo?.Date.AddDays(1);

            if (ShouldInclude(filter, "ProjectPayment", "In"))
            {
                var query = _unitOfWork.ProjectPayment.GetQueryable(p => p.AttachmentUrl != null)
                    .Include(p => p.Project)
                    .Include(p => p.CreatedByEmployee)
                    .AsQueryable();

                if (dateFrom.HasValue) query = query.Where(p => p.PaymentDate >= dateFrom.Value);
                if (dateTo.HasValue) query = query.Where(p => p.PaymentDate < dateTo.Value);
                if (filter.ProjectId.HasValue) query = query.Where(p => p.ProjectId == filter.ProjectId.Value);

                var items = await query.ToListAsync();
                all.AddRange(items.Select(p => new InvoiceDTO
                {
                    SourceType = "ProjectPayment",
                    SourceId = p.Id,
                    Direction = "In",
                    Amount = p.Amount,
                    Date = p.PaymentDate,
                    AttachmentUrl = p.AttachmentUrl,
                    AttachmentFileName = p.AttachmentFileName,
                    Title = $"دفعة من مشروع {p.Project.ProjectCode}",
                    Notes = p.Notes,
                    ProjectId = p.ProjectId,
                    ProjectName = p.Project.Name,
                    CreatedByEmployeeName = p.CreatedByEmployee.Name
                }));
            }

            if (ShouldInclude(filter, "ProjectExpense", "Out"))
            {
                var query = _unitOfWork.ProjectExpense.GetQueryable(e => e.AttachmentUrl != null)
                    .Include(e => e.Project)
                    .Include(e => e.CreatedByEmployee)
                    .AsQueryable();

                if (dateFrom.HasValue) query = query.Where(e => e.ExpenseDate >= dateFrom.Value);
                if (dateTo.HasValue) query = query.Where(e => e.ExpenseDate < dateTo.Value);
                if (filter.ProjectId.HasValue) query = query.Where(e => e.ProjectId == filter.ProjectId.Value);

                var items = await query.ToListAsync();
                all.AddRange(items.Select(e => new InvoiceDTO
                {
                    SourceType = "ProjectExpense",
                    SourceId = e.Id,
                    Direction = "Out",
                    Amount = e.Amount,
                    Date = e.ExpenseDate,
                    AttachmentUrl = e.AttachmentUrl,
                    AttachmentFileName = e.AttachmentFileName,
                    Title = $"مصروف مشروع {e.Project.ProjectCode}: {e.Description}",
                    Notes = e.Description,
                    ProjectId = e.ProjectId,
                    ProjectName = e.Project.Name,
                    CreatedByEmployeeName = e.CreatedByEmployee.Name
                }));
            }

            if (ShouldInclude(filter, "StockPurchase", "Out"))
            {
                var query = _unitOfWork.StockMovement.GetQueryable(m => m.AttachmentUrl != null && m.MovementType == Enums.MovementType.Purchase)
                    .Include(m => m.Material)
                    .Include(m => m.Supplier)
                    .Include(m => m.CreatedByEmployee)
                    .AsQueryable();

                if (dateFrom.HasValue) query = query.Where(m => m.MovementDate >= dateFrom.Value);
                if (dateTo.HasValue) query = query.Where(m => m.MovementDate < dateTo.Value);
                if (filter.SupplierId.HasValue) query = query.Where(m => m.SupplierId == filter.SupplierId.Value);

                var items = await query.ToListAsync();
                all.AddRange(items.Select(m => new InvoiceDTO
                {
                    SourceType = "StockPurchase",
                    SourceId = m.Id,
                    Direction = "Out",
                    Amount = m.Quantity * m.UnitPriceAtTime,
                    Date = m.MovementDate,
                    AttachmentUrl = m.AttachmentUrl,
                    AttachmentFileName = m.AttachmentFileName,
                    Title = $"شراء مخزون: {m.Material.Name}",
                    Notes = m.Notes,
                    SupplierId = m.SupplierId,
                    SupplierName = m.Supplier?.Name,
                    CreatedByEmployeeName = m.CreatedByEmployee.Name
                }));
            }

            if (ShouldInclude(filter, "StockIssue", "Out"))
            {
                var query = _unitOfWork.StockMovement.GetQueryable(m => m.AttachmentUrl != null && m.MovementType == Enums.MovementType.IssueToProject)
                    .Include(m => m.Material)
                    .Include(m => m.Project)
                    .Include(m => m.CreatedByEmployee)
                    .AsQueryable();

                if (dateFrom.HasValue) query = query.Where(m => m.MovementDate >= dateFrom.Value);
                if (dateTo.HasValue) query = query.Where(m => m.MovementDate < dateTo.Value);
                if (filter.ProjectId.HasValue) query = query.Where(m => m.ProjectId == filter.ProjectId.Value);

                var items = await query.ToListAsync();
                all.AddRange(items.Select(m => new InvoiceDTO
                {
                    SourceType = "StockIssue",
                    SourceId = m.Id,
                    Direction = "Out",
                    Amount = m.Quantity * m.UnitPriceAtTime,
                    Date = m.MovementDate,
                    AttachmentUrl = m.AttachmentUrl,
                    AttachmentFileName = m.AttachmentFileName,
                    Title = $"صرف مخزون لمشروع {m.Project?.ProjectCode}: {m.Material.Name}",
                    Notes = m.Notes,
                    ProjectId = m.ProjectId,
                    ProjectName = m.Project?.Name,
                    CreatedByEmployeeName = m.CreatedByEmployee.Name
                }));
            }

            if (ShouldInclude(filter, "SupplierPayment", "Out"))
            {
                var query = _unitOfWork.SupplierPayment.GetQueryable(p => p.AttachmentUrl != null)
                    .Include(p => p.Supplier)
                    .Include(p => p.CreatedByEmployee)
                    .AsQueryable();

                if (dateFrom.HasValue) query = query.Where(p => p.PaymentDate >= dateFrom.Value);
                if (dateTo.HasValue) query = query.Where(p => p.PaymentDate < dateTo.Value);
                if (filter.SupplierId.HasValue) query = query.Where(p => p.SupplierId == filter.SupplierId.Value);

                var items = await query.ToListAsync();
                all.AddRange(items.Select(p => new InvoiceDTO
                {
                    SourceType = "SupplierPayment",
                    SourceId = p.Id,
                    Direction = "Out",
                    Amount = p.Amount,
                    Date = p.PaymentDate,
                    AttachmentUrl = p.AttachmentUrl,
                    AttachmentFileName = p.AttachmentFileName,
                    Title = $"دفعة للمورد: {p.Supplier.Name}",
                    Notes = p.Notes,
                    SupplierId = p.SupplierId,
                    SupplierName = p.Supplier.Name,
                    CreatedByEmployeeName = p.CreatedByEmployee.Name
                }));
            }

            if (ShouldInclude(filter, "MiscExpense", "Out"))
            {
                var query = _unitOfWork.MiscExpense.GetQueryable(e => e.AttachmentUrl != null)
                    .Include(e => e.CreatedByEmployee)
                    .AsQueryable();

                if (dateFrom.HasValue) query = query.Where(e => e.ExpenseDate >= dateFrom.Value);
                if (dateTo.HasValue) query = query.Where(e => e.ExpenseDate < dateTo.Value);

                // مفيهوش مشروع أو مورد مربوط - فلو المستخدم فلتر بمشروع أو مورد، النوع ده مايظهرش خالص
                if (filter.ProjectId.HasValue || filter.SupplierId.HasValue)
                    query = query.Where(e => false);

                var items = await query.ToListAsync();
                all.AddRange(items.Select(e => new InvoiceDTO
                {
                    SourceType = "MiscExpense",
                    SourceId = e.Id,
                    Direction = "Out",
                    Amount = e.Amount,
                    Date = e.ExpenseDate,
                    AttachmentUrl = e.AttachmentUrl,
                    AttachmentFileName = e.AttachmentFileName,
                    Title = $"مصروف نثري: {e.Notes}",
                    Notes = e.Notes,
                    CreatedByEmployeeName = e.CreatedByEmployee.Name
                }));
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim();
                all = all.Where(i =>
                    i.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (i.Notes != null && i.Notes.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (i.ProjectName != null && i.ProjectName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (i.SupplierName != null && i.SupplierName.Contains(term, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            var ordered = all.OrderByDescending(i => i.Date).ToList();
            var totalCount = ordered.Count;
            var page = Math.Max(filter.Page, 1);
            var pageSize = Math.Clamp(filter.PageSize, 1, 200);

            return new PagedResultDTO<InvoiceDTO>
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList()
            };
        }

        private static bool ShouldInclude(InvoiceFilterDTO filter, string sourceType, string direction)
        {
            if (!string.IsNullOrWhiteSpace(filter.SourceType) && filter.SourceType != sourceType) return false;
            if (!string.IsNullOrWhiteSpace(filter.Direction) && filter.Direction != direction) return false;
            return true;
        }

        public async Task<List<GeneratedInvoiceListDTO>> GetGeneratedInvoicesAsync()
        {
            var invoices = await _unitOfWork.GeneratedInvoice.GetQueryable(null)
                .Include(i => i.Project)
                .Include(i => i.LineItems)
                .OrderByDescending(i => i.IssueDate)
                .ToListAsync();

            return invoices.Select(i => new GeneratedInvoiceListDTO
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                Title = i.Title,
                IssueDate = i.IssueDate,
                RecipientName = i.RecipientName,
                Total = i.LineItems.Sum(li => li.Quantity * li.UnitPrice) * (1 + (i.TaxPercent ?? 0) / 100m),
                ProjectName = i.Project?.Name,
                ShowSignature = i.ShowSignature
            }).ToList();
        }

        public async Task<GeneratedInvoiceDTO?> GetGeneratedInvoiceByIdAsync(int id)
        {
            var invoice = await _unitOfWork.GeneratedInvoice.GetQueryable(i => i.Id == id)
                .Include(i => i.Project)
                .Include(i => i.CreatedByEmployee)
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync();

            return invoice == null ? null : MapGeneratedInvoice(invoice);
        }

        public async Task<GeneratedInvoiceDTO> CreateGeneratedInvoiceAsync(CreateGeneratedInvoiceDTO model, int createdByEmployeeId)
        {
            if (model.LineItems.Count == 0)
                throw new Exception("لازم بند واحد على الأقل في الفاتورة");
            if (string.IsNullOrWhiteSpace(model.RecipientName))
                throw new Exception("اسم المستلم مطلوب");

            var invoice = new GeneratedInvoice
            {
                InvoiceNumber = await GenerateInvoiceNumberAsync(),
                Title = model.Title,
                IssueDate = model.IssueDate,
                DueDate = model.DueDate,
                RecipientName = model.RecipientName,
                RecipientAddress = model.RecipientAddress,
                RecipientPhone = model.RecipientPhone,
                ProjectId = model.ProjectId,
                Notes = model.Notes,
                TaxPercent = model.TaxPercent,
                CreatorDisplayName = model.CreatorDisplayName,
                ShowCreatorName = model.ShowCreatorName,
                ShowSignature = model.ShowSignature,
                CreatedByEmployeeId = createdByEmployeeId,
                CreatedAt = TimeHelper.NowInEgypt,
                LineItems = model.LineItems.Select(li => new GeneratedInvoiceLineItem
                {
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice
                }).ToList()
            };

            await _unitOfWork.GeneratedInvoice.AddAsync(invoice);
            await _unitOfWork.CompleteAsync();

            return (await GetGeneratedInvoiceByIdAsync(invoice.Id))!;
        }

        public async Task<bool> DeleteGeneratedInvoiceAsync(int id)
        {
            var invoice = await _unitOfWork.GeneratedInvoice.GetByIdAsync(id);
            if (invoice == null) return false;

            _unitOfWork.GeneratedInvoice.Delete(invoice);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        private async Task<string> GenerateInvoiceNumberAsync()
        {
            var year = TimeHelper.NowInEgypt.Year;
            var countThisYear = (await _unitOfWork.GeneratedInvoice.FindAllAsync(i => i.IssueDate.Year == year)).Count();
            return $"INV-{year}-{(countThisYear + 1):D4}";
        }

        private static GeneratedInvoiceDTO MapGeneratedInvoice(GeneratedInvoice i) => new()
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            Title = i.Title,
            IssueDate = i.IssueDate,
            DueDate = i.DueDate,
            RecipientName = i.RecipientName,
            RecipientAddress = i.RecipientAddress,
            RecipientPhone = i.RecipientPhone,
            ProjectId = i.ProjectId,
            ProjectName = i.Project?.Name,
            Notes = i.Notes,
            TaxPercent = i.TaxPercent,
            CreatorDisplayName = i.CreatorDisplayName,
            ShowCreatorName = i.ShowCreatorName,
            ShowSignature = i.ShowSignature,
            CreatedByEmployeeName = i.CreatedByEmployee.Name,
            CreatedAt = i.CreatedAt,
            LineItems = i.LineItems.Select(li => new GeneratedInvoiceLineItemDTO
            {
                Id = li.Id,
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice
            }).ToList()
        };
    }
}
