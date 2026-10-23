using KMG.Core.DTOs.Employee;
using KMG.Core.Enums;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using KMG.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KMG.Core.Services
{
    public class InsuranceService : IInsuranceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICashBoxService _cashBoxService;

        public InsuranceService(IUnitOfWork unitOfWork, ICashBoxService cashBoxService)
        {
            _unitOfWork = unitOfWork;
            _cashBoxService = cashBoxService;
        }

        public async Task<List<EmployeeInsuranceDTO>> GetEmployeeInsuranceSummaryAsync()
        {
            var employees = await _unitOfWork.Employee
                .GetQueryable(e => !e.Suspended && e.InsuranceAmount > 0)
                .OrderBy(e => e.Name)
                .ToListAsync();

            return employees.Select(e => new EmployeeInsuranceDTO
            {
                EmployeeId = e.Id,
                EmployeeName = e.Name,
                InsuranceAmount = e.InsuranceAmount
            }).ToList();
        }

        public async Task<List<InsurancePayoutDTO>> GetPayoutHistoryAsync()
        {
            var payouts = await _unitOfWork.InsurancePayout.GetQueryable(null)
                .Include(p => p.CreatedByEmployee)
                .OrderByDescending(p => p.PeriodStart)
                .ToListAsync();

            return payouts.Select(MapPayout).ToList();
        }

        public async Task<InsurancePayoutDTO> PayInsuranceAsync(PayInsuranceDTO model, int createdByEmployeeId)
        {
            if (model.PeriodEnd < model.PeriodStart)
                throw new Exception("تاريخ نهاية الفترة لازم يكون بعد تاريخ البداية");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var overlapping = await _unitOfWork.InsurancePayout
                    .GetQueryable(p => p.PeriodStart <= model.PeriodEnd && p.PeriodEnd >= model.PeriodStart)
                    .AnyAsync();
                if (overlapping)
                    throw new Exception("تم صرف تأمينات بالفعل عن فترة متداخلة مع الفترة المطلوبة");

                var totalAmount = await _unitOfWork.Employee
                    .GetQueryable(e => !e.Suspended && e.InsuranceAmount > 0)
                    .SumAsync(e => e.InsuranceAmount);

                if (totalAmount <= 0)
                    throw new Exception("لا يوجد موظفين لهم قيمة تأمين لصرفها");

                var payout = new InsurancePayout
                {
                    PeriodStart = model.PeriodStart,
                    PeriodEnd = model.PeriodEnd,
                    TotalAmount = totalAmount,
                    PaidDate = TimeHelper.NowInEgypt,
                    Notes = model.Notes,
                    CreatedByEmployeeId = createdByEmployeeId
                };

                await _unitOfWork.InsurancePayout.AddAsync(payout);
                await _unitOfWork.CompleteAsync(); // نحتاج payout.Id عشان نربط بيه حركة الخزنة

                await _cashBoxService.RecordTransactionAsync(
                    amountCash: -totalAmount,
                    amountCredit: 0,
                    type: TransactionType.EmployeeInsuranceOut,
                    description: $"صرف تأمينات الموظفين عن الفترة {model.PeriodStart:yyyy-MM-dd} إلى {model.PeriodEnd:yyyy-MM-dd}",
                    createdByEmployeeId: createdByEmployeeId,
                    insurancePayoutId: payout.Id);

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

        private static InsurancePayoutDTO MapPayout(InsurancePayout p) => new()
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
