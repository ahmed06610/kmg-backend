using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KMG.Core.DTOs.Auth;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using KMG.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KMG.Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<RoleIdentity> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly JWT _jwtSettings;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<RoleIdentity> roleManager,
            IUnitOfWork unitOfWork,
            IOptions<JWT> jwtSettings)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<AuthResponseDTO> LoginAsync(LoginDTO model)
        {
            var user = await _userManager.FindByNameAsync(model.UsernameOrEmail)
                       ?? await _userManager.FindByEmailAsync(model.UsernameOrEmail);

            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return new AuthResponseDTO { Message = "بيانات الدخول غير صحيحة" };

            var employee = (await _unitOfWork.Employee.FindAllAsync(e => e.ApplicationUserId == user.Id)).FirstOrDefault();
            if (employee == null)
                return new AuthResponseDTO { Message = "لا يوجد موظف مرتبط بهذا الحساب" };

            if (employee.Suspended)
                return new AuthResponseDTO { Message = "الحساب موقوف عن العمل" };

            return await BuildAuthResponseAsync(user, employee.Id);
        }

        public async Task<AuthResponseDTO> RegisterEmployeeAsync(RegisterEmployeeDTO model)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var emailToCheck = model.Email ?? $"{model.UserName}@kmg.local";
                if (await _userManager.FindByEmailAsync(emailToCheck) != null)
                    return new AuthResponseDTO { Message = "البريد الإلكتروني مستخدم بالفعل" };

                if (await _userManager.FindByNameAsync(model.UserName) != null)
                    return new AuthResponseDTO { Message = "اسم المستخدم مستخدم بالفعل" };

                var role = await _roleManager.FindByIdAsync(model.RoleId);
                if (role == null || role.Name == null)
                    return new AuthResponseDTO { Message = "الدور المحدد غير موجود" };

                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = emailToCheck,
                    PhoneNumber = model.Phone,
                    EmailConfirmed = true,
                    Name = model.Name,
                    CreatedAt = TimeHelper.NowInEgypt
                };

                var createResult = await _userManager.CreateAsync(user, model.Password);
                if (!createResult.Succeeded)
                    return new AuthResponseDTO { Message = string.Join(", ", createResult.Errors.Select(e => e.Description)) };

                var addRoleResult = await _userManager.AddToRoleAsync(user, role.Name);
                if (!addRoleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    return new AuthResponseDTO { Message = string.Join(", ", addRoleResult.Errors.Select(e => e.Description)) };
                }

                var employee = new Employee
                {
                    ApplicationUserId = user.Id,
                    Name = model.Name,
                    Phone = model.Phone,
                    ManagerId = model.ManagerId,
                    EmployeeType = model.EmployeeType,
                    WageType = model.WageType,
                    WageAmount = model.WageAmount,
                    CreatedAt = TimeHelper.NowInEgypt,
                    Suspended = false
                };

                await _unitOfWork.Employee.AddAsync(employee);

                await AssignAbilitiesAsync(user.Id, model.RoleId, model.AbilityIds);

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return await BuildAuthResponseAsync(user, employee.Id);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<AuthResponseDTO> EditEmployeeAsync(EditEmployeeDTO model)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var employee = await _unitOfWork.Employee.GetByIdAsync(model.Id);
                if (employee == null)
                    return new AuthResponseDTO { Message = "الموظف غير موجود" };

                if (employee.ApplicationUserId == null)
                    return new AuthResponseDTO { Message = "هذا الموظف ليس له حساب دخول للنظام (عامل بدون تسجيل دخول)" };

                var user = await _userManager.FindByIdAsync(employee.ApplicationUserId);
                if (user == null)
                    return new AuthResponseDTO { Message = "المستخدم المرتبط غير موجود" };

                employee.Name = model.Name;
                employee.Phone = model.Phone;
                employee.ManagerId = model.ManagerId;
                employee.Suspended = model.Suspended;
                employee.EmployeeType = model.EmployeeType;
                employee.WageType = model.WageType;
                employee.WageAmount = model.WageAmount;
                _unitOfWork.Employee.Update(employee);

                user.Name = model.Name;
                user.UserName = model.UserName;
                user.Email = model.Email ?? user.Email;
                user.PhoneNumber = model.Phone ?? user.PhoneNumber;
                var updateUserResult = await _userManager.UpdateAsync(user);
                if (!updateUserResult.Succeeded)
                    return new AuthResponseDTO { Message = string.Join(", ", updateUserResult.Errors.Select(e => e.Description)) };

                if (!string.IsNullOrEmpty(model.Password))
                {
                    await _userManager.RemovePasswordAsync(user);
                    var addPasswordResult = await _userManager.AddPasswordAsync(user, model.Password);
                    if (!addPasswordResult.Succeeded)
                        return new AuthResponseDTO { Message = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description)) };
                }

                var role = await _roleManager.FindByIdAsync(model.RoleId);
                if (role?.Name != null)
                {
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    if (!currentRoles.Contains(role.Name))
                    {
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        await _userManager.AddToRoleAsync(user, role.Name);
                    }
                }

                if (model.AbilityIds != null)
                {
                    var existingUserAbilities = (await _unitOfWork.UserAbility.FindAllAsync(ua => ua.UserId == user.Id)).ToList();
                    foreach (var ua in existingUserAbilities)
                        _unitOfWork.UserAbility.Delete(ua);

                    var validAbilityIds = (await _unitOfWork.Ability.FindAllAsync(a => model.AbilityIds.Contains(a.Id))).Select(a => a.Id);
                    foreach (var abilityId in validAbilityIds)
                        await _unitOfWork.UserAbility.AddAsync(new UserAbility { UserId = user.Id, AbilityId = abilityId });
                }

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return new AuthResponseDTO { IsAuthenticated = true, Message = "تم تحديث بيانات الموظف بنجاح" };
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<List<RoleDto>> GetAllRolesAsync()
        {
            return await _roleManager.Roles.Select(r => new RoleDto { Id = r.Id, Name = r.Name! }).ToListAsync();
        }

        public async Task<List<AbilitySelectionDTO>> GetAbilitiesForRoleAsync(string roleId)
        {
            var allAbilities = await _unitOfWork.Ability.GetAllAsync();
            var roleAbilityIds = (await _unitOfWork.RolesAbility.FindAllAsync(ra => ra.RoleId == roleId))
                .Select(ra => ra.AbilityId).ToList();

            return allAbilities.Select(a => new AbilitySelectionDTO
            {
                Id = a.Id,
                AbilityName = a.AbilityName,
                IsRelated = roleAbilityIds.Contains(a.Id)
            }).ToList();
        }

        private async Task AssignAbilitiesAsync(string userId, string roleId, List<int>? explicitAbilityIds)
        {
            List<int> effectiveAbilityIds;

            if (explicitAbilityIds != null && explicitAbilityIds.Any())
            {
                effectiveAbilityIds = (await _unitOfWork.Ability.FindAllAsync(a => explicitAbilityIds.Contains(a.Id)))
                    .Select(a => a.Id).ToList();
            }
            else
            {
                effectiveAbilityIds = (await _unitOfWork.RolesAbility.FindAllAsync(ra => ra.RoleId == roleId))
                    .Select(ra => ra.AbilityId).ToList();
            }

            foreach (var abilityId in effectiveAbilityIds.Distinct())
            {
                await _unitOfWork.UserAbility.AddAsync(new UserAbility { UserId = userId, AbilityId = abilityId });
            }
        }

        private async Task<AuthResponseDTO> BuildAuthResponseAsync(ApplicationUser user, int employeeId)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault() ?? string.Empty;

            var userAbilities = (await _unitOfWork.UserAbility.FindAllWithIncludes(ua => ua.UserId == user.Id, ua => ua.Ability))
                .Select(ua => new AbilityDTO { Id = ua.Ability.Id, Label = ua.Ability.AbilityName, Href = ua.Ability.Url })
                .ToList();

            var jwtToken = CreateJwtToken(user, employeeId, roleName, userAbilities);

            return new AuthResponseDTO
            {
                IsAuthenticated = true,
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                ExpiresOn = jwtToken.ValidTo,
                UserId = user.Id,
                Email = user.Email,
                Name = user.Name,
                EmployeeId = employeeId,
                RoleName = roleName,
                Abilities = userAbilities
            };
        }

        private JwtSecurityToken CreateJwtToken(ApplicationUser user, int employeeId, string roleName, List<AbilityDTO> abilities)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(JwtRegisteredClaimNames.Sub, user.UserName ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Role, roleName),
                new("EmployeeId", employeeId.ToString()),
                new("abilities", string.Join(",", abilities.Select(a => a.Label)))
            };

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            return new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: TimeHelper.NowInEgypt.AddDays(_jwtSettings.DurationInDays),
                signingCredentials: signingCredentials);
        }
    }
}
