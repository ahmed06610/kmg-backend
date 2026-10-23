using KMG.Core.DTOs.Employee;
using KMG.Core.Enums;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace KMG.Core.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IUnitOfWork _unitOfWork;

        public EmployeeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private static IQueryable<Models.Employee> IncludeAll(IQueryable<Models.Employee> query) =>
            query.Include(e => e.ApplicationUser).Include(e => e.Manager).Include(e => e.Advances);

        public async Task<List<EmployeeListDTO>> GetAllAsync()
        {
            var employees = await IncludeAll(_unitOfWork.Employee.GetQueryable(null)).ToListAsync();
            var roleIdByUserId = await GetRoleIdByUserIdAsync(employees.Select(e => e.ApplicationUserId));
            return employees.Select(e => Map(e, roleIdByUserId)).ToList();
        }

        public async Task<EmployeeListDTO?> GetByIdAsync(int id)
        {
            var employee = await IncludeAll(_unitOfWork.Employee.GetQueryable(e => e.Id == id)).FirstOrDefaultAsync();
            if (employee == null) return null;
            var roleIdByUserId = await GetRoleIdByUserIdAsync(new[] { employee.ApplicationUserId });
            return Map(employee, roleIdByUserId);
        }

        private async Task<Dictionary<string, string>> GetRoleIdByUserIdAsync(IEnumerable<string?> userIds)
        {
            var result = new Dictionary<string, string>();
            foreach (var userId in userIds.Where(id => id != null).Distinct())
            {
                var roleIds = await _unitOfWork.UserRole.GetUserRoleIdsAsync(userId!);
                var roleId = roleIds.FirstOrDefault();
                if (roleId != null) result[userId!] = roleId;
            }
            return result;
        }

        public async Task<int> CreateWorkerAsync(CreateWorkerDTO model)
        {
            var employee = new Models.Employee
            {
                Name = model.Name,
                Phone = model.Phone,
                EmployeeType = model.EmployeeType,
                WageType = model.WageType,
                WageAmount = model.WageAmount,
                InsuranceAmount = model.InsuranceAmount,
                MisrInsuranceAmount = model.MisrInsuranceAmount,
                ManagerId = model.ManagerId,
                CreatedAt = TimeHelper.NowInEgypt,
                Suspended = false
            };

            await _unitOfWork.Employee.AddAsync(employee);
            await _unitOfWork.CompleteAsync();
            return employee.Id;
        }

        public async Task<bool> UpdateWorkerAsync(UpdateWorkerDTO model)
        {
            var employee = await _unitOfWork.Employee.GetByIdAsync(model.Id);
            if (employee == null) return false;

            if (employee.ApplicationUserId != null)
                throw new Exception("هذا الموظف له حساب دخول للنظام - عدّل بياناته من شاشة تسجيل الموظفين");

            employee.Name = model.Name;
            employee.Phone = model.Phone;
            employee.EmployeeType = model.EmployeeType;
            employee.WageType = model.WageType;
            employee.WageAmount = model.WageAmount;
            employee.InsuranceAmount = model.InsuranceAmount;
            employee.MisrInsuranceAmount = model.MisrInsuranceAmount;
            employee.ManagerId = model.ManagerId;
            employee.Suspended = model.Suspended;
            _unitOfWork.Employee.Update(employee);

            await _unitOfWork.CompleteAsync();
            return true;
        }

        private static EmployeeListDTO Map(Models.Employee e, Dictionary<string, string> roleIdByUserId) => new()
        {
            Id = e.Id,
            Name = e.Name,
            Phone = e.Phone,
            Email = e.ApplicationUser?.Email,
            UserName = e.ApplicationUser?.UserName,
            EmployeeType = e.EmployeeType,
            WageType = e.WageType,
            WageAmount = e.WageAmount,
            InsuranceAmount = e.InsuranceAmount,
            MisrInsuranceAmount = e.MisrInsuranceAmount,
            ManagerId = e.ManagerId,
            ManagerName = e.Manager?.Name,
            Suspended = e.Suspended,
            HasLoginAccount = e.ApplicationUserId != null,
            RoleId = e.ApplicationUserId != null && roleIdByUserId.TryGetValue(e.ApplicationUserId, out var roleId) ? roleId : null,
            RemainingAdvances = e.Advances.Where(a => a.Status == AdvanceStatus.Active).Sum(a => a.RemainingAmount)
        };
    }
}
