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

        /// <summary>
        /// تعديل بيانات موظف بدون حساب دخول (عامل). الموظفين اللي عندهم حساب دخول
        /// بيتعدّلوا عن طريق AuthService.EditEmployeeAsync بدل الميثود دي.
        /// </summary>
        Task<bool> UpdateWorkerAsync(UpdateWorkerDTO model);
    }
}
