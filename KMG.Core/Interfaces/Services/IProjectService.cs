using KMG.Core.DTOs.Project;

namespace KMG.Core.Interfaces.Services
{
    public interface IProjectService
    {
        Task<List<ProjectListDTO>> GetAllAsync();
        Task<ProjectDetailsDTO?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateProjectDTO model, int createdByEmployeeId);
        Task<bool> UpdateStatusAsync(UpdateProjectStatusDTO model, int employeeId);

        Task<ProjectPaymentDTO> RecordPaymentAsync(CreateProjectPaymentDTO model, int createdByEmployeeId);
        Task<ProjectExpenseDTO> RecordExpenseAsync(CreateProjectExpenseDTO model, int createdByEmployeeId);
        Task<ProjectAttachmentDTO> AddAttachmentAsync(CreateProjectAttachmentDTO model, int uploadedByEmployeeId);
    }
}
