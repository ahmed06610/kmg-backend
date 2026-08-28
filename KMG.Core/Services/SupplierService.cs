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
            var totalPaid = supplier.Payments.Sum(p => p.Amount);

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
                Payments = supplier.Payments.OrderByDescending(p => p.PaymentDate).Select(p => new SupplierPaymentDTO
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    AmountCash = p.AmountCash,
                    AmountCredit = p.AmountCredit,
                    PaymentDate = p.PaymentDate,
                    Notes = p.Notes
                }).ToList(),
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

        public async Task<SupplierPaymentDTO> RecordPaymentAsync(CreateSupplierPaymentDTO model, int createdByEmployeeId)
        {
            if (model.AmountCash < 0 || model.AmountCredit < 0)
                throw new Exception("لا يمكن أن تكون قيمة الكاش أو الكريديت سالبة");
            if (model.AmountCash + model.AmountCredit <= 0)
                throw new Exception("قيمة الدفعة يجب أن تكون أكبر من صفر");

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
                var totalPaid = supplier.Payments.Sum(p => p.Amount);
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
                    CreatedByEmployeeId = createdByEmployeeId
                };

                await _unitOfWork.SupplierPayment.AddAsync(payment);

                await _cashBoxService.RecordTransactionAsync(
                    amountCash: -model.AmountCash,
                    amountCredit: -model.AmountCredit,
                    type: TransactionType.SupplierPaymentOut,
                    description: $"سداد دفعة للمورد: {supplier.Name}",
                    createdByEmployeeId: createdByEmployeeId,
                    supplierId: model.SupplierId);

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return new SupplierPaymentDTO
                {
                    Id = payment.Id,
                    Amount = payment.Amount,
                    AmountCash = payment.AmountCash,
                    AmountCredit = payment.AmountCredit,
                    PaymentDate = payment.PaymentDate,
                    Notes = payment.Notes
                };
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private static SupplierListDTO MapList(Supplier s)
        {
            var totalPurchases = s.StockMovements.Where(m => m.MovementType == MovementType.Purchase).Sum(m => m.Quantity * m.UnitPriceAtTime);
            var totalPaid = s.Payments.Sum(p => p.Amount);
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
