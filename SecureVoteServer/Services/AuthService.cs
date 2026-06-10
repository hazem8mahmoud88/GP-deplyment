using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SecureVote.Abstractions;
using SecureVote.Authentication;
using SecureVote.Contracts.Auth;
using SecureVote.Entities;
using SecureVote.Errors;
using SecureVote.Persistence;
using SecureVote.Constants;

namespace SecureVote.Services;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
    Task<Result<LoginResponse>> RegisterAdminAsync(RegisterAdminRequest request);
    Task<Result<LoginResponse>> RegisterOrganizerAsync(RegisterOrganizerRequest request);
}

public class AuthService(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    ApplicationDbContext context,
    IJwtProvider jwtProvider,
    Microsoft.Extensions.Options.IOptions<JwtOptions> jwtOptionsAccessor) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptionsAccessor.Value;
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        
        if (user == null)
            return Result.Failure<LoginResponse>(AuthErrors.InvalidCredentials);

        if (!user.IsActive)
            return Result.Failure<LoginResponse>(AuthErrors.UserDisabled);

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        
        if (!result.Succeeded)
            return Result.Failure<LoginResponse>(AuthErrors.InvalidCredentials);

        int? adminId = null;
        int? organizerId = null;

        if (user.Role == AppRoles.Admin)
        {
            var admin = await context.Admins.FirstOrDefaultAsync(a => a.UserId == user.Id);
            adminId = admin?.Id;
        }
        else if (user.Role == AppRoles.Organizer)
        {
            var organizer = await context.Organizers.FirstOrDefaultAsync(o => o.UserId == user.Id);
            organizerId = organizer?.Id;
        }

        var token = jwtProvider.GenerateToken(user, user.Role, adminId ?? organizerId);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryInMinutes);

        var response = new LoginResponse(
            Token: token,
            Email: user.Email!,
            Username: user.UserName!,
            Role: user.Role,
            AdminId: adminId,
            OrganizerId: organizerId,
            ExpiresAt: expiresAt
        );

        return Result.Success(response);
    }

    public async Task<Result<LoginResponse>> RegisterAdminAsync(RegisterAdminRequest request)
    {
        if (await userManager.FindByEmailAsync(request.Email) != null)
            return Result.Failure<LoginResponse>(AuthErrors.DuplicateEmail);

        if (await userManager.FindByNameAsync(request.Username) != null)
            return Result.Failure<LoginResponse>(AuthErrors.DuplicateUsername);

        var user = new User
        {
            Email = request.Email,
            UserName = request.Username,
            Role = AppRoles.Admin,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        
        if (!result.Succeeded)
            return Result.Failure<LoginResponse>(AuthErrors.PasswordValidationFailed(result.Errors));

        var admin = new Admin
        {
            UserId = user.Id,
            FullName = request.FullName,
            Department = request.Department,
            PhoneNumber = request.PhoneNumber
        };

        context.Admins.Add(admin);
        await context.SaveChangesAsync();

        var token = jwtProvider.GenerateToken(user, AppRoles.Admin, admin.Id);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryInMinutes);

        return Result.Success(new LoginResponse(
            Token: token,
            Email: user.Email,
            Username: user.UserName,
            Role: AppRoles.Admin,
            AdminId: admin.Id,
            OrganizerId: null,
            ExpiresAt: expiresAt
        ));
    }

    public async Task<Result<LoginResponse>> RegisterOrganizerAsync(RegisterOrganizerRequest request)
    {
        if (await userManager.FindByEmailAsync(request.Email) != null)
            return Result.Failure<LoginResponse>(AuthErrors.DuplicateEmail);

        if (await userManager.FindByNameAsync(request.Username) != null)
            return Result.Failure<LoginResponse>(AuthErrors.DuplicateUsername);

        var user = new User
        {
            Email = request.Email,
            UserName = request.Username,
            Role = AppRoles.Organizer,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        
        if (!result.Succeeded)
            return Result.Failure<LoginResponse>(AuthErrors.PasswordValidationFailed(result.Errors));

        var organizer = new Organizer
        {
            UserId = user.Id,
            FullName = request.FullName,
            Organization = request.Organization,
            PhoneNumber = request.PhoneNumber
        };

        context.Organizers.Add(organizer);
        await context.SaveChangesAsync();

        var token = jwtProvider.GenerateToken(user, AppRoles.Organizer, organizer.Id);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryInMinutes);

        return Result.Success(new LoginResponse(
            Token: token,
            Email: user.Email,
            Username: user.UserName,
            Role: AppRoles.Organizer,
            AdminId: null,
            OrganizerId: organizer.Id,
            ExpiresAt: expiresAt
        ));
    }
}
