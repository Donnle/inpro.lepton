using System;
using System.Threading.Tasks;
using Grpc.Core;
using inpro.lepton.Grpc;
using Microsoft.Extensions.Logging;
using Volo.Abp.Account;
using Volo.Abp.Identity;

namespace inpro.lepton;

// ЦЕ – обгортка над існуючими ABP сервісами. Логіка лишається та сама.
public class AccountGrpcService : AccountService.AccountServiceBase
{
    private readonly ILogger<AccountGrpcService> _logger;
    private readonly IAccountAppService _accountAppService;

    // IAccountAppService – стандартний інтерфейс з Volo.Abp.Account (його вже підключено в твоєму модулі)
    public AccountGrpcService(ILogger<AccountGrpcService> logger, IAccountAppService accountAppService)
    {
        _logger = logger;
        _accountAppService = accountAppService;
    }

    public override async Task<RegisterReply> Register(RegisterRequest request, ServerCallContext context)
    {
        // ABP DTO: RegisterDto { AppName, UserName, EmailAddress, Password }
        var dto = new RegisterDto
        {
            AppName = string.IsNullOrWhiteSpace(request.AppName) ? "Default" : request.AppName,
            UserName = request.UserName?.Trim(),
            EmailAddress = request.Email?.Trim(),
            Password = request.Password ?? string.Empty
        };

        // Викликаємо ту саму логіку, що й HTTP контролер
        IdentityUserDto user = await _accountAppService.RegisterAsync(dto);

        _logger.LogInformation("User registered via gRPC: {UserId} {UserName}", user.Id, user.UserName);

        return new RegisterReply
        {
            UserId = user.Id.ToString(),
            UserName = user.UserName,
            Email = user.Email
        };
    }

    public override async Task<Google.Protobuf.WellKnownTypes.Empty> SendPasswordResetCode(
        SendPasswordResetCodeRequest request, ServerCallContext context)
    {
        // ABP DTO: SendPasswordResetCodeDto { AppName, Email }
        await _accountAppService.SendPasswordResetCodeAsync(new SendPasswordResetCodeDto
        {
            AppName = string.IsNullOrWhiteSpace(request.AppName) ? "Default" : request.AppName,
            Email = request.Email?.Trim()
        });

        _logger.LogInformation("Sent password reset code for {Email}", request.Email);

        return new Google.Protobuf.WellKnownTypes.Empty();
    }

    public override async Task<VerifyPasswordResetTokenReply> VerifyPasswordResetToken(
        VerifyPasswordResetTokenRequest request,
        ServerCallContext context)
    {
        // ABP: повертає bool
        bool isValid = await _accountAppService.VerifyPasswordResetTokenAsync(
            new VerifyPasswordResetTokenInput
            {
                UserId = Guid.Parse(request.UserId),
                ResetToken = request.ResetToken
            });

        // gRPC-відповідь очікує поле IsValid -> підставляємо bool
        return new VerifyPasswordResetTokenReply { IsValid = isValid };
    }

    public override async Task<Google.Protobuf.WellKnownTypes.Empty> ResetPassword(
        ResetPasswordRequest request, ServerCallContext context)
    {
        // ABP DTO: ResetPasswordDto { UserId, ResetToken, Password }
        await _accountAppService.ResetPasswordAsync(new ResetPasswordDto
        {
            UserId = Guid.Parse(request.UserId),
            ResetToken = request.ResetToken,
            Password = request.Password
        });

        _logger.LogInformation("Password reset for {UserId}", request.UserId);

        return new Google.Protobuf.WellKnownTypes.Empty();
    }
}
