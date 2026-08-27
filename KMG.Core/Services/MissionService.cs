using KMG.Core.DTOs.Mission;
using KMG.Core.Enums;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using KMG.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KMG.Core.Services
{
    public class MissionService : IMissionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICashBoxService _cashBoxService;

        public MissionService(IUnitOfWork unitOfWork, ICashBoxService cashBoxService)
        {
            _unitOfWork = unitOfWork;
            _cashBoxService = cashBoxService;
        }

        private static IQueryable<Mission> IncludeAll(IQueryable<Mission> query) =>
            query.Include(m => m.Project)
                 .Include(m => m.ForemanEmployee)
                 .Include(m => m.MissionWorkers).ThenInclude(w => w.Employee);

        public async Task<List<MissionListDTO>> GetByProjectAsync(int projectId)
        {
            var missions = await IncludeAll(_unitOfWork.Mission.GetQueryable(m => m.ProjectId == projectId))
                .OrderByDescending(m => m.StartDate)
                .ToListAsync();

            return missions.Select(MapList).ToList();
        }

        public async Task<MissionDetailsDTO?> GetByIdAsync(int id)
        {
            var mission = await IncludeAll(_unitOfWork.Mission.GetQueryable(m => m.Id == id)).FirstOrDefaultAsync();
            if (mission == null) return null;

            var list = MapList(mission);
            return new MissionDetailsDTO
            {
                Id = list.Id,
                ProjectId = list.ProjectId,
                ProjectCode = list.ProjectCode,
                ForemanName = list.ForemanName,
                StartDate = list.StartDate,
                EndDate = list.EndDate,
                AdvanceAmount = list.AdvanceAmount,
                ActualSpent = list.ActualSpent,
                SettlementDifference = list.SettlementDifference,
                Status = list.Status,
                TotalLaborCost = list.TotalLaborCost,
                Notes = mission.Notes,
                Workers = mission.MissionWorkers.Select(w => new MissionWorkerDTO
                {
                    EmployeeId = w.EmployeeId,
                    EmployeeName = w.Employee.Name,
                    DaysCount = w.DaysCount
                }).ToList()
            };
        }

        public async Task<int> CreateAsync(CreateMissionDTO model, int createdByEmployeeId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var project = await _unitOfWork.Project.GetByIdAsync(model.ProjectId)
                    ?? throw new Exception("المشروع غير موجود");

                var mission = new Mission
                {
                    ProjectId = model.ProjectId,
                    ForemanEmployeeId = model.ForemanEmployeeId,
                    StartDate = model.StartDate,
                    AdvanceAmount = model.AdvanceAmount,
                    ActualSpent = 0,
                    Status = MissionStatus.Open,
                    Notes = model.Notes
                };

                await _unitOfWork.Mission.AddAsync(mission);
                await _unitOfWork.CompleteAsync(); // نحتاج mission.Id

                foreach (var worker in model.Workers)
                {
                    await _unitOfWork.MissionWorker.AddAsync(new MissionWorker
                    {
                        MissionId = mission.Id,
                        EmployeeId = worker.EmployeeId,
                        DaysCount = worker.DaysCount
                    });
                }

                if (model.AdvanceAmount > 0)
                {
                    await _cashBoxService.RecordTransactionAsync(
                        amountCash: -model.AdvanceAmount,
                        amountCredit: 0,
                        type: TransactionType.MissionAdvanceOut,
                        description: $"تسليم عهدة مأمورية لمشروع {project.ProjectCode}",
                        createdByEmployeeId: createdByEmployeeId,
                        projectId: model.ProjectId,
                        missionId: mission.Id);
                }

                await _unitOfWork.ProjectAudit.AddAsync(new ProjectAudit
                {
                    ProjectId = model.ProjectId,
                    EmployeeId = createdByEmployeeId,
                    ActionDate = TimeHelper.NowInEgypt,
                    ActionDescription = $"فتح مأمورية جديدة بعهدة {model.AdvanceAmount}"
                });

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return mission.Id;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<MissionDetailsDTO> SettleAsync(SettleMissionDTO model, int settledByEmployeeId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var mission = await _unitOfWork.Mission.GetQueryable(m => m.Id == model.MissionId)
                    .Include(m => m.Project)
                    .FirstOrDefaultAsync() ?? throw new Exception("المأمورية غير موجودة");

                mission.EndDate = model.EndDate;
                mission.ActualSpent = model.ActualSpent;
                mission.Status = MissionStatus.Settled;
                if (!string.IsNullOrWhiteSpace(model.Notes)) mission.Notes = model.Notes;
                _unitOfWork.Mission.Update(mission);

                var difference = mission.AdvanceAmount - mission.ActualSpent;

                if (difference > 0)
                {
                    // فلوس فاضلة من العهدة رجعت للخزنة
                    await _cashBoxService.RecordTransactionAsync(
                        amountCash: difference,
                        amountCredit: 0,
                        type: TransactionType.MissionSettlementIn,
                        description: $"استرجاع فارق عهدة مأمورية لمشروع {mission.Project.ProjectCode}",
                        createdByEmployeeId: settledByEmployeeId,
                        projectId: mission.ProjectId,
                        missionId: mission.Id);
                }
                else if (difference < 0)
                {
                    // العهدة متكفتش، الفرق ده مصروف نثري إضافي هيتسدد لرئيس العمال
                    var extra = -difference;

                    var expense = new ProjectExpense
                    {
                        ProjectId = mission.ProjectId,
                        Amount = extra,
                        Category = ExpenseCategory.MissionSettlementDiff,
                        Description = $"فرق تسوية عهدة مأمورية رقم {mission.Id}",
                        ExpenseDate = model.EndDate,
                        CreatedByEmployeeId = settledByEmployeeId
                    };
                    await _unitOfWork.ProjectExpense.AddAsync(expense);
                    await _unitOfWork.CompleteAsync();

                    await _cashBoxService.RecordTransactionAsync(
                        amountCash: -extra,
                        amountCredit: 0,
                        type: TransactionType.MissionSettlementIn,
                        description: $"سداد فارق عهدة مأمورية لمشروع {mission.Project.ProjectCode}",
                        createdByEmployeeId: settledByEmployeeId,
                        projectId: mission.ProjectId,
                        projectExpenseId: expense.Id,
                        missionId: mission.Id);
                }

                await _unitOfWork.ProjectAudit.AddAsync(new ProjectAudit
                {
                    ProjectId = mission.ProjectId,
                    EmployeeId = settledByEmployeeId,
                    ActionDate = TimeHelper.NowInEgypt,
                    ActionDescription = $"تسوية عهدة المأمورية: المصروف الفعلي {model.ActualSpent} (الفرق {difference})"
                });

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return (await GetByIdAsync(mission.Id))!;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private static MissionListDTO MapList(Mission m) => new()
        {
            Id = m.Id,
            ProjectId = m.ProjectId,
            ProjectCode = m.Project.ProjectCode,
            ForemanName = m.ForemanEmployee.Name,
            StartDate = m.StartDate,
            EndDate = m.EndDate,
            AdvanceAmount = m.AdvanceAmount,
            ActualSpent = m.ActualSpent,
            SettlementDifference = m.SettlementDifference,
            Status = m.Status.ToString(),
            TotalLaborCost = m.TotalLaborCost
        };
    }
}
