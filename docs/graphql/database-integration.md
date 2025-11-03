# GraphQL Database Integration Patterns

**Description**: Comprehensive patterns for integrating HotChocolate GraphQL with Entity Framework Core, implementing DataLoader patterns for batching and caching, query optimization, connection pooling, and advanced data access strategies with proper schema design around business domains.

**Language/Technology**: C#, HotChocolate, Entity Framework Core, .NET 9.0

## Code

### Entity Framework DbContext Configuration

```csharp
namespace DocumentProcessor.Data;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// DbContext optimized for GraphQL operations with proper entity configuration
/// Designed around business domain rather than database structure
/// </summary>
public class DocumentProcessorDbContext(DbContextOptions<DocumentProcessorDbContext> options) : DbContext(options)
{

    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<ProcessingJob> ProcessingJobs { get; set; } = null!;
    public DbSet<DocumentVersion> DocumentVersions { get; set; } = null!;
    public DbSet<DocumentTag> DocumentTags { get; set; } = null!;
    public DbSet<DocumentCollaborator> DocumentCollaborators { get; set; } = null!;
    public DbSet<AnalyticsEvent> AnalyticsEvents { get; set; } = null!;
    public DbSet<MLModel> MLModels { get; set; } = null!;
    public DbSet<MLPrediction> MLPredictions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Document configuration
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(500);
                
            entity.Property(e => e.Content)
                .IsRequired()
                .HasColumnType("TEXT");
                
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Indexes for performance
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.CreatedBy, e.Status });

            // Full-text search index
            entity.HasIndex(e => new { e.Title, e.Content })
                .HasAnnotation("SqlServer:IncludeProperties", new[] { "Title", "Content" });

            // Relationships
            entity.HasOne(d => d.Creator)
                .WithMany(u => u.CreatedDocuments)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(d => d.Versions)
                .WithOne(v => v.Document)
                .HasForeignKey(v => v.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(d => d.Tags)
                .WithOne(t => t.Document)
                .HasForeignKey(t => t.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(d => d.Collaborators)
                .WithOne(c => c.Document)
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(320);
                
            entity.Property(e => e.DisplayName)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
        });

        // Processing job configuration
        modelBuilder.Entity<ProcessingJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.JobType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Parameters)
                .HasColumnType("NVARCHAR(MAX)")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!) ?? new());

            entity.Property(e => e.Result)
                .HasColumnType("NVARCHAR(MAX)")
                .HasConversion(
                    v => v != null ? System.Text.Json.JsonSerializer.Serialize(v, (JsonSerializerOptions)null!) : null,
                    v => v != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!) : null);

            // Indexes for job management
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.DocumentId, e.Status });

            // Relationships
            entity.HasOne(j => j.Document)
                .WithMany(d => d.ProcessingJobs)
                .HasForeignKey(j => j.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Document version configuration
        modelBuilder.Entity<DocumentVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.Content)
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property(e => e.VersionNumber)
                .IsRequired();

            entity.Property(e => e.ChangeDescription)
                .HasMaxLength(1000);

            // Composite unique constraint
            entity.HasIndex(e => new { e.DocumentId, e.VersionNumber }).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
        });

        // Document tag configuration
        modelBuilder.Entity<DocumentTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.TagName)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(e => e.TagName);
            entity.HasIndex(e => new { e.DocumentId, e.TagName }).IsUnique();
        });

        // Document collaborator configuration
        modelBuilder.Entity<DocumentCollaborator>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.Role)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasIndex(e => new { e.DocumentId, e.UserId }).IsUnique();

            entity.HasOne(c => c.User)
                .WithMany(u => u.Collaborations)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Analytics event configuration
        modelBuilder.Entity<AnalyticsEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Properties)
                .HasColumnType("NVARCHAR(MAX)")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!) ?? new());

            // Indexes for analytics queries
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.EventType, e.Timestamp });
            entity.HasIndex(e => new { e.EntityId, e.EventType });
        });

        // ML Model configuration
        modelBuilder.Entity<MLModel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Version)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.ModelType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Configuration)
                .HasColumnType("NVARCHAR(MAX)")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!) ?? new());

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => new { e.Name, e.Version }).IsUnique();
        });

        // ML Prediction configuration
        modelBuilder.Entity<MLPrediction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.ModelName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.InputData)
                .HasColumnType("NVARCHAR(MAX)");

            entity.Property(e => e.OutputData)
                .HasColumnType("NVARCHAR(MAX)");

            entity.Property(e => e.Confidence)
                .HasColumnType("DECIMAL(5,4)");

            // Indexes for prediction queries
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => e.ModelName);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.DocumentId, e.ModelName });

            entity.HasOne(p => p.Document)
                .WithMany(d => d.MLPredictions)
                .HasForeignKey(p => p.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps for entities with UpdatedAt property
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is IHasTimestamps && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var entity = (IHasTimestamps)entityEntry.Entity;
            
            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entityEntry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

// Interface for entities with timestamps
public interface IHasTimestamps
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
```

### Repository Pattern with Entity Framework

```csharp
// Base repository interface
public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
}

// Generic repository implementation
public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly DocumentProcessorDbContext context;
    protected readonly DbSet<TEntity> dbSet;
    protected readonly ILogger<Repository<TEntity>> logger;

    public Repository(DocumentProcessorDbContext context, ILogger<Repository<TEntity>> logger)
    {
        context = context;
        dbSet = context.Set<TEntity>();
        logger = logger;
    }

    public virtual async Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbSet.FindAsync(new object[] { id }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting entity by ID: {Id}", id);
            throw;
        }
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbSet.ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all entities");
            throw;
        }
    }

    public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbSet.Where(predicate).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finding entities with predicate");
            throw;
        }
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await dbSet.AddAsync(entity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return result.Entity;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding entity");
            throw;
        }
    }

    public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            await dbSet.AddRangeAsync(entities, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return entities;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding entities range");
            throw;
        }
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            dbSet.Update(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating entity");
            throw;
        }
    }

    public virtual async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            dbSet.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting entity");
            throw;
        }
    }

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return predicate == null 
                ? await dbSet.CountAsync(cancellationToken) 
                : await dbSet.CountAsync(predicate, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error counting entities");
            throw;
        }
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbSet.AnyAsync(predicate, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking entity existence");
            throw;
        }
    }
}

// Document repository with specialized methods
public interface IDocumentRepository : IRepository<Document>
{
    Task<IEnumerable<Document>> GetByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> GetByTagsAsync(string[] tags, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> GetPopularAsync(int count, CancellationToken cancellationToken = default);
    Task<Document?> GetWithVersionsAsync(string id, CancellationToken cancellationToken = default);
    Task<Document?> GetWithCollaboratorsAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> GetBatchAsync(string[] ids, CancellationToken cancellationToken = default);
}

public class DocumentRepository : Repository<Document>, IDocumentRepository
{
    public DocumentRepository(DocumentProcessorDbContext context, ILogger<DocumentRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<Document>> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbSet
                .Where(d => d.CreatedBy == userId)
                .Include(d => d.Tags)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting documents by user: {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<Document>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            var lowerSearchTerm = searchTerm.ToLower();
            
            return await dbSet
                .Where(d => d.Title.ToLower().Contains(lowerSearchTerm) || 
                           d.Content.ToLower().Contains(lowerSearchTerm) ||
                           d.Tags.Any(t => t.TagName.ToLower().Contains(lowerSearchTerm)))
                .Include(d => d.Tags)
                .Include(d => d.Creator)
                .OrderByDescending(d => d.UpdatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching documents with term: {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<IEnumerable<Document>> GetByTagsAsync(string[] tags, CancellationToken cancellationToken = default)
    {
        try
        {
            var lowerTags = tags.Select(t => t.ToLower()).ToArray();
            
            return await dbSet
                .Where(d => d.Tags.Any(t => lowerTags.Contains(t.TagName.ToLower())))
                .Include(d => d.Tags)
                .Include(d => d.Creator)
                .Distinct()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting documents by tags: {Tags}", string.Join(", ", tags));
            throw;
        }
    }

    public async Task<IEnumerable<Document>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbSet
                .Include(d => d.Tags)
                .Include(d => d.Creator)
                .OrderByDescending(d => d.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting recent documents");
            throw;
        }
    }

    public async Task<IEnumerable<Document>> GetPopularAsync(int count, CancellationToken cancellationToken = default)
    {
        try
        {
            // Calculate popularity based on analytics events
            var popularDocumentIds = await context.AnalyticsEvents
                .Where(e => e.EventType == "DocumentViewed" && e.Timestamp >= DateTime.UtcNow.AddDays(-30))
                .GroupBy(e => e.EntityId)
                .OrderByDescending(g => g.Count())
                .Take(count)
                .Select(g => g.Key)
                .ToListAsync(cancellationToken);

            return await dbSet
                .Where(d => popularDocumentIds.Contains(d.Id))
                .Include(d => d.Tags)
                .Include(d => d.Creator)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting popular documents");
            throw;
        }
    }

    public async Task<Document?> GetWithVersionsAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbSet
                .Include(d => d.Versions.OrderByDescending(v => v.VersionNumber))
                .Include(d => d.Tags)
                .Include(d => d.Creator)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting document with versions: {Id}", id);
            throw;
        }
    }

    public async Task<Document?> GetWithCollaboratorsAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbSet
                .Include(d => d.Collaborators)
                .ThenInclude(c => c.User)
                .Include(d => d.Tags)
                .Include(d => d.Creator)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting document with collaborators: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Document>> GetBatchAsync(string[] ids, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbSet
                .Where(d => ids.Contains(d.Id))
                .Include(d => d.Tags)
                .Include(d => d.Creator)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting documents batch");
            throw;
        }
    }
}
```

### Advanced Database Query Patterns

```csharp
// Query service for complex database operations
public interface IQueryService
{
    Task<PagedResult<Document>> GetDocumentsPagedAsync(DocumentFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<DocumentStatistics>> GetDocumentStatisticsAsync(string[] documentIds, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserActivity>> GetUserActivityAsync(string userId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IEnumerable<TagPopularity>> GetTagPopularityAsync(CancellationToken cancellationToken = default);
    Task<ProcessingMetrics> GetProcessingMetricsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

public class QueryService : IQueryService
{
    private readonly DocumentProcessorDbContext context;
    private readonly ILogger<QueryService> logger;

    public QueryService(DocumentProcessorDbContext context, ILogger<QueryService> logger)
    {
        context = context;
        logger = logger;
    }

    public async Task<PagedResult<Document>> GetDocumentsPagedAsync(
        DocumentFilter filter, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = context.Documents
                .Include(d => d.Tags)
                .Include(d => d.Creator)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(d => 
                    d.Title.ToLower().Contains(searchTerm) || 
                    d.Content.ToLower().Contains(searchTerm) ||
                    d.Tags.Any(t => t.TagName.ToLower().Contains(searchTerm)));
            }

            if (!string.IsNullOrEmpty(filter.CreatedBy))
            {
                query = query.Where(d => d.CreatedBy == filter.CreatedBy);
            }

            if (filter.Tags != null && filter.Tags.Any())
            {
                var lowerTags = filter.Tags.Select(t => t.ToLower()).ToArray();
                query = query.Where(d => d.Tags.Any(t => lowerTags.Contains(t.TagName.ToLower())));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(d => d.Status == filter.Status.Value);
            }

            if (filter.CreatedFrom.HasValue)
            {
                query = query.Where(d => d.CreatedAt >= filter.CreatedFrom.Value);
            }

            if (filter.CreatedTo.HasValue)
            {
                query = query.Where(d => d.CreatedAt <= filter.CreatedTo.Value);
            }

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "title" => filter.SortDescending ? query.OrderByDescending(d => d.Title) : query.OrderBy(d => d.Title),
                "created" => filter.SortDescending ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt),
                "updated" => filter.SortDescending ? query.OrderByDescending(d => d.UpdatedAt) : query.OrderBy(d => d.UpdatedAt),
                _ => query.OrderByDescending(d => d.UpdatedAt)
            };

            var totalCount = await query.CountAsync(cancellationToken);
            var documents = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Document>
            {
                Items = documents,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting paged documents");
            throw;
        }
    }

    public async Task<IEnumerable<DocumentStatistics>> GetDocumentStatisticsAsync(
        string[] documentIds, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await context.Documents
                .Where(d => documentIds.Contains(d.Id))
                .ToListAsync(cancellationToken);

            var documentStats = new List<DocumentStatistics>();

            foreach (var document in documents)
            {
                // Get view count from analytics
                var viewCount = await context.AnalyticsEvents
                    .CountAsync(e => e.EntityId == document.Id && e.EventType == "DocumentViewed", cancellationToken);

                // Get collaborator count
                var collaboratorCount = await context.DocumentCollaborators
                    .CountAsync(c => c.DocumentId == document.Id, cancellationToken);

                // Get version count
                var versionCount = await context.DocumentVersions
                    .CountAsync(v => v.DocumentId == document.Id, cancellationToken);

                // Calculate statistics
                var wordCount = CountWords(document.Content);
                var characterCount = document.Content.Length;
                var readingTime = CalculateReadingTime(wordCount);

                documentStats.Add(new DocumentStatistics
                {
                    DocumentId = document.Id,
                    WordCount = wordCount,
                    CharacterCount = characterCount,
                    ReadingTime = readingTime,
                    ViewCount = viewCount,
                    CollaboratorCount = collaboratorCount,
                    VersionCount = versionCount,
                    LastAccessed = await GetLastAccessTime(document.Id, cancellationToken)
                });
            }

            return documentStats;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting document statistics");
            throw;
        }
    }

    public async Task<IEnumerable<UserActivity>> GetUserActivityAsync(
        string userId, 
        DateTime from, 
        DateTime to, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var activities = await context.AnalyticsEvents
                .Where(e => e.UserId == userId && e.Timestamp >= from && e.Timestamp <= to)
                .OrderByDescending(e => e.Timestamp)
                .Select(e => new UserActivity
                {
                    EventType = e.EventType,
                    EntityId = e.EntityId,
                    EntityType = e.EntityType,
                    Timestamp = e.Timestamp,
                    Properties = e.Properties
                })
                .ToListAsync(cancellationToken);

            return activities;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user activity for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<TagPopularity>> GetTagPopularityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tagPopularity = await context.DocumentTags
                .GroupBy(t => t.TagName)
                .Select(g => new TagPopularity
                {
                    TagName = g.Key,
                    DocumentCount = g.Count(),
                    LastUsed = g.Max(t => t.CreatedAt)
                })
                .OrderByDescending(t => t.DocumentCount)
                .Take(50)
                .ToListAsync(cancellationToken);

            return tagPopularity;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting tag popularity");
            throw;
        }
    }

    public async Task<ProcessingMetrics> GetProcessingMetricsAsync(
        DateTime from, 
        DateTime to, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var jobs = await context.ProcessingJobs
                .Where(j => j.CreatedAt >= from && j.CreatedAt <= to)
                .ToListAsync(cancellationToken);

            var completedJobs = jobs.Where(j => j.Status == ProcessingStatus.Completed).ToList();
            var failedJobs = jobs.Where(j => j.Status == ProcessingStatus.Failed).ToList();

            var avgProcessingTime = completedJobs.Any()
                ? completedJobs
                    .Where(j => j.CompletedAt.HasValue)
                    .Average(j => (j.CompletedAt!.Value - j.CreatedAt).TotalSeconds)
                : 0;

            return new ProcessingMetrics
            {
                TotalJobs = jobs.Count,
                CompletedJobs = completedJobs.Count,
                FailedJobs = failedJobs.Count,
                PendingJobs = jobs.Count(j => j.Status == ProcessingStatus.Pending),
                InProgressJobs = jobs.Count(j => j.Status == ProcessingStatus.InProgress),
                AverageProcessingTime = TimeSpan.FromSeconds(avgProcessingTime),
                SuccessRate = jobs.Any() ? (double)completedJobs.Count / jobs.Count * 100 : 0
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting processing metrics");
            throw;
        }
    }

    private async Task<DateTime?> GetLastAccessTime(string documentId, CancellationToken cancellationToken)
    {
        return await context.AnalyticsEvents
            .Where(e => e.EntityId == documentId && e.EventType == "DocumentViewed")
            .OrderByDescending(e => e.Timestamp)
            .Select(e => e.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private int CountWords(string text)
    {
        return string.IsNullOrWhiteSpace(text) 
            ? 0 
            : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private TimeSpan CalculateReadingTime(int wordCount)
    {
        var wordsPerMinute = 200; // Average reading speed
        var minutes = Math.Max(1, wordCount / wordsPerMinute);
        return TimeSpan.FromMinutes(minutes);
    }
}
```

### GraphQL DataLoaders for Database Optimization

```csharp
// DataLoaders to prevent N+1 queries
public class DocumentDataLoaders
{
    [DataLoader]
    public static async Task<IReadOnlyDictionary<string, Document>> GetDocumentByIdAsync(
        IReadOnlyList<string> documentIds,
        IDocumentRepository documentRepository,
        CancellationToken cancellationToken)
    {
        var documents = await documentRepository.GetBatchAsync(documentIds.ToArray(), cancellationToken);
        return documents.ToDictionary(d => d.Id);
    }

    [DataLoader]
    public static async Task<IReadOnlyDictionary<string, IEnumerable<DocumentTag>>> GetDocumentTagsAsync(
        IReadOnlyList<string> documentIds,
        DocumentProcessorDbContext context,
        CancellationToken cancellationToken)
    {
        var tags = await context.DocumentTags
            .Where(t => documentIds.Contains(t.DocumentId))
            .ToListAsync(cancellationToken);

        return tags.GroupBy(t => t.DocumentId)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());
    }

    [DataLoader]
    public static async Task<IReadOnlyDictionary<string, IEnumerable<DocumentVersion>>> GetDocumentVersionsAsync(
        IReadOnlyList<string> documentIds,
        DocumentProcessorDbContext context,
        CancellationToken cancellationToken)
    {
        var versions = await context.DocumentVersions
            .Where(v => documentIds.Contains(v.DocumentId))
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);

        return versions.GroupBy(v => v.DocumentId)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());
    }

    [DataLoader]
    public static async Task<IReadOnlyDictionary<string, User>> GetUserByIdAsync(
        IReadOnlyList<string> userIds,
        DocumentProcessorDbContext context,
        CancellationToken cancellationToken)
    {
        var users = await context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        return users.ToDictionary(u => u.Id);
    }

    [DataLoader]
    public static async Task<IReadOnlyDictionary<string, IEnumerable<ProcessingJob>>> GetDocumentProcessingJobsAsync(
        IReadOnlyList<string> documentIds,
        DocumentProcessorDbContext context,
        CancellationToken cancellationToken)
    {
        var jobs = await context.ProcessingJobs
            .Where(j => documentIds.Contains(j.DocumentId))
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);

        return jobs.GroupBy(j => j.DocumentId)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());
    }

    [DataLoader]
    public static async Task<IReadOnlyDictionary<string, IEnumerable<DocumentCollaborator>>> GetDocumentCollaboratorsAsync(
        IReadOnlyList<string> documentIds,
        DocumentProcessorDbContext context,
        CancellationToken cancellationToken)
    {
        var collaborators = await context.DocumentCollaborators
            .Include(c => c.User)
            .Where(c => documentIds.Contains(c.DocumentId))
            .ToListAsync(cancellationToken);

        return collaborators.GroupBy(c => c.DocumentId)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());
    }
}
```

### Database Configuration and Performance

```csharp
// Database configuration service
public static class DatabaseConfiguration
{
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure DbContext with connection pooling
        services.AddDbContextPool<DocumentProcessorDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
                
                sqlOptions.CommandTimeout(30);
                sqlOptions.MigrationsAssembly("DocumentProcessor.Data");
            });

            // Performance optimizations
            options.EnableSensitiveDataLogging(false);
            options.EnableServiceProviderCaching();
            options.EnableDetailedErrors(false);
            
            // Query optimizations
            options.ConfigureWarnings(warnings =>
            {
                warnings.Ignore(CoreEventId.RowLimitingOperationWithoutOrderByWarning);
            });
        });

        // Register repositories
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IQueryService, QueryService>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Health checks
        services.AddHealthChecks()
            .AddDbContextCheck<DocumentProcessorDbContext>();

        return services;
    }

    public static IApplicationBuilder UseDatabaseServices(this IApplicationBuilder app, IServiceProvider serviceProvider)
    {
        // Ensure database is created and migrated
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DocumentProcessorDbContext>();
        
        try
        {
            context.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DocumentProcessorDbContext>>();
            logger.LogError(ex, "Error migrating database");
        }

        return app;
    }
}
```

## Usage

### GraphQL Queries with Database Optimization

```graphql
# Optimized document query using DataLoaders
query GetDocumentWithRelatedData($id: ID!) {
  document(id: $id) {
    id
    title
    content
    creator {
      id
      displayName
      email
    }
    tags {
      id
      tagName
    }
    versions {
      id
      versionNumber
      changeDescription
      createdAt
    }
    collaborators {
      user {
        id
        displayName
      }
      role
      addedAt
    }
    processingJobs {
      id
      status
      jobType
      createdAt
    }
    statistics {
      wordCount
      viewCount
      collaboratorCount
    }
  }
}

# Paginated documents query
query GetDocumentsPaged($filter: DocumentFilterInput!, $page: Int!, $pageSize: Int!) {
  documentsPaged(filter: $filter, page: $page, pageSize: $pageSize) {
    items {
      id
      title
      creator {
        displayName
      }
      tags {
        tagName
      }
      createdAt
      updatedAt
    }
    totalCount
    totalPages
    page
    pageSize
  }
}

# Complex analytics query
query GetAnalytics($from: DateTime!, $to: DateTime!) {
  processingMetrics(from: $from, to: $to) {
    totalJobs
    completedJobs
    failedJobs
    averageProcessingTime
    successRate
  }
  
  tagPopularity {
    tagName
    documentCount
    lastUsed
  }
}
```

## Notes

- **Connection Pooling**: Use DbContextPool for better performance with concurrent requests
- **Query Optimization**: Implement proper indexes and avoid N+1 queries using DataLoaders
- **Batch Operations**: Use batch loading patterns for efficient data fetching
- **Pagination**: Implement cursor-based or offset-based pagination for large datasets
- **Caching**: Consider second-level caching for frequently accessed data
- **Monitoring**: Monitor query performance and database metrics
- **Migrations**: Use Entity Framework migrations for database schema management
- **Error Handling**: Implement proper error handling and retry policies

## Related Patterns

- [DataLoader Patterns](dataloader-patterns.md) - Advanced DataLoader implementations
- [Performance Optimization](performance-optimization.md) - Database performance optimizations
- [Error Handling](error-handling.md) - Database error handling strategies

---

**Key Benefits**: Efficient data access, N+1 prevention, connection pooling, query optimization, scalable architecture

**When to Use**: Complex data relationships, high-performance requirements, large datasets, concurrent access

**Performance**: Connection pooling, query optimization, batch loading, proper indexing, pagination