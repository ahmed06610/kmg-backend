using KMG.Core.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KMG.EF.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, RoleIdentity, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Ability> Abilities { get; set; } = null!;
        public DbSet<RolesAbility> RolesAbilities { get; set; } = null!;
        public DbSet<UserAbility> UserAbilities { get; set; } = null!;

        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<SupplierPayment> SupplierPayments { get; set; } = null!;

        public DbSet<Material> Materials { get; set; } = null!;
        public DbSet<StockMovement> StockMovements { get; set; } = null!;

        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<ProjectPayment> ProjectPayments { get; set; } = null!;
        public DbSet<ProjectExpense> ProjectExpenses { get; set; } = null!;
        public DbSet<ProjectAttachment> ProjectAttachments { get; set; } = null!;
        public DbSet<ProjectAudit> ProjectAudits { get; set; } = null!;

        public DbSet<Mission> Missions { get; set; } = null!;
        public DbSet<MissionWorker> MissionWorkers { get; set; } = null!;

        public DbSet<Advance> Advances { get; set; } = null!;
        public DbSet<PayrollAdjustment> PayrollAdjustments { get; set; } = null!;
        public DbSet<PayrollPayout> PayrollPayouts { get; set; } = null!;

        public DbSet<CashBox> CashBoxes { get; set; } = null!;
        public DbSet<CashBoxTransaction> CashBoxTransactions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RoleIdentity>().ToTable("AspNetRoles");

            // كل الحقول decimal في النظام بتمثل مبالغ مالية أو كميات - نثبت الدقة بدل الافتراضي (18,0)
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                         .SelectMany(t => t.GetProperties())
                         .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("decimal(18,2)");
            }

            // ---------- Ability system ----------
            modelBuilder.Entity<RolesAbility>()
                .HasOne(ra => ra.RoleIdentity)
                .WithMany(r => r.RolesAbilities)
                .HasForeignKey(ra => ra.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolesAbility>()
                .HasOne(ra => ra.Ability)
                .WithMany(a => a.RolesAbilities)
                .HasForeignKey(ra => ra.AbilityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserAbility>()
                .HasOne(ua => ua.User)
                .WithMany()
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserAbility>()
                .HasOne(ua => ua.Ability)
                .WithMany(a => a.UserAbilities)
                .HasForeignKey(ua => ua.AbilityId)
                .OnDelete(DeleteBehavior.Cascade);

            // ---------- Employee ----------
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Manager)
                .WithMany()
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.ApplicationUser)
                .WithMany()
                .HasForeignKey(e => e.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.ApplicationUserId)
                .IsUnique();

            // ---------- Supplier ----------
            modelBuilder.Entity<SupplierPayment>()
                .HasOne(sp => sp.Supplier)
                .WithMany(s => s.Payments)
                .HasForeignKey(sp => sp.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplierPayment>()
                .HasOne(sp => sp.CreatedByEmployee)
                .WithMany()
                .HasForeignKey(sp => sp.CreatedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- Stock ----------
            modelBuilder.Entity<StockMovement>()
                .HasOne(m => m.Material)
                .WithMany(mat => mat.StockMovements)
                .HasForeignKey(m => m.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(m => m.Project)
                .WithMany(p => p.StockMovements)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(m => m.Supplier)
                .WithMany(s => s.StockMovements)
                .HasForeignKey(m => m.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(m => m.CreatedByEmployee)
                .WithMany()
                .HasForeignKey(m => m.CreatedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- Project ----------
            modelBuilder.Entity<Project>()
                .HasIndex(p => p.ProjectCode)
                .IsUnique();

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Client)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Project>()
                .HasOne(p => p.CreatedByEmployee)
                .WithMany()
                .HasForeignKey(p => p.CreatedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjectPayment>()
                .HasOne(pp => pp.Project)
                .WithMany(p => p.Payments)
                .HasForeignKey(pp => pp.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectPayment>()
                .HasOne(pp => pp.CreatedByEmployee)
                .WithMany()
                .HasForeignKey(pp => pp.CreatedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjectExpense>()
                .HasOne(pe => pe.Project)
                .WithMany(p => p.Expenses)
                .HasForeignKey(pe => pe.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectExpense>()
                .HasOne(pe => pe.CreatedByEmployee)
                .WithMany()
                .HasForeignKey(pe => pe.CreatedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjectAttachment>()
                .HasOne(pa => pa.Project)
                .WithMany(p => p.Attachments)
                .HasForeignKey(pa => pa.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectAttachment>()
                .HasOne(pa => pa.UploadedByEmployee)
                .WithMany()
                .HasForeignKey(pa => pa.UploadedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjectAudit>()
                .HasOne(pa => pa.Project)
                .WithMany(p => p.AuditLogs)
                .HasForeignKey(pa => pa.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectAudit>()
                .HasOne(pa => pa.Employee)
                .WithMany()
                .HasForeignKey(pa => pa.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- Mission ----------
            modelBuilder.Entity<Mission>()
                .HasOne(m => m.Project)
                .WithMany(p => p.Missions)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Mission>()
                .HasOne(m => m.ForemanEmployee)
                .WithMany(e => e.MissionsAsForeman)
                .HasForeignKey(m => m.ForemanEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MissionWorker>()
                .HasOne(mw => mw.Mission)
                .WithMany(m => m.MissionWorkers)
                .HasForeignKey(mw => mw.MissionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MissionWorker>()
                .HasOne(mw => mw.Employee)
                .WithMany(e => e.MissionsAsWorker)
                .HasForeignKey(mw => mw.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- Payroll ----------
            modelBuilder.Entity<Advance>()
                .HasOne(a => a.Employee)
                .WithMany(e => e.Advances)
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PayrollAdjustment>()
                .HasOne(pa => pa.Employee)
                .WithMany(e => e.PayrollAdjustments)
                .HasForeignKey(pa => pa.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PayrollPayout>()
                .HasOne(pp => pp.Employee)
                .WithMany(e => e.PayrollPayouts)
                .HasForeignKey(pp => pp.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PayrollPayout>()
                .HasOne(pp => pp.CreatedByEmployee)
                .WithMany()
                .HasForeignKey(pp => pp.CreatedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- CashBox ----------
            modelBuilder.Entity<CashBoxTransaction>()
                .HasOne(t => t.CashBox)
                .WithMany(cb => cb.Transactions)
                .HasForeignKey(t => t.CashBoxId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CashBoxTransaction>()
                .HasOne(t => t.Project)
                .WithMany(p => p.CashBoxTransactions)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashBoxTransaction>()
                .HasOne(t => t.Supplier)
                .WithMany()
                .HasForeignKey(t => t.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashBoxTransaction>()
                .HasOne(t => t.ProjectExpense)
                .WithMany(pe => pe.CashBoxTransactions)
                .HasForeignKey(t => t.ProjectExpenseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashBoxTransaction>()
                .HasOne(t => t.Mission)
                .WithMany(m => m.CashBoxTransactions)
                .HasForeignKey(t => t.MissionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashBoxTransaction>()
                .HasOne(t => t.PayrollPayout)
                .WithMany(pp => pp.CashBoxTransactions)
                .HasForeignKey(t => t.PayrollPayoutId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashBoxTransaction>()
                .HasOne(t => t.Advance)
                .WithMany(a => a.CashBoxTransactions)
                .HasForeignKey(t => t.AdvanceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashBoxTransaction>()
                .HasOne(t => t.CreatedByEmployee)
                .WithMany()
                .HasForeignKey(t => t.CreatedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
