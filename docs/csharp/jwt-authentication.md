# JWT Authentication Implementation

**Description**: Complete JWT (JSON Web Token) authentication implementation for ASP.NET Core applications with secure token generation, validation, and refresh token support.

**Language/Technology**: C#, ASP.NET Core 8+

**Code**:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

// JWT Service for token operations
public interface IJwtService
{
    Task<TokenResponse> GenerateTokenAsync(string userId, string email, IEnumerable<string> roles);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken);
    Task RevokeTokenAsync(string refreshToken);
    ClaimsPrincipal? ValidateToken(string token);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration configuration;
    private readonly IUserRepository userRepository;
    private readonly IRefreshTokenRepository refreshTokenRepository;
    private readonly ILogger<JwtService> logger;

    public JwtService(
        IConfiguration configuration,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<JwtService> logger)
    {
        this.configuration = configuration;
        this.userRepository = userRepository;
        this.refreshTokenRepository = refreshTokenRepository;
        this.logger = logger;
    }

    public async Task<TokenResponse> GenerateTokenAsync(string userId, string email, IEnumerable<string> roles)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);
        
        // Create claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add role claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["AccessTokenExpirationMinutes"]!)),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(token);

        // Generate refresh token
        var refreshToken = GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = userId,
            ExpiryDate = DateTime.UtcNow.AddDays(int.Parse(jwtSettings["RefreshTokenExpirationDays"]!)),
            Created = DateTime.UtcNow
        };

        await refreshTokenRepository.AddAsync(refreshTokenEntity);
        await refreshTokenRepository.SaveChangesAsync();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = tokenDescriptor.Expires.Value,
            TokenType = "Bearer"
        };
    }

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await refreshTokenRepository.GetByTokenAsync(refreshToken);
        
        if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiryDate < DateTime.UtcNow)
        {
            throw new SecurityTokenException("Invalid or expired refresh token");
        }

        var user = await userRepository.GetByIdAsync(storedToken.UserId);
        if (user == null || !user.IsActive)
        {
            throw new SecurityTokenException("User not found or inactive");
        }

        // Revoke old refresh token
        storedToken.IsRevoked = true;
        storedToken.RevokedDate = DateTime.UtcNow;
        
        // Generate new tokens
        var roles = await userRepository.GetUserRolesAsync(user.Id);
        var tokenResponse = await GenerateTokenAsync(user.Id, user.Email, roles);

        await refreshTokenRepository.SaveChangesAsync();
        
        logger.LogInformation("Token refreshed for user {UserId}", user.Id);
        
        return tokenResponse;
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        var storedToken = await refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (storedToken != null)
        {
            storedToken.IsRevoked = true;
            storedToken.RevokedDate = DateTime.UtcNow;
            await refreshTokenRepository.SaveChangesAsync();
        }
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return principal;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Token validation failed: {Error}", ex.Message);
            return null;
        }
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}

// Authentication controller
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtService jwtService;
    private readonly IUserService userService;
    private readonly ILogger<AuthController> logger;

    public AuthController(IJwtService jwtService, IUserService userService, ILogger<AuthController> logger)
    {
        this.jwtService = jwtService;
        this.userService = userService;
        this.logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { Message = "Email and password are required" });
            }

            // Authenticate user
            var user = await userService.AuthenticateAsync(request.Email, request.Password);
            if (user == null)
            {
                logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
                return Unauthorized(new { Message = "Invalid credentials" });
            }

            // Check if account is locked
            if (user.IsLocked)
            {
                return Unauthorized(new { Message = "Account is locked. Please contact support." });
            }

            // Generate tokens
            var roles = await userService.GetUserRolesAsync(user.Id);
            var tokenResponse = await jwtService.GenerateTokenAsync(user.Id, user.Email, roles);

            // Reset failed login attempts on successful login
            await userService.ResetFailedLoginAttemptsAsync(user.Id);

            logger.LogInformation("Successful login for user: {UserId}", user.Id);

            return Ok(tokenResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            return StatusCode(500, new { Message = "An error occurred during login" });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new { Message = "Refresh token is required" });
            }

            var tokenResponse = await jwtService.RefreshTokenAsync(request.RefreshToken);
            return Ok(tokenResponse);
        }
        catch (SecurityTokenException ex)
        {
            logger.LogWarning("Invalid refresh token attempt: {Error}", ex.Message);
            return Unauthorized(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { Message = "An error occurred during token refresh" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        try
        {
            if (!string.IsNullOrEmpty(request.RefreshToken))
            {
                await jwtService.RevokeTokenAsync(request.RefreshToken);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            logger.LogInformation("User logged out: {UserId}", userId);

            return Ok(new { Message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { Message = "An error occurred during logout" });
        }
    }
}

// DTOs
public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);
public record LogoutRequest(string? RefreshToken);

public record TokenResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required string TokenType { get; init; }
}

// Models
public class RefreshToken
{
    public int Id { get; set; }
    public required string Token { get; set; }
    public required string UserId { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime Created { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedDate { get; set; }
}
```

**Usage**:

```csharp
// Program.cs - JWT Configuration
var builder = WebApplication.CreateBuilder(args);

// Add JWT authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Add("Token-Expired", "true");
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<IJwtService, JwtService>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// appsettings.json JWT configuration
{
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-that-is-at-least-256-bits-long",
    "Issuer": "your-app-name",
    "Audience": "your-app-users",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}

// Using in controllers
[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all actions
public class UsersController : ControllerBase
{
    [HttpGet("profile")]
    public IActionResult GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
        
        return Ok(new { UserId = userId, Email = email, Roles = roles });
    }

    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin")] // Role-based authorization
    public IActionResult AdminOnly()
    {
        return Ok(new { Message = "Admin access granted" });
    }
}

// Client-side usage example
public class AuthService
{
    private readonly HttpClient httpClient;
    private readonly ILocalStorageService localStorage;

    public async Task<bool> LoginAsync(string email, string password)
    {
        var request = new { Email = email, Password = password };
        var response = await httpClient.PostAsJsonAsync("api/auth/login", request);
        
        if (response.IsSuccessStatusCode)
        {
            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            await localStorage.SetItemAsync("accessToken", tokenResponse!.AccessToken);
            await localStorage.SetItemAsync("refreshToken", tokenResponse.RefreshToken);
            return true;
        }
        
        return false;
    }

    public async Task<bool> RefreshTokenAsync()
    {
        var refreshToken = await localStorage.GetItemAsync<string>("refreshToken");
        if (string.IsNullOrEmpty(refreshToken)) return false;

        var request = new { RefreshToken = refreshToken };
        var response = await httpClient.PostAsJsonAsync("api/auth/refresh", request);
        
        if (response.IsSuccessStatusCode)
        {
            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            await localStorage.SetItemAsync("accessToken", tokenResponse!.AccessToken);
            await localStorage.SetItemAsync("refreshToken", tokenResponse.RefreshToken);
            return true;
        }
        
        return false;
    }
}
```

**Prerequisites**:

- .NET 8 or later
- Microsoft.AspNetCore.Authentication.JwtBearer package
- Entity Framework Core (for refresh token storage)
- A secure secret key (at least 256 bits)

**Notes**:

- **Security**: Use a strong, randomly generated secret key and store it securely (Azure Key Vault, etc.)
- **Token Expiration**: Keep access tokens short-lived (15 minutes) and use refresh tokens for longer sessions
- **HTTPS Only**: Always use HTTPS in production to protect tokens in transit
- **Token Storage**: Store refresh tokens securely in database with expiration and revocation support
- **Rate Limiting**: Implement rate limiting on authentication endpoints to prevent brute force attacks
- **Logging**: Log authentication events for security monitoring
- **Refresh Token Rotation**: Consider implementing refresh token rotation for enhanced security

**Related Snippets**:

- [Role-Based Authorization](role-based-authorization.md)
- [Password Security](password-security.md)
- [OAuth Integration](oauth-integration.md)

**References**:

- [JWT.io](https://jwt.io/)
- [ASP.NET Core Authentication](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/)
- [RFC 7519 - JSON Web Token](https://tools.ietf.org/html/rfc7519)

---

*Created: 2025-11-01*  
*Last Updated: 2025-11-01*  
*Tags: #jwt #authentication #security #aspnetcore #tokens*
