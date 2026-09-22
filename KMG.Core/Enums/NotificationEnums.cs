namespace KMG.Core.Enums
{
    public enum NotificationType
    {
        LowStock = 1,
        CheckDueSoon = 2,
        ProjectPaymentFollowUp = 3,
        SupplierPaymentDue = 4,
        PayrollMonthEnd = 5,
        MissionOpenTooLong = 6,
        ProjectClosedWithBalance = 7
    }

    public enum NotificationSeverity
    {
        Info = 1,
        Warning = 2,
        Critical = 3
    }
}
