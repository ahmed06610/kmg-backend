using KMG.Core.DTOs.Project;

namespace KMG.Core.Interfaces.Services
{
    public interface IProjectService
    {
        Task<List<ProjectListDTO>> GetAllAsync();
        Task<ProjectDetailsDTO?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateProjectDTO model, int createdByEmployeeId);
        Task<bool> UpdateAsync(UpdateProjectDTO model, int employeeId);
        Task<bool> DeleteAsync(int id);
        Task<bool> UpdateStatusAsync(UpdateProjectStatusDTO model, int employeeId);
        Task<bool> RecoverInsuranceAsync(int projectId, int employeeId);
        Task<bool> RecoverGuaranteeAsync(int projectId, int employeeId);

        Task<ProjectPaymentDTO> RecordPaymentAsync(CreateProjectPaymentDTO model, int createdByEmployeeId);
        Task<ProjectPaymentDTO> UpdatePaymentAsync(UpdateProjectPaymentDTO model, int employeeId);
        Task<bool> DeletePaymentAsync(int id, int employeeId);
        Task<List<ProjectPaymentDTO>> GetPendingChecksAsync();
        Task<ProjectPaymentDTO> ResolveCheckAsync(ResolveProjectPaymentCheckDTO model, int employeeId);

        Task<ProjectExpenseDTO> RecordExpenseAsync(CreateProjectExpenseDTO model, int createdByEmployeeId);
        Task<ProjectExpenseDTO> UpdateExpenseAsync(UpdateProjectExpenseDTO model, int employeeId);
        Task<bool> DeleteExpenseAsync(int id, int employeeId);

        Task<ProjectAttachmentDTO> AddAttachmentAsync(CreateProjectAttachmentDTO model, int uploadedByEmployeeId);
        Task<bool> UpdateAttachmentAsync(UpdateProjectAttachmentDTO model);
        Task<string?> DeleteAttachmentAsync(int id);

        Task<ProjectWriteOffDTO> CreateWriteOffAsync(CreateProjectWriteOffDTO model, int employeeId);
        Task<ProjectWriteOffDTO> UpdateWriteOffAsync(UpdateProjectWriteOffDTO model, int employeeId);
        Task<bool> DeleteWriteOffAsync(int id, int employeeId);
    }
}
