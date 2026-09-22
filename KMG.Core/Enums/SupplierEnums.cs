namespace KMG.Core.Enums
{
    public enum CheckStatus
    {
        Pending = 1,   // شيك لسه معلق، محددله تاريخ استحقاق
        Cleared = 2,   // اتصرف فعليًا وخُصم من الخزنة
        Cancelled = 3  // اتلغى - ميتحسبش كدفعة خالص
    }

    public enum CheckResolutionAction
    {
        Clear = 1,       // تم الصرف - يتخصم من الخزنة الآن
        Reschedule = 2,  // لسه - يتحدد تاريخ استحقاق جديد
        Cancel = 3       // إلغاء الشيك
    }
}
