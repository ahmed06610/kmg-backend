using KMG.Core.DTOs.Payroll;

namespace KMG.Core.Interfaces.Services
{
    public interface IPayrollService
    {
        Task<List<AdvanceDTO>> GetAdvancesAsync(int? employeeId = null);
        Task<AdvanceDTO> CreateAdvanceAsync(CreateAdvanceDTO model, int createdByEmployeeId);
        Task<AdvanceDTO> UpdateAdvanceAsync(UpdateAdvanceDTO model, int employeeId);
        Task<bool> DeleteAdvanceAsync(int id);

        Task<List<PayrollAdjustmentDTO>> GetAdjustmentsAsync(int? employeeId = null);
        Task<PayrollAdjustmentDTO> CreateAdjustmentAsync(CreateAdjustmentDTO model);
        Task<PayrollAdjustmentDTO> UpdateAdjustmentAsync(UpdateAdjustmentDTO model);
        Task<bool> DeleteAdjustmentAsync(int id);

        Task<PayrollPreviewDTO> PreviewPayrollAsync(RunPayrollDTO model);
        Task<PayrollPayoutDTO> RunPayrollAsync(RunPayrollDTO model, int createdByEmployeeId);
        Task<List<PayrollPayoutDTO>> GetPayoutHistoryAsync(int? employeeId = null);
    }
}
