using KMG.Core.Interfaces;
using KMG.EF.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace KMG.EF.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        public IAbilityRepository Ability { get; }
        public IRolesAbilityRepository RolesAbility { get; }
        public IUserAbilityRepository UserAbility { get; }
        public IUserRepository User { get; }
        public IUserRoleRepository UserRole { get; }

        public IEmployeeRepository Employee { get; }
        public IClientRepository Client { get; }
        public ISupplierRepository Supplier { get; }
        public ISupplierPaymentRepository SupplierPayment { get; }

        public IMaterialRepository Material { get; }
        public IMaterialCategoryRepository MaterialCategory { get; }
        public IStockMovementRepository StockMovement { get; }

        public IProjectRepository Project { get; }
        public IProjectPaymentRepository ProjectPayment { get; }
        public IProjectExpenseRepository ProjectExpense { get; }
        public IProjectAttachmentRepository ProjectAttachment { get; }
        public IProjectAuditRepository ProjectAudit { get; }
        public IProjectWriteOffRepository ProjectWriteOff { get; }

        public IMissionRepository Mission { get; }
        public IMissionWorkerRepository MissionWorker { get; }

        public IAdvanceRepository Advance { get; }
        public IPayrollAdjustmentRepository PayrollAdjustment { get; }
        public IPayrollPayoutRepository PayrollPayout { get; }

        public ICashBoxRepository CashBox { get; }
        public ICashBoxTransactionRepository CashBoxTransaction { get; }
        public IMiscExpenseRepository MiscExpense { get; }

        public IAiPromptConfigRepository AiPromptConfig { get; }
        public INotificationRepository Notification { get; }
        public IAiTenderResultRepository AiTenderResult { get; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;

            Ability = new AbilityRepository(context);
            RolesAbility = new RolesAbilityRepository(context);
            UserAbility = new UserAbilityRepository(context);
            User = new UserRepository(context);
            UserRole = new UserRoleRepository(context);

            Employee = new EmployeeRepository(context);
            Client = new ClientRepository(context);
            Supplier = new SupplierRepository(context);
            SupplierPayment = new SupplierPaymentRepository(context);

            Material = new MaterialRepository(context);
            MaterialCategory = new MaterialCategoryRepository(context);
            StockMovement = new StockMovementRepository(context);

            Project = new ProjectRepository(context);
            ProjectPayment = new ProjectPaymentRepository(context);
            ProjectExpense = new ProjectExpenseRepository(context);
            ProjectAttachment = new ProjectAttachmentRepository(context);
            ProjectAudit = new ProjectAuditRepository(context);
            ProjectWriteOff = new ProjectWriteOffRepository(context);

            Mission = new MissionRepository(context);
            MissionWorker = new MissionWorkerRepository(context);

            Advance = new AdvanceRepository(context);
            PayrollAdjustment = new PayrollAdjustmentRepository(context);
            PayrollPayout = new PayrollPayoutRepository(context);

            CashBox = new CashBoxRepository(context);
            CashBoxTransaction = new CashBoxTransactionRepository(context);
            MiscExpense = new MiscExpenseRepository(context);

            AiPromptConfig = new AiPromptConfigRepository(context);
            Notification = new NotificationRepository(context);
            AiTenderResult = new AiTenderResultRepository(context);
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
            return _transaction;
        }

        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();

        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task CommitAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }
}
