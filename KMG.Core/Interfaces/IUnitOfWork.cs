using Microsoft.EntityFrameworkCore.Storage;

namespace KMG.Core.Interfaces
{
    public interface IUnitOfWork
    {
        IAbilityRepository Ability { get; }
        IRolesAbilityRepository RolesAbility { get; }
        IUserAbilityRepository UserAbility { get; }
        IUserRepository User { get; }
        IUserRoleRepository UserRole { get; }

        IEmployeeRepository Employee { get; }
        IClientRepository Client { get; }
        ISupplierRepository Supplier { get; }
        ISupplierPaymentRepository SupplierPayment { get; }

        IMaterialRepository Material { get; }
        IMaterialCategoryRepository MaterialCategory { get; }
        IStockMovementRepository StockMovement { get; }

        IProjectRepository Project { get; }
        IProjectPaymentRepository ProjectPayment { get; }
        IProjectExpenseRepository ProjectExpense { get; }
        IProjectAttachmentRepository ProjectAttachment { get; }
        IProjectAuditRepository ProjectAudit { get; }
        IProjectWriteOffRepository ProjectWriteOff { get; }

        IMissionRepository Mission { get; }
        IMissionWorkerRepository MissionWorker { get; }

        IAdvanceRepository Advance { get; }
        IPayrollAdjustmentRepository PayrollAdjustment { get; }
        IPayrollPayoutRepository PayrollPayout { get; }

        ICashBoxRepository CashBox { get; }
        ICashBoxTransactionRepository CashBoxTransaction { get; }
        IMiscExpenseRepository MiscExpense { get; }

        IAiPromptConfigRepository AiPromptConfig { get; }
        INotificationRepository Notification { get; }
        IAiTenderResultRepository AiTenderResult { get; }

        Task<int> CompleteAsync();
        Task RollbackAsync();
        Task CommitAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
