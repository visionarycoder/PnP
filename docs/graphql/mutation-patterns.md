# GraphQL Mutation Patterns

**Description**: Comprehensive GraphQL mutation patterns for document processing operations, batch processing, and ML workflow management using HotChocolate.

**Language/Technology**: C# / HotChocolate

## Code

### Document Operations

```csharp
namespace DocumentProcessor.GraphQL.Mutations;

using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;

[MutationType]
public class DocumentMutations
{
    // Single document creation with validation
    [Authorize(Policy = "CanCreateDocuments")]
    public async Task<CreateDocumentPayload> CreateDocumentAsync(
        CreateDocumentInput input,
        [Service] IDocumentService documentService,
        [Service] IValidator<CreateDocumentInput> validator,
        [Service] IClusterClient orleans,
        CancellationToken cancellationToken)
    {
        // Validate input
        var validationResult = await validator.ValidateAsync(input, cancellationToken);
        if (!validationResult.IsValid)
        {
            return CreateDocumentPayload.FromErrors(validationResult.Errors
                .Select(e => new UserError(e.ErrorMessage, e.ErrorCode)));
        }

        try
        {
            // Create document
            var document = await documentService.CreateAsync(input, cancellationToken);

            // Trigger asynchronous processing
            var processingGrain = orleans.GetGrain<IDocumentProcessorGrain>(document.Id);
            _ = Task.Run(async () =>
            {
                await processingGrain.ProcessDocumentAsync(new ProcessingRequest
                {
                    DocumentId = document.Id,
                    Content = document.Content,
                    Options = input.ProcessingOptions ?? new ProcessingOptions(),
                    Priority = input.Priority ?? ProcessingPriority.Normal
                });
            }, cancellationToken);

            return new CreateDocumentPayload(document);
        }
        catch (Exception ex)
        {
            return CreateDocumentPayload.FromError(ex.Message, "CREATION_FAILED");
        }
    }

    // Batch document upload with progress tracking
    [Authorize(Policy = "CanCreateDocuments")]
    public async Task<BatchUploadPayload> UploadDocumentBatchAsync(
        BatchUploadInput input,
        [Service] IBatchProcessingService batchService,
        [Service] ITopicEventSender eventSender,
        CancellationToken cancellationToken)
    {
        try
        {
            var batchId = Guid.NewGuid().ToString();
            var batch = await batchService.CreateBatchAsync(batchId, input, cancellationToken);

            // Start background processing
            _ = Task.Run(async () =>
            {
                await ProcessBatchWithProgress(batch, eventSender, cancellationToken);
            }, cancellationToken);

            return new BatchUploadPayload(batch);
        }
        catch (Exception ex)
        {
            return BatchUploadPayload.FromError(ex.Message, "BATCH_UPLOAD_FAILED");
        }
    }

    // Document update with optimistic concurrency
    [Authorize(Policy = "CanEditDocuments")]
    public async Task<UpdateDocumentPayload> UpdateDocumentAsync(
        UpdateDocumentInput input,
        [Service] IDocumentService documentService,
        CancellationToken cancellationToken)
    {
        try
        {
            var document = await documentService.UpdateAsync(input, cancellationToken);
            return new UpdateDocumentPayload(document);
        }
        catch (OptimisticConcurrencyException)
        {
            return UpdateDocumentPayload.FromError(
                "Document was modified by another user. Please refresh and try again.",
                "OPTIMISTIC_CONCURRENCY_CONFLICT");
        }
        catch (Exception ex)
        {
            return UpdateDocumentPayload.FromError(ex.Message, "UPDATE_FAILED");
        }
    }

    // Document deletion with cascade options
    [Authorize(Policy = "CanDeleteDocuments")]
    public async Task<DeleteDocumentPayload> DeleteDocumentAsync(
        DeleteDocumentInput input,
        [Service] IDocumentService documentService,
        [Service] IClusterClient orleans,
        CancellationToken cancellationToken)
    {
        try
        {
            // Cancel any active processing
            if (input.CancelProcessing)
            {
                var processingGrain = orleans.GetGrain<IDocumentProcessorGrain>(input.DocumentId);
                await processingGrain.CancelProcessingAsync();
            }

            await documentService.DeleteAsync(input, cancellationToken);
            return new DeleteDocumentPayload(input.DocumentId);
        }
        catch (Exception ex)
        {
            return DeleteDocumentPayload.FromError(ex.Message, "DELETION_FAILED");
        }
    }
}
```

### Processing Control Mutations

```csharp
[ExtendObjectType<Mutation>]
public class ProcessingMutations
{
    // Start processing with custom options
    [Authorize(Policy = "CanProcessDocuments")]
    public async Task<ProcessingPayload> StartProcessingAsync(
        StartProcessingInput input,
        [Service] IClusterClient orleans,
        CancellationToken cancellationToken)
    {
        try
        {
            var processingGrain = orleans.GetGrain<IDocumentProcessorGrain>(input.DocumentId);
            var result = await processingGrain.ProcessDocumentAsync(new ProcessingRequest
            {
                DocumentId = input.DocumentId,
                Options = input.Options,
                Priority = input.Priority,
                CallbackUrl = input.CallbackUrl
            });

            return new ProcessingPayload(result);
        }
        catch (Exception ex)
        {
            return ProcessingPayload.FromError(ex.Message, "PROCESSING_START_FAILED");
        }
    }

    // Bulk reprocessing with filtering
    [Authorize(Policy = "CanProcessDocuments")]
    public async Task<BulkProcessingPayload> ReprocessDocumentsAsync(
        BulkProcessingInput input,
        [Service] IBulkProcessingService bulkService,
        [Service] IDocumentRepository documentRepository,
        CancellationToken cancellationToken)
    {
        try
        {
            // Apply filters to get document IDs
            var query = documentRepository.GetQueryable();
            
            if (input.Filters != null)
            {
                query = ApplyFilters(query, input.Filters);
            }

            var documentIds = await query.Select(d => d.Id).ToListAsync(cancellationToken);
            
            if (documentIds.Count > input.MaxDocuments)
            {
                return BulkProcessingPayload.FromError(
                    $"Filter matches {documentIds.Count} documents, exceeds limit of {input.MaxDocuments}",
                    "TOO_MANY_DOCUMENTS");
            }

            var bulkResult = await bulkService.StartBulkProcessingAsync(
                documentIds, input.Options, cancellationToken);

            return new BulkProcessingPayload(bulkResult);
        }
        catch (Exception ex)
        {
            return BulkProcessingPayload.FromError(ex.Message, "BULK_PROCESSING_FAILED");
        }
    }

    // Cancel processing operations
    [Authorize(Policy = "CanProcessDocuments")]
    public async Task<CancelProcessingPayload> CancelProcessingAsync(
        CancelProcessingInput input,
        [Service] IClusterClient orleans,
        CancellationToken cancellationToken)
    {
        try
        {
            var results = new List<ProcessingCancellationResult>();

            foreach (var documentId in input.DocumentIds)
            {
                try
                {
                    var processingGrain = orleans.GetGrain<IDocumentProcessorGrain>(documentId);
                    await processingGrain.CancelProcessingAsync();
                    
                    results.Add(new ProcessingCancellationResult
                    {
                        DocumentId = documentId,
                        Success = true
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new ProcessingCancellationResult
                    {
                        DocumentId = documentId,
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            return new CancelProcessingPayload(results);
        }
        catch (Exception ex)
        {
            return CancelProcessingPayload.FromError(ex.Message, "CANCELLATION_FAILED");
        }
    }
}
```

### ML Model Management Mutations

```csharp
[ExtendObjectType<Mutation>]
public class ModelMutations
{
    // Train new classification model
    [Authorize(Policy = "CanManageModels")]
    public async Task<TrainModelPayload> TrainClassificationModelAsync(
        TrainClassificationInput input,
        [Service] IMLTrainingService trainingService,
        CancellationToken cancellationToken)
    {
        try
        {
            var trainingJob = await trainingService.StartTrainingAsync(input, cancellationToken);
            return new TrainModelPayload(trainingJob);
        }
        catch (Exception ex)
        {
            return TrainModelPayload.FromError(ex.Message, "TRAINING_FAILED");
        }
    }

    // Update model configuration
    [Authorize(Policy = "CanManageModels")]
    public async Task<UpdateModelPayload> UpdateModelConfigurationAsync(
        UpdateModelConfigInput input,
        [Service] IModelConfigurationService configService,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedConfig = await configService.UpdateAsync(input, cancellationToken);
            return new UpdateModelPayload(updatedConfig);
        }
        catch (Exception ex)
        {
            return UpdateModelPayload.FromError(ex.Message, "MODEL_UPDATE_FAILED");
        }
    }

    // Deploy model to production
    [Authorize(Policy = "CanDeployModels")]
    public async Task<DeployModelPayload> DeployModelAsync(
        DeployModelInput input,
        [Service] IModelDeploymentService deploymentService,
        CancellationToken cancellationToken)
    {
        try
        {
            var deployment = await deploymentService.DeployAsync(input, cancellationToken);
            return new DeployModelPayload(deployment);
        }
        catch (Exception ex)
        {
            return DeployModelPayload.FromError(ex.Message, "DEPLOYMENT_FAILED");
        }
    }
}
```

### Input Types

```csharp
// Document creation input
[InputType]
public class CreateDocumentInput
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DocumentMetadataInput? Metadata { get; set; }

    public ProcessingOptionsInput? ProcessingOptions { get; set; }

    public ProcessingPriority? Priority { get; set; }

    public List<string>? Tags { get; set; }
}

// Batch upload input
[InputType]
public class BatchUploadInput
{
    [Required]
    public List<DocumentUploadItem> Documents { get; set; } = new();

    public ProcessingOptionsInput? DefaultProcessingOptions { get; set; }

    public ProcessingPriority Priority { get; set; } = ProcessingPriority.Normal;

    public int MaxConcurrency { get; set; } = 5;

    public string? BatchName { get; set; }
}

[InputType]
public class DocumentUploadItem
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DocumentMetadataInput? Metadata { get; set; }

    public ProcessingOptionsInput? ProcessingOptions { get; set; }
}

// Update input with version control
[InputType]
public class UpdateDocumentInput
{
    [Required]
    public string DocumentId { get; set; } = string.Empty;

    [Required]
    public long Version { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public DocumentMetadataInput? Metadata { get; set; }

    public List<string>? Tags { get; set; }

    public bool TriggerReprocessing { get; set; } = false;
}

// Processing options input
[InputType]
public class ProcessingOptionsInput
{
    public List<ProcessingType>? EnabledTypes { get; set; }

    public ClassificationOptionsInput? ClassificationOptions { get; set; }

    public SentimentOptionsInput? SentimentOptions { get; set; }

    public TopicModelingOptionsInput? TopicOptions { get; set; }

    public SummarizationOptionsInput? SummaryOptions { get; set; }

    public bool StoreIntermediateResults { get; set; } = false;

    public int? MaxRetries { get; set; } = 3;

    public TimeSpan? Timeout { get; set; }
}

[InputType]
public class ClassificationOptionsInput
{
    public string? ModelName { get; set; }
    public float ConfidenceThreshold { get; set; } = 0.7f;
    public List<string>? TargetCategories { get; set; }
    public bool IncludeProbabilities { get; set; } = true;
}
```

### Payload Types

```csharp
// Base payload with error handling
[ObjectType]
public abstract class BasePayload
{
    public List<UserError> Errors { get; } = new();

    protected BasePayload() { }

    protected BasePayload(UserError error)
    {
        Errors.Add(error);
    }

    protected BasePayload(IEnumerable<UserError> errors)
    {
        Errors.AddRange(errors);
    }

    public static T FromError<T>(string message, string code = "UNKNOWN_ERROR") 
        where T : BasePayload, new()
    {
        return new T().AddError(message, code);
    }

    public static T FromErrors<T>(IEnumerable<UserError> errors)
        where T : BasePayload, new()
    {
        var payload = new T();
        payload.Errors.AddRange(errors);
        return payload;
    }

    protected T AddError<T>(string message, string code) where T : BasePayload
    {
        Errors.Add(new UserError(message, code));
        return (T)this;
    }
}

// Document operation payloads
[ObjectType]
public class CreateDocumentPayload : BasePayload
{
    public Document? Document { get; private set; }

    public CreateDocumentPayload() { }

    public CreateDocumentPayload(Document document)
    {
        Document = document;
    }

    public CreateDocumentPayload(UserError error) : base(error) { }

    public CreateDocumentPayload(IEnumerable<UserError> errors) : base(errors) { }
}

[ObjectType]
public class BatchUploadPayload : BasePayload
{
    public DocumentBatch? Batch { get; private set; }

    public BatchUploadPayload() { }

    public BatchUploadPayload(DocumentBatch batch)
    {
        Batch = batch;
    }

    public BatchUploadPayload(UserError error) : base(error) { }
}

[ObjectType]
public class ProcessingPayload : BasePayload
{
    public ProcessingResult? Result { get; private set; }

    public ProcessingPayload() { }

    public ProcessingPayload(ProcessingResult result)
    {
        Result = result;
    }

    public ProcessingPayload(UserError error) : base(error) { }
}

// Complex operation payloads
[ObjectType]
public class BulkProcessingPayload : BasePayload
{
    public BulkProcessingResult? Result { get; private set; }

    public BulkProcessingPayload() { }

    public BulkProcessingPayload(BulkProcessingResult result)
    {
        Result = result;
    }

    public BulkProcessingPayload(UserError error) : base(error) { }
}

[ObjectType]
public class BulkProcessingResult
{
    public string BatchId { get; set; } = string.Empty;
    public int TotalDocuments { get; set; }
    public int QueuedDocuments { get; set; }
    public int SkippedDocuments { get; set; }
    public ProcessingPriority Priority { get; set; }
    public DateTime StartedAt { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
    public List<string> QueuedDocumentIds { get; set; } = new();
    public List<ProcessingSkipReason> SkippedReasons { get; set; } = new();
}
```

### Transaction and Error Handling

```csharp
// Transactional mutations
[ExtendObjectType<Mutation>]
public class TransactionalMutations
{
    // Multi-operation transaction
    [Authorize(Policy = "CanManageDocuments")]
    public async Task<TransactionPayload> ExecuteDocumentTransactionAsync(
        DocumentTransactionInput input,
        [Service] IDocumentTransactionService transactionService,
        CancellationToken cancellationToken)
    {
        using var transaction = await transactionService.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var results = new List<OperationResult>();

            foreach (var operation in input.Operations)
            {
                var result = operation.Type switch
                {
                    OperationType.Create => await ExecuteCreateOperation(operation, transactionService, cancellationToken),
                    OperationType.Update => await ExecuteUpdateOperation(operation, transactionService, cancellationToken),
                    OperationType.Delete => await ExecuteDeleteOperation(operation, transactionService, cancellationToken),
                    _ => throw new ArgumentException($"Unknown operation type: {operation.Type}")
                };

                results.Add(result);

                if (!result.Success && input.StopOnError)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return TransactionPayload.FromError($"Operation failed: {result.ErrorMessage}", "OPERATION_FAILED");
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return new TransactionPayload(results);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return TransactionPayload.FromError(ex.Message, "TRANSACTION_FAILED");
        }
    }

    // Compensating transaction pattern
    [Authorize(Policy = "CanManageDocuments")]
    public async Task<CompensationPayload> CompensateFailedOperationAsync(
        CompensationInput input,
        [Service] ICompensationService compensationService,
        CancellationToken cancellationToken)
    {
        try
        {
            var compensationResult = await compensationService.CompensateAsync(input, cancellationToken);
            return new CompensationPayload(compensationResult);
        }
        catch (Exception ex)
        {
            return CompensationPayload.FromError(ex.Message, "COMPENSATION_FAILED");
        }
    }
}
```

## Usage

### Mutation Examples

```graphql
# Create document with processing options
mutation CreateDocument {
  createDocument(
    input: {
      title: "Research Paper: ML in Healthcare"
      content: "This paper explores the application of machine learning..."
      metadata: {
        authorId: "user-123"
        source: "internal"
        language: "en"
        customProperties: {
          "department": "research"
          "priority": "high"
        }
      }
      processingOptions: {
        enabledTypes: [CLASSIFICATION, SENTIMENT, TOPIC_MODELING]
        classificationOptions: {
          modelName: "healthcare-classifier"
          confidenceThreshold: 0.8
        }
      }
      priority: HIGH
    }
  ) {
    document {
      id
      title
      status
      createdAt
    }
    errors {
      message
      code
    }
  }
}

# Batch upload with progress tracking
mutation BatchUpload {
  uploadDocumentBatch(
    input: {
      documents: [
        {
          title: "Document 1"
          content: "Content 1..."
        }
        {
          title: "Document 2"
          content: "Content 2..."
        }
      ]
      defaultProcessingOptions: {
        enabledTypes: [CLASSIFICATION, SENTIMENT]
      }
      priority: NORMAL
      maxConcurrency: 3
      batchName: "Research Papers Batch 1"
    }
  ) {
    batch {
      id
      totalDocuments
      status
      createdAt
    }
    errors {
      message
      code
    }
  }
}

# Bulk reprocessing with filters
mutation BulkReprocess {
  reprocessDocuments(
    input: {
      filters: {
        statusIn: [COMPLETED]
        createdAfter: "2024-01-01"
        categories: ["research"]
      }
      options: {
        enabledTypes: [TOPIC_MODELING]
        topicOptions: {
          topicCount: 10
          includeKeywords: true
        }
      }
      maxDocuments: 1000
    }
  ) {
    result {
      batchId
      totalDocuments
      queuedDocuments
      estimatedDuration
    }
    errors {
      message
      code
    }
  }
}
```

## Notes

- **Validation**: Always validate inputs before processing to prevent invalid data
- **Transactions**: Use transactions for multi-step operations to ensure consistency
- **Error Handling**: Provide comprehensive error information with specific error codes
- **Authorization**: Apply appropriate authorization checks for all mutation operations
- **Async Processing**: Use background processing for long-running operations
- **Progress Tracking**: Implement progress tracking for batch operations
- **Compensation**: Consider compensation patterns for complex distributed operations
- **Idempotency**: Design mutations to be idempotent where possible

## Related Patterns

- [Query Patterns](query-patterns.md) - Data retrieval patterns
- [Subscription Patterns](subscription-patterns.md) - Real-time updates
- [Authorization](authorization.md) - Security patterns

---

**Key Benefits**: Robust error handling, transactional consistency, batch operations, progress tracking

**When to Use**: Document management systems, ML workflow automation, batch processing operations

**Performance**: Async processing, transaction optimization, bulk operation support