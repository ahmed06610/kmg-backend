using KMG.Core.DTOs.Supplier;
using KMG.Core.Enums;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using KMG.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KMG.Core.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICashBoxService _cashBoxService;

        public SupplierService(IUnitOfWork unitOfWork, ICashBoxService cashBoxService)
        {
            _unitOfWork = unitOfWork;
            _cashBoxService = cashBoxService;
        }

        public async Task<List<SupplierListDTO>> GetAllAsync()
        {
            var suppliers = await _unitOfWork.Supplier.GetQueryable(null)
                .Include(s => s.StockMovements)
                .Include(s => s.Payments)
                .AsSplitQuery()
                .ToListAsync();
            return suppliers.Select(MapList).ToList();
        }

        public async Task<SupplierDetailsDTO?> GetByIdAsync(int id)
        {
            var supplier = await _unitOfWork.Supplier.GetQueryable(s => s.Id == id)
                .Include(s => s.StockMovements).ThenInclude(sm => sm.Material)
                .Include(s => s.Payments)
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (supplier == null) return null;

            var totalPurchases = supplier.StockMovements.Where(m => m.MovementType == MovementType.Purchase).Sum(m => m.Quantity * m.UnitPriceAtTime);
            var totalPaid = CountedPayments(supplier.Payments).Sum(p => p.Amount);

            return new SupplierDetailsDTO
            {
                Id = supplier.Id,
                Name = supplier.Name,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address,
                CreatedAt = supplier.CreatedAt,
                TotalPurchases = totalPurchases,
                TotalPaid = totalPaid,
                TotalRemaining = totalPurchases - totalPaid,
                Payments = supplier.Payments.OrderByDescending(p => p.PaymentDate).Select(p => MapPayment(p, supplier.Name)).ToList(),
                Purchases = supplier.StockMovements.Where(m => m.MovementType == MovementType.Purchase)
                    .OrderByDescending(m => m.MovementDate)
                    .Select(m => new SupplierPurchaseDTO
                    {
                        Id = m.Id,
                        MaterialName = m.Material.Name,
                        Quantity = m.Quantity,
                        UnitPriceAtTime = m.UnitPriceAtTime,
                        MovementDate = m.MovementDate
                    }).ToList()
            };
        }

        public async Task<int> CreateAsync(CreateSupplierDTO model)
        {
            var supplier = new Supplier
            {
                Name = model.Name,
                Phone = model.Phone,
                Email = model.Email,
                Address = model.Address,
                CreatedAt = TimeHelper.NowInEgypt
            };

            await _unitOfWork.Supplier.AddAsync(supplier);
            await _unitOfWork.CompleteAsync();
            return supplier.Id;
        }

        public async Task<bool> UpdateAsync(UpdateSupplierDTO model)
        {
            var supplier = await _unitOfWork.Supplier.GetByIdAsync(model.Id);
            if (supplier == null) return false;

            supplier.Name = model.Name;
            supplier.Phone = model.Phone;
            supplier.Email = model.Email;
            supplier.Address = model.Address;

            _unitOfWork.Supplier.Update(supplier);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var supplier = await _unitOfWork.Supplier.GetByIdAsync(id);
            if (supplier == null) return false;

            try
            {
                _unitOfWork.Supplier.Delete(supplier);
                await _unitOfWork.CompleteAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                throw new Exception("لا يمكن حذف مورد له معاملات مسجلة (مشتريات أو دفعات) في النظام");
            }
        }

        public async Task<SupplierPaymentDTO> RecordPaymentAsync(CreateSupplierPaymentDTO model, int createdByEmployeeId)
        {
            if (model.AmountCash < 0 || model.AmountCredit < 0)
                throw new Exception("لا يمكن أن تكون قيمة الكاش أو الكريديت سالبة");
            if (model.AmountCash + model.AmountCredit <= 0)
                throw new Exception("قيمة الدفعة يجب أن تكون أكبر من صفر");
            if (model.IsCheck && model.AmountCredit != 0)
                throw new Exception("دفعة الشيك لا يمكن تقسيمها كاش/كريديت");
            if (model.IsCheck && model.CheckDueDate == null)
                throw new Exception("لازم تحديد تاريخ استحقاق الشيك");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var supplier = await _unitOfWork.Supplier.GetQueryable(s => s.Id == model.SupplierId)
                    .Include(s => s.StockMovements)
                    .Include(s => s.Payments)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync()
                    ?? throw new Exception("المورد غير موجود");

                var totalPurchases = supplier.StockMovements.Where(m => m.MovementType == MovementType.Purchase).Sum(m => m.Quantity * m.UnitPriceAtTime);
                var totalPaid = CountedPayments(supplier.Payments).Sum(p => p.Amount);
                var outstanding = totalPurchases - totalPaid;

                if (model.AmountCash + model.AmountCredit > outstanding)
                    throw new Exception($"قيمة الدفعة أكبر من المستحق على المورد (المستحق: {outstanding})");

                var payment = new SupplierPayment
                {
                    SupplierId = model.SupplierId,
                    Amount = model.AmountCash + model.AmountCredit,
                    AmountCash = model.AmountCash,
                    AmountCredit = model.AmountCredit,
                    PaymentDate = model.PaymentDate,
                    Notes = model.Notes,
                    IsCheck = model.IsCheck,
                    CheckDueDate = model.IsCheck ? model.CheckDueDate : null,
                    CheckStatus = model.IsCheck ? Enums.CheckStatus.Pending : null,
                    CreatedByEmployeeId = createdByEmployeeId
                };

                await _unitOfWork.SupplierPayment.AddAsync(payment);
                await _unitOfWork.CompleteAsync(); // نحتاج payment.Id عشان نربط بيه حركة الخزنة

                // دفعة الشيك متتسجلش في الخزنة إلا لما تتأكد إنها اتصرفت فعليًا (ResolveCheckAsync)
                if (!model.IsCheck)
                {
                    await _cashBoxService.RecordTransactionAsync(
                        amountCash: -model.AmountCash,
                        amountCredit: -model.AmountCredit,
                        type: TransactionType.SupplierPaymentOut,
                        description: $"سداد دفعة للمورد: {supplier.Name}",
                        createdByEmployeeId: createdByEmployeeId,
                        supplierId: model.SupplierId,
                        supplierPaymentId: payment.Id);
                }

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return MapPayment(payment, supplier.Name);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<SupplierPaymentDTO> UpdatePaymentAsync(UpdateSupplierPaymentDTO model, int employeeId)
        {
            if (model.AmountCash < 0 || model.AmountCredit < 0)
                throw new Exception("لا يمكن أن تكون قيمة الكاش أو الكريديت سالبة");
            var newAmount = model.AmountCash + model.AmountCredit;
            if (newAmount <= 0)
                throw new Exception("قيمة الدفعة يجب أن تكون أكبر من صفر");
            if (model.IsCheck && model.AmountCredit != 0)
                throw new Exception("دفعة الشيك لا يمكن تقسيمها كاش/كريديت");
            if (model.IsCheck && model.CheckDueDate == null)
                throw new Exception("لازم تحديد تاريخ استحقاق الشيك");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var payment = await _unitOfWork.SupplierPayment.GetQueryable(p => p.Id == model.Id)
                    .Include(p => p.Supplier).ThenInclude(s => s.StockMovements)
                    .Include(p => p.Supplier).ThenInclude(s => s.Payments)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync() ?? throw new Exception("الدفعة غير موجودة");

                var supplier = payment.Supplier;
                var totalPurchases = supplier.StockMovements.Where(m => m.MovementType == MovementType.Purchase).Sum(m => m.Quantity * m.UnitPriceAtTime);
                var totalPaidExcludingThis = CountedPayments(supplier.Payments.Where(p => p.Id != payment.Id)).Sum(p => p.Amount);

                if (totalPaidExcludingThis + newAmount > totalPurchases)
                    throw new Exception($"قيمة الدفعة أكبر من المستحق على المورد (المستحق: {totalPurchases - totalPaidExcludingThis})");

                var wasPostedToCashBox = !payment.IsCheck || payment.CheckStatus == Enums.CheckStatus.Cleared;
                if (wasPostedToCashBox)
                    await _cashBoxService.ReverseAsync(t => t.SupplierPaymentId == payment.Id);

                payment.AmountCash = model.AmountCash;
                payment.AmountCredit = model.AmountCredit;
                payment.Amount = newAmount;
                payment.PaymentDate = model.PaymentDate;
                payment.Notes = model.Notes;
                payment.IsCheck = model.IsCheck;
                payment.CheckDueDate = model.IsCheck ? model.CheckDueDate : null;
                payment.CheckStatus = model.IsCheck ? (payment.CheckStatus ?? Enums.CheckStatus.Pending) : null;
                _unitOfWork.SupplierPayment.Update(payment);

                var shouldPostToCashBox = !payment.IsCheck || payment.CheckStatus == Enums.CheckStatus.Cleared;
                if (shouldPostToCashBox)
                {
                    await _cashBoxService.RecordTransactionAsync(
                        amountCash: -payment.AmountCash,
                        amountCredit: -payment.AmountCredit,
                        type: TransactionType.SupplierPaymentOut,
                        description: $"تعديل دفعة للمورد: {supplier.Name}",
                        createdByEmployeeId: employeeId,
                        supplierId: payment.SupplierId,
                        supplierPaymentId: payment.Id);
                }

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return MapPayment(payment, supplier.Name);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeletePaymentAsync(int id)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var payment = await _unitOfWork.SupplierPayment.GetByIdAsync(id);
                if (payment == null) return false;

                var wasPostedToCashBox = !payment.IsCheck || payment.CheckStatus == Enums.CheckStatus.Cleared;
                if (wasPostedToCashBox)
                    await _cashBoxService.ReverseAsync(t => t.SupplierPaymentId == id);

                _unitOfWork.SupplierPayment.Delete(payment);

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<List<SupplierPaymentDTO>> GetPendingChecksAsync()
        {
            var payments = await _unitOfWork.SupplierPayment
                .GetQueryable(p => p.IsCheck && p.CheckStatus == Enums.CheckStatus.Pending)
                .Include(p => p.Supplier)
                .OrderBy(p => p.CheckDueDate)
                .ToListAsync();

            return payments.Select(p => MapPayment(p, p.Supplier.Name)).ToList();
        }

        public async Task<SupplierPaymentDTO> ResolveCheckAsync(ResolveCheckDTO model, int employeeId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var payment = await _unitOfWork.SupplierPayment.GetQueryable(p => p.Id == model.PaymentId)
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync() ?? throw new Exception("الدفعة غير موجودة");

                if (!payment.IsCheck || payment.CheckStatus != Enums.CheckStatus.Pending)
                    throw new Exception("الدفعة دي مش شيك معلّق");

                switch (model.Action)
                {
                    case CheckResolutionAction.Clear:
                        await _cashBoxService.RecordTransactionAsync(
                            amountCash: -payment.AmountCash,
                            amountCredit: -payment.AmountCredit,
                            type: TransactionType.SupplierPaymentOut,
                            description: $"صرف شيك للمورد: {payment.Supplier.Name}",
                            createdByEmployeeId: employeeId,
                            supplierId: payment.SupplierId,
                            supplierPaymentId: payment.Id);
                        payment.CheckStatus = Enums.CheckStatus.Cleared;
                        break;

                    case CheckResolutionAction.Reschedule:
                        if (model.NewDueDate == null)
                            throw new Exception("لازم تحديد تاريخ استحقاق جديد");
                        payment.CheckDueDate = model.NewDueDate;
                        break;

                    case CheckResolutionAction.Cancel:
                        payment.CheckStatus = Enums.CheckStatus.Cancelled;
                        break;
                }

                _unitOfWork.SupplierPayment.Update(payment);
                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return MapPayment(payment, payment.Supplier.Name);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private static IEnumerable<SupplierPayment> CountedPayments(IEnumerable<SupplierPayment> payments) =>
            payments.Where(p => p.CheckStatus != Enums.CheckStatus.Cancelled);

        private static SupplierPaymentDTO MapPayment(SupplierPayment p, string supplierName) => new()
        {
            Id = p.Id,
            SupplierId = p.SupplierId,
            SupplierName = supplierName,
            Amount = p.Amount,
            AmountCash = p.AmountCash,
            AmountCredit = p.AmountCredit,
            PaymentDate = p.PaymentDate,
            Notes = p.Notes,
            IsCheck = p.IsCheck,
            CheckDueDate = p.CheckDueDate,
            CheckStatus = p.CheckStatus?.ToString()
        };

        private static SupplierListDTO MapList(Supplier s)
        {
            var totalPurchases = s.StockMovements.Where(m => m.MovementType == MovementType.Purchase).Sum(m => m.Quantity * m.UnitPriceAtTime);
            var totalPaid = CountedPayments(s.Payments).Sum(p => p.Amount);
            return new SupplierListDTO
            {
                Id = s.Id,
                Name = s.Name,
                Phone = s.Phone,
                Email = s.Email,
                TotalPurchases = totalPurchases,
                TotalPaid = totalPaid,
                TotalRemaining = totalPurchases - totalPaid
            };
        }
    }
}
