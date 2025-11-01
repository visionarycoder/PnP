# OAuth 2.0 and OpenID Connect Integration

**Description**: Complete OAuth 2.0 and OpenID Connect implementation for ASP.NET Core with support for multiple providers (Google, Microsoft, GitHub), PKCE, state validation, and secure token handling.

**Language/Technology**: C#, ASP.NET Core 8+

**Code**:

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;
using System.Text.Json;

// OAuth configuration options
public class OAuthOptions
{
    public Dictionary<string, OAuthProviderConfig> Providers { get; set; } = new();
    public string DefaultCallbackPath { get; set; } = "/signin-oauth";
    public string DefaultSignOutPath { get; set; } = "/signout";
    public bool RequireHttps { get; set; } = true;
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromHours(1);
    public bool SaveTokens { get; set; } = true;
}

public class OAuthProviderConfig
{
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public string? Authority { get; set; }
    public string[]? Scopes { get; set; }
    public string? CallbackPath { get; set; }
    public bool UsePkce { get; set; } = true;
    public Dictionary<string, string>? AdditionalParameters { get; set; }
}

// OAuth service interface
public interface IOAuthService
{
    Task<AuthenticationResult> AuthenticateAsync(string provider, string? returnUrl = null);
    Task<AuthenticationResult> HandleCallbackAsync(string provider, string code, string state);
    Task<TokenResponse?> RefreshTokenAsync(string provider, string refreshToken);
    Task<UserProfile?> GetUserProfileAsync(string provider, string accessToken);
    Task SignOutAsync(string provider);
    string GenerateAuthorizationUrl(string provider, string? returnUrl = null);
}

// Authentication result
public class AuthenticationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
    public TokenResponse? Tokens { get; set; }
    public UserProfile? UserProfile { get; set; }
    public string? RedirectUrl { get; set; }
}

public class TokenResponse
{
    public required string AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? IdToken { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public string[]? Scopes { get; set; }
}

public class UserProfile
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public string? Name { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Picture { get; set; }
    public string Provider { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalClaims { get; set; } = new();
}

// OAuth service implementation
public class OAuthService : IOAuthService
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly OAuthOptions options;
    private readonly ILogger<OAuthService> logger;
    private readonly IOAuthStateService stateService;

    public OAuthService(
        IHttpClientFactory httpClientFactory,
        IOptions<OAuthOptions> options,
        ILogger<OAuthService> logger,
        IOAuthStateService stateService)
    {
        httpClientFactory = httpClientFactory;
        options = options.Value;
        this.logger = logger;
        this.stateService = stateService;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string provider, string? returnUrl = null)
    {
        try
        {
            var config = GetProviderConfig(provider);
            var authUrl = GenerateAuthorizationUrl(provider, returnUrl);

            return new AuthenticationResult
            {
                Success = true,
                RedirectUrl = authUrl
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initiating OAuth authentication for provider {Provider}", provider);
            return new AuthenticationResult
            {
                Success = false,
                Error = "authentication_error",
                ErrorDescription = "Failed to initiate authentication"
            };
        }
    }

    public async Task<AuthenticationResult> HandleCallbackAsync(string provider, string code, string state)
    {
        try
        {
            // Validate state parameter
            if (!await stateService.ValidateStateAsync(state))
            {
                return new AuthenticationResult
                {
                    Success = false,
                    Error = "invalid_state",
                    ErrorDescription = "Invalid or expired state parameter"
                };
            }

            var config = GetProviderConfig(provider);
            var tokens = await ExchangeCodeForTokensAsync(provider, code, state);
            
            if (tokens == null)
            {
                return new AuthenticationResult
                {
                    Success = false,
                    Error = "token_exchange_failed",
                    ErrorDescription = "Failed to exchange authorization code for tokens"
                };
            }

            var userProfile = await GetUserProfileAsync(provider, tokens.AccessToken);

            return new AuthenticationResult
            {
                Success = true,
                Tokens = tokens,
                UserProfile = userProfile
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling OAuth callback for provider {Provider}", provider);
            return new AuthenticationResult
            {
                Success = false,
                Error = "callback_error",
                ErrorDescription = "Failed to process OAuth callback"
            };
        }
    }

    public string GenerateAuthorizationUrl(string provider, string? returnUrl = null)
    {
        var config = GetProviderConfig(provider);
        var state = stateService.GenerateState(provider, returnUrl);
        
        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = config.ClientId,
            ["response_type"] = "code",
            ["scope"] = string.Join(" ", config.Scopes ?? GetDefaultScopes(provider)),
            ["state"] = state,
            ["redirect_uri"] = GetRedirectUri(provider)
        };

        // Add PKCE parameters if enabled
        if (config.UsePkce)
        {
            var (codeVerifier, codeChallenge) = GeneratePkceParameters();
            stateService.StorePkceVerifier(state, codeVerifier);
            
            parameters["code_challenge"] = codeChallenge;
            parameters["code_challenge_method"] = "S256";
        }

        // Add provider-specific parameters
        if (config.AdditionalParameters != null)
        {
            foreach (var kvp in config.AdditionalParameters)
            {
                parameters[kvp.Key] = kvp.Value;
            }
        }

        var authUrl = GetAuthorizationEndpoint(provider);
        var queryString = string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        
        return $"{authUrl}?{queryString}";
    }

    public async Task<TokenResponse?> RefreshTokenAsync(string provider, string refreshToken)
    {
        try
        {
            var config = GetProviderConfig(provider);
            using var httpClient = httpClientFactory.CreateClient();

            var parameters = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = config.ClientId,
                ["client_secret"] = config.ClientSecret
            };

            var content = new FormUrlEncodedContent(parameters);
            var tokenEndpoint = GetTokenEndpoint(provider);
            
            var response = await httpClient.PostAsync(tokenEndpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Token refresh failed for provider {Provider}: {Error}", provider, responseContent);
                return null;
            }

            var tokenData = JsonSerializer.Deserialize<JsonElement>(responseContent);
            return ParseTokenResponse(tokenData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing token for provider {Provider}", provider);
            return null;
        }
    }

    public async Task<UserProfile?> GetUserProfileAsync(string provider, string accessToken)
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);

            var userInfoEndpoint = GetUserInfoEndpoint(provider);
            var response = await httpClient.GetAsync(userInfoEndpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to get user profile for provider {Provider}", provider);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var userData = JsonSerializer.Deserialize<JsonElement>(content);

            return MapUserProfile(provider, userData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user profile for provider {Provider}", provider);
            return null;
        }
    }

    public async Task SignOutAsync(string provider)
    {
        // Provider-specific sign-out logic
        await Task.CompletedTask;
    }

    private async Task<TokenResponse?> ExchangeCodeForTokensAsync(string provider, string code, string state)
    {
        var config = GetProviderConfig(provider);
        using var httpClient = httpClientFactory.CreateClient();

        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = GetRedirectUri(provider),
            ["client_id"] = config.ClientId,
            ["client_secret"] = config.ClientSecret
        };

        // Add PKCE verifier if used
        if (config.UsePkce)
        {
            var codeVerifier = await stateService.GetPkceVerifierAsync(state);
            if (!string.IsNullOrEmpty(codeVerifier))
            {
                parameters["code_verifier"] = codeVerifier;
            }
        }

        var content = new FormUrlEncodedContent(parameters);
        var tokenEndpoint = GetTokenEndpoint(provider);
        
        var response = await httpClient.PostAsync(tokenEndpoint, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Token exchange failed for provider {Provider}: {Error}", provider, responseContent);
            return null;
        }

        var tokenData = JsonSerializer.Deserialize<JsonElement>(responseContent);
        return ParseTokenResponse(tokenData);
    }

    private OAuthProviderConfig GetProviderConfig(string provider)
    {
        if (!options.Providers.TryGetValue(provider, out var config))
        {
            throw new InvalidOperationException($"OAuth provider '{provider}' is not configured");
        }
        return config;
    }

    private string GetAuthorizationEndpoint(string provider) => provider.ToLowerInvariant() switch
    {
        "google" => "https://accounts.google.com/o/oauth2/v2/auth",
        "microsoft" => "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
        "github" => "https://github.com/login/oauth/authorize",
        "azure" => "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
        _ => options.Providers[provider].Authority + "/oauth2/authorize"
    };

    private string GetTokenEndpoint(string provider) => provider.ToLowerInvariant() switch
    {
        "google" => "https://oauth2.googleapis.com/token",
        "microsoft" => "https://login.microsoftonline.com/common/oauth2/v2.0/token",
        "github" => "https://github.com/login/oauth/access_token",
        "azure" => "https://login.microsoftonline.com/common/oauth2/v2.0/token",
        _ => options.Providers[provider].Authority + "/oauth2/token"
    };

    private string GetUserInfoEndpoint(string provider) => provider.ToLowerInvariant() switch
    {
        "google" => "https://www.googleapis.com/oauth2/v2/userinfo",
        "microsoft" => "https://graph.microsoft.com/v1.0/me",
        "github" => "https://api.github.com/user",
        "azure" => "https://graph.microsoft.com/v1.0/me",
        _ => options.Providers[provider].Authority + "/userinfo"
    };

    private string[] GetDefaultScopes(string provider) => provider.ToLowerInvariant() switch
    {
        "google" => ["openid", "email", "profile"],
        "microsoft" => ["openid", "email", "profile"],
        "github" => ["user:email"],
        "azure" => ["openid", "email", "profile"],
        _ => ["openid", "email", "profile"]
    };

    private string GetRedirectUri(string provider)
    {
        var config = GetProviderConfig(provider);
        return config.CallbackPath ?? $"{options.DefaultCallbackPath}/{provider}";
    }

    private (string codeVerifier, string codeChallenge) GeneratePkceParameters()
    {
        var codeVerifier = Convert.ToBase64String(RandomNumberGenerator.GetBytes(96))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        using var sha256 = SHA256.Create();
        var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        var codeChallenge = Convert.ToBase64String(challengeBytes)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        return (codeVerifier, codeChallenge);
    }

    private TokenResponse ParseTokenResponse(JsonElement tokenData)
    {
        return new TokenResponse
        {
            AccessToken = tokenData.GetProperty("access_token").GetString()!,
            RefreshToken = tokenData.TryGetProperty("refresh_token", out var refresh) ? refresh.GetString() : null,
            IdToken = tokenData.TryGetProperty("id_token", out var id) ? id.GetString() : null,
            ExpiresIn = tokenData.TryGetProperty("expires_in", out var expires) ? expires.GetInt32() : 3600,
            TokenType = tokenData.TryGetProperty("token_type", out var type) ? type.GetString()! : "Bearer",
            Scopes = tokenData.TryGetProperty("scope", out var scope) ? scope.GetString()?.Split(' ') : null
        };
    }

    private UserProfile MapUserProfile(string provider, JsonElement userData)
    {
        return provider.ToLowerInvariant() switch
        {
            "google" => MapGoogleProfile(userData),
            "microsoft" => MapMicrosoftProfile(userData),
            "github" => MapGitHubProfile(userData),
            "azure" => MapMicrosoftProfile(userData),
            _ => MapGenericProfile(provider, userData)
        };
    }

    private UserProfile MapGoogleProfile(JsonElement userData)
    {
        return new UserProfile
        {
            Id = userData.GetProperty("id").GetString()!,
            Email = userData.GetProperty("email").GetString()!,
            Name = userData.TryGetProperty("name", out var name) ? name.GetString() : null,
            FirstName = userData.TryGetProperty("given_name", out var firstName) ? firstName.GetString() : null,
            LastName = userData.TryGetProperty("family_name", out var lastName) ? lastName.GetString() : null,
            Picture = userData.TryGetProperty("picture", out var picture) ? picture.GetString() : null,
            Provider = "google"
        };
    }

    private UserProfile MapMicrosoftProfile(JsonElement userData)
    {
        return new UserProfile
        {
            Id = userData.GetProperty("id").GetString()!,
            Email = userData.TryGetProperty("mail", out var mail) ? mail.GetString() : 
                   userData.TryGetProperty("userPrincipalName", out var upn) ? upn.GetString() : null!,
            Name = userData.TryGetProperty("displayName", out var name) ? name.GetString() : null,
            FirstName = userData.TryGetProperty("givenName", out var firstName) ? firstName.GetString() : null,
            LastName = userData.TryGetProperty("surname", out var lastName) ? lastName.GetString() : null,
            Provider = "microsoft"
        };
    }

    private UserProfile MapGitHubProfile(JsonElement userData)
    {
        return new UserProfile
        {
            Id = userData.GetProperty("id").GetInt32().ToString(),
            Email = userData.TryGetProperty("email", out var email) ? email.GetString()! : string.Empty,
            Name = userData.TryGetProperty("name", out var name) ? name.GetString() : null,
            Picture = userData.TryGetProperty("avatar_url", out var avatar) ? avatar.GetString() : null,
            Provider = "github"
        };
    }

    private UserProfile MapGenericProfile(string provider, JsonElement userData)
    {
        return new UserProfile
        {
            Id = userData.TryGetProperty("sub", out var sub) ? sub.GetString()! : 
                 userData.TryGetProperty("id", out var id) ? id.GetString()! : Guid.NewGuid().ToString(),
            Email = userData.TryGetProperty("email", out var email) ? email.GetString()! : string.Empty,
            Name = userData.TryGetProperty("name", out var name) ? name.GetString() : null,
            Provider = provider
        };
    }
}

// OAuth state management service
public interface IOAuthStateService
{
    string GenerateState(string provider, string? returnUrl = null);
    Task<bool> ValidateStateAsync(string state);
    Task StorePkceVerifierAsync(string state, string codeVerifier);
    Task<string?> GetPkceVerifierAsync(string state);
    void StorePkceVerifier(string state, string codeVerifier);
}

public class OAuthStateService : IOAuthStateService
{
    private readonly IMemoryCache cache;
    private readonly ILogger<OAuthStateService> logger;
    
    public OAuthStateService(IMemoryCache cache, ILogger<OAuthStateService> logger)
    {
        cache = cache;
        this.logger = logger;
    }

    public string GenerateState(string provider, string? returnUrl = null)
    {
        var stateData = new OAuthState
        {
            Provider = provider,
            ReturnUrl = returnUrl,
            CreatedAt = DateTime.UtcNow,
            Nonce = Guid.NewGuid().ToString("N")
        };

        var state = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(stateData));
        
        // Store in cache with expiration
        cache.Set($"oauth_state_{state}", stateData, TimeSpan.FromMinutes(10));
        
        return state;
    }

    public async Task<bool> ValidateStateAsync(string state)
    {
        try
        {
            var cacheKey = $"oauth_state_{state}";
            if (cache.TryGetValue(cacheKey, out OAuthState? stateData))
            {
                // Remove from cache to prevent replay
                cache.Remove(cacheKey);
                
                // Check expiration (additional safety)
                return stateData.CreatedAt.AddMinutes(10) > DateTime.UtcNow;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating OAuth state");
            return false;
        }
    }

    public async Task StorePkceVerifierAsync(string state, string codeVerifier)
    {
        cache.Set($"pkce_{state}", codeVerifier, TimeSpan.FromMinutes(10));
        await Task.CompletedTask;
    }

    public async Task<string?> GetPkceVerifierAsync(string state)
    {
        var cacheKey = $"pkce_{state}";
        if (cache.TryGetValue(cacheKey, out string? verifier))
        {
            cache.Remove(cacheKey);
            return verifier;
        }
        return await Task.FromResult<string?>(null);
    }

    public void StorePkceVerifier(string state, string codeVerifier)
    {
        cache.Set($"pkce_{state}", codeVerifier, TimeSpan.FromMinutes(10));
    }
}

// OAuth state model
public class OAuthState
{
    public required string Provider { get; set; }
    public string? ReturnUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string Nonce { get; set; }
}

// OAuth controller
[ApiController]
[Route("api/[controller]")]
public class OAuthController : ControllerBase
{
    private readonly IOAuthService oauthService;
    private readonly IUserService userService;
    private readonly IJwtService jwtService;
    private readonly ILogger<OAuthController> logger;

    public OAuthController(
        IOAuthService oauthService,
        IUserService userService,
        IJwtService jwtService,
        ILogger<OAuthController> logger)
    {
        oauthService = oauthService;
        this.userService = userService;
        this.jwtService = jwtService;
        this.logger = logger;
    }

    [HttpGet("login/{provider}")]
    public IActionResult Login(string provider, [FromQuery] string? returnUrl = null)
    {
        var authUrl = oauthService.GenerateAuthorizationUrl(provider, returnUrl);
        return Redirect(authUrl);
    }

    [HttpGet("callback/{provider}")]
    public async Task<IActionResult> Callback(string provider, [FromQuery] string code, [FromQuery] string state)
    {
        var result = await oauthService.HandleCallbackAsync(provider, code, state);
        
        if (!result.Success)
        {
            logger.LogWarning("OAuth callback failed for provider {Provider}: {Error}", provider, result.Error);
            return BadRequest(new { Error = result.Error, Description = result.ErrorDescription });
        }

        // Find or create user
        var user = await userService.FindByEmailAsync(result.UserProfile!.Email);
        if (user == null)
        {
            user = await userService.CreateFromOAuthAsync(result.UserProfile);
        }
        else
        {
            // Update user profile with latest OAuth data
            await userService.UpdateFromOAuthAsync(user.Id, result.UserProfile);
        }

        // Generate JWT tokens for the user
        var roles = await userService.GetUserRolesAsync(user.Id);
        var tokenResponse = await jwtService.GenerateTokenAsync(user.Id, user.Email, roles);

        return Ok(tokenResponse);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshOAuthTokenRequest request)
    {
        var tokenResponse = await oauthService.RefreshTokenAsync(request.Provider, request.RefreshToken);
        
        if (tokenResponse == null)
        {
            return Unauthorized(new { Message = "Failed to refresh token" });
        }

        return Ok(tokenResponse);
    }

    [HttpGet("profile/{provider}")]
    [Authorize]
    public async Task<IActionResult> GetProfile(string provider, [FromQuery] string accessToken)
    {
        var profile = await oauthService.GetUserProfileAsync(provider, accessToken);
        
        if (profile == null)
        {
            return NotFound(new { Message = "Profile not found" });
        }

        return Ok(profile);
    }
}

// DTOs
public record RefreshOAuthTokenRequest(string Provider, string RefreshToken);
```

**Usage**:

```csharp
// Program.cs - OAuth Configuration
var builder = WebApplication.CreateBuilder(args);

// Configure OAuth options
builder.Services.Configure<OAuthOptions>(options =>
{
    options.Providers["google"] = new OAuthProviderConfig
    {
        ClientId = builder.Configuration["OAuth:Google:ClientId"]!,
        ClientSecret = builder.Configuration["OAuth:Google:ClientSecret"]!,
        Scopes = ["openid", "email", "profile"],
        UsePkce = true
    };

    options.Providers["microsoft"] = new OAuthProviderConfig
    {
        ClientId = builder.Configuration["OAuth:Microsoft:ClientId"]!,
        ClientSecret = builder.Configuration["OAuth:Microsoft:ClientSecret"]!,
        Scopes = ["openid", "email", "profile"],
        UsePkce = true
    };

    options.Providers["github"] = new OAuthProviderConfig
    {
        ClientId = builder.Configuration["OAuth:GitHub:ClientId"]!,
        ClientSecret = builder.Configuration["OAuth:GitHub:ClientSecret"]!,
        Scopes = ["user:email"],
        UsePkce = false // GitHub doesn't support PKCE for web apps
    };
});

// Register services
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IOAuthService, OAuthService>();
builder.Services.AddScoped<IOAuthStateService, OAuthStateService>();

// Configure ASP.NET Core Authentication (alternative approach)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
    })
    .AddGoogle("Google", options =>
    {
        options.ClientId = builder.Configuration["OAuth:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["OAuth:Google:ClientSecret"]!;
        options.SaveTokens = true;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
    })
    .AddMicrosoftAccount("Microsoft", options =>
    {
        options.ClientId = builder.Configuration["OAuth:Microsoft:ClientId"]!;
        options.ClientSecret = builder.Configuration["OAuth:Microsoft:ClientSecret"]!;
        options.SaveTokens = true;
    })
    .AddOAuth("GitHub", options =>
    {
        options.ClientId = builder.Configuration["OAuth:GitHub:ClientId"]!;
        options.ClientSecret = builder.Configuration["OAuth:GitHub:ClientSecret"]!;
        options.CallbackPath = "/signin-github";
        options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
        options.TokenEndpoint = "https://github.com/login/oauth/access_token";
        options.UserInformationEndpoint = "https://api.github.com/user";
        options.SaveTokens = true;
        options.Scope.Add("user:email");
        
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
        options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
        
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Accept.Add(new("application/json"));
                request.Headers.Authorization = new("token", context.AccessToken);

                var response = await context.Backchannel.SendAsync(request);
                var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                context.RunClaimActions(user.RootElement);
            }
        };
    });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configuration example (appsettings.json)
{
  "OAuth": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret"
    },
    "Microsoft": {
      "ClientId": "your-microsoft-client-id",
      "ClientSecret": "your-microsoft-client-secret"
    },
    "GitHub": {
      "ClientId": "your-github-client-id",
      "ClientSecret": "your-github-client-secret"
    }
  }
}

// Usage in Razor Pages or MVC
public class LoginModel : PageModel
{
    public IActionResult OnGetLoginProvider(string provider, string returnUrl = null)
    {
        var redirectUrl = Url.Page("./Login", pageHandler: "Callback", values: new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        
        return Challenge(properties, provider);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
    {
        if (remoteError != null)
        {
            ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
            return Page();
        }

        var info = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (info?.Principal?.Identity?.IsAuthenticated != true)
        {
            ModelState.AddModelError(string.Empty, "Error loading external login information.");
            return Page();
        }

        // Process user login...
        return LocalRedirect(returnUrl ?? "/");
    }
}

// Client-side OAuth login (JavaScript)
class OAuthClient {
    async initiateLogin(provider, returnUrl) {
        const response = await fetch(`/api/oauth/login/${provider}?returnUrl=${encodeURIComponent(returnUrl)}`);
        const data = await response.json();
        
        if (data.redirectUrl) {
            window.location.href = data.redirectUrl;
        }
    }

    async handleCallback() {
        const urlParams = new URLSearchParams(window.location.search);
        const code = urlParams.get('code');
        const state = urlParams.get('state');
        const provider = this.getProviderFromUrl();

        const response = await fetch(`/api/oauth/callback/${provider}?code=${code}&state=${state}`);
        const tokens = await response.json();

        if (tokens.accessToken) {
            localStorage.setItem('accessToken', tokens.accessToken);
            localStorage.setItem('refreshToken', tokens.refreshToken);
            return true;
        }

        return false;
    }

    getProviderFromUrl() {
        // Extract provider from callback URL
        const path = window.location.pathname;
        return path.split('/').pop();
    }
}

// Security middleware for OAuth
public class OAuthSecurityMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<OAuthSecurityMiddleware> logger;

    public OAuthSecurityMiddleware(RequestDelegate next, ILogger<OAuthSecurityMiddleware> logger)
    {
        next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Log OAuth requests
        if (context.Request.Path.StartsWithSegments("/api/oauth"))
        {
            logger.LogInformation("OAuth request: {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        await next(context);
    }
}

app.UseMiddleware<OAuthSecurityMiddleware>();
```

**Prerequisites**:

- .NET 8 or later
- Microsoft.AspNetCore.Authentication.Google package
- Microsoft.AspNetCore.Authentication.MicrosoftAccount package
- Microsoft.AspNetCore.Authentication.OAuth package
- Microsoft.AspNetCore.Authentication.OpenIdConnect package
- Registered OAuth applications with providers

**Notes**:

- **PKCE**: Use Proof Key for Code Exchange (PKCE) for enhanced security, especially in public clients
- **State Parameter**: Always validate the state parameter to prevent CSRF attacks
- **Token Storage**: Store refresh tokens securely and implement proper token lifecycle management
- **Scope Management**: Request minimal scopes necessary for your application functionality
- **Error Handling**: Implement comprehensive error handling for OAuth flows
- **Security Headers**: Add appropriate security headers to prevent clickjacking and other attacks
- **HTTPS**: Always use HTTPS in production for OAuth flows
- **Provider Differences**: Handle provider-specific differences in token formats and user profile structures

**Related Snippets**:

- [JWT Authentication](jwt-authentication.md)
- [Role-Based Authorization](role-based-authorization.md)
- [Password Security](password-security.md)

**References**:

- [OAuth 2.0 RFC](https://tools.ietf.org/html/rfc6749)
- [OpenID Connect Core](https://openid.net/specs/openid-connect-core-1_0.html)
- [PKCE RFC](https://tools.ietf.org/html/rfc7636)
- [ASP.NET Core External Authentication](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/)

---

*Created: 2025-11-01*  
*Last Updated: 2025-11-01*  
*Tags: #oauth #openid-connect #authentication #security #aspnetcore #pkce*
