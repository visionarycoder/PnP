# Enterprise GraphQL Schema Design Patterns

**Description**: Advanced enterprise schema design patterns using HotChocolate including federated schemas, domain-driven design, backward-compatible evolution, comprehensive validation, and enterprise security integration with performance optimization.

**Language/Technology**: C#, HotChocolate, .NET 9.0, Schema Federation, Domain-Driven Design
**Enterprise Features**: Schema federation, domain modeling, backward compatibility, comprehensive validation, security integration, and performance monitoring

## Code

### Domain-Driven Schema Design

```csharp
namespace DocumentProcessor.GraphQL.Types;

using HotChocolate;
using HotChocolate.Types;

// Document aggregate root
[ObjectType("Document")]
public class DocumentType : ObjectType<Document>
{
    protected override void Configure(IObjectTypeDescriptor<Document> descriptor)
    {
        descriptor
            .Description("A document in the processing system");

        descriptor
            .Field(d => d.Id)
            .Type<NonNullType<IdType>>()
            .Description("Unique document identifier");

        descriptor
            .Field(d => d.Title)
            .Type<NonNullType<StringType>>()
            .Description("Document title");

        descriptor
            .Field(d => d.Content)
            .Type<NonNullType<StringType>>()
            .Description("Document content")
            .Authorize("ReadContent");

        descriptor
            .Field(d => d.GetProcessingResultsAsync(default!, default!))
            .Name("processingResults")
            .Description("Processing results for this document")
            .UsePaging<ProcessingResultType>()
            .UseFiltering<ProcessingResultFilterInput>()
            .UseSorting<ProcessingResultSortInput>();
    }
}

// Value object types
[ObjectType("DocumentMetadata")]
public class DocumentMetadataType : ObjectType<DocumentMetadata>
{
    protected override void Configure(IObjectTypeDescriptor<DocumentMetadata> descriptor)
    {
        descriptor
            .Description("Document metadata and properties");

        descriptor
            .Field(m => m.CustomProperties)
            .Type<AnyType>()
            .Description("Dynamic properties as JSON");
    }
}

// Polymorphic processing results
[UnionType("ProcessingOutput")]
public class ProcessingOutputType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Type<ClassificationResultType>();
        descriptor.Type<SentimentResultType>();
        descriptor.Type<TopicResultType>();
        descriptor.Type<SummarizationResultType>();
        descriptor.Type<EntityExtractionResultType>();
    }
}

[ObjectType("ClassificationResult")]
public class ClassificationResultType : ObjectType<ClassificationResult>
{
    protected override void Configure(IObjectTypeDescriptor<ClassificationResult> descriptor)
    {
        descriptor
            .Description("Text classification result");

        descriptor
            .Field(r => r.CategoryScores)
            .Type<ListType<CategoryScoreType>>()
            .Resolve(ctx =>
            {
                var result = ctx.Parent<ClassificationResult>();
                return result.CategoryScores.Select(kvp => new CategoryScore
                {
                    Category = kvp.Key,
                    Score = kvp.Value
                });
            });
    }
}

[ObjectType("SentimentResult")]
public class SentimentResultType : ObjectType<SentimentResult>
{
    protected override void Configure(IObjectTypeDescriptor<SentimentResult> descriptor)
    {
        descriptor
            .Description("Sentiment analysis result");

        descriptor
            .Field(r => r.EmotionScores)
            .Type<ListType<EmotionScoreType>>()
            .Resolve(ctx =>
            {
                var result = ctx.Parent<SentimentResult>();
                return result.EmotionScores.Select(kvp => new EmotionScore
                {
                    Emotion = kvp.Key,
                    Score = kvp.Value
                });
            });
    }
}

// Supporting types for complex data structures
[ObjectType]
public class CategoryScore
{
    public string Category { get; set; } = string.Empty;
    public float Score { get; set; }
}

[ObjectType]
public class EmotionScore
{
    public string Emotion { get; set; } = string.Empty;
    public float Score { get; set; }
}
```

### Interface-Based Design

```csharp
// Common interface for all processing results
[InterfaceType("ProcessingResultInterface")]
public class ProcessingResultInterfaceType : InterfaceType<IProcessingResult>
{
    protected override void Configure(IInterfaceTypeDescriptor<IProcessingResult> descriptor)
    {
        descriptor
            .Description("Common interface for all processing results");

        descriptor
            .Field(r => r.Id)
            .Type<NonNullType<IdType>>();

        descriptor
            .Field(r => r.DocumentId)
            .Type<NonNullType<IdType>>();

        descriptor
            .Field(r => r.ProcessingType)
            .Type<NonNullType<ProcessingTypeType>>();

        descriptor
            .Field(r => r.Status)
            .Type<NonNullType<ProcessingStatusType>>();

        descriptor
            .Field(r => r.Confidence)
            .Type<FloatType>();
    }
}

// Implement interface in concrete types
[ObjectType("ProcessingResult")]
public class ProcessingResultType : ObjectType<ProcessingResult>
{
    protected override void Configure(IObjectTypeDescriptor<ProcessingResult> descriptor)
    {
        descriptor
            .Implements<ProcessingResultInterfaceType>();

        descriptor
            .Field(r => r.Output)
            .Type<ProcessingOutputType>()
            .Description("Polymorphic processing output");

        descriptor
            .Field(r => r.Duration)
            .Type<TimeSpanType>()
            .Description("Processing duration");

        descriptor
            .Field(r => r.Metrics)
            .Type<AnyType>()
            .Description("Performance metrics as JSON");
    }
}
```

### Custom Scalar Types

```csharp
// Custom scalar for handling large text content
public class LargeTextType : ScalarType<string, string>
{
    public LargeTextType() : base("LargeText")
    {
        Description = "Large text content with compression support";
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        if (resultValue is null)
            return NullValueNode.Default;

        if (resultValue is string s)
        {
            // Compress large content for transport
            return new StringValueNode(s.Length > 10000 ? CompressText(s) : s);
        }

        throw new SerializationException($"Cannot serialize {resultValue}");
    }

    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        if (runtimeValue is null)
        {
            resultValue = null;
            return true;
        }

        if (runtimeValue is string s)
        {
            resultValue = s;
            return true;
        }

        resultValue = null;
        return false;
    }

    private static string CompressText(string text)
    {
        // Implement compression logic
        return text; // Simplified for example
    }
}

// Custom scalar for confidence scores
public class ConfidenceScoreType : ScalarType<float, float>
{
    public ConfidenceScoreType() : base("ConfidenceScore")
    {
        Description = "Confidence score between 0.0 and 1.0";
    }

    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        if (resultValue is float f && f >= 0.0f && f <= 1.0f)
        {
            runtimeValue = f;
            return true;
        }

        runtimeValue = null;
        return false;
    }
}
```

### Input Type Design

```csharp
// Document creation input with validation
[InputType("CreateDocumentInput")]
public class CreateDocumentInputType : InputObjectType<CreateDocumentInput>
{
    protected override void Configure(IInputObjectTypeDescriptor<CreateDocumentInput> descriptor)
    {
        descriptor
            .Description("Input for creating a new document");

        descriptor
            .Field(i => i.Title)
            .Type<NonNullType<StringType>>()
            .Description("Document title (required)")
            .Directive("length", new { min = 1, max = 200 });

        descriptor
            .Field(i => i.Content)
            .Type<NonNullType<LargeTextType>>()
            .Description("Document content (required)")
            .Directive("length", new { min = 1, max = 1000000 });

        descriptor
            .Field(i => i.ProcessingOptions)
            .Type<ProcessingOptionsInputType>()
            .Description("Optional processing configuration");
    }
}

// Filtering input with complex predicates
[InputType("DocumentFilter")]
public class DocumentFilterInputType : InputObjectType<DocumentFilter>
{
    protected override void Configure(IInputObjectTypeDescriptor<DocumentFilter> descriptor)
    {
        descriptor
            .Field(f => f.TitleContains)
            .Type<StringType>()
            .Description("Filter by title containing text");

        descriptor
            .Field(f => f.CreatedAfter)
            .Type<DateTimeType>()
            .Description("Filter by creation date");

        descriptor
            .Field(f => f.StatusIn)
            .Type<ListType<ProcessingStatusType>>()
            .Description("Filter by processing status");

        descriptor
            .Field(f => f.CategoryScoreAbove)
            .Type<ConfidenceScoreType>()
            .Description("Filter by minimum category confidence");

        descriptor
            .Field(f => f.And)
            .Type<ListType<DocumentFilterInputType>>()
            .Description("Logical AND operation");

        descriptor
            .Field(f => f.Or)
            .Type<ListType<DocumentFilterInputType>>()
            .Description("Logical OR operation");
    }
}
```

### Enum Type Design

```csharp
// Comprehensive enum with descriptions
[EnumType("ProcessingStatus")]
public class ProcessingStatusType : EnumType<ProcessingStatus>
{
    protected override void Configure(IEnumTypeDescriptor<ProcessingStatus> descriptor)
    {
        descriptor
            .Description("Status of document processing");

        descriptor
            .Value(ProcessingStatus.Pending)
            .Description("Queued for processing");

        descriptor
            .Value(ProcessingStatus.InProgress)
            .Description("Currently being processed");

        descriptor
            .Value(ProcessingStatus.Completed)
            .Description("Processing completed successfully");

        descriptor
            .Value(ProcessingStatus.Failed)
            .Description("Processing failed with errors");

        descriptor
            .Value(ProcessingStatus.Cancelled)
            .Description("Processing was cancelled");

        descriptor
            .Value(ProcessingStatus.Retrying)
            .Description("Retrying after transient failure");
    }
}

// Processing type with categories
[EnumType("ProcessingType")]
public class ProcessingTypeType : EnumType<ProcessingType>
{
    protected override void Configure(IEnumTypeDescriptor<ProcessingType> descriptor)
    {
        descriptor
            .Description("Type of ML processing");

        descriptor
            .Value(ProcessingType.Classification)
            .Description("Text classification");

        descriptor
            .Value(ProcessingType.Sentiment)
            .Description("Sentiment analysis");

        descriptor
            .Value(ProcessingType.TopicModeling)
            .Description("Topic modeling and extraction");

        descriptor
            .Value(ProcessingType.Summarization)
            .Description("Text summarization");

        descriptor
            .Value(ProcessingType.EntityExtraction)
            .Description("Named entity recognition");

        descriptor
            .Value(ProcessingType.KeywordExtraction)
            .Description("Keyword and phrase extraction");
    }
}
```

## Usage

### Schema Registration

```csharp
// Startup.cs or Program.cs
services
    .AddGraphQLServer()
    .AddQueryType<DocumentQueries>()
    .AddMutationType<DocumentMutations>()
    .AddSubscriptionType<DocumentSubscriptions>()
    .AddType<DocumentType>()
    .AddType<ProcessingResultType>()
    .AddType<ProcessingOutputType>()
    .AddType<LargeTextType>()
    .AddType<ConfidenceScoreType>()
    .AddType<ProcessingStatusType>()
    .AddType<ProcessingTypeType>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();
```

### Schema-First Development

```csharp
// Define schema in code-first approach
public class DocumentSchema : Schema
{
    public DocumentSchema(IServiceProvider services) : base(services)
    {
    }

    protected override void Configure(ISchemaTypeDescriptor descriptor)
    {
        descriptor
            .AddQueryType<DocumentQueries>()
            .AddMutationType<DocumentMutations>()
            .AddSubscriptionType<DocumentSubscriptions>();

        // Add custom directives
        descriptor.AddDirectiveType<AuthorizeDirectiveType>();
        descriptor.AddDirectiveType<LengthDirectiveType>();
        descriptor.AddDirectiveType<RateLimitDirectiveType>();
    }
}
```

## Notes

- **Type Safety**: Leverage C#'s type system to ensure schema correctness at compile time
- **Performance**: Use appropriate field resolvers and avoid N+1 queries with DataLoaders
- **Versioning**: Design for schema evolution using deprecation and optional fields
- **Security**: Apply authorization at the field level where appropriate
- **Documentation**: Provide comprehensive descriptions for all types and fields
- **Validation**: Implement custom validators for complex business rules
- **Caching**: Consider caching expensive field resolvers

## Related Patterns

- [Query Patterns](query-patterns.md) - Advanced querying capabilities
- [DataLoader Patterns](dataloader-patterns.md) - Efficient data loading
- [Authorization](authorization.md) - Security and access control

---

**Key Benefits**: Type safety, schema evolution, comprehensive documentation, robust validation

**When to Use**: Building complex GraphQL APIs, document processing systems, ML result exposure

**Performance**: Field-level resolution, DataLoader integration, query optimization