using KMG.Core.DTOs.Employee;

namespace KMG.Core.Interfaces.Services
{
    public interface IEmployeeService
    {
        Task<List<EmployeeListDTO>> GetAllAsync();
        Task<EmployeeListDTO?> GetByIdAsync(int id);

        /// <summary>
        /// إنشاء موظف (عادةً عامل) بدون حساب دخول للنظام - يُستخدم للعمال اللي بيروحوا مأموريات
        /// ومحتاجين نتابع أجرهم وسلفهم بس مش محتاجين يسجلوا دخول للنظام.
        /// </summary>
        Task<int> CreateWorkerAsync(CreateWorkerDTO model);
    }
}
