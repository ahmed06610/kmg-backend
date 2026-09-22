namespace KMG.Core.DTOs.Mission
{
    public class MissionListDTO
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectCode { get; set; } = string.Empty;
        public int ForemanEmployeeId { get; set; }
        public string ForemanName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal AdvanceAmount { get; set; }
        public decimal ActualSpent { get; set; }
        public decimal SettlementDifference { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalLaborCost { get; set; }
    }

    public class MissionDetailsDTO : MissionListDTO
    {
        public string? Notes { get; set; }
        public List<MissionWorkerDTO> Workers { get; set; } = new();
    }

    public class MissionWorkerDTO
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int DaysCount { get; set; }
    }

    public class MissionWorkerInputDTO
    {
        public int EmployeeId { get; set; }
        public int DaysCount { get; set; }
    }

    public class CreateMissionDTO
    {
        public int ProjectId { get; set; }
        public int ForemanEmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public decimal AdvanceAmount { get; set; }
        public string? Notes { get; set; }
        public List<MissionWorkerInputDTO> Workers { get; set; } = new();
    }

    public class SettleMissionDTO
    {
        public int MissionId { get; set; }
        public DateTime EndDate { get; set; }
        public decimal ActualSpent { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateMissionDTO
    {
        public int MissionId { get; set; }
        public int ForemanEmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public decimal AdvanceAmount { get; set; }
        public string? Notes { get; set; }
    }
}
