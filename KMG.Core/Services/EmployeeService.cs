using KMG.Core.DTOs.Employee;
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
            query.Include(e => e.ApplicationUser).Include(e => e.Manager);

        public async Task<List<EmployeeListDTO>> GetAllAsync()
        {
            var employees = await IncludeAll(_unitOfWork.Employee.GetQueryable(null)).ToListAsync();
            return employees.Select(Map).ToList();
        }

        public async Task<EmployeeListDTO?> GetByIdAsync(int id)
        {
            var employee = await IncludeAll(_unitOfWork.Employee.GetQueryable(e => e.Id == id)).FirstOrDefaultAsync();
            return employee == null ? null : Map(employee);
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
                ManagerId = model.ManagerId,
                CreatedAt = TimeHelper.NowInEgypt,
                Suspended = false
            };

            await _unitOfWork.Employee.AddAsync(employee);
            await _unitOfWork.CompleteAsync();
            return employee.Id;
        }

        private static EmployeeListDTO Map(Models.Employee e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            Phone = e.Phone,
            Email = e.ApplicationUser?.Email,
            EmployeeType = e.EmployeeType,
            WageType = e.WageType,
            WageAmount = e.WageAmount,
            ManagerName = e.Manager?.Name,
            Suspended = e.Suspended,
            HasLoginAccount = e.ApplicationUserId != null
        };
    }
}
