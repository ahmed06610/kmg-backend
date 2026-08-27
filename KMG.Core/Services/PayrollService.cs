using KMG.Core.DTOs.Payroll;
using KMG.Core.Enums;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using KMG.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KMG.Core.Services
{
    public class PayrollService : IPayrollService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICashBoxService _cashBoxService;

        public PayrollService(IUnitOfWork unitOfWork, ICashBoxService cashBoxService)
        {
            _unitOfWork = unitOfWork;
            _cashBoxService = cashBoxService;
        }

        // ---------------- Advances ----------------

        public async Task<List<AdvanceDTO>> GetAdvancesAsync(int? employeeId = null)
        {
            var advances = await _unitOfWork.Advance.GetQueryable(a => employeeId == null || a.EmployeeId == employeeId)
                .Include(a => a.Employee)
                .OrderByDescending(a => a.IssueDate)
                .ToListAsync();

            return advances.Select(a => MapAdvance(a)).ToList();
        }

        public async Task<AdvanceDTO> CreateAdvanceAsync(CreateAdvanceDTO model, int createdByEmployeeId)
        {
            if (model.TotalAmount <= 0)
                throw new Exception("قيمة السلفة يجب أن تكون أكبر من صفر");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var employee = await _unitOfWork.Employee.GetQueryable(e => e.Id == model.EmployeeId)
                    .FirstOrDefaultAsync() ?? throw new Exception("الموظف غير موجود");

                var advance = new Advance
                {
                    EmployeeId = model.EmployeeId,
                    TotalAmount = model.TotalAmount,
                    InstallmentAmount = model.InstallmentAmount,
                    RemainingAmount = model.TotalAmount,
                    IssueDate = TimeHelper.NowInEgypt,
                    Status = AdvanceStatus.Active,
                    Notes = model.Notes
                };

                await _unitOfWork.Advance.AddAsync(advance);
                await _unitOfWork.CompleteAsync(); // نحتاج advance.Id عشان نربط بيه حركة الخزنة

                await _cashBoxService.RecordTransactionAsync(
                    amountCash: -model.TotalAmount,
                    amountCredit: 0,
                    type: TransactionType.AdvanceOut,
                    description: $"تسليم سلفة للموظف: {employee.Name}",
                    createdByEmployeeId: createdByEmployeeId,
                    advanceId: advance.Id);

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return MapAdvance(advance, employee);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ---------------- Adjustments (خصومات / حوافز) ----------------

        public async Task<List<PayrollAdjustmentDTO>> GetAdjustmentsAsync(int? employeeId = null)
        {
            var adjustments = await _unitOfWork.PayrollAdjustment.GetQueryable(a => employeeId == null || a.EmployeeId == employeeId)
                .Include(a => a.Employee)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            return adjustments.Select(a => MapAdjustment(a)).ToList();
        }

        public async Task<PayrollAdjustmentDTO> CreateAdjustmentAsync(CreateAdjustmentDTO model)
        {
            var adjustment = new PayrollAdjustment
            {
                EmployeeId = model.EmployeeId,
                Type = model.Type,
                Amount = model.Amount,
                Reason = model.Reason,
                Date = model.Date,
                Applied = false
            };

            await _unitOfWork.PayrollAdjustment.AddAsync(adjustment);
            await _unitOfWork.CompleteAsync();

            var employee = await _unitOfWork.Employee.GetQueryable(e => e.Id == model.EmployeeId)
                .FirstAsync();

            return MapAdjustment(adjustment, employee);
        }

        // ---------------- Payroll run ----------------

        public async Task<PayrollPreviewDTO> PreviewPayrollAsync(RunPayrollDTO model)
        {
            var (preview, _, _, _) = await ComputeAsync(model.EmployeeId, model.PeriodStart, model.PeriodEnd);
            return preview;
        }

        public async Task<PayrollPayoutDTO> RunPayrollAsync(RunPayrollDTO model, int createdByEmployeeId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var overlappingPayout = await _unitOfWork.PayrollPayout
                    .GetQueryable(p => p.EmployeeId == model.EmployeeId
                        && p.PeriodStart <= model.PeriodEnd && p.PeriodEnd >= model.PeriodStart)
                    .FirstOrDefaultAsync();
                if (overlappingPayout != null)
                    throw new Exception("تم صرف راتب لهذا الموظف عن فترة متداخلة مع الفترة المطلوبة بالفعل");

                var (preview, adjustmentsToApply, advancesToCharge, employee) =
                    await ComputeAsync(model.EmployeeId, model.PeriodStart, model.PeriodEnd);

                if (preview.NetPaid < 0)
                    throw new Exception($"صافي المستحق سالب ({preview.NetPaid}) - الخصومات وأقساط السلف أكبر من المستحق الأساسي");

                var payout = new PayrollPayout
                {
                    EmployeeId = model.EmployeeId,
                    PeriodStart = model.PeriodStart,
                    PeriodEnd = model.PeriodEnd,
                    BaseAmount = preview.BaseAmount,
                    MissionDoubleUpAmount = preview.MissionDoubleUpAmount,
                    DeductionsAmount = preview.DeductionsAmount,
                    BonusAmount = preview.BonusAmount,
                    AdvanceInstallmentAmount = preview.AdvanceInstallmentAmount,
                    NetPaid = preview.NetPaid,
                    PaidDate = TimeHelper.NowInEgypt,
                    CreatedByEmployeeId = createdByEmployeeId
                };

                await _unitOfWork.PayrollPayout.AddAsync(payout);
                await _unitOfWork.CompleteAsync();

                foreach (var adjustment in adjustmentsToApply)
                {
                    adjustment.Applied = true;
                    _unitOfWork.PayrollAdjustment.Update(adjustment);
                }

                foreach (var (advance, chargedAmount) in advancesToCharge)
                {
                    advance.RemainingAmount -= chargedAmount;
                    if (advance.RemainingAmount <= 0)
                    {
                        advance.RemainingAmount = 0;
                        advance.Status = AdvanceStatus.Settled;
                    }
                    _unitOfWork.Advance.Update(advance);
                }

                await _cashBoxService.RecordTransactionAsync(
                    amountCash: -payout.NetPaid,
                    amountCredit: 0,
                    type: TransactionType.PayrollOut,
                    description: $"صرف مستحقات الموظف {employee.Name} عن الفترة {model.PeriodStart:yyyy-MM-dd} إلى {model.PeriodEnd:yyyy-MM-dd}",
                    createdByEmployeeId: createdByEmployeeId,
                    payrollPayoutId: payout.Id);

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return MapPayout(payout, employee);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<List<PayrollPayoutDTO>> GetPayoutHistoryAsync(int? employeeId = null)
        {
            var payouts = await _unitOfWork.PayrollPayout.GetQueryable(p => employeeId == null || p.EmployeeId == employeeId)
                .Include(p => p.Employee)
                .OrderByDescending(p => p.PaidDate)
                .ToListAsync();

            return payouts.Select(p => MapPayout(p, p.Employee)).ToList();
        }

        /// <summary>
        /// المحرك الأساسي لحساب مستحقات الموظف عن فترة معينة، بما في ذلك مضاعفة يومية أيام المأمورية
        /// (بما فيها الأيام التي تقع داخل هذه الفترة فقط - الأيام الواقعة في فترة تالية تُرحَّل تلقائيًا
        /// لأنها لن تُحتسب هنا وستُحسب عند تشغيل الرواتب للفترة التالية).
        /// </summary>
        private async Task<(PayrollPreviewDTO Preview, List<PayrollAdjustment> AdjustmentsToApply, List<(Advance Advance, decimal Charged)> AdvancesToCharge, Employee Employee)>
            ComputeAsync(int employeeId, DateTime periodStart, DateTime periodEnd)
        {
            var employee = await _unitOfWork.Employee.GetQueryable(e => e.Id == employeeId)
                .FirstOrDefaultAsync()
                ?? throw new Exception("الموظف غير موجود");

            decimal baseAmount;
            decimal missionDoubleUp = 0;

            if (employee.WageType == WageType.Monthly)
            {
                baseAmount = employee.WageAmount;
            }
            else
            {
                var periodDays = (periodEnd.Date - periodStart.Date).Days + 1;
                baseAmount = employee.WageAmount * periodDays;

                var missionWorkerRecords = await _unitOfWork.MissionWorker
                    .GetQueryable(w => w.EmployeeId == employeeId)
                    .Include(w => w.Mission)
                    .ToListAsync();

                foreach (var record in missionWorkerRecords)
                {
                    var missionStart = record.Mission.StartDate.Date;
                    var missionEnd = missionStart.AddDays(Math.Max(record.DaysCount - 1, 0));

                    var overlapStart = missionStart > periodStart.Date ? missionStart : periodStart.Date;
                    var overlapEnd = missionEnd < periodEnd.Date ? missionEnd : periodEnd.Date;

                    if (overlapEnd >= overlapStart)
                    {
                        var daysInPeriod = (overlapEnd - overlapStart).Days + 1;
                        missionDoubleUp += daysInPeriod * employee.WageAmount;
                    }
                }
            }

            var adjustments = await _unitOfWork.PayrollAdjustment
                .GetQueryable(a => a.EmployeeId == employeeId && !a.Applied
                    && a.Date >= periodStart.Date && a.Date <= periodEnd.Date)
                .ToListAsync();

            var deductions = adjustments.Where(a => a.Type == AdjustmentType.Deduction).Sum(a => a.Amount);
            var bonuses = adjustments.Where(a => a.Type == AdjustmentType.Bonus).Sum(a => a.Amount);

            var activeAdvances = await _unitOfWork.Advance
                .GetQueryable(a => a.EmployeeId == employeeId && a.Status == AdvanceStatus.Active)
                .ToListAsync();

            var advancesToCharge = new List<(Advance, decimal)>();
            decimal advanceInstallmentTotal = 0;
            foreach (var advance in activeAdvances)
            {
                var charged = Math.Min(advance.InstallmentAmount, advance.RemainingAmount);
                if (charged > 0)
                {
                    advancesToCharge.Add((advance, charged));
                    advanceInstallmentTotal += charged;
                }
            }

            var netPaid = baseAmount + missionDoubleUp + bonuses - deductions - advanceInstallmentTotal;

            var preview = new PayrollPreviewDTO
            {
                EmployeeId = employeeId,
                EmployeeName = employee.Name,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                BaseAmount = baseAmount,
                MissionDoubleUpAmount = missionDoubleUp,
                DeductionsAmount = deductions,
                BonusAmount = bonuses,
                AdvanceInstallmentAmount = advanceInstallmentTotal,
                NetPaid = netPaid
            };

            return (preview, adjustments, advancesToCharge, employee);
        }

        private static AdvanceDTO MapAdvance(Advance a, Employee? employee = null) => new()
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            EmployeeName = (employee ?? a.Employee).Name,
            TotalAmount = a.TotalAmount,
            InstallmentAmount = a.InstallmentAmount,
            RemainingAmount = a.RemainingAmount,
            IssueDate = a.IssueDate,
            Status = a.Status.ToString(),
            Notes = a.Notes
        };

        private static PayrollAdjustmentDTO MapAdjustment(PayrollAdjustment a, Employee? employee = null) => new()
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            EmployeeName = (employee ?? a.Employee).Name,
            Type = a.Type.ToString(),
            Amount = a.Amount,
            Reason = a.Reason,
            Date = a.Date,
            Applied = a.Applied
        };

        private static PayrollPayoutDTO MapPayout(PayrollPayout p, Employee employee) => new()
        {
            Id = p.Id,
            EmployeeId = p.EmployeeId,
            EmployeeName = employee.Name,
            PeriodStart = p.PeriodStart,
            PeriodEnd = p.PeriodEnd,
            BaseAmount = p.BaseAmount,
            MissionDoubleUpAmount = p.MissionDoubleUpAmount,
            DeductionsAmount = p.DeductionsAmount,
            BonusAmount = p.BonusAmount,
            AdvanceInstallmentAmount = p.AdvanceInstallmentAmount,
            NetPaid = p.NetPaid,
            PaidDate = p.PaidDate
        };
    }
}
