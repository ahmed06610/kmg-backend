using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Models;
using KMG.EF.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KMG.Api.Helper
{
    public static class DbInitializer
    {
        public const string OwnerRole = "صاحب العمل";
        public const string AccountantRole = "المحاسب";

        private static readonly string[] AllAbilities =
        {
            "إدارة الموظفين", "عرض الموظفين",
            "إدارة العملاء", "عرض العملاء",
            "إدارة الموردين", "عرض الموردين",
            "إدارة المخزن", "عرض المخزن",
            "إدارة المشاريع", "عرض المشاريع",
            "إدارة المصاريف",
            "إدارة المأموريات",
            "إدارة الرواتب",
            "عرض الخزنة",
            "عرض لوحة التحكم",
            "إدارة تكامل AI"
        };

        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<RoleIdentity>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var unitOfWork = services.GetRequiredService<IUnitOfWork>();

            await SeedRolesAsync(roleManager);
            await SeedAbilitiesAsync(context);
            await SeedRoleAbilitiesAsync(context, roleManager);
            await SeedOwnerAsync(context, userManager, roleManager, unitOfWork);
            await SyncOwnerAbilitiesAsync(context, userManager, roleManager);
        }

        // الـ JWT بياخد صلاحيات المستخدم من UserAbilities المباشرة بس (مش من الدور)،
        // فأي صلاحية جديدة تتضاف للنظام بعد ما يكون صاحب العمل متسجل بالفعل محتاجة
        // تتزامن يدويًا كده على كل تشغيل، وإلا هتفضل شغالة في الباك اند (السماحية بتتفحص
        // من الدور برضه) بس متختفيش من الواجهة لحد ما تتزامن هنا
        private static async Task SyncOwnerAbilitiesAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<RoleIdentity> roleManager)
        {
            var ownerRole = await roleManager.FindByNameAsync(OwnerRole);
            if (ownerRole == null) return;

            var roleAbilityIds = await context.RolesAbilities
                .Where(ra => ra.RoleId == ownerRole.Id)
                .Select(ra => ra.AbilityId)
                .ToListAsync();

            var ownerUsers = await userManager.GetUsersInRoleAsync(OwnerRole);
            foreach (var user in ownerUsers)
            {
                var existingAbilityIds = await context.UserAbilities
                    .Where(ua => ua.UserId == user.Id)
                    .Select(ua => ua.AbilityId)
                    .ToListAsync();

                var missingAbilityIds = roleAbilityIds.Except(existingAbilityIds);
                foreach (var abilityId in missingAbilityIds)
                    context.UserAbilities.Add(new UserAbility { UserId = user.Id, AbilityId = abilityId });
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedRolesAsync(RoleManager<RoleIdentity> roleManager)
        {
            foreach (var roleName in new[] { OwnerRole, AccountantRole })
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new RoleIdentity(roleName));
            }
        }

        private static async Task SeedAbilitiesAsync(ApplicationDbContext context)
        {
            var existing = await context.Abilities.Select(a => a.AbilityName).ToListAsync();
            var missing = AllAbilities.Except(existing).Select(name => new Ability { AbilityName = name });

            if (missing.Any())
            {
                context.Abilities.AddRange(missing);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedRoleAbilitiesAsync(ApplicationDbContext context, RoleManager<RoleIdentity> roleManager)
        {
            var abilities = await context.Abilities.ToListAsync();

            async Task AssignAsync(string roleName, IEnumerable<string> abilityNames)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if (role == null) return;

                foreach (var abilityName in abilityNames)
                {
                    var ability = abilities.FirstOrDefault(a => a.AbilityName == abilityName);
                    if (ability == null) continue;

                    var exists = await context.RolesAbilities.AnyAsync(ra => ra.RoleId == role.Id && ra.AbilityId == ability.Id);
                    if (!exists)
                        context.RolesAbilities.Add(new RolesAbility { RoleId = role.Id, AbilityId = ability.Id });
                }

                await context.SaveChangesAsync();
            }

            // صاحب العمل: كل الصلاحيات
            await AssignAsync(OwnerRole, AllAbilities);

            // المحاسب: كل حاجة ما عدا إدارة حسابات الموظفين نفسها وتكامل الـ AI
            await AssignAsync(AccountantRole, AllAbilities.Where(a => a != "إدارة الموظفين" && a != "إدارة تكامل AI"));
        }

        private static async Task SeedOwnerAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<RoleIdentity> roleManager,
            IUnitOfWork unitOfWork)
        {
            const string ownerEmail = "owner@kmg.local";
            const string ownerPassword = "Owner@123";

            var ownerUser = await userManager.FindByEmailAsync(ownerEmail);
            if (ownerUser != null) return; // already seeded

            ownerUser = new ApplicationUser
            {
                UserName = "owner",
                Email = ownerEmail,
                EmailConfirmed = true,
                Name = "صاحب العمل",
                CreatedAt = TimeHelper.NowInEgypt
            };

            var result = await userManager.CreateAsync(ownerUser, ownerPassword);
            if (!result.Succeeded)
                throw new Exception("فشل إنشاء مستخدم صاحب العمل: " + string.Join(", ", result.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(ownerUser, OwnerRole);

            var employee = new Employee
            {
                ApplicationUserId = ownerUser.Id,
                Name = ownerUser.Name,
                EmployeeType = Core.Enums.EmployeeType.Admin,
                WageType = Core.Enums.WageType.Monthly,
                WageAmount = 0,
                CreatedAt = TimeHelper.NowInEgypt,
                Suspended = false
            };

            await unitOfWork.Employee.AddAsync(employee);

            var ownerRole = await roleManager.FindByNameAsync(OwnerRole);
            if (ownerRole != null)
            {
                var roleAbilities = await context.RolesAbilities.Where(ra => ra.RoleId == ownerRole.Id).ToListAsync();
                foreach (var ra in roleAbilities)
                {
                    context.UserAbilities.Add(new UserAbility { UserId = ownerUser.Id, AbilityId = ra.AbilityId });
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
