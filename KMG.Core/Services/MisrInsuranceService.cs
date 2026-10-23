using KMG.Core.DTOs.Employee;
using KMG.Core.Enums;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using KMG.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KMG.Core.Services
{
    public class MisrInsuranceService : IMisrInsuranceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICashBoxService _cashBoxService;

        public MisrInsuranceService(IUnitOfWork unitOfWork, ICashBoxService cashBoxService)
        {
            _unitOfWork = unitOfWork;
            _cashBoxService = cashBoxService;
        }

        public async Task<List<EmployeeMisrInsuranceDTO>> GetEmployeeMisrInsuranceSummaryAsync()
        {
            var employees = await _unitOfWork.Employee
                .GetQueryable(e => !e.Suspended && e.MisrInsuranceAmount > 0)
                .OrderBy(e => e.Name)
                .ToListAsync();

            return employees.Select(e => new EmployeeMisrInsuranceDTO
            {
                EmployeeId = e.Id,
                EmployeeName = e.Name,
                MisrInsuranceAmount = e.MisrInsuranceAmount
            }).ToList();
        }

        public async Task<List<MisrInsurancePayoutDTO>> GetPayoutHistoryAsync()
        {
            var payouts = await _unitOfWork.MisrInsurancePayout.GetQueryable(null)
                .Include(p => p.CreatedByEmployee)
                .OrderByDescending(p => p.PeriodStart)
                .ToListAsync();

            return payouts.Select(MapPayout).ToList();
        }

        public async Task<MisrInsurancePayoutDTO> PayMisrInsuranceAsync(PayMisrInsuranceDTO model, int createdByEmployeeId)
        {
            if (model.PeriodEnd < model.PeriodStart)
                throw new Exception("تاريخ نهاية الفترة لازم يكون بعد تاريخ البداية");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var overlapping = await _unitOfWork.MisrInsurancePayout
                    .GetQueryable(p => p.PeriodStart <= model.PeriodEnd && p.PeriodEnd >= model.PeriodStart)
                    .AnyAsync();
                if (overlapping)
                    throw new Exception("تم صرف مصر للتأمين بالفعل عن فترة متداخلة مع الفترة المطلوبة");

                var totalAmount = await _unitOfWork.Employee
                    .GetQueryable(e => !e.Suspended && e.MisrInsuranceAmount > 0)
                    .SumAsync(e => e.MisrInsuranceAmount);

                if (totalAmount <= 0)
                    throw new Exception("لا يوجد موظفين لهم قيمة مصر للتأمين لصرفها");

                var payout = new MisrInsurancePayout
                {
                    PeriodStart = model.PeriodStart,
                    PeriodEnd = model.PeriodEnd,
                    TotalAmount = totalAmount,
                    PaidDate = TimeHelper.NowInEgypt,
                    Notes = model.Notes,
                    CreatedByEmployeeId = createdByEmployeeId
                };

                await _unitOfWork.MisrInsurancePayout.AddAsync(payout);
                await _unitOfWork.CompleteAsync(); // نحتاج payout.Id عشان نربط بيه حركة الخزنة

                await _cashBoxService.RecordTransactionAsync(
                    amountCash: -totalAmount,
                    amountCredit: 0,
                    type: TransactionType.MisrInsuranceOut,
                    description: $"صرف مصر للتأمين للموظفين عن الفترة {model.PeriodStart:yyyy-MM-dd} إلى {model.PeriodEnd:yyyy-MM-dd}",
                    createdByEmployeeId: createdByEmployeeId,
                    misrInsurancePayoutId: payout.Id);

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                var employee = await _unitOfWork.Employee.GetQueryable(e => e.Id == createdByEmployeeId).FirstAsync();
                payout.CreatedByEmployee = employee;
                return MapPayout(payout);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private static MisrInsurancePayoutDTO MapPayout(MisrInsurancePayout p) => new()
        {
            Id = p.Id,
            PeriodStart = p.PeriodStart,
            PeriodEnd = p.PeriodEnd,
            TotalAmount = p.TotalAmount,
            PaidDate = p.PaidDate,
            Notes = p.Notes,
            CreatedByEmployeeName = p.CreatedByEmployee.Name
        };
    }
}
