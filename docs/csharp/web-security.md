# Web Security Best Practices

**Description**: Comprehensive web security implementation for ASP.NET Core applications covering HTTPS enforcement, CSRF protection, XSS prevention, Content Security Policy, and secure headers configuration.

**Language/Technology**: C#, ASP.NET Core 8+

**Code**:

```csharp
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

// Security configuration options
public class WebSecurityOptions
{
    public HttpsOptions Https { get; set; } = new();
    public CsrfOptions Csrf { get; set; } = new();
    public XssOptions Xss { get; set; } = new();
    public CspOptions ContentSecurityPolicy { get; set; } = new();
    public SecurityHeadersOptions Headers { get; set; } = new();
    public RateLimitOptions RateLimit { get; set; } = new();
}

public class HttpsOptions
{
    public bool Enforce { get; set; } = true;
    public int Port { get; set; } = 443;
    public bool UseHsts { get; set; } = true;
    public TimeSpan HstsMaxAge { get; set; } = TimeSpan.FromDays(365);
    public bool IncludeSubDomains { get; set; } = true;
    public bool Preload { get; set; } = false;
}

public class CsrfOptions
{
    public bool Enabled { get; set; } = true;
    public string CookieName { get; set; } = "__RequestVerificationToken";
    public string HeaderName { get; set; } = "X-CSRF-TOKEN";
    public bool RequireSsl { get; set; } = true;
    public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromMinutes(20);
}

public class XssOptions
{
    public bool EnableFiltering { get; set; } = true;
    public bool SanitizeInput { get; set; } = true;
    public bool ValidateOutput { get; set; } = true;
    public string[] AllowedTags { get; set; } = ["b", "i", "u", "em", "strong"];
    public string[] AllowedAttributes { get; set; } = ["class", "id"];
}

public class CspOptions
{
    public bool Enabled { get; set; } = true;
    public bool ReportOnly { get; set; } = false;
    public string DefaultSrc { get; set; } = "'self'";
    public string ScriptSrc { get; set; } = "'self' 'unsafe-inline'";
    public string StyleSrc { get; set; } = "'self' 'unsafe-inline'";
    public string ImgSrc { get; set; } = "'self' data: https:";
    public string ConnectSrc { get; set; } = "'self'";
    public string FontSrc { get; set; } = "'self'";
    public string ObjectSrc { get; set; } = "'none'";
    public string MediaSrc { get; set; } = "'self'";
    public string FrameSrc { get; set; } = "'none'";
    public string ReportUri { get; set; } = "/api/security/csp-report";
}

public class SecurityHeadersOptions
{
    public bool XFrameOptions { get; set; } = true;
    public string XFrameValue { get; set; } = "DENY";
    public bool XContentTypeOptions { get; set; } = true;
    public bool XssProtection { get; set; } = true;
    public bool ReferrerPolicy { get; set; } = true;
    public string ReferrerValue { get; set; } = "strict-origin-when-cross-origin";
    public bool PermissionsPolicy { get; set; } = true;
    public string[] DisabledFeatures { get; set; } = ["camera", "microphone", "geolocation"];
}

public class RateLimitOptions
{
    public bool Enabled { get; set; } = true;
    public int RequestsPerMinute { get; set; } = 60;
    public int BurstLimit { get; set; } = 10;
    public TimeSpan WindowSize { get; set; } = TimeSpan.FromMinutes(1);
    public string[] ExemptPaths { get; set; } = ["/health", "/metrics"];
}

// Security middleware
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate next;
    private readonly WebSecurityOptions options;
    private readonly ILogger<SecurityHeadersMiddleware> logger;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        IOptions<WebSecurityOptions> options,
        ILogger<SecurityHeadersMiddleware> logger)
    {
        next = next;
        options = options.Value;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before processing request
        AddSecurityHeaders(context);

        // Add CSP header
        if (options.ContentSecurityPolicy.Enabled)
        {
            AddContentSecurityPolicy(context);
        }

        await next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        if (options.Headers.XFrameOptions)
        {
            headers["X-Frame-Options"] = options.Headers.XFrameValue;
        }

        if (options.Headers.XContentTypeOptions)
        {
            headers["X-Content-Type-Options"] = "nosniff";
        }

        if (options.Headers.XssProtection)
        {
            headers["X-XSS-Protection"] = "1; mode=block";
        }

        if (options.Headers.ReferrerPolicy)
        {
            headers["Referrer-Policy"] = options.Headers.ReferrerValue;
        }

        if (options.Headers.PermissionsPolicy)
        {
            var policy = string.Join(", ", options.Headers.DisabledFeatures.Select(f => $"{f}=()"));
            headers["Permissions-Policy"] = policy;
        }

        // Remove server header for security
        headers.Remove("Server");
    }

    private void AddContentSecurityPolicy(HttpContext context)
    {
        var csp = options.ContentSecurityPolicy;
        var policy = new StringBuilder();

        policy.Append($"default-src {csp.DefaultSrc}; ");
        policy.Append($"script-src {csp.ScriptSrc}; ");
        policy.Append($"style-src {csp.StyleSrc}; ");
        policy.Append($"img-src {csp.ImgSrc}; ");
        policy.Append($"connect-src {csp.ConnectSrc}; ");
        policy.Append($"font-src {csp.FontSrc}; ");
        policy.Append($"object-src {csp.ObjectSrc}; ");
        policy.Append($"media-src {csp.MediaSrc}; ");
        policy.Append($"frame-src {csp.FrameSrc}; ");

        if (!string.IsNullOrEmpty(csp.ReportUri))
        {
            policy.Append($"report-uri {csp.ReportUri}; ");
        }

        var headerName = csp.ReportOnly ? "Content-Security-Policy-Report-Only" : "Content-Security-Policy";
        context.Response.Headers[headerName] = policy.ToString();
    }
}

// CSRF protection service
public interface ICsrfProtectionService
{
    string GenerateToken(HttpContext context);
    Task<bool> ValidateTokenAsync(HttpContext context, string token);
    Task<bool> ValidateRequestAsync(HttpContext context);
}

public class CsrfProtectionService : ICsrfProtectionService
{
    private readonly IAntiforgery antiforgery;
    private readonly CsrfOptions options;
    private readonly ILogger<CsrfProtectionService> logger;

    public CsrfProtectionService(
        IAntiforgery antiforgery,
        IOptions<WebSecurityOptions> options,
        ILogger<CsrfProtectionService> logger)
    {
        antiforgery = antiforgery;
        options = options.Value.Csrf;
        this.logger = logger;
    }

    public string GenerateToken(HttpContext context)
    {
        var tokenSet = antiforgery.GetAndStoreTokens(context);
        return tokenSet.RequestToken!;
    }

    public async Task<bool> ValidateTokenAsync(HttpContext context, string token)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException ex)
        {
            logger.LogWarning(ex, "CSRF token validation failed for request {Path}", context.Request.Path);
            return false;
        }
    }

    public async Task<bool> ValidateRequestAsync(HttpContext context)
    {
        if (!options.Enabled)
        {
            return true;
        }

        // Skip validation for safe HTTP methods
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            return true;
        }

        return await ValidateTokenAsync(context, string.Empty);
    }
}

// XSS protection service
public interface IXssProtectionService
{
    string SanitizeInput(string input);
    string SanitizeHtml(string html);
    bool IsValidInput(string input);
    string EncodeForHtml(string input);
    string EncodeForAttribute(string input);
    string EncodeForJavaScript(string input);
}

public class XssProtectionService : IXssProtectionService
{
    private readonly XssOptions options;
    private readonly HtmlEncoder htmlEncoder;
    private readonly JavaScriptEncoder jsEncoder;
    private readonly ILogger<XssProtectionService> logger;

    public XssProtectionService(
        IOptions<WebSecurityOptions> options,
        HtmlEncoder htmlEncoder,
        JavaScriptEncoder jsEncoder,
        ILogger<XssProtectionService> logger)
    {
        options = options.Value.Xss;
        this.htmlEncoder = htmlEncoder;
        this.jsEncoder = jsEncoder;
        this.logger = logger;
    }

    public string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        if (!options.SanitizeInput)
        {
            return input;
        }

        // Remove potentially dangerous patterns
        var sanitized = input;
        var dangerousPatterns = new[]
        {
            @"<script[^>]*>.*?</script>",
            @"javascript:",
            @"vbscript:",
            @"onload=",
            @"onerror=",
            @"onclick=",
            @"onmouseover=",
            @"<iframe",
            @"<object",
            @"<embed"
        };

        foreach (var pattern in dangerousPatterns)
        {
            sanitized = Regex.Replace(sanitized, pattern, string.Empty, RegexOptions.IgnoreCase);
        }

        return sanitized;
    }

    public string SanitizeHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return html;
        }

        // Simple HTML sanitization - for production use a library like HtmlSanitizer
        var allowedTags = options.AllowedTags.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allowedAttributes = options.AllowedAttributes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // This is a simplified implementation - use AntiXSS or HtmlSanitizer in production
        return Regex.Replace(html, @"<[^>]+>", match =>
        {
            var tag = match.Value;
            var tagName = Regex.Match(tag, @"</?(\w+)", RegexOptions.IgnoreCase).Groups[1].Value;

            if (!allowedTags.Contains(tagName))
            {
                return string.Empty;
            }

            return tag;
        });
    }

    public bool IsValidInput(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return true;
        }

        // Check for XSS patterns
        var xssPatterns = new[]
        {
            @"<script",
            @"javascript:",
            @"vbscript:",
            @"on\w+\s*=",
            @"<iframe",
            @"<object",
            @"<embed",
            @"eval\s*\(",
            @"expression\s*\("
        };

        foreach (var pattern in xssPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
            {
                logger.LogWarning("Potentially malicious input detected: {Pattern}", pattern);
                return false;
            }
        }

        return true;
    }

    public string EncodeForHtml(string input)
    {
        return htmlEncoder.Encode(input);
    }

    public string EncodeForAttribute(string input)
    {
        return htmlEncoder.Encode(input);
    }

    public string EncodeForJavaScript(string input)
    {
        return jsEncoder.Encode(input);
    }
}

// Input validation middleware
public class InputValidationMiddleware
{
    private readonly RequestDelegate next;
    private readonly IXssProtectionService xssProtection;
    private readonly ILogger<InputValidationMiddleware> logger;

    public InputValidationMiddleware(
        RequestDelegate next,
        IXssProtectionService xssProtection,
        ILogger<InputValidationMiddleware> logger)
    {
        next = next;
        this.xssProtection = xssProtection;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Validate query parameters
        foreach (var param in context.Request.Query)
        {
            if (!xssProtection.IsValidInput(param.Value))
            {
                logger.LogWarning("Malicious input detected in query parameter {Key}", param.Key);
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid input detected");
                return;
            }
        }

        // Validate form data for POST requests
        if (HttpMethods.IsPost(context.Request.Method) && context.Request.HasFormContentType)
        {
            var form = await context.Request.ReadFormAsync();
            foreach (var field in form)
            {
                if (!xssProtection.IsValidInput(field.Value))
                {
                    logger.LogWarning("Malicious input detected in form field {Key}", field.Key);
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid input detected");
                    return;
                }
            }
        }

        await next(context);
    }
}

// Rate limiting middleware
public class RateLimitingMiddleware
{
    private readonly RequestDelegate next;
    private readonly RateLimitOptions options;
    private readonly IMemoryCache cache;
    private readonly ILogger<RateLimitingMiddleware> logger;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IOptions<WebSecurityOptions> options,
        IMemoryCache cache,
        ILogger<RateLimitingMiddleware> logger)
    {
        next = next;
        options = options.Value.RateLimit;
        this.cache = cache;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!options.Enabled)
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value;
        if (options.ExemptPaths.Any(ep => path?.StartsWith(ep, StringComparison.OrdinalIgnoreCase) == true))
        {
            await next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        var key = $"rate_limit_{clientId}";

        if (cache.TryGetValue(key, out RateLimitInfo? info))
        {
            if (info!.RequestCount >= options.RequestsPerMinute)
            {
                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = "60";
                await context.Response.WriteAsync("Rate limit exceeded");
                return;
            }

            info.RequestCount++;
        }
        else
        {
            info = new RateLimitInfo { RequestCount = 1, WindowStart = DateTime.UtcNow };
            cache.Set(key, info, options.WindowSize);
        }

        await next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Use IP address as client identifier
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            return forwarded.Split(',').First().Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

public class RateLimitInfo
{
    public int RequestCount { get; set; }
    public DateTime WindowStart { get; set; }
}

// Security controller for CSP reporting
[ApiController]
[Route("api/[controller]")]
public class SecurityController : ControllerBase
{
    private readonly ILogger<SecurityController> logger;

    public SecurityController(ILogger<SecurityController> logger)
    {
        logger = logger;
    }

    [HttpPost("csp-report")]
    public IActionResult CspReport([FromBody] CspReportRequest report)
    {
        logger.LogWarning("CSP Violation: {Report}", JsonSerializer.Serialize(report));

        // Store violation for analysis
        // You might want to store this in a database or send to a monitoring service

        return Ok();
    }

    [HttpGet("nonce/{type}")]
    public IActionResult GenerateNonce(string type)
    {
        var nonce = GenerateSecureNonce();
        
        // Store nonce for this request
        HttpContext.Items[$"csp_nonce_{type}"] = nonce;
        
        return Ok(new { nonce });
    }

    private string GenerateSecureNonce()
    {
        var bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

public record CspReportRequest
{
    [JsonPropertyName("csp-report")]
    public CspReport Report { get; set; } = new();
}

public record CspReport
{
    [JsonPropertyName("document-uri")]
    public string DocumentUri { get; set; } = string.Empty;

    [JsonPropertyName("referrer")]
    public string Referrer { get; set; } = string.Empty;

    [JsonPropertyName("violated-directive")]
    public string ViolatedDirective { get; set; } = string.Empty;

    [JsonPropertyName("blocked-uri")]
    public string BlockedUri { get; set; } = string.Empty;

    [JsonPropertyName("original-policy")]
    public string OriginalPolicy { get; set; } = string.Empty;
}

// Secure attribute for marking controllers/actions as requiring enhanced security
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SecureAttribute : Attribute, IAuthorizationFilter
{
    public bool RequireHttps { get; set; } = true;
    public bool ValidateCsrf { get; set; } = true;
    public bool ValidateInput { get; set; } = true;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (RequireHttps && !context.HttpContext.Request.IsHttps)
        {
            context.Result = new StatusCodeResult(400);
            return;
        }

        // Additional security checks can be added here
    }
}

// Extension methods for security configuration
public static class SecurityExtensions
{
    public static IServiceCollection AddWebSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<WebSecurityOptions>(configuration.GetSection("WebSecurity"));
        
        services.AddAntiforgery(options =>
        {
            var csrfOptions = configuration.GetSection("WebSecurity:Csrf").Get<CsrfOptions>() ?? new();
            
            options.Cookie.Name = csrfOptions.CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = csrfOptions.SameSite;
            options.HeaderName = csrfOptions.HeaderName;
        });

        services.AddScoped<ICsrfProtectionService, CsrfProtectionService>();
        services.AddScoped<IXssProtectionService, XssProtectionService>();
        
        services.AddSingleton<HtmlEncoder>(HtmlEncoder.Create(UnicodeRanges.All));
        services.AddSingleton<JavaScriptEncoder>(JavaScriptEncoder.Create(UnicodeRanges.All));

        return services;
    }

    public static IApplicationBuilder UseWebSecurity(this IApplicationBuilder app)
    {
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<RateLimitingMiddleware>();
        app.UseMiddleware<InputValidationMiddleware>();
        
        return app;
    }

    public static IApplicationBuilder UseSecureHsts(
        this IApplicationBuilder app,
        HttpsOptions options)
    {
        if (options.UseHsts)
        {
            app.UseHsts();
        }

        return app;
    }
}
```

**Usage**:

```csharp
// Program.cs - Security Configuration
var builder = WebApplication.CreateBuilder(args);

// Add web security services
builder.Services.AddWebSecurity(builder.Configuration);

// Configure HTTPS
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    options.HttpsPort = 443;
});

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

var app = builder.Build();

// Configure security middleware pipeline
if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseWebSecurity(); // Add security middleware

app.UseAuthentication();
app.UseAuthorization();

// Configuration (appsettings.json)
{
  "WebSecurity": {
    "Https": {
      "Enforce": true,
      "UseHsts": true,
      "HstsMaxAge": "365.00:00:00",
      "IncludeSubDomains": true,
      "Preload": true
    },
    "Csrf": {
      "Enabled": true,
      "CookieName": "__RequestVerificationToken",
      "HeaderName": "X-CSRF-TOKEN",
      "RequireSsl": true,
      "SameSite": "Strict",
      "TokenLifetime": "00:20:00"
    },
    "Xss": {
      "EnableFiltering": true,
      "SanitizeInput": true,
      "ValidateOutput": true,
      "AllowedTags": ["b", "i", "u", "em", "strong", "p", "br"],
      "AllowedAttributes": ["class", "id", "style"]
    },
    "ContentSecurityPolicy": {
      "Enabled": true,
      "ReportOnly": false,
      "DefaultSrc": "'self'",
      "ScriptSrc": "'self' 'nonce-{random}'",
      "StyleSrc": "'self' 'unsafe-inline'",
      "ImgSrc": "'self' data: https:",
      "ConnectSrc": "'self'",
      "ReportUri": "/api/security/csp-report"
    },
    "Headers": {
      "XFrameOptions": true,
      "XFrameValue": "DENY",
      "XContentTypeOptions": true,
      "XssProtection": true,
      "ReferrerPolicy": true,
      "ReferrerValue": "strict-origin-when-cross-origin",
      "PermissionsPolicy": true,
      "DisabledFeatures": ["camera", "microphone", "geolocation", "usb"]
    },
    "RateLimit": {
      "Enabled": true,
      "RequestsPerMinute": 60,
      "BurstLimit": 10,
      "WindowSize": "00:01:00",
      "ExemptPaths": ["/health", "/metrics", "/api/security/nonce"]
    }
  }
}

// Controller usage with security attributes
[ApiController]
[Route("api/[controller]")]
[Secure(RequireHttps = true, ValidateCsrf = true)]
public class UserController : ControllerBase
{
    private readonly IXssProtectionService xssProtection;
    private readonly ICsrfProtectionService csrfProtection;

    public UserController(
        IXssProtectionService xssProtection,
        ICsrfProtectionService csrfProtection)
    {
        xssProtection = xssProtection;
        this.csrfProtection = csrfProtection;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var token = csrfProtection.GenerateToken(HttpContext);
        Response.Headers["X-CSRF-Token"] = token;
        
        return Ok(new { Message = "Secure endpoint", CsrfToken = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        // Validate CSRF token
        if (!await csrfProtection.ValidateRequestAsync(HttpContext))
        {
            return BadRequest("Invalid CSRF token");
        }

        // Sanitize input
        var sanitizedName = xssProtection.SanitizeInput(request.Name);
        var sanitizedEmail = xssProtection.SanitizeInput(request.Email);

        // Validate input
        if (!xssProtection.IsValidInput(sanitizedName) || 
            !xssProtection.IsValidInput(sanitizedEmail))
        {
            return BadRequest("Invalid input detected");
        }

        // Process request...
        return Ok();
    }
}

// Razor Pages usage
public class IndexModel : PageModel
{
    private readonly IXssProtectionService xssProtection;

    public IndexModel(IXssProtectionService xssProtection)
    {
        xssProtection = xssProtection;
    }

    public string SafeContent { get; set; } = string.Empty;

    public void OnGet(string userInput)
    {
        // Safely encode user input for display
        SafeContent = xssProtection.EncodeForHtml(userInput ?? string.Empty);
    }
}

// Razor view with CSP nonce
@{
    var nonce = Html.Raw(ViewContext.HttpContext.Items["csp_nonce_script"]);
}

<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <title>Secure Page</title>
</head>
<body>
    <h1>@Html.Encode(Model.SafeContent)</h1>
    
    <!-- Safe inline script with nonce -->
    <script nonce="@nonce">
        // Safe JavaScript code
        console.log('Page loaded securely');
    </script>
    
    <!-- CSRF token for AJAX requests -->
    <meta name="csrf-token" content="@Html.AntiForgeryToken()" />
    
    <script nonce="@nonce">
        // Setup CSRF token for AJAX
        const token = document.querySelector('meta[name="csrf-token"]').getAttribute('content');
        
        fetch('/api/user', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': token
            },
            body: JSON.stringify({ name: 'John Doe' })
        });
    </script>
</body>
</html>

// Client-side security helper
class SecurityHelper {
    static getCsrfToken() {
        const meta = document.querySelector('meta[name="csrf-token"]');
        return meta ? meta.getAttribute('content') : null;
    }

    static sanitizeHtml(str) {
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    static isSecureContext() {
        return window.isSecureContext && location.protocol === 'https:';
    }

    static setupSecureAjax() {
        // Setup global AJAX settings
        const token = this.getCsrfToken();
        if (token) {
            // jQuery example
            if (typeof $ !== 'undefined') {
                $.ajaxSetup({
                    beforeSend: function(xhr) {
                        xhr.setRequestHeader('X-CSRF-TOKEN', token);
                    }
                });
            }
        }
    }
}

// Initialize security on page load
document.addEventListener('DOMContentLoaded', function() {
    SecurityHelper.setupSecureAjax();
    
    // Ensure we're in a secure context
    if (!SecurityHelper.isSecureContext()) {
        console.warn('Application is not running in a secure context (HTTPS)');
    }
});

public record CreateUserRequest(string Name, string Email, string Description);
```

**Prerequisites**:

- .NET 8 or later
- ASP.NET Core framework
- Microsoft.AspNetCore.DataProtection package
- Microsoft.Extensions.Caching.Memory package

**Notes**:

- **HTTPS**: Always enforce HTTPS in production with proper HSTS headers
- **CSP**: Implement strict Content Security Policy with nonces for inline scripts
- **Input Validation**: Validate and sanitize all user input on both client and server
- **CSRF Protection**: Use anti-forgery tokens for all state-changing operations
- **XSS Prevention**: Encode output and sanitize HTML content appropriately
- **Rate Limiting**: Implement rate limiting to prevent abuse and DoS attacks
- **Security Headers**: Add comprehensive security headers to prevent various attacks
- **Error Handling**: Don't expose sensitive information in error messages
- **Logging**: Log security events for monitoring and incident response

**Related Snippets**:

- [JWT Authentication](jwt-authentication.md)
- [OAuth Integration](oauth-integration.md)
- [Role-Based Authorization](role-based-authorization.md)
- [Password Security](password-security.md)
- [Polly Patterns](polly-patterns.md) - For Bulkhead isolation as an alternative to rate limiting

**References**:

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [Content Security Policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP)
- [CSRF Prevention](https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html)

---

*Created: 2025-11-01*  
*Last Updated: 2025-11-01*  
*Tags: #web-security #https #csrf #xss #csp #security-headers #aspnetcore*
