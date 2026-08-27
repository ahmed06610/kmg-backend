using KMG.Core.DTOs.Auth;
using KMG.Core.DTOs.Client;
using KMG.Core.DTOs.Employee;
using KMG.Core.DTOs.Mission;
using KMG.Core.DTOs.Payroll;
using KMG.Core.DTOs.Project;
using KMG.Core.DTOs.Stock;
using KMG.Core.DTOs.Supplier;
using KMG.Core.Enums;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;

namespace KMG.Api.Helper
{
    /// <summary>
    /// بيانات تجريبية لتشغيل الفرونت اند عليها والاختبار. تشتغل مرة واحدة بس (لو مفيش عملاء في الداتابيز)
    /// وبتمر على نفس الـ Services الحقيقية بالظبط زي أي طلب حقيقي، عشان كل حسابات الخزنة والأرباح تطلع متسقة.
    /// شيل الاستدعاء بتاعها من Program.cs قبل أي نشر حقيقي (Production).
    /// </summary>
    public static class DummyDataSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;

            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            if ((await unitOfWork.Client.GetAllAsync()).Any())
                return; // اتزرعت قبل كده

            var authService = sp.GetRequiredService<IAuthService>();
            var employeeService = sp.GetRequiredService<IEmployeeService>();
            var clientService = sp.GetRequiredService<IClientService>();
            var supplierService = sp.GetRequiredService<ISupplierService>();
            var stockService = sp.GetRequiredService<IStockService>();
            var projectService = sp.GetRequiredService<IProjectService>();
            var missionService = sp.GetRequiredService<IMissionService>();
            var payrollService = sp.GetRequiredService<IPayrollService>();

            var ownerEmployee = (await unitOfWork.Employee.GetAllAsync()).First();
            var ownerId = ownerEmployee.Id;

            // ---------------- حساب دخول تجريبي للمحاسب ----------------
            var roles = await authService.GetAllRolesAsync();
            var accountantRoleId = roles.First(r => r.Name == DbInitializer.AccountantRole).Id;
            await authService.RegisterEmployeeAsync(new RegisterEmployeeDTO
            {
                UserName = "accountant",
                Name = "أحمد المحاسب",
                Password = "Accountant@123",
                Phone = "01055555555",
                RoleId = accountantRoleId,
                EmployeeType = EmployeeType.Admin,
                WageType = WageType.Monthly,
                WageAmount = 8000
            });

            // ---------------- عمال بدون حساب دخول ----------------
            var foremanId = await employeeService.CreateWorkerAsync(new CreateWorkerDTO
            { Name = "عم أحمد - ريس عمال", Phone = "01011111111", WageAmount = 250 });
            var worker1Id = await employeeService.CreateWorkerAsync(new CreateWorkerDTO
            { Name = "محمد علي", Phone = "01022222222", WageAmount = 200 });
            var worker2Id = await employeeService.CreateWorkerAsync(new CreateWorkerDTO
            { Name = "كريم حسن", Phone = "01033333333", WageAmount = 200 });
            var worker3Id = await employeeService.CreateWorkerAsync(new CreateWorkerDTO
            { Name = "سيد إبراهيم", Phone = "01044444444", WageAmount = 180 });

            // ---------------- العملاء ----------------
            var client1Id = await clientService.CreateAsync(new CreateClientDTO
            { Name = "شركة توشيبا مصر", Phone = "0223456789", Email = "contact@toshiba-eg.com", Address = "القاهرة الجديدة" });
            var client2Id = await clientService.CreateAsync(new CreateClientDTO
            { Name = "محطة وقود النور", Phone = "01099999999", Address = "طريق مصر إسكندرية الصحراوي" });
            var client3Id = await clientService.CreateAsync(new CreateClientDTO
            { Name = "شركة بتروجيت للتوريدات", Phone = "0233221100", Address = "مدينة نصر" });

            // ---------------- الموردين ----------------
            var supplier1Id = await supplierService.CreateAsync(new CreateSupplierDTO
            { Name = "مصنع الألوميتال الحديث", Phone = "01288888888" });
            var supplier2Id = await supplierService.CreateAsync(new CreateSupplierDTO
            { Name = "مورد الزجاج والتشطيبات", Phone = "01277777777" });

            // ---------------- الخامات ----------------
            var mAlucobond = await stockService.CreateMaterialAsync(new CreateMaterialDTO
            { Name = "لوح ألوكوبند", Unit = "متر مربع", UnitPrice = 300, MinimumThreshold = 50, InitialQuantity = 0 });
            var mProfile = await stockService.CreateMaterialAsync(new CreateMaterialDTO
            { Name = "بروفايل ألوميتال", Unit = "متر طولي", UnitPrice = 80, MinimumThreshold = 100, InitialQuantity = 0 });
            var mGlass = await stockService.CreateMaterialAsync(new CreateMaterialDTO
            { Name = "زجاج سيكوريت", Unit = "متر مربع", UnitPrice = 450, MinimumThreshold = 20, InitialQuantity = 0 });
            var mScrews = await stockService.CreateMaterialAsync(new CreateMaterialDTO
            { Name = "مسامير تثبيت خاصة", Unit = "علبة", UnitPrice = 25, MinimumThreshold = 30, InitialQuantity = 0 });

            await stockService.RecordPurchaseAsync(new CreatePurchaseDTO { MaterialId = mAlucobond, Quantity = 300, UnitPrice = 300, SupplierId = supplier1Id, Notes = "شراء أولي" }, ownerId);
            await stockService.RecordPurchaseAsync(new CreatePurchaseDTO { MaterialId = mProfile, Quantity = 500, UnitPrice = 80, SupplierId = supplier1Id, Notes = "شراء أولي" }, ownerId);
            await stockService.RecordPurchaseAsync(new CreatePurchaseDTO { MaterialId = mGlass, Quantity = 60, UnitPrice = 450, SupplierId = supplier2Id, Notes = "شراء أولي" }, ownerId);
            // كمية قليلة عمدًا عشان تظهر تنبيه نقص المخزون (المتاح أقل من الحد الأدنى 30)
            await stockService.RecordPurchaseAsync(new CreatePurchaseDTO { MaterialId = mScrews, Quantity = 20, UnitPrice = 25, SupplierId = supplier2Id, Notes = "شراء أولي" }, ownerId);

            // سداد جزئي لمورد الألوميتال
            await supplierService.RecordPaymentAsync(new CreateSupplierPaymentDTO
            { SupplierId = supplier1Id, AmountCash = 60000, AmountCredit = 0, PaymentDate = DateTime.UtcNow.AddDays(-10), Notes = "دفعة تحت الحساب" }, ownerId);

            // ==================== مشروع 1: تصنيع وتنفيذ (مناقصة) ====================
            var project1Id = await projectService.CreateAsync(new CreateProjectDTO
            {
                ProjectType = ProjectType.ManufactureExecution,
                ClientId = client1Id,
                ContractValue = 500000,
                Description = "واجهة كلادينج - فرع جديد",
                TenderInsuranceAmount = 5000,
                TenderTaxAmount = 2000
            }, ownerId);

            await stockService.IssueToProjectAsync(new CreateIssueDTO { MaterialId = mAlucobond, Quantity = 150, ProjectId = project1Id, Notes = "صرف للتصنيع" }, ownerId);
            await stockService.IssueToProjectAsync(new CreateIssueDTO { MaterialId = mProfile, Quantity = 200, ProjectId = project1Id, Notes = "صرف للتصنيع" }, ownerId);

            await projectService.RecordExpenseAsync(new CreateProjectExpenseDTO
            { ProjectId = project1Id, Amount = 5000, Category = ExpenseCategory.TenderInsurance, Description = "تأمين ابتدائي على المناقصة", ExpenseDate = DateTime.UtcNow.AddDays(-20) }, ownerId);
            await projectService.RecordExpenseAsync(new CreateProjectExpenseDTO
            { ProjectId = project1Id, Amount = 2000, Category = ExpenseCategory.TenderTax, Description = "ضريبة المناقصة", ExpenseDate = DateTime.UtcNow.AddDays(-20) }, ownerId);
            await projectService.RecordExpenseAsync(new CreateProjectExpenseDTO
            { ProjectId = project1Id, Amount = 1500, Category = ExpenseCategory.Procedural, Description = "دفعة إجرائية", ExpenseDate = DateTime.UtcNow.AddDays(-15) }, ownerId);

            await projectService.RecordPaymentAsync(new CreateProjectPaymentDTO
            { ProjectId = project1Id, AmountCash = 150000, AmountCredit = 0, PaymentDate = DateTime.UtcNow.AddDays(-18), Notes = "دفعة مقدمة" }, ownerId);

            var mission1Id = await missionService.CreateAsync(new CreateMissionDTO
            {
                ProjectId = project1Id,
                ForemanEmployeeId = foremanId,
                StartDate = DateTime.UtcNow.AddDays(-10),
                AdvanceAmount = 4000,
                Notes = "تركيب الواجهة",
                Workers = new List<MissionWorkerInputDTO>
                {
                    new() { EmployeeId = worker1Id, DaysCount = 10 },
                    new() { EmployeeId = worker2Id, DaysCount = 10 }
                }
            }, ownerId);

            await missionService.SettleAsync(new SettleMissionDTO
            { MissionId = mission1Id, EndDate = DateTime.UtcNow.AddDays(-1), ActualSpent = 3800, Notes = "تمت التسوية" }, ownerId);

            await projectService.UpdateStatusAsync(new UpdateProjectStatusDTO { ProjectId = project1Id, Status = ProjectStatus.Mission }, ownerId);

            // ==================== مشروع 2: مقاولات من الباطن (تعامل مباشر) ====================
            var project2Id = await projectService.CreateAsync(new CreateProjectDTO
            {
                ProjectType = ProjectType.Subcontracting,
                ClientId = client2Id,
                ContractValue = 120000,
                Description = "لافتة محطة وقود + كلادينج المدخل"
            }, ownerId);

            await stockService.IssueToProjectAsync(new CreateIssueDTO { MaterialId = mGlass, Quantity = 15, ProjectId = project2Id, Notes = "صرف للتركيب" }, ownerId);
            await stockService.IssueToProjectAsync(new CreateIssueDTO { MaterialId = mScrews, Quantity = 10, ProjectId = project2Id, Notes = "صرف للتركيب" }, ownerId);

            await projectService.RecordExpenseAsync(new CreateProjectExpenseDTO
            { ProjectId = project2Id, Amount = 800, Category = ExpenseCategory.Breakdown, Description = "عطل عربية النقل", ExpenseDate = DateTime.UtcNow.AddDays(-5) }, ownerId);

            await projectService.RecordPaymentAsync(new CreateProjectPaymentDTO
            { ProjectId = project2Id, AmountCash = 60000, AmountCredit = 0, PaymentDate = DateTime.UtcNow.AddDays(-12), Notes = "دفعة أولى" }, ownerId);
            await projectService.RecordPaymentAsync(new CreateProjectPaymentDTO
            { ProjectId = project2Id, AmountCash = 30000, AmountCredit = 10000, PaymentDate = DateTime.UtcNow.AddDays(-3), Notes = "دفعة ثانية" }, ownerId);

            // مأمورية لسه مفتوحة (مش متسواة) عشان نعرض الحالة دي في الفرونت
            await missionService.CreateAsync(new CreateMissionDTO
            {
                ProjectId = project2Id,
                ForemanEmployeeId = foremanId,
                StartDate = DateTime.UtcNow.AddDays(-2),
                AdvanceAmount = 1500,
                Notes = "تركيب اللافتة",
                Workers = new List<MissionWorkerInputDTO> { new() { EmployeeId = worker3Id, DaysCount = 5 } }
            }, ownerId);

            await projectService.UpdateStatusAsync(new UpdateProjectStatusDTO { ProjectId = project2Id, Status = ProjectStatus.Mission }, ownerId);

            // ==================== مشروع 3: توريد خامات ====================
            var project3Id = await projectService.CreateAsync(new CreateProjectDTO
            {
                ProjectType = ProjectType.Supply,
                ClientId = client3Id,
                ContractValue = 52000,
                Description = "توريد لوحات معدنية جاهزة",
                SupplyProfitMargin = 15
            }, ownerId);

            await projectService.RecordExpenseAsync(new CreateProjectExpenseDTO
            { ProjectId = project3Id, Amount = 45000, Category = ExpenseCategory.Other, Description = "تكلفة تصنيع الخامة لدى مصنع خارجي", ExpenseDate = DateTime.UtcNow.AddDays(-7) }, ownerId);

            await projectService.RecordPaymentAsync(new CreateProjectPaymentDTO
            { ProjectId = project3Id, AmountCash = 52000, AmountCredit = 0, PaymentDate = DateTime.UtcNow.AddDays(-1), Notes = "سداد كامل" }, ownerId);

            await projectService.UpdateStatusAsync(new UpdateProjectStatusDTO { ProjectId = project3Id, Status = ProjectStatus.Completed }, ownerId);

            // ---------------- سلف وخصومات وحوافز ----------------
            await payrollService.CreateAdvanceAsync(new CreateAdvanceDTO
            { EmployeeId = worker2Id, TotalAmount = 2000, InstallmentAmount = 200, Notes = "سلفة ظروف طارئة" });

            await payrollService.CreateAdjustmentAsync(new CreateAdjustmentDTO
            { EmployeeId = worker1Id, Type = AdjustmentType.Bonus, Amount = 100, Reason = "مجهود إضافي في التركيب", Date = DateTime.UtcNow.AddDays(-6) });
            await payrollService.CreateAdjustmentAsync(new CreateAdjustmentDTO
            { EmployeeId = worker3Id, Type = AdjustmentType.Deduction, Amount = 50, Reason = "تأخير عن موعد العمل", Date = DateTime.UtcNow.AddDays(-2) });

            // صرف مستحقات محمد علي عن آخر أسبوع (هيدخل فيها فرق مضاعفة أيام المأمورية المتسواة)
            await payrollService.RunPayrollAsync(new RunPayrollDTO
            {
                EmployeeId = worker1Id,
                PeriodStart = DateTime.UtcNow.Date.AddDays(-10),
                PeriodEnd = DateTime.UtcNow.Date.AddDays(-4)
            }, ownerId);
        }
    }
}
