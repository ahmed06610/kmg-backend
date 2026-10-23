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

        public async Task<AdvanceDTO> UpdateAdvanceAsync(UpdateAdvanceDTO model, int employeeId)
        {
            if (model.TotalAmount <= 0)
                throw new Exception("قيمة السلفة يجب أن تكون أكبر من صفر");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var advance = await _unitOfWork.Advance.GetQueryable(a => a.Id == model.Id)
                    .Include(a => a.Employee)
                    .FirstOrDefaultAsync() ?? throw new Exception("السلفة غير موجودة");

                if (advance.RemainingAmount != advance.TotalAmount)
                    throw new Exception("لا يمكن تعديل سلفة تم خصم أقساط منها بالفعل");

                await _cashBoxService.ReverseAsync(t => t.AdvanceId == advance.Id);

                advance.TotalAmount = model.TotalAmount;
                advance.InstallmentAmount = model.InstallmentAmount;
                advance.RemainingAmount = model.TotalAmount;
                advance.Notes = model.Notes;
                _unitOfWork.Advance.Update(advance);

                await _cashBoxService.RecordTransactionAsync(
                    amountCash: -model.TotalAmount,
                    amountCredit: 0,
                    type: TransactionType.AdvanceOut,
                    description: $"تعديل سلفة للموظف: {advance.Employee.Name}",
                    createdByEmployeeId: employeeId,
                    advanceId: advance.Id);

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return MapAdvance(advance);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteAdvanceAsync(int id)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var advance = await _unitOfWork.Advance.GetByIdAsync(id);
                if (advance == null) return false;

                if (advance.RemainingAmount != advance.TotalAmount)
                    throw new Exception("لا يمكن حذف سلفة تم خصم أقساط منها بالفعل");

                await _cashBoxService.ReverseAsync(t => t.AdvanceId == id);
                _unitOfWork.Advance.Delete(advance);

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
                Applied = false,
                Hours = model.Hours,
                HourlyRate = model.HourlyRate
            };

            await _unitOfWork.PayrollAdjustment.AddAsync(adjustment);
            await _unitOfWork.CompleteAsync();

            var employee = await _unitOfWork.Employee.GetQueryable(e => e.Id == model.EmployeeId)
                .FirstAsync();

            return MapAdjustment(adjustment, employee);
        }

        public async Task<PayrollAdjustmentDTO> UpdateAdjustmentAsync(UpdateAdjustmentDTO model)
        {
            var adjustment = await _unitOfWork.PayrollAdjustment.GetQueryable(a => a.Id == model.Id)
                .Include(a => a.Employee)
                .FirstOrDefaultAsync() ?? throw new Exception("التسوية غير موجودة");

            if (adjustment.Applied)
                throw new Exception("لا يمكن تعديل تسوية تم تطبيقها بالفعل في راتب مصروف");

            adjustment.Type = model.Type;
            adjustment.Amount = model.Amount;
            adjustment.Reason = model.Reason;
            adjustment.Date = model.Date;
            adjustment.Hours = model.Hours;
            adjustment.HourlyRate = model.HourlyRate;
            _unitOfWork.PayrollAdjustment.Update(adjustment);
            await _unitOfWork.CompleteAsync();

            return MapAdjustment(adjustment);
        }

        public async Task<bool> DeleteAdjustmentAsync(int id)
        {
            var adjustment = await _unitOfWork.PayrollAdjustment.GetByIdAsync(id);
            if (adjustment == null) return false;

            if (adjustment.Applied)
                throw new Exception("لا يمكن حذف تسوية تم تطبيقها بالفعل في راتب مصروف");

            _unitOfWork.PayrollAdjustment.Delete(adjustment);
            await _unitOfWork.CompleteAsync();
            return true;
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
                    InsuranceAmount = preview.InsuranceAmount,
                    MisrInsuranceAmount = preview.MisrInsuranceAmount,
                    NetPaid = preview.NetPaid,
                    WorkshopDays = preview.WorkshopDays,
                    MissionDays = preview.MissionDays,
                    HourlyDeductionAmount = preview.HourlyDeductionAmount,
                    HourlyBonusAmount = preview.HourlyBonusAmount,
                    PaidDate = TimeHelper.NowInEgypt,
                    CreatedByEmployeeId = createdByEmployeeId
                };

                await _unitOfWork.PayrollPayout.AddAsync(payout);
                await _unitOfWork.CompleteAsync();

                foreach (var adjustment in adjustmentsToApply)
                {
                    adjustment.Applied = true;
                    adjustment.AppliedInPayrollPayoutId = payout.Id;
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

                    await _unitOfWork.PayrollAdvanceCharge.AddAsync(new PayrollAdvanceCharge
                    {
                        PayrollPayoutId = payout.Id,
                        AdvanceId = advance.Id,
                        ChargedAmount = chargedAmount
                    });
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

        // تعديل مباشر لأرقام راتب اتصرف بالفعل (زي غلطة كتابة في قيمة ما) - بيرجّع حركة الخزنة القديمة
        // ويسجل واحدة جديدة بالصافي المعدّل. مينفعش غير على القيم نفسها - لو محتاج يلغي خصم/سلفة
        // مرتبطة بالراتب ده، يحذف الراتب كله بدل كده (DeletePayrollPayoutAsync) وبعدين يصرفه تاني.
        public async Task<PayrollPayoutDTO> UpdatePayrollPayoutAsync(UpdatePayrollPayoutDTO model, int employeeId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var payout = await _unitOfWork.PayrollPayout.GetQueryable(p => p.Id == model.Id)
                    .Include(p => p.Employee)
                    .FirstOrDefaultAsync() ?? throw new Exception("الراتب غير موجود");

                var netPaid = model.BaseAmount + model.MissionDoubleUpAmount + model.BonusAmount
                    - model.DeductionsAmount - model.AdvanceInstallmentAmount - model.InsuranceAmount - model.MisrInsuranceAmount;
                if (netPaid < 0)
                    throw new Exception($"صافي المستحق سالب ({netPaid}) - راجع القيم المدخلة");

                await _cashBoxService.ReverseAsync(t => t.PayrollPayoutId == payout.Id);

                payout.BaseAmount = model.BaseAmount;
                payout.MissionDoubleUpAmount = model.MissionDoubleUpAmount;
                payout.DeductionsAmount = model.DeductionsAmount;
                payout.BonusAmount = model.BonusAmount;
                payout.AdvanceInstallmentAmount = model.AdvanceInstallmentAmount;
                payout.InsuranceAmount = model.InsuranceAmount;
                payout.MisrInsuranceAmount = model.MisrInsuranceAmount;
                payout.NetPaid = netPaid;
                _unitOfWork.PayrollPayout.Update(payout);

                await _cashBoxService.RecordTransactionAsync(
                    amountCash: -netPaid,
                    amountCredit: 0,
                    type: TransactionType.PayrollOut,
                    description: $"تعديل مستحقات الموظف {payout.Employee.Name} عن الفترة {payout.PeriodStart:yyyy-MM-dd} إلى {payout.PeriodEnd:yyyy-MM-dd}",
                    createdByEmployeeId: employeeId,
                    payrollPayoutId: payout.Id);

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return MapPayout(payout, payout.Employee);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // إلغاء راتب كامل: بيرجّع حركة الخزنة، وأي خصم/حافز اتطبق بسبب الراتب ده يرجع "غير مطبّق"
        // تاني عشان يتحسب في أول راتب جديد، وأي قسط سلفة اتخصم بسببه يرجع لرصيد السلفة
        public async Task<bool> DeletePayrollPayoutAsync(int id)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var payout = await _unitOfWork.PayrollPayout.GetByIdAsync(id);
                if (payout == null) return false;

                await _cashBoxService.ReverseAsync(t => t.PayrollPayoutId == id);

                var unAppliedAdjustments = await _unitOfWork.PayrollAdjustment
                    .GetQueryable(a => a.AppliedInPayrollPayoutId == id)
                    .ToListAsync();
                foreach (var adjustment in unAppliedAdjustments)
                {
                    adjustment.Applied = false;
                    adjustment.AppliedInPayrollPayoutId = null;
                    _unitOfWork.PayrollAdjustment.Update(adjustment);
                }

                var advanceCharges = await _unitOfWork.PayrollAdvanceCharge
                    .GetQueryable(c => c.PayrollPayoutId == id)
                    .Include(c => c.Advance)
                    .ToListAsync();
                foreach (var charge in advanceCharges)
                {
                    charge.Advance.RemainingAmount += charge.ChargedAmount;
                    if (charge.Advance.Status == AdvanceStatus.Settled && charge.Advance.RemainingAmount > 0)
                        charge.Advance.Status = AdvanceStatus.Active;
                    _unitOfWork.Advance.Update(charge.Advance);
                }
                _unitOfWork.PayrollAdvanceCharge.DeleteRange(advanceCharges);

                _unitOfWork.PayrollPayout.Delete(payout);

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
            var periodDays = (periodEnd.Date - periodStart.Date).Days + 1;
            decimal missionDaysInPeriod = 0;
            // يوم الجمعة إجازة للعامل اليومي - مالوش يومية عليه، فبيتشال من أيام الفترة
            var fridaysInPeriod = CountFridays(periodStart.Date, periodEnd.Date);
            decimal missionFridayDays = 0;

            if (employee.WageType == WageType.Monthly)
            {
                baseAmount = employee.WageAmount;
                fridaysInPeriod = 0; // الشهري راتبه ثابت، مفيش استبعاد أيام
            }
            else
            {
                baseAmount = employee.WageAmount * (periodDays - fridaysInPeriod);

                var missionWorkerRecords = await _unitOfWork.MissionWorker
                    .GetQueryable(w => w.EmployeeId == employeeId)
                    .Include(w => w.Mission)
                    .ToListAsync();

                // بنحسب التداخل بحدود نصف مفتوحة [بداية، نهاية) بالكسور، عشان DaysCount ممكن يبقى
                // رقم كسري (3.5 يوم مثلًا) - .TotalDays بيدينا الفرق بالساعات كسر، مش يوم صحيح مقطوع
                var periodStartDay = periodStart.Date;
                var periodEndExclusive = periodEnd.Date.AddDays(1);

                foreach (var record in missionWorkerRecords)
                {
                    var missionStartDay = record.Mission.StartDate.Date;
                    var missionEndExclusive = missionStartDay.AddDays((double)record.DaysCount);

                    var overlapStart = missionStartDay > periodStartDay ? missionStartDay : periodStartDay;
                    var overlapEndExclusive = missionEndExclusive < periodEndExclusive ? missionEndExclusive : periodEndExclusive;

                    if (overlapEndExclusive > overlapStart)
                    {
                        // اليومية العادية اتحسبت بالفعل في baseAmount لأيام الفترة (من غير الجمع) - هنا بنضيف
                        // الفرق بين الأجر اليومي اللي المستخدم حدده لأيام المأمورية دي وبين اليومية العادية.
                        // الجمعة اللي العامل بيكون فيها في مأمورية بيشتغل فعلًا، فبتاخد أجر المأمورية كامل:
                        // الفرق + اليومية العادية اللي اتشالت من الأساسي بسبب الجمعة
                        var daysInPeriod = (decimal)(overlapEndExclusive - overlapStart).TotalDays;
                        var fridayPortion = FridayOverlapDays(overlapStart, overlapEndExclusive);
                        missionDoubleUp += daysInPeriod * (record.DailyRate - employee.WageAmount) + fridayPortion * employee.WageAmount;
                        missionDaysInPeriod += daysInPeriod;
                        missionFridayDays += fridayPortion;
                    }
                }
            }

            var adjustments = await _unitOfWork.PayrollAdjustment
                .GetQueryable(a => a.EmployeeId == employeeId && !a.Applied
                    && a.Date >= periodStart.Date && a.Date <= periodEnd.Date)
                .ToListAsync();

            var deductions = adjustments.Where(a => a.Type == AdjustmentType.Deduction).Sum(a => a.Amount);
            var bonuses = adjustments.Where(a => a.Type == AdjustmentType.Bonus).Sum(a => a.Amount);
            // جزء من الخصم/الحافز محسوب بالساعة - للعرض/التصدير بس (مش جزء زيادة في الحساب، هو أصلًا داخل deductions/bonuses)
            var hourlyDeduction = adjustments.Where(a => a.Type == AdjustmentType.Deduction && a.Hours != null).Sum(a => a.Amount);
            var hourlyBonus = adjustments.Where(a => a.Type == AdjustmentType.Bonus && a.Hours != null).Sum(a => a.Amount);

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

            var netPaid = baseAmount + missionDoubleUp + bonuses - deductions - advanceInstallmentTotal
                - employee.InsuranceAmount - employee.MisrInsuranceAmount;

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
                InsuranceAmount = employee.InsuranceAmount,
                MisrInsuranceAmount = employee.MisrInsuranceAmount,
                NetPaid = netPaid,
                // أيام الورشة = أيام الفترة من غير الجمع ومن غير أيام المأمورية (اللي مش جمعة)
                WorkshopDays = Math.Max(periodDays - fridaysInPeriod - (missionDaysInPeriod - missionFridayDays), 0),
                MissionDays = missionDaysInPeriod,
                ExcludedFridays = Math.Max(fridaysInPeriod - missionFridayDays, 0),
                HourlyDeductionAmount = hourlyDeduction,
                HourlyBonusAmount = hourlyBonus
            };

            return (preview, adjustments, advancesToCharge, employee);
        }

        private static int CountFridays(DateTime fromDay, DateTime toDay)
        {
            var count = 0;
            for (var day = fromDay; day <= toDay; day = day.AddDays(1))
                if (day.DayOfWeek == DayOfWeek.Friday) count++;
            return count;
        }

        // جزء المدة [start, endExclusive) اللي واقع في أيام جمعة - بالكسور، عشان أيام المأمورية ممكن تبقى كسرية
        private static decimal FridayOverlapDays(DateTime start, DateTime endExclusive)
        {
            decimal total = 0;
            for (var day = start.Date; day < endExclusive; day = day.AddDays(1))
            {
                if (day.DayOfWeek != DayOfWeek.Friday) continue;
                var from = day > start ? day : start;
                var to = day.AddDays(1) < endExclusive ? day.AddDays(1) : endExclusive;
                if (to > from) total += (decimal)(to - from).TotalDays;
            }
            return total;
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
            Applied = a.Applied,
            Hours = a.Hours,
            HourlyRate = a.HourlyRate
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
            InsuranceAmount = p.InsuranceAmount,
            MisrInsuranceAmount = p.MisrInsuranceAmount,
            NetPaid = p.NetPaid,
            WorkshopDays = p.WorkshopDays,
            MissionDays = p.MissionDays,
            HourlyDeductionAmount = p.HourlyDeductionAmount,
            HourlyBonusAmount = p.HourlyBonusAmount,
            PaidDate = p.PaidDate
        };
    }
}
