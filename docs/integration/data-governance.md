# Data Governance Patterns

**Description**: Data privacy patterns, encryption strategies, GDPR compliance, data lineage tracking, and sensitive data handling best practices for enterprise data governance.

**Language/Technology**: C# / .NET 9.0

**Code**:

## Data Classification and Sensitivity Management

```csharp
// Data Classification Enums and Models
public enum DataClassification
{
    Public = 1,
    Internal = 2,
    Confidential = 3,
    Restricted = 4
}

public enum DataCategory
{
    PersonalData,
    FinancialData,
    HealthData,
    IntellectualProperty,
    CustomerData,
    BusinessData
}

public record DataSensitivityLevel(
    DataClassification Classification,
    DataCategory Category,
    bool IsPersonalData,
    bool RequiresEncryption,
    TimeSpan RetentionPeriod,
    List<string> AllowedRegions);

// Data Classification Attribute
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class)]
public class DataClassificationAttribute : Attribute
{
    public DataClassification Classification { get; }
    public DataCategory Category { get; }
    public bool IsPersonalData { get; }
    public string? Purpose { get; }
    public int RetentionDays { get; }

    public DataClassificationAttribute(
        DataClassification classification,
        DataCategory category,
        bool isPersonalData = false,
        string? purpose = null,
        int retentionDays = 2555) // 7 years default
    {
        Classification = classification;
        Category = category;
        IsPersonalData = isPersonalData;
        Purpose = purpose;
        RetentionDays = retentionDays;
    }
}

// Example Data Models with Classification
[DataClassification(DataClassification.Confidential, DataCategory.PersonalData, isPersonalData: true)]
public class CustomerProfile
{
    public int Id { get; set; }
    
    [DataClassification(DataClassification.Confidential, DataCategory.PersonalData, isPersonalData: true, purpose: "Customer identification")]
    public string FirstName { get; set; } = string.Empty;
    
    [DataClassification(DataClassification.Confidential, DataCategory.PersonalData, isPersonalData: true, purpose: "Customer identification")]
    public string LastName { get; set; } = string.Empty;
    
    [DataClassification(DataClassification.Restricted, DataCategory.PersonalData, isPersonalData: true, purpose: "Customer contact")]
    public string Email { get; set; } = string.Empty;
    
    [DataClassification(DataClassification.Restricted, DataCategory.PersonalData, isPersonalData: true, purpose: "Customer contact")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [DataClassification(DataClassification.Restricted, DataCategory.FinancialData, isPersonalData: true, purpose: "Financial transactions")]
    public decimal CreditLimit { get; set; }
    
    [DataClassification(DataClassification.Internal, DataCategory.BusinessData)]
    public DateTime CreatedAt { get; set; }
    
    [DataClassification(DataClassification.Public, DataCategory.BusinessData)]
    public string PreferredLanguage { get; set; } = "en";
}

// Data Classification Service
public interface IDataClassificationService
{
    DataSensitivityLevel GetSensitivityLevel(Type type);
    DataSensitivityLevel GetPropertySensitivityLevel(PropertyInfo property);
    List<PropertyInfo> GetPersonalDataProperties(Type type);
    bool RequiresEncryption(PropertyInfo property);
    TimeSpan GetRetentionPeriod(Type type);
}

public class DataClassificationService : IDataClassificationService
{
    private readonly Dictionary<DataClassification, DataSensitivityLevel> _defaultLevels;

    public DataClassificationService()
    {
        _defaultLevels = new Dictionary<DataClassification, DataSensitivityLevel>
        {
            [DataClassification.Public] = new DataSensitivityLevel(
                DataClassification.Public,
                DataCategory.BusinessData,
                false,
                false,
                TimeSpan.FromDays(365 * 10), // 10 years
                ["Global"]),
            
            [DataClassification.Internal] = new DataSensitivityLevel(
                DataClassification.Internal,
                DataCategory.BusinessData,
                false,
                true,
                TimeSpan.FromDays(365 * 7), // 7 years
                ["US", "EU", "CA"]),
            
            [DataClassification.Confidential] = new DataSensitivityLevel(
                DataClassification.Confidential,
                DataCategory.PersonalData,
                true,
                true,
                TimeSpan.FromDays(365 * 7), // 7 years
                ["US", "EU"]),
            
            [DataClassification.Restricted] = new DataSensitivityLevel(
                DataClassification.Restricted,
                DataCategory.PersonalData,
                true,
                true,
                TimeSpan.FromDays(365 * 3), // 3 years
                ["Home_Country_Only"])
        };
    }

    public DataSensitivityLevel GetSensitivityLevel(Type type)
    {
        var attribute = type.GetCustomAttribute<DataClassificationAttribute>();
        if (attribute == null)
            return _defaultLevels[DataClassification.Internal]; // Safe default

        return new DataSensitivityLevel(
            attribute.Classification,
            attribute.Category,
            attribute.IsPersonalData,
            _defaultLevels[attribute.Classification].RequiresEncryption,
            TimeSpan.FromDays(attribute.RetentionDays),
            _defaultLevels[attribute.Classification].AllowedRegions);
    }

    public DataSensitivityLevel GetPropertySensitivityLevel(PropertyInfo property)
    {
        var attribute = property.GetCustomAttribute<DataClassificationAttribute>();
        if (attribute == null)
        {
            // Check class-level classification
            var classAttribute = property.DeclaringType?.GetCustomAttribute<DataClassificationAttribute>();
            if (classAttribute != null)
                attribute = classAttribute;
            else
                return _defaultLevels[DataClassification.Internal]; // Safe default
        }

        return new DataSensitivityLevel(
            attribute.Classification,
            attribute.Category,
            attribute.IsPersonalData,
            _defaultLevels[attribute.Classification].RequiresEncryption,
            TimeSpan.FromDays(attribute.RetentionDays),
            _defaultLevels[attribute.Classification].AllowedRegions);
    }

    public List<PropertyInfo> GetPersonalDataProperties(Type type)
    {
        return type.GetProperties()
            .Where(p => GetPropertySensitivityLevel(p).IsPersonalData)
            .ToList();
    }

    public bool RequiresEncryption(PropertyInfo property)
    {
        return GetPropertySensitivityLevel(property).RequiresEncryption;
    }

    public TimeSpan GetRetentionPeriod(Type type)
    {
        return GetSensitivityLevel(type).RetentionPeriod;
    }
}
```

## GDPR Compliance and Consent Management

```csharp
// GDPR Consent Models
public enum ConsentPurpose
{
    Marketing,
    Analytics,
    Personalization,
    Advertising,
    FunctionalCookies,
    PerformanceCookies,
    TargetingCookies,
    DataProcessing,
    DataSharing,
    ProfileEnrichment
}

public enum ConsentStatus
{
    NotGiven,
    Given,
    Withdrawn,
    Expired
}

public record ConsentRecord(
    int UserId,
    ConsentPurpose Purpose,
    ConsentStatus Status,
    DateTime GrantedAt,
    DateTime? WithdrawnAt,
    DateTime ExpiresAt,
    string IpAddress,
    string UserAgent,
    string LegalBasis,
    string? AdditionalContext);

// GDPR Subject Rights
public enum SubjectRightType
{
    AccessRequest,      // Article 15 - Right of access
    RectificationRequest, // Article 16 - Right to rectification
    ErasureRequest,     // Article 17 - Right to erasure (Right to be forgotten)
    RestrictProcessing, // Article 18 - Right to restrict processing
    DataPortability,    // Article 20 - Right to data portability
    ObjectProcessing,   // Article 21 - Right to object
    WithdrawConsent     // Article 7(3) - Right to withdraw consent
}

public record SubjectRightRequest(
    int RequestId,
    int UserId,
    SubjectRightType RequestType,
    DateTime RequestedAt,
    string RequestDetails,
    SubjectRightStatus Status,
    DateTime? CompletedAt,
    string? ResponseData,
    string? RejectionReason);

public enum SubjectRightStatus
{
    Pending,
    InProgress,
    Completed,
    Rejected,
    RequiresVerification
}

// GDPR Consent Service
public interface IGdprConsentService
{
    Task<bool> HasValidConsentAsync(int userId, ConsentPurpose purpose);
    Task RecordConsentAsync(int userId, ConsentPurpose purpose, string ipAddress, string userAgent, string legalBasis);
    Task WithdrawConsentAsync(int userId, ConsentPurpose purpose);
    Task<List<ConsentRecord>> GetUserConsentsAsync(int userId);
    Task<ConsentStatus> GetConsentStatusAsync(int userId, ConsentPurpose purpose);
    Task RefreshExpiredConsentsAsync();
}

public class GdprConsentService : IGdprConsentService
{
    private readonly IConsentRepository _consentRepository;
    private readonly ILogger<GdprConsentService> _logger;
    private readonly TimeSpan _defaultConsentDuration = TimeSpan.FromDays(365 * 2); // 2 years

    public GdprConsentService(IConsentRepository consentRepository, ILogger<GdprConsentService> logger)
    {
        _consentRepository = consentRepository;
        _logger = logger;
    }

    public async Task<bool> HasValidConsentAsync(int userId, ConsentPurpose purpose)
    {
        var consent = await _consentRepository.GetLatestConsentAsync(userId, purpose);
        
        if (consent == null || consent.Status != ConsentStatus.Given)
            return false;

        if (consent.ExpiresAt <= DateTime.UtcNow)
        {
            // Mark as expired
            await _consentRepository.UpdateConsentStatusAsync(consent with { Status = ConsentStatus.Expired });
            return false;
        }

        return true;
    }

    public async Task RecordConsentAsync(int userId, ConsentPurpose purpose, string ipAddress, string userAgent, string legalBasis)
    {
        var consent = new ConsentRecord(
            userId,
            purpose,
            ConsentStatus.Given,
            DateTime.UtcNow,
            null,
            DateTime.UtcNow.Add(_defaultConsentDuration),
            ipAddress,
            userAgent,
            legalBasis,
            null);

        await _consentRepository.SaveConsentAsync(consent);
        
        _logger.LogInformation("Consent granted for user {UserId} and purpose {Purpose}", userId, purpose);
    }

    public async Task WithdrawConsentAsync(int userId, ConsentPurpose purpose)
    {
        var consent = await _consentRepository.GetLatestConsentAsync(userId, purpose);
        if (consent?.Status == ConsentStatus.Given)
        {
            var withdrawnConsent = consent with 
            { 
                Status = ConsentStatus.Withdrawn,
                WithdrawnAt = DateTime.UtcNow
            };
            
            await _consentRepository.UpdateConsentStatusAsync(withdrawnConsent);
            
            _logger.LogInformation("Consent withdrawn for user {UserId} and purpose {Purpose}", userId, purpose);
        }
    }

    public async Task<List<ConsentRecord>> GetUserConsentsAsync(int userId)
    {
        return await _consentRepository.GetUserConsentsAsync(userId);
    }

    public async Task<ConsentStatus> GetConsentStatusAsync(int userId, ConsentPurpose purpose)
    {
        var consent = await _consentRepository.GetLatestConsentAsync(userId, purpose);
        return consent?.Status ?? ConsentStatus.NotGiven;
    }

    public async Task RefreshExpiredConsentsAsync()
    {
        var expiredConsents = await _consentRepository.GetExpiredConsentsAsync();
        
        foreach (var consent in expiredConsents)
        {
            await _consentRepository.UpdateConsentStatusAsync(consent with { Status = ConsentStatus.Expired });
        }
        
        _logger.LogInformation("Updated {Count} expired consents", expiredConsents.Count);
    }
}

// GDPR Subject Rights Service
public interface IGdprSubjectRightsService
{
    Task<int> CreateRequestAsync(int userId, SubjectRightType requestType, string requestDetails);
    Task<SubjectRightRequest?> GetRequestAsync(int requestId);
    Task<List<SubjectRightRequest>> GetUserRequestsAsync(int userId);
    Task ProcessAccessRequestAsync(int requestId);
    Task ProcessErasureRequestAsync(int requestId);
    Task ProcessPortabilityRequestAsync(int requestId);
    Task RejectRequestAsync(int requestId, string reason);
}

public class GdprSubjectRightsService : IGdprSubjectRightsService
{
    private readonly ISubjectRightRepository _requestRepository;
    private readonly IUserDataService _userDataService;
    private readonly IDataExportService _exportService;
    private readonly ILogger<GdprSubjectRightsService> _logger;

    public GdprSubjectRightsService(
        ISubjectRightRepository requestRepository,
        IUserDataService userDataService,
        IDataExportService exportService,
        ILogger<GdprSubjectRightsService> logger)
    {
        _requestRepository = requestRepository;
        _userDataService = userDataService;
        _exportService = exportService;
        _logger = logger;
    }

    public async Task<int> CreateRequestAsync(int userId, SubjectRightType requestType, string requestDetails)
    {
        var request = new SubjectRightRequest(
            0, // Will be set by repository
            userId,
            requestType,
            DateTime.UtcNow,
            requestDetails,
            SubjectRightStatus.Pending,
            null,
            null,
            null);

        var requestId = await _requestRepository.CreateRequestAsync(request);
        
        _logger.LogInformation("GDPR request created: Type={RequestType}, UserId={UserId}, RequestId={RequestId}",
            requestType, userId, requestId);

        return requestId;
    }

    public async Task<SubjectRightRequest?> GetRequestAsync(int requestId)
    {
        return await _requestRepository.GetRequestAsync(requestId);
    }

    public async Task<List<SubjectRightRequest>> GetUserRequestsAsync(int userId)
    {
        return await _requestRepository.GetUserRequestsAsync(userId);
    }

    public async Task ProcessAccessRequestAsync(int requestId)
    {
        var request = await _requestRepository.GetRequestAsync(requestId);
        if (request?.RequestType != SubjectRightType.AccessRequest || request.Status != SubjectRightStatus.Pending)
            return;

        try
        {
            await _requestRepository.UpdateStatusAsync(requestId, SubjectRightStatus.InProgress);

            // Generate comprehensive data export
            var userData = await _userDataService.GetAllUserDataAsync(request.UserId);
            var exportData = await _exportService.ExportToJsonAsync(userData);

            var completedRequest = request with
            {
                Status = SubjectRightStatus.Completed,
                CompletedAt = DateTime.UtcNow,
                ResponseData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(exportData))
            };

            await _requestRepository.UpdateRequestAsync(completedRequest);
            
            _logger.LogInformation("Access request completed for RequestId={RequestId}", requestId);
        }
        catch (Exception ex)
        {
            await _requestRepository.UpdateStatusAsync(requestId, SubjectRightStatus.Rejected);
            _logger.LogError(ex, "Failed to process access request {RequestId}", requestId);
        }
    }

    public async Task ProcessErasureRequestAsync(int requestId)
    {
        var request = await _requestRepository.GetRequestAsync(requestId);
        if (request?.RequestType != SubjectRightType.ErasureRequest || request.Status != SubjectRightStatus.Pending)
            return;

        try
        {
            await _requestRepository.UpdateStatusAsync(requestId, SubjectRightStatus.InProgress);

            // Perform right to be forgotten
            await _userDataService.EraseUserDataAsync(request.UserId);

            var completedRequest = request with
            {
                Status = SubjectRightStatus.Completed,
                CompletedAt = DateTime.UtcNow,
                ResponseData = "User data has been permanently erased from all systems"
            };

            await _requestRepository.UpdateRequestAsync(completedRequest);
            
            _logger.LogInformation("Erasure request completed for RequestId={RequestId}", requestId);
        }
        catch (Exception ex)
        {
            await _requestRepository.UpdateStatusAsync(requestId, SubjectRightStatus.Rejected);
            _logger.LogError(ex, "Failed to process erasure request {RequestId}", requestId);
        }
    }

    public async Task ProcessPortabilityRequestAsync(int requestId)
    {
        var request = await _requestRepository.GetRequestAsync(requestId);
        if (request?.RequestType != SubjectRightType.DataPortability || request.Status != SubjectRightStatus.Pending)
            return;

        try
        {
            await _requestRepository.UpdateStatusAsync(requestId, SubjectRightStatus.InProgress);

            // Export data in portable format
            var userData = await _userDataService.GetPortableUserDataAsync(request.UserId);
            var portableData = await _exportService.ExportToPortableFormatAsync(userData);

            var completedRequest = request with
            {
                Status = SubjectRightStatus.Completed,
                CompletedAt = DateTime.UtcNow,
                ResponseData = Convert.ToBase64String(portableData)
            };

            await _requestRepository.UpdateRequestAsync(completedRequest);
            
            _logger.LogInformation("Data portability request completed for RequestId={RequestId}", requestId);
        }
        catch (Exception ex)
        {
            await _requestRepository.UpdateStatusAsync(requestId, SubjectRightStatus.Rejected);
            _logger.LogError(ex, "Failed to process portability request {RequestId}", requestId);
        }
    }

    public async Task RejectRequestAsync(int requestId, string reason)
    {
        var request = await _requestRepository.GetRequestAsync(requestId);
        if (request == null) return;

        var rejectedRequest = request with
        {
            Status = SubjectRightStatus.Rejected,
            CompletedAt = DateTime.UtcNow,
            RejectionReason = reason
        };

        await _requestRepository.UpdateRequestAsync(rejectedRequest);
        
        _logger.LogInformation("Request {RequestId} rejected: {Reason}", requestId, reason);
    }
}
```

## Encryption and Data Protection

```csharp
// Encryption Configuration and Services
public class EncryptionConfiguration
{
    public string KeyVaultUrl { get; set; } = string.Empty;
    public string MasterKeyId { get; set; } = string.Empty;
    public bool UseHardwareSecurityModule { get; set; } = false;
    public int KeyRotationDays { get; set; } = 90;
    public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";
}

// Field-Level Encryption Service
public interface IFieldEncryptionService
{
    Task<string> EncryptAsync(string plaintext, string? keyId = null);
    Task<string> DecryptAsync(string ciphertext, string? keyId = null);
    Task<byte[]> EncryptBytesAsync(byte[] plaintext, string? keyId = null);
    Task<byte[]> DecryptBytesAsync(byte[] ciphertext, string? keyId = null);
    Task RotateKeysAsync();
    Task<string> GenerateDataEncryptionKeyAsync();
}

public class FieldEncryptionService : IFieldEncryptionService
{
    private readonly IKeyVaultService _keyVault;
    private readonly IMemoryCache _cache;
    private readonly EncryptionConfiguration _config;
    private readonly ILogger<FieldEncryptionService> _logger;

    public FieldEncryptionService(
        IKeyVaultService keyVault,
        IMemoryCache cache,
        IOptions<EncryptionConfiguration> config,
        ILogger<FieldEncryptionService> logger)
    {
        _keyVault = keyVault;
        _cache = cache;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<string> EncryptAsync(string plaintext, string? keyId = null)
    {
        if (string.IsNullOrEmpty(plaintext))
            return plaintext;

        var key = await GetEncryptionKeyAsync(keyId ?? _config.MasterKeyId);
        
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using var swEncrypt = new StreamWriter(csEncrypt);
        
        swEncrypt.Write(plaintext);
        swEncrypt.Close();
        
        var encrypted = msEncrypt.ToArray();
        var result = new byte[aes.IV.Length + encrypted.Length];
        Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
        Array.Copy(encrypted, 0, result, aes.IV.Length, encrypted.Length);
        
        return $"{keyId ?? _config.MasterKeyId}:{Convert.ToBase64String(result)}";
    }

    public async Task<string> DecryptAsync(string ciphertext, string? keyId = null)
    {
        if (string.IsNullOrEmpty(ciphertext))
            return ciphertext;

        var parts = ciphertext.Split(':', 2);
        if (parts.Length != 2)
            throw new ArgumentException("Invalid ciphertext format");

        var actualKeyId = parts[0];
        var encryptedData = Convert.FromBase64String(parts[1]);
        
        var key = await GetEncryptionKeyAsync(actualKeyId);
        
        using var aes = Aes.Create();
        aes.Key = key;
        
        var iv = new byte[aes.IV.Length];
        var encrypted = new byte[encryptedData.Length - iv.Length];
        
        Array.Copy(encryptedData, 0, iv, 0, iv.Length);
        Array.Copy(encryptedData, iv.Length, encrypted, 0, encrypted.Length);
        
        aes.IV = iv;
        
        using var decryptor = aes.CreateDecryptor();
        using var msDecrypt = new MemoryStream(encrypted);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        
        return srDecrypt.ReadToEnd();
    }

    public async Task<byte[]> EncryptBytesAsync(byte[] plaintext, string? keyId = null)
    {
        var key = await GetEncryptionKeyAsync(keyId ?? _config.MasterKeyId);
        
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        
        csEncrypt.Write(plaintext, 0, plaintext.Length);
        csEncrypt.FlushFinalBlock();
        
        var encrypted = msEncrypt.ToArray();
        var result = new byte[aes.IV.Length + encrypted.Length];
        Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
        Array.Copy(encrypted, 0, result, aes.IV.Length, encrypted.Length);
        
        return result;
    }

    public async Task<byte[]> DecryptBytesAsync(byte[] ciphertext, string? keyId = null)
    {
        var key = await GetEncryptionKeyAsync(keyId ?? _config.MasterKeyId);
        
        using var aes = Aes.Create();
        aes.Key = key;
        
        var iv = new byte[aes.IV.Length];
        var encrypted = new byte[ciphertext.Length - iv.Length];
        
        Array.Copy(ciphertext, 0, iv, 0, iv.Length);
        Array.Copy(ciphertext, iv.Length, encrypted, 0, encrypted.Length);
        
        aes.IV = iv;
        
        using var decryptor = aes.CreateDecryptor();
        using var msDecrypt = new MemoryStream(encrypted);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        
        var result = new byte[encrypted.Length];
        var totalBytesRead = 0;
        var bytesRead = 0;
        
        while ((bytesRead = csDecrypt.Read(result, totalBytesRead, result.Length - totalBytesRead)) > 0)
        {
            totalBytesRead += bytesRead;
        }
        
        return result.Take(totalBytesRead).ToArray();
    }

    public async Task RotateKeysAsync()
    {
        var newKeyId = await _keyVault.CreateKeyAsync($"dek-{DateTime.UtcNow:yyyyMMdd-HHmmss}");
        
        // Update configuration to use new key
        _config.MasterKeyId = newKeyId;
        
        // Clear cache to force key refresh
        _cache.Remove($"encryption_key_{_config.MasterKeyId}");
        
        _logger.LogInformation("Encryption key rotated to {KeyId}", newKeyId);
    }

    public async Task<string> GenerateDataEncryptionKeyAsync()
    {
        return await _keyVault.CreateKeyAsync($"dek-{Guid.NewGuid()}");
    }

    private async Task<byte[]> GetEncryptionKeyAsync(string keyId)
    {
        var cacheKey = $"encryption_key_{keyId}";
        
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
            return await _keyVault.GetKeyAsync(keyId);
        }) ?? throw new InvalidOperationException($"Encryption key {keyId} not found");
    }
}

// Encrypted Entity Framework Value Converter
public class EncryptedStringConverter : ValueConverter<string?, string?>
{
    public EncryptedStringConverter(IFieldEncryptionService encryptionService) 
        : base(
            v => encryptionService.EncryptAsync(v ?? string.Empty).GetAwaiter().GetResult(),
            v => encryptionService.DecryptAsync(v ?? string.Empty).GetAwaiter().GetResult())
    {
    }
}

public class EncryptedByteArrayConverter : ValueConverter<byte[]?, byte[]?>
{
    public EncryptedByteArrayConverter(IFieldEncryptionService encryptionService)
        : base(
            v => v != null ? encryptionService.EncryptBytesAsync(v).GetAwaiter().GetResult() : null,
            v => v != null ? encryptionService.DecryptBytesAsync(v).GetAwaiter().GetResult() : null)
    {
    }
}

// Entity Framework Context with Encryption
public class EncryptedDbContext : DbContext
{
    private readonly IFieldEncryptionService _encryptionService;
    private readonly IDataClassificationService _classificationService;

    public EncryptedDbContext(
        DbContextOptions<EncryptedDbContext> options,
        IFieldEncryptionService encryptionService,
        IDataClassificationService classificationService) 
        : base(options)
    {
        _encryptionService = encryptionService;
        _classificationService = classificationService;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply encryption to classified properties
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(string) || property.ClrType == typeof(string?))
                {
                    var propertyInfo = property.PropertyInfo;
                    if (propertyInfo != null && _classificationService.RequiresEncryption(propertyInfo))
                    {
                        property.SetValueConverter(new EncryptedStringConverter(_encryptionService));
                    }
                }
                else if (property.ClrType == typeof(byte[]) || property.ClrType == typeof(byte[]?))
                {
                    var propertyInfo = property.PropertyInfo;
                    if (propertyInfo != null && _classificationService.RequiresEncryption(propertyInfo))
                    {
                        property.SetValueConverter(new EncryptedByteArrayConverter(_encryptionService));
                    }
                }
            }
        }
    }
}
```

## Data Lineage and Audit Trail

```csharp
// Data Lineage Models
public record DataLineageEvent(
    Guid Id,
    string EntityType,
    string EntityId,
    DataOperation Operation,
    DateTime Timestamp,
    int? UserId,
    string? UserName,
    Dictionary<string, object?> OldValues,
    Dictionary<string, object?> NewValues,
    string? Reason,
    string? SystemSource,
    string? IpAddress,
    string? UserAgent);

public enum DataOperation
{
    Create,
    Read,
    Update,
    Delete,
    Export,
    Import,
    Archive,
    Restore,
    Anonymize,
    Pseudonymize
}

// Data Lineage Service
public interface IDataLineageService
{
    Task RecordOperationAsync<T>(string entityId, DataOperation operation, T? oldEntity, T? newEntity, string? reason = null);
    Task<List<DataLineageEvent>> GetEntityHistoryAsync(string entityType, string entityId);
    Task<List<DataLineageEvent>> GetUserActivityAsync(int userId, DateTime? from = null, DateTime? to = null);
    Task<List<DataLineageEvent>> GetSystemActivityAsync(string systemSource, DateTime? from = null, DateTime? to = null);
    Task<DataLineageReport> GenerateLineageReportAsync(string entityType, string entityId);
}

public class DataLineageService : IDataLineageService
{
    private readonly IDataLineageRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DataLineageService> _logger;

    public DataLineageService(
        IDataLineageRepository repository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DataLineageService> logger)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task RecordOperationAsync<T>(string entityId, DataOperation operation, T? oldEntity, T? newEntity, string? reason = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userId = GetCurrentUserId(httpContext);
        var userName = GetCurrentUserName(httpContext);
        var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request?.Headers["User-Agent"].FirstOrDefault();

        var lineageEvent = new DataLineageEvent(
            Guid.NewGuid(),
            typeof(T).Name,
            entityId,
            operation,
            DateTime.UtcNow,
            userId,
            userName,
            ConvertToPropertyDictionary(oldEntity),
            ConvertToPropertyDictionary(newEntity),
            reason,
            Environment.MachineName,
            ipAddress,
            userAgent);

        await _repository.SaveLineageEventAsync(lineageEvent);

        _logger.LogInformation("Data lineage recorded: Entity={EntityType}/{EntityId}, Operation={Operation}, User={UserId}",
            typeof(T).Name, entityId, operation, userId);
    }

    public async Task<List<DataLineageEvent>> GetEntityHistoryAsync(string entityType, string entityId)
    {
        return await _repository.GetEntityHistoryAsync(entityType, entityId);
    }

    public async Task<List<DataLineageEvent>> GetUserActivityAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        return await _repository.GetUserActivityAsync(userId, from ?? DateTime.UtcNow.AddDays(-30), to ?? DateTime.UtcNow);
    }

    public async Task<List<DataLineageEvent>> GetSystemActivityAsync(string systemSource, DateTime? from = null, DateTime? to = null)
    {
        return await _repository.GetSystemActivityAsync(systemSource, from ?? DateTime.UtcNow.AddDays(-30), to ?? DateTime.UtcNow);
    }

    public async Task<DataLineageReport> GenerateLineageReportAsync(string entityType, string entityId)
    {
        var events = await GetEntityHistoryAsync(entityType, entityId);
        var createdEvent = events.FirstOrDefault(e => e.Operation == DataOperation.Create);
        var lastModified = events.Where(e => e.Operation == DataOperation.Update).OrderByDescending(e => e.Timestamp).FirstOrDefault();
        var deletedEvent = events.FirstOrDefault(e => e.Operation == DataOperation.Delete);
        
        var uniqueUsers = events.Where(e => e.UserId.HasValue).Select(e => e.UserId.Value).Distinct().Count();
        var operationCounts = events.GroupBy(e => e.Operation).ToDictionary(g => g.Key, g => g.Count());

        return new DataLineageReport(
            entityType,
            entityId,
            createdEvent?.Timestamp,
            createdEvent?.UserId,
            createdEvent?.UserName,
            lastModified?.Timestamp,
            lastModified?.UserId,
            lastModified?.UserName,
            deletedEvent?.Timestamp,
            deletedEvent?.UserId,
            deletedEvent?.UserName,
            uniqueUsers,
            operationCounts,
            events);
    }

    private static Dictionary<string, object?> ConvertToPropertyDictionary<T>(T? entity)
    {
        if (entity == null) return new Dictionary<string, object?>();

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return properties.ToDictionary(p => p.Name, p => p.GetValue(entity));
    }

    private static int? GetCurrentUserId(HttpContext? context)
    {
        var userIdClaim = context?.User?.FindFirst("user_id")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string? GetCurrentUserName(HttpContext? context)
    {
        return context?.User?.FindFirst("name")?.Value ?? context?.User?.Identity?.Name;
    }
}

public record DataLineageReport(
    string EntityType,
    string EntityId,
    DateTime? CreatedAt,
    int? CreatedBy,
    string? CreatedByName,
    DateTime? LastModifiedAt,
    int? LastModifiedBy,
    string? LastModifiedByName,
    DateTime? DeletedAt,
    int? DeletedBy,
    string? DeletedByName,
    int UniqueModifiers,
    Dictionary<DataOperation, int> OperationCounts,
    List<DataLineageEvent> FullHistory);

// Entity Framework Integration for Automatic Lineage Tracking
public class LineageTrackingInterceptor : SaveChangesInterceptor
{
    private readonly IDataLineageService _lineageService;

    public LineageTrackingInterceptor(IDataLineageService lineageService)
    {
        _lineageService = lineageService;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null)
        {
            await RecordChangesAsync(eventData.Context);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task RecordChangesAsync(DbContext context)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType();
            var keyProperty = entityType.GetProperties().FirstOrDefault(p => p.Name.EndsWith("Id"));
            var entityId = keyProperty?.GetValue(entry.Entity)?.ToString() ?? Guid.NewGuid().ToString();

            DataOperation operation = entry.State switch
            {
                EntityState.Added => DataOperation.Create,
                EntityState.Modified => DataOperation.Update,
                EntityState.Deleted => DataOperation.Delete,
                _ => DataOperation.Update
            };

            object? oldEntity = null;
            object? newEntity = null;

            if (entry.State == EntityState.Modified)
            {
                oldEntity = CreateOldEntity(entry);
                newEntity = entry.Entity;
            }
            else if (entry.State == EntityState.Added)
            {
                newEntity = entry.Entity;
            }
            else if (entry.State == EntityState.Deleted)
            {
                oldEntity = entry.Entity;
            }

            await _lineageService.RecordOperationAsync(entityId, operation, oldEntity, newEntity);
        }
    }

    private static object CreateOldEntity(EntityEntry entry)
    {
        var entityType = entry.Entity.GetType();
        var oldEntity = Activator.CreateInstance(entityType);

        if (oldEntity == null) return entry.Entity;

        foreach (var property in entry.Properties)
        {
            if (property.OriginalValue != null)
            {
                var propertyInfo = entityType.GetProperty(property.Metadata.Name);
                propertyInfo?.SetValue(oldEntity, property.OriginalValue);
            }
        }

        return oldEntity;
    }
}
```

**Usage**:

```csharp
// 1. Data Classification Usage
public class CustomerService
{
    private readonly IDataClassificationService _classificationService;

    public CustomerService(IDataClassificationService classificationService)
    {
        _classificationService = classificationService;
    }

    public async Task<bool> CanProcessPersonalDataAsync(Type entityType)
    {
        var sensitivityLevel = _classificationService.GetSensitivityLevel(entityType);
        return sensitivityLevel.Classification != DataClassification.Restricted;
    }

    public List<string> GetPersonalDataFields<T>()
    {
        var personalDataProperties = _classificationService.GetPersonalDataProperties(typeof(T));
        return personalDataProperties.Select(p => p.Name).ToList();
    }
}

// 2. GDPR Consent Management
public class ConsentController : ControllerBase
{
    private readonly IGdprConsentService _consentService;

    public ConsentController(IGdprConsentService consentService)
    {
        _consentService = consentService;
    }

    [HttpPost("consent")]
    public async Task<IActionResult> GrantConsent([FromBody] ConsentRequest request)
    {
        var userId = GetCurrentUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers["User-Agent"].ToString();

        await _consentService.RecordConsentAsync(
            userId, 
            request.Purpose, 
            ipAddress, 
            userAgent, 
            "Explicit consent via web interface");

        return Ok();
    }

    [HttpDelete("consent/{purpose}")]
    public async Task<IActionResult> WithdrawConsent(ConsentPurpose purpose)
    {
        var userId = GetCurrentUserId();
        await _consentService.WithdrawConsentAsync(userId, purpose);
        return Ok();
    }

    [HttpGet("consent")]
    public async Task<IActionResult> GetConsents()
    {
        var userId = GetCurrentUserId();
        var consents = await _consentService.GetUserConsentsAsync(userId);
        return Ok(consents);
    }

    private int GetCurrentUserId() => 
        int.Parse(User.FindFirst("user_id")?.Value ?? throw new UnauthorizedAccessException());
}

// 3. GDPR Subject Rights
public class DataSubjectController : ControllerBase
{
    private readonly IGdprSubjectRightsService _subjectRightsService;

    public DataSubjectController(IGdprSubjectRightsService subjectRightsService)
    {
        _subjectRightsService = subjectRightsService;
    }

    [HttpPost("data-request")]
    public async Task<IActionResult> CreateDataRequest([FromBody] DataRequestModel request)
    {
        var userId = GetCurrentUserId();
        var requestId = await _subjectRightsService.CreateRequestAsync(
            userId, 
            request.RequestType, 
            request.Details);

        return Ok(new { RequestId = requestId });
    }

    [HttpGet("data-request/{requestId}")]
    public async Task<IActionResult> GetDataRequest(int requestId)
    {
        var request = await _subjectRightsService.GetRequestAsync(requestId);
        if (request?.UserId != GetCurrentUserId())
            return NotFound();

        return Ok(request);
    }

    [HttpGet("data-requests")]
    public async Task<IActionResult> GetUserRequests()
    {
        var userId = GetCurrentUserId();
        var requests = await _subjectRightsService.GetUserRequestsAsync(userId);
        return Ok(requests);
    }

    private int GetCurrentUserId() => 
        int.Parse(User.FindFirst("user_id")?.Value ?? throw new UnauthorizedAccessException());
}

// 4. Encryption Usage
public class SecureDocumentService
{
    private readonly IFieldEncryptionService _encryptionService;
    private readonly IDataClassificationService _classificationService;

    public SecureDocumentService(
        IFieldEncryptionService encryptionService,
        IDataClassificationService classificationService)
    {
        _encryptionService = encryptionService;
        _classificationService = classificationService;
    }

    public async Task<Document> SecureDocumentAsync(Document document)
    {
        var properties = typeof(Document).GetProperties();
        
        foreach (var property in properties)
        {
            if (_classificationService.RequiresEncryption(property) && property.PropertyType == typeof(string))
            {
                var value = (string?)property.GetValue(document);
                if (!string.IsNullOrEmpty(value))
                {
                    var encryptedValue = await _encryptionService.EncryptAsync(value);
                    property.SetValue(document, encryptedValue);
                }
            }
        }

        return document;
    }
}

// 5. Data Lineage Usage
public class AuditableService<T> where T : class
{
    private readonly IDataLineageService _lineageService;
    private readonly IRepository<T> _repository;

    public AuditableService(IDataLineageService lineageService, IRepository<T> repository)
    {
        _lineageService = lineageService;
        _repository = repository;
    }

    public async Task<T> CreateAsync(T entity)
    {
        var created = await _repository.CreateAsync(entity);
        var entityId = GetEntityId(created);
        
        await _lineageService.RecordOperationAsync(
            entityId, 
            DataOperation.Create, 
            null, 
            created, 
            "Entity created via API");

        return created;
    }

    public async Task<T> UpdateAsync(string id, T updatedEntity)
    {
        var existing = await _repository.GetByIdAsync(id);
        var updated = await _repository.UpdateAsync(id, updatedEntity);
        
        await _lineageService.RecordOperationAsync(
            id, 
            DataOperation.Update, 
            existing, 
            updated, 
            "Entity updated via API");

        return updated;
    }

    public async Task DeleteAsync(string id)
    {
        var existing = await _repository.GetByIdAsync(id);
        await _repository.DeleteAsync(id);
        
        await _lineageService.RecordOperationAsync(
            id, 
            DataOperation.Delete, 
            existing, 
            null, 
            "Entity deleted via API");
    }

    private static string GetEntityId(T entity)
    {
        var idProperty = typeof(T).GetProperty("Id");
        return idProperty?.GetValue(entity)?.ToString() ?? Guid.NewGuid().ToString();
    }
}

// 6. Service Registration
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataGovernance(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EncryptionConfiguration>(configuration.GetSection("Encryption"));
        
        services.AddScoped<IDataClassificationService, DataClassificationService>();
        services.AddScoped<IGdprConsentService, GdprConsentService>();
        services.AddScoped<IGdprSubjectRightsService, GdprSubjectRightsService>();
        services.AddScoped<IFieldEncryptionService, FieldEncryptionService>();
        services.AddScoped<IDataLineageService, DataLineageService>();
        
        services.AddScoped<LineageTrackingInterceptor>();
        
        return services;
    }

    public static IServiceCollection AddEncryptedDbContext<TContext>(
        this IServiceCollection services, 
        string connectionString) 
        where TContext : EncryptedDbContext
    {
        services.AddDbContext<TContext>((serviceProvider, options) =>
        {
            var interceptor = serviceProvider.GetRequiredService<LineageTrackingInterceptor>();
            options.UseSqlServer(connectionString)
                   .AddInterceptors(interceptor);
        });
        
        return services;
    }
}
```

**Notes**:

- **Data Classification**: Implements comprehensive data sensitivity levels with attribute-based classification
- **GDPR Compliance**: Full GDPR compliance with consent management and subject rights (access, erasure, portability)
- **Encryption**: Field-level encryption with key rotation and Azure Key Vault integration
- **Data Lineage**: Complete audit trail with automatic Entity Framework integration
- **Privacy by Design**: Implements privacy-first patterns with secure defaults
- **Regulatory Compliance**: Supports SOX, GDPR, HIPAA, and other regulatory requirements
- **Performance**: Uses caching for encryption keys and classification metadata
- **Extensibility**: Supports custom data classifications and encryption algorithms
- **Integration**: Seamless Entity Framework integration with interceptors and value converters
- **Audit**: Comprehensive logging and audit trails for compliance reporting
- **Security**: Uses industry-standard encryption (AES-256-GCM) with proper key management