using KMG.Core.DTOs.Notification;
using KMG.Core.Enums;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using KMG.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KMG.Core.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;

        // عتبات قابلة للتعديل لاحقًا لو الاحتياج اتغير
        private const int CheckDueSoonDays = 3;
        private const int MissionOpenTooLongDays = 14;
        private const int PayrollReminderDaysBeforeMonthEnd = 5;

        public NotificationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private class Candidate
        {
            public string DedupeKey { get; set; } = string.Empty;
            public NotificationType Type { get; set; }
            public NotificationSeverity Severity { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string? LinkUrl { get; set; }
        }

        public async Task<List<NotificationDTO>> GetActiveAsync()
        {
            await SyncAsync();

            var notifications = await _unitOfWork.Notification.GetQueryable(n => !n.IsDismissed).ToListAsync();

            return notifications
                .OrderByDescending(n => n.Severity)
                .ThenByDescending(n => n.CreatedAt)
                .Select(Map)
                .ToList();
        }

        public async Task MarkReadAsync(int id)
        {
            var notification = await _unitOfWork.Notification.GetByIdAsync(id);
            if (notification == null) return;

            notification.IsRead = true;
            _unitOfWork.Notification.Update(notification);
            await _unitOfWork.CompleteAsync();
        }

        public async Task MarkAllReadAsync()
        {
            var notifications = await _unitOfWork.Notification.GetQueryable(n => !n.IsDismissed && !n.IsRead).ToListAsync();
            foreach (var n in notifications)
            {
                n.IsRead = true;
                _unitOfWork.Notification.Update(n);
            }
            await _unitOfWork.CompleteAsync();
        }

        public async Task DismissAsync(int id)
        {
            var notification = await _unitOfWork.Notification.GetByIdAsync(id);
            if (notification == null) return;

            notification.IsDismissed = true;
            _unitOfWork.Notification.Update(notification);
            await _unitOfWork.CompleteAsync();
        }

        private static NotificationDTO Map(Notification n) => new()
        {
            Id = n.Id,
            Type = n.Type.ToString(),
            Severity = n.Severity.ToString(),
            Title = n.Title,
            Message = n.Message,
            LinkUrl = n.LinkUrl,
            CreatedAt = n.CreatedAt,
            IsRead = n.IsRead
        };

        // بيحسب كل التنبيهات اللي "المفروض تكون موجودة دلوقتي" من الداتا الفعلية، يحدّث/ينشئ الجديد،
        // ويمسح أي تنبيه قديم اتحلت مشكلته (زي مخزون اتجدد أو شيك اتصرف) - عشان التنبيهات تفضل مطابقة للواقع
        private async Task SyncAsync()
        {
            var candidates = await GenerateCandidatesAsync();
            var candidateByKey = candidates.ToDictionary(c => c.DedupeKey);

            var existing = await _unitOfWork.Notification.GetAllAsync();
            var existingList = existing.ToList();
            var existingByKey = existingList.ToDictionary(n => n.DedupeKey);

            foreach (var candidate in candidates)
            {
                if (existingByKey.TryGetValue(candidate.DedupeKey, out var existingNotification))
                {
                    if (!existingNotification.IsDismissed &&
                        (existingNotification.Title != candidate.Title || existingNotification.Message != candidate.Message))
                    {
                        existingNotification.Title = candidate.Title;
                        existingNotification.Message = candidate.Message;
                        existingNotification.Severity = candidate.Severity;
                        _unitOfWork.Notification.Update(existingNotification);
                    }
                }
                else
                {
                    await _unitOfWork.Notification.AddAsync(new Notification
                    {
                        DedupeKey = candidate.DedupeKey,
                        Type = candidate.Type,
                        Severity = candidate.Severity,
                        Title = candidate.Title,
                        Message = candidate.Message,
                        LinkUrl = candidate.LinkUrl,
                        CreatedAt = TimeHelper.NowInEgypt,
                        IsRead = false,
                        IsDismissed = false
                    });
                }
            }

            var resolved = existingList.Where(n => !candidateByKey.ContainsKey(n.DedupeKey)).ToList();
            if (resolved.Count > 0)
                _unitOfWork.Notification.DeleteRange(resolved);

            await _unitOfWork.CompleteAsync();
        }

        private async Task<List<Candidate>> GenerateCandidatesAsync()
        {
            var candidates = new List<Candidate>();
            var now = TimeHelper.NowInEgypt;

            candidates.AddRange(await LowStockCandidatesAsync());
            candidates.AddRange(await CheckDueSoonCandidatesAsync(now));

            var projects = await _unitOfWork.Project.GetQueryable(null)
                .Include(p => p.Payments)
                .Include(p => p.WriteOffs)
                .AsSplitQuery()
                .ToListAsync();

            candidates.AddRange(ProjectPaymentFollowUpCandidates(projects, now));
            candidates.AddRange(ProjectClosedWithBalanceCandidates(projects));
            candidates.AddRange(await SupplierPaymentDueCandidatesAsync(now));
            candidates.AddRange(await PayrollMonthEndCandidatesAsync(now));
            candidates.AddRange(await MissionOpenTooLongCandidatesAsync(now));

            return candidates;
        }

        private async Task<List<Candidate>> LowStockCandidatesAsync()
        {
            var materials = await _unitOfWork.Material.GetAllAsync();

            return materials.Where(m => m.IsLowStock).Select(m => new Candidate
            {
                DedupeKey = $"LowStock:{m.Id}",
                Type = NotificationType.LowStock,
                Severity = NotificationSeverity.Warning,
                Title = "مخزون منخفض",
                Message = $"الكمية المتاحة من \"{m.Name}\" وصلت لـ {m.Quantity} {m.Unit} (الحد الأدنى {m.MinimumThreshold} {m.Unit})",
                LinkUrl = "/stock"
            }).ToList();
        }

        private async Task<List<Candidate>> CheckDueSoonCandidatesAsync(DateTime now)
        {
            var pendingChecks = await _unitOfWork.SupplierPayment
                .GetQueryable(p => p.IsCheck && p.CheckStatus == CheckStatus.Pending && p.CheckDueDate != null)
                .Include(p => p.Supplier)
                .ToListAsync();

            var candidates = new List<Candidate>();
            foreach (var check in pendingChecks)
            {
                var dueDate = check.CheckDueDate!.Value;
                if (dueDate.Date > now.Date.AddDays(CheckDueSoonDays)) continue;

                var overdue = dueDate.Date < now.Date;
                candidates.Add(new Candidate
                {
                    DedupeKey = $"CheckDueSoon:{check.Id}",
                    Type = NotificationType.CheckDueSoon,
                    Severity = overdue ? NotificationSeverity.Critical : NotificationSeverity.Warning,
                    Title = overdue ? "شيك متأخر" : "شيك مستحق قريبًا",
                    Message = $"شيك المورد \"{check.Supplier.Name}\" بقيمة {check.Amount:N2} {(overdue ? "متأخر عن تاريخ" : "مستحق في")} {dueDate:yyyy-MM-dd}",
                    LinkUrl = $"/suppliers/{check.SupplierId}"
                });
            }
            return candidates;
        }

        private static List<Candidate> ProjectPaymentFollowUpCandidates(List<Project> projects, DateTime now)
        {
            var monthKey = now.ToString("yyyyMM");
            return projects
                .Where(p => p.Status != ProjectStatus.Completed && p.Status != ProjectStatus.Closed && p.RemainingBalance > 0)
                .Select(p => new Candidate
                {
                    DedupeKey = $"ProjectPaymentFollowUp:{p.Id}:{monthKey}",
                    Type = NotificationType.ProjectPaymentFollowUp,
                    Severity = NotificationSeverity.Info,
                    Title = "متابعة تحصيل مشروع",
                    Message = $"مشروع \"{p.Name}\" لسه متبقي منه {p.RemainingBalance:N2} من قيمة العقد - يستحق متابعة",
                    LinkUrl = $"/projects/{p.Id}"
                }).ToList();
        }

        private static List<Candidate> ProjectClosedWithBalanceCandidates(List<Project> projects)
        {
            return projects
                .Where(p => (p.Status == ProjectStatus.Completed || p.Status == ProjectStatus.Closed) && p.RemainingBalance > 0)
                .Select(p => new Candidate
                {
                    DedupeKey = $"ProjectClosedWithBalance:{p.Id}",
                    Type = NotificationType.ProjectClosedWithBalance,
                    Severity = NotificationSeverity.Critical,
                    Title = "مشروع مقفول برصيد غير محصل",
                    Message = $"مشروع \"{p.Name}\" وصل لحالة \"{p.Status.Arabic()}\" ولسه متبقي منه {p.RemainingBalance:N2} غير محصل ولا مسجل كخصم أعمال",
                    LinkUrl = $"/projects/{p.Id}"
                }).ToList();
        }

        private async Task<List<Candidate>> SupplierPaymentDueCandidatesAsync(DateTime now)
        {
            var suppliers = await _unitOfWork.Supplier.GetQueryable(null)
                .Include(s => s.StockMovements)
                .Include(s => s.Payments)
                .AsSplitQuery()
                .ToListAsync();

            var monthKey = now.ToString("yyyyMM");
            var candidates = new List<Candidate>();

            foreach (var supplier in suppliers)
            {
                var totalPurchases = supplier.StockMovements
                    .Where(m => m.MovementType == MovementType.Purchase)
                    .Sum(m => m.Quantity * m.UnitPriceAtTime);
                var totalPaid = supplier.Payments
                    .Where(p => p.CheckStatus != CheckStatus.Cancelled)
                    .Sum(p => p.Amount);
                var remaining = totalPurchases - totalPaid;

                if (remaining <= 0) continue;

                candidates.Add(new Candidate
                {
                    DedupeKey = $"SupplierPaymentDue:{supplier.Id}:{monthKey}",
                    Type = NotificationType.SupplierPaymentDue,
                    Severity = NotificationSeverity.Info,
                    Title = "مستحقات مورد",
                    Message = $"المورد \"{supplier.Name}\" له مستحقات متبقية بقيمة {remaining:N2}",
                    LinkUrl = $"/suppliers/{supplier.Id}"
                });
            }
            return candidates;
        }

        private async Task<List<Candidate>> PayrollMonthEndCandidatesAsync(DateTime now)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var daysLeft = (monthEnd.Date - now.Date).Days;

            if (daysLeft > PayrollReminderDaysBeforeMonthEnd || daysLeft < 0) return new List<Candidate>();

            var activeEmployees = await _unitOfWork.Employee.GetQueryable(e => !e.Suspended).ToListAsync();
            var payouts = await _unitOfWork.PayrollPayout
                .GetQueryable(p => p.PeriodStart <= monthEnd && p.PeriodEnd >= monthStart)
                .ToListAsync();

            var paidEmployeeIds = payouts.Select(p => p.EmployeeId).ToHashSet();
            var unpaidCount = activeEmployees.Count(e => !paidEmployeeIds.Contains(e.Id));

            if (unpaidCount == 0) return new List<Candidate>();

            return new List<Candidate>
            {
                new()
                {
                    DedupeKey = $"PayrollMonthEnd:{now:yyyyMM}",
                    Type = NotificationType.PayrollMonthEnd,
                    Severity = NotificationSeverity.Warning,
                    Title = "قرب نهاية الشهر - مرتبات",
                    Message = $"باقي {daysLeft} يوم على نهاية الشهر و{unpaidCount} موظف لسه ما اتصرفش مرتبه",
                    LinkUrl = "/payroll"
                }
            };
        }

        private async Task<List<Candidate>> MissionOpenTooLongCandidatesAsync(DateTime now)
        {
            var openMissions = await _unitOfWork.Mission.GetQueryable(m => m.Status == MissionStatus.Open)
                .Include(m => m.Project)
                .Include(m => m.ForemanEmployee)
                .ToListAsync();

            var candidates = new List<Candidate>();
            foreach (var mission in openMissions)
            {
                var daysOpen = (int)(now.Date - mission.StartDate.Date).TotalDays;
                if (daysOpen < MissionOpenTooLongDays) continue;

                candidates.Add(new Candidate
                {
                    DedupeKey = $"MissionOpenTooLong:{mission.Id}",
                    Type = NotificationType.MissionOpenTooLong,
                    Severity = NotificationSeverity.Warning,
                    Title = "مأمورية مفتوحة لفترة طويلة",
                    Message = $"مأمورية {mission.ForemanEmployee.Name} في مشروع \"{mission.Project.Name}\" مفتوحة من {daysOpen} يوم من غير تسوية",
                    LinkUrl = $"/projects/{mission.ProjectId}"
                });
            }
            return candidates;
        }
    }
}
