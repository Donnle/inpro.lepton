using System.Threading.Tasks;
using Grpc.Core;
using inpro.lepton.Grpc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Volo.Abp.Identity;         // IdentityUserManager
using Volo.Abp.MultiTenancy;     // ICurrentTenant, ITenantStore, TenantConfiguration
using AbpIdentityUser = Volo.Abp.Identity.IdentityUser;

namespace inpro.lepton;

public class LoginGrpcService : LoginService.LoginServiceBase
{
    private readonly ILogger<LoginGrpcService> _logger;
    private readonly SignInManager<AbpIdentityUser> _signInManager;
    private readonly IdentityUserManager _userManager;
    private readonly ICurrentTenant _currentTenant;
    private readonly ITenantStore _tenantStore;   // <-- додали

    public LoginGrpcService(
        ILogger<LoginGrpcService> logger,
        SignInManager<AbpIdentityUser> signInManager,
        IdentityUserManager userManager,
        ICurrentTenant currentTenant,
        ITenantStore tenantStore)                  // <-- додали
    {
        _logger = logger;
        _signInManager = signInManager;
        _userManager = userManager;
        _currentTenant = currentTenant;
        _tenantStore = tenantStore;               // <-- додали
    }

    public override async Task<CheckPasswordReply> CheckPassword(CheckPasswordRequest request, ServerCallContext context)
    {
        using (await ChangeTenantAsync(request.TenancyName))
        {
            var user = await FindByUserNameOrEmailAsync(request.UserNameOrEmail);
            if (user == null)
            {
                return new CheckPasswordReply { IsValid = false };
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            return new CheckPasswordReply { IsValid = result.Succeeded };
        }
    }

    public override async Task<LoginReply> Login(LoginRequest request, ServerCallContext context)
    {
        using (await ChangeTenantAsync(request.TenancyName))
        {
            var user = await FindByUserNameOrEmailAsync(request.UserNameOrEmail);
            if (user == null)
            {
                return new LoginReply { Success = false, Error = "Invalid user name or password." };
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                request.Password,
                isPersistent: request.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in via gRPC: {UserName}", user.UserName);
                return new LoginReply { Success = true };
            }

            return new LoginReply
            {
                Success = false,
                RequiresTwoFactor = result.RequiresTwoFactor,
                IsLockedOut = result.IsLockedOut,
                Error = result.IsLockedOut
                    ? "User is locked out."
                    : (result.RequiresTwoFactor ? "Two-factor required." : "Invalid user name or password.")
            };
        }
    }

    public override async Task<Google.Protobuf.WellKnownTypes.Empty> Logout(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out via gRPC");
        return new Google.Protobuf.WellKnownTypes.Empty();
    }

    // ---------------- helpers ----------------

    // Знайти tenant за ІМ'ЯМ і повернути scope для _currentTenant.Change(...)
    private async Task<System.IDisposable> ChangeTenantAsync(string? tenancyName)
    {
        if (string.IsNullOrWhiteSpace(tenancyName))
        {
            // host (без тенанта)
            return _currentTenant.Change(null);
        }

        // ITenantStore реалізується модулем TenantManagement і вміє шукати за name
        var tenant = await _tenantStore.FindAsync(tenancyName);
        // Якщо не знайшли — теж працюємо як host
        return tenant == null
            ? _currentTenant.Change(null)
            // Якщо твій ICurrentTenant.Change має перегрузку (Guid?, string?),
            // краще передати обидва. Якщо нема — достатньо Id.
            : _currentTenant.Change(tenant.Id, tenant.Name);
            // Якщо твоя версія підтримує лише (Guid?), заміни на:
            // : _currentTenant.Change(tenant.Id);
    }

    private async Task<AbpIdentityUser?> FindByUserNameOrEmailAsync(string? userNameOrEmail)
    {
        if (string.IsNullOrWhiteSpace(userNameOrEmail)) return null;

        var user = await _userManager.FindByNameAsync(userNameOrEmail);
        if (user != null) return user;

        user = await _userManager.FindByEmailAsync(userNameOrEmail);
        return user;
    }
}
