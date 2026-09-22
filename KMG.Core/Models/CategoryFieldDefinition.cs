namespace KMG.Core.Models
{
    /// <summary>
    /// تعريف حقل إضافي مخصص لفئة مخزون معينة (مثلاً "اللون" لفئة "دهانات").
    /// متخزنة كـ JSON جوه عمود MaterialCategory.ExtraFieldDefinitions - مش جدول منفصل.
    /// </summary>
    public class CategoryFieldDefinition
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string FieldType { get; set; } = "text"; // text | number
    }
}
