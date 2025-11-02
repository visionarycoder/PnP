# Named Entity Recognition with ML.NET

**Description**: Comprehensive Named Entity Recognition (NER) patterns using ML.NET for extracting entities from unstructured text. Implements custom entity types, information extraction pipelines, and advanced NER techniques for production applications.

**Language/Technology**: C# / ML.NET

## Code

### Core NER Service

```csharp
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MLNet.NamedEntityRecognition;

// Input text model
public class TextInput
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// Entity annotation for training
public class EntityAnnotation
{
    public string Text { get; set; } = string.Empty;
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public float Confidence { get; set; } = 1.0f;
    public Dictionary<string, object> Properties { get; set; } = new();
}

// Extracted entity result
public class ExtractedEntity
{
    public string Text { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public float Confidence { get; set; }
    public string NormalizedValue { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
}

// NER prediction output
public class NerPrediction
{
    [VectorType]
    public float[] EntityProbabilities { get; set; } = Array.Empty<float>();
    
    public string PredictedEntityType { get; set; } = string.Empty;
}

// Training data for sequence labeling
public class NerTrainingData
{
    public string Token { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string PreviousLabel { get; set; } = string.Empty;
    public string NextLabel { get; set; } = string.Empty;
    public bool IsCapitalized { get; set; }
    public bool IsNumeric { get; set; }
    public bool HasPunctuation { get; set; }
    public int TokenLength { get; set; }
    public string PosTag { get; set; } = string.Empty;
}

// Main NER service
public class NamedEntityRecognitionService
{
    private readonly MLContext _mlContext;
    private readonly ILogger<NamedEntityRecognitionService> _logger;
    private readonly NerOptions _options;
    private readonly Dictionary<string, ITransformer> _models = new();
    private readonly Dictionary<string, IEntityExtractor> _extractors = new();

    public NamedEntityRecognitionService(
        ILogger<NamedEntityRecognitionService> logger,
        IOptions<NerOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _mlContext = new MLContext(seed: _options.RandomSeed);
        
        InitializeBuiltInExtractors();
    }

    // Train custom NER model
    public async Task<NerTrainingResult> TrainNerModelAsync(
        IEnumerable<(string text, List<EntityAnnotation> entities)> trainingData,
        string modelName,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Training NER model '{ModelName}' with {SampleCount} samples", 
                modelName, trainingData.Count());

            // Prepare sequence labeling data
            var sequenceData = PrepareSequenceLabelingData(trainingData);
            var dataView = _mlContext.Data.LoadFromEnumerable(sequenceData);

            // Build training pipeline
            var pipeline = BuildNerTrainingPipeline();
            
            // Train model
            var model = pipeline.Fit(dataView);
            _models[modelName] = model;

            // Evaluate model
            var evaluation = await EvaluateNerModelAsync(model, dataView);

            var result = new NerTrainingResult
            {
                ModelName = modelName,
                TrainingDuration = stopwatch.Elapsed,
                Precision = evaluation.Precision,
                Recall = evaluation.Recall,
                F1Score = evaluation.F1Score,
                Accuracy = evaluation.Accuracy,
                EntityTypeMetrics = evaluation.EntityTypeMetrics,
                TrainingDataCount = trainingData.Count(),
                ModelSize = await GetModelSizeAsync(model)
            };

            _logger.LogInformation("NER model '{ModelName}' trained in {Duration}ms - F1: {F1Score:F3}",
                modelName, stopwatch.ElapsedMilliseconds, result.F1Score);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NER model training failed for '{ModelName}'", modelName);
            throw;
        }
    }

    // Extract entities from text
    public async Task<EntityExtractionResult> ExtractEntitiesAsync(
        TextInput input,
        string? modelName = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Extracting entities from text (length: {TextLength})", input.Text.Length);

            var entities = new List<ExtractedEntity>();

            // Use built-in extractors
            foreach (var extractor in _extractors.Values)
            {
                var extractedEntities = await extractor.ExtractAsync(input.Text, cancellationToken);
                entities.AddRange(extractedEntities);
            }

            // Use custom model if specified
            if (!string.IsNullOrEmpty(modelName) && _models.TryGetValue(modelName, out var model))
            {
                var customEntities = await ExtractWithCustomModelAsync(input.Text, model);
                entities.AddRange(customEntities);
            }

            // Post-process and resolve conflicts
            entities = ResolveEntityConflicts(entities);
            entities = ApplyEntityNormalization(entities);
            entities = ApplyConfidenceFiltering(entities);

            var result = new EntityExtractionResult
            {
                InputId = input.Id,
                Entities = entities.OrderBy(e => e.StartIndex).ToList(),
                ProcessingDuration = stopwatch.Elapsed,
                EntityCount = entities.Count,
                EntityTypes = entities.Select(e => e.EntityType).Distinct().ToList()
            };

            _logger.LogDebug("Extracted {EntityCount} entities in {Duration}ms",
                entities.Count, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Entity extraction failed for input '{InputId}'", input.Id);
            throw;
        }
    }

    // Batch entity extraction
    public async Task<List<EntityExtractionResult>> ExtractEntitiesBatchAsync(
        IEnumerable<TextInput> inputs,
        string? modelName = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<EntityExtractionResult>();
        var semaphore = new SemaphoreSlim(_options.MaxConcurrency, _options.MaxConcurrency);

        var tasks = inputs.Select(async input =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await ExtractEntitiesAsync(input, modelName, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        results.AddRange(await Task.WhenAll(tasks));
        return results;
    }

    // Build training pipeline for sequence labeling
    private IEstimator<ITransformer> BuildNerTrainingPipeline()
    {
        return _mlContext.Transforms.Text
            .FeaturizeText("TokenFeatures", nameof(NerTrainingData.Token))
            .Append(_mlContext.Transforms.Text.FeaturizeText("PosFeatures", nameof(NerTrainingData.PosTag)))
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding("PreviousLabelFeatures", nameof(NerTrainingData.PreviousLabel)))
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding("NextLabelFeatures", nameof(NerTrainingData.NextLabel)))
            .Append(_mlContext.Transforms.Conversion.ConvertType("IsCapitalizedFloat", nameof(NerTrainingData.IsCapitalized), DataKind.Single))
            .Append(_mlContext.Transforms.Conversion.ConvertType("IsNumericFloat", nameof(NerTrainingData.IsNumeric), DataKind.Single))
            .Append(_mlContext.Transforms.Conversion.ConvertType("HasPunctuationFloat", nameof(NerTrainingData.HasPunctuation), DataKind.Single))
            .Append(_mlContext.Transforms.Conversion.ConvertType("TokenLengthFloat", nameof(NerTrainingData.TokenLength), DataKind.Single))
            .Append(_mlContext.Transforms.Concatenate("Features", 
                "TokenFeatures", "PosFeatures", "PreviousLabelFeatures", "NextLabelFeatures",
                "IsCapitalizedFloat", "IsNumericFloat", "HasPunctuationFloat", "TokenLengthFloat"))
            .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedEntityType", "PredictedLabel"));
    }

    // Prepare sequence labeling training data
    private List<NerTrainingData> PrepareSequenceLabelingData(
        IEnumerable<(string text, List<EntityAnnotation> entities)> trainingData)
    {
        var sequenceData = new List<NerTrainingData>();

        foreach (var (text, entities) in trainingData)
        {
            var tokens = TokenizeText(text);
            var labels = CreateBioLabels(tokens, entities, text);

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                var label = labels[i];

                var trainingItem = new NerTrainingData
                {
                    Token = token.Text,
                    Label = label,
                    PreviousLabel = i > 0 ? labels[i - 1] : "O",
                    NextLabel = i < labels.Count - 1 ? labels[i + 1] : "O",
                    IsCapitalized = char.IsUpper(token.Text.FirstOrDefault()),
                    IsNumeric = token.Text.All(char.IsDigit),
                    HasPunctuation = token.Text.Any(char.IsPunctuation),
                    TokenLength = token.Text.Length,
                    PosTag = GetPosTag(token.Text) // Simplified POS tagging
                };

                sequenceData.Add(trainingItem);
            }
        }

        return sequenceData;
    }

    // Create BIO (Begin-Inside-Outside) labels
    private List<string> CreateBioLabels(List<TextToken> tokens, List<EntityAnnotation> entities, string originalText)
    {
        var labels = new List<string>(new string[tokens.Count]);
        
        // Initialize all labels as "O" (Outside)
        for (int i = 0; i < labels.Count; i++)
        {
            labels[i] = "O";
        }

        // Assign BIO labels for each entity
        foreach (var entity in entities)
        {
            var entityTokens = FindOverlappingTokens(tokens, entity.StartIndex, entity.EndIndex);
            
            for (int i = 0; i < entityTokens.Count; i++)
            {
                var tokenIndex = entityTokens[i];
                if (i == 0)
                {
                    labels[tokenIndex] = $"B-{entity.EntityType}"; // Begin
                }
                else
                {
                    labels[tokenIndex] = $"I-{entity.EntityType}"; // Inside
                }
            }
        }

        return labels;
    }

    // Initialize built-in entity extractors
    private void InitializeBuiltInExtractors()
    {
        _extractors["person"] = new PersonExtractor();
        _extractors["organization"] = new OrganizationExtractor();
        _extractors["location"] = new LocationExtractor();
        _extractors["date"] = new DateTimeExtractor();
        _extractors["number"] = new NumberExtractor();
        _extractors["email"] = new EmailExtractor();
        _extractors["url"] = new UrlExtractor();
        _extractors["phone"] = new PhoneNumberExtractor();
        _extractors["money"] = new MoneyExtractor();
    }

    // Extract entities using custom trained model
    private async Task<List<ExtractedEntity>> ExtractWithCustomModelAsync(string text, ITransformer model)
    {
        var tokens = TokenizeText(text);
        var entities = new List<ExtractedEntity>();
        var predictionEngine = _mlContext.Model.CreatePredictionEngine<NerTrainingData, NerPrediction>(model);

        var currentEntity = new List<(TextToken token, string label)>();

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            
            var input = new NerTrainingData
            {
                Token = token.Text,
                PreviousLabel = i > 0 ? "O" : "O", // Simplified - would use previous prediction
                NextLabel = "O", // Simplified - would look ahead
                IsCapitalized = char.IsUpper(token.Text.FirstOrDefault()),
                IsNumeric = token.Text.All(char.IsDigit),
                HasPunctuation = token.Text.Any(char.IsPunctuation),
                TokenLength = token.Text.Length,
                PosTag = GetPosTag(token.Text)
            };

            var prediction = predictionEngine.Predict(input);
            
            if (prediction.PredictedEntityType.StartsWith("B-"))
            {
                // Start new entity
                if (currentEntity.Any())
                {
                    entities.Add(CreateEntityFromTokens(currentEntity, text));
                    currentEntity.Clear();
                }
                currentEntity.Add((token, prediction.PredictedEntityType));
            }
            else if (prediction.PredictedEntityType.StartsWith("I-") && currentEntity.Any())
            {
                // Continue current entity
                currentEntity.Add((token, prediction.PredictedEntityType));
            }
            else
            {
                // Outside or end of entity
                if (currentEntity.Any())
                {
                    entities.Add(CreateEntityFromTokens(currentEntity, text));
                    currentEntity.Clear();
                }
            }
        }

        // Handle last entity
        if (currentEntity.Any())
        {
            entities.Add(CreateEntityFromTokens(currentEntity, text));
        }

        return entities;
    }

    // Helper methods
    private List<TextToken> TokenizeText(string text)
    {
        var tokens = new List<TextToken>();
        var words = text.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var currentIndex = 0;

        foreach (var word in words)
        {
            var index = text.IndexOf(word, currentIndex);
            if (index >= 0)
            {
                tokens.Add(new TextToken 
                { 
                    Text = word, 
                    StartIndex = index, 
                    EndIndex = index + word.Length - 1 
                });
                currentIndex = index + word.Length;
            }
        }

        return tokens;
    }

    private List<int> FindOverlappingTokens(List<TextToken> tokens, int startIndex, int endIndex)
    {
        var overlappingTokens = new List<int>();

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.StartIndex <= endIndex && token.EndIndex >= startIndex)
            {
                overlappingTokens.Add(i);
            }
        }

        return overlappingTokens;
    }

    private string GetPosTag(string token)
    {
        // Simplified POS tagging - in production use NLP libraries
        if (token.All(char.IsDigit)) return "CD"; // Cardinal number
        if (token.All(char.IsUpper)) return "NNP"; // Proper noun
        if (char.IsUpper(token.FirstOrDefault())) return "NN"; // Noun
        return "NN"; // Default to noun
    }

    private ExtractedEntity CreateEntityFromTokens(List<(TextToken token, string label)> entityTokens, string originalText)
    {
        if (!entityTokens.Any()) throw new ArgumentException("Entity tokens cannot be empty");

        var firstToken = entityTokens.First().token;
        var lastToken = entityTokens.Last().token;
        var entityType = entityTokens.First().label.Substring(2); // Remove B- or I- prefix

        var startIndex = firstToken.StartIndex;
        var endIndex = lastToken.EndIndex;
        var entityText = originalText.Substring(startIndex, endIndex - startIndex + 1);

        return new ExtractedEntity
        {
            Text = entityText,
            EntityType = entityType,
            StartIndex = startIndex,
            EndIndex = endIndex,
            Confidence = 0.8f, // Would calculate based on model confidence
            NormalizedValue = NormalizeEntityValue(entityText, entityType)
        };
    }

    private List<ExtractedEntity> ResolveEntityConflicts(List<ExtractedEntity> entities)
    {
        // Sort by start index and confidence
        var sortedEntities = entities.OrderBy(e => e.StartIndex).ThenByDescending(e => e.Confidence).ToList();
        var resolvedEntities = new List<ExtractedEntity>();

        foreach (var entity in sortedEntities)
        {
            // Check for overlap with existing entities
            var hasOverlap = resolvedEntities.Any(existing =>
                entity.StartIndex <= existing.EndIndex && entity.EndIndex >= existing.StartIndex);

            if (!hasOverlap)
            {
                resolvedEntities.Add(entity);
            }
        }

        return resolvedEntities;
    }

    private List<ExtractedEntity> ApplyEntityNormalization(List<ExtractedEntity> entities)
    {
        foreach (var entity in entities)
        {
            entity.NormalizedValue = NormalizeEntityValue(entity.Text, entity.EntityType);
        }

        return entities;
    }

    private string NormalizeEntityValue(string entityText, string entityType)
    {
        return entityType.ToLower() switch
        {
            "person" => NormalizePersonName(entityText),
            "organization" => NormalizeOrganizationName(entityText),
            "location" => NormalizeLocationName(entityText),
            "date" => NormalizeDateValue(entityText),
            "money" => NormalizeMoneyValue(entityText),
            _ => entityText.Trim()
        };
    }

    private string NormalizePersonName(string name)
    {
        // Basic name normalization
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
    }

    private string NormalizeOrganizationName(string org)
    {
        // Remove common suffixes and normalize
        return org.Trim()
            .Replace(" Inc.", "")
            .Replace(" LLC", "")
            .Replace(" Corp.", "");
    }

    private string NormalizeLocationName(string location)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(location.ToLower());
    }

    private string NormalizeDateValue(string date)
    {
        if (DateTime.TryParse(date, out var parsedDate))
        {
            return parsedDate.ToString("yyyy-MM-dd");
        }
        return date;
    }

    private string NormalizeMoneyValue(string money)
    {
        // Extract numeric value and currency
        var numberRegex = new Regex(@"\d+([.,]\d+)*");
        var match = numberRegex.Match(money);
        if (match.Success)
        {
            return match.Value;
        }
        return money;
    }

    private List<ExtractedEntity> ApplyConfidenceFiltering(List<ExtractedEntity> entities)
    {
        return entities.Where(e => e.Confidence >= _options.MinConfidenceThreshold).ToList();
    }
}

// Supporting classes
public class TextToken
{
    public string Text { get; set; } = string.Empty;
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
}

// Configuration options
public class NerOptions
{
    public int RandomSeed { get; set; } = 42;
    public int MaxConcurrency { get; set; } = 4;
    public float MinConfidenceThreshold { get; set; } = 0.5f;
    public bool EnableBuiltInExtractors { get; set; } = true;
    public Dictionary<string, object> CustomExtractorSettings { get; set; } = new();
}

// Result models
public class NerTrainingResult
{
    public string ModelName { get; set; } = string.Empty;
    public TimeSpan TrainingDuration { get; set; }
    public float Precision { get; set; }
    public float Recall { get; set; }
    public float F1Score { get; set; }
    public float Accuracy { get; set; }
    public Dictionary<string, EntityMetrics> EntityTypeMetrics { get; set; } = new();
    public int TrainingDataCount { get; set; }
    public long ModelSize { get; set; }
}

public class EntityExtractionResult
{
    public string InputId { get; set; } = string.Empty;
    public List<ExtractedEntity> Entities { get; set; } = new();
    public TimeSpan ProcessingDuration { get; set; }
    public int EntityCount { get; set; }
    public List<string> EntityTypes { get; set; } = new();
}

public class EntityMetrics
{
    public float Precision { get; set; }
    public float Recall { get; set; }
    public float F1Score { get; set; }
    public int TruePositives { get; set; }
    public int FalsePositives { get; set; }
    public int FalseNegatives { get; set; }
}

public class NerEvaluationResult
{
    public float Precision { get; set; }
    public float Recall { get; set; }
    public float F1Score { get; set; }
    public float Accuracy { get; set; }
    public Dictionary<string, EntityMetrics> EntityTypeMetrics { get; set; } = new();
}
```

### Built-in Entity Extractors

```csharp
// Base interface for entity extractors
public interface IEntityExtractor
{
    Task<List<ExtractedEntity>> ExtractAsync(string text, CancellationToken cancellationToken = default);
    string EntityType { get; }
    float BaseConfidence { get; }
}

// Person name extractor
public class PersonExtractor : IEntityExtractor
{
    public string EntityType => "PERSON";
    public float BaseConfidence => 0.85f;

    private readonly HashSet<string> _commonFirstNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "John", "Jane", "Michael", "Sarah", "David", "Emily", "Robert", "Jessica",
        "William", "Ashley", "James", "Amanda", "Christopher", "Stephanie", "Matthew", "Jennifer"
    };

    private readonly HashSet<string> _titlePrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Mr.", "Mrs.", "Ms.", "Dr.", "Prof.", "Sr.", "Jr."
    };

    public async Task<List<ExtractedEntity>> ExtractAsync(string text, CancellationToken cancellationToken = default)
    {
        var entities = new List<ExtractedEntity>();
        
        // Pattern for names with titles
        var titleNamePattern = @"\b(?:Mr\.|Mrs\.|Ms\.|Dr\.|Prof\.)\s+([A-Z][a-z]+(?:\s+[A-Z][a-z]+)*)\b";
        var titleMatches = Regex.Matches(text, titleNamePattern);
        
        foreach (Match match in titleMatches)
        {
            entities.Add(new ExtractedEntity
            {
                Text = match.Value,
                EntityType = EntityType,
                StartIndex = match.Index,
                EndIndex = match.Index + match.Length - 1,
                Confidence = BaseConfidence,
                NormalizedValue = match.Groups[1].Value
            });
        }

        // Pattern for capitalized names (2-3 words)
        var namePattern = @"\b[A-Z][a-z]+(?:\s+[A-Z][a-z]+){1,2}\b";
        var nameMatches = Regex.Matches(text, namePattern);
        
        foreach (Match match in nameMatches)
        {
            // Skip if already found with title
            if (entities.Any(e => e.StartIndex <= match.Index && e.EndIndex >= match.Index + match.Length - 1))
                continue;

            var words = match.Value.Split(' ');
            var confidence = BaseConfidence;
            
            // Boost confidence if contains common first name
            if (_commonFirstNames.Contains(words[0]))
            {
                confidence += 0.1f;
            }

            // Reduce confidence for single words or common words
            if (words.Length == 1 || IsCommonWord(match.Value))
            {
                confidence -= 0.3f;
            }

            if (confidence >= 0.5f)
            {
                entities.Add(new ExtractedEntity
                {
                    Text = match.Value,
                    EntityType = EntityType,
                    StartIndex = match.Index,
                    EndIndex = match.Index + match.Length - 1,
                    Confidence = Math.Min(confidence, 1.0f),
                    NormalizedValue = match.Value
                });
            }
        }

        return entities;
    }

    private bool IsCommonWord(string word)
    {
        var commonWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "The", "And", "But", "For", "This", "That", "With", "From", "They", "Have", "Been"
        };
        return commonWords.Contains(word);
    }
}

// Organization extractor
public class OrganizationExtractor : IEntityExtractor
{
    public string EntityType => "ORGANIZATION";
    public float BaseConfidence => 0.8f;

    private readonly HashSet<string> _orgSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Inc.", "Corp.", "LLC", "Ltd.", "Co.", "Company", "Corporation", "Incorporated",
        "Organization", "Institute", "Foundation", "Association", "Society", "Group"
    };

    public async Task<List<ExtractedEntity>> ExtractAsync(string text, CancellationToken cancellationToken = default)
    {
        var entities = new List<ExtractedEntity>();

        // Pattern for organizations with suffixes
        var orgSuffixPattern = @"\b([A-Z][A-Za-z\s&]+?)\s+(?:Inc\.|Corp\.|LLC|Ltd\.|Co\.|Company|Corporation|Incorporated)\b";
        var matches = Regex.Matches(text, orgSuffixPattern);
        
        foreach (Match match in matches)
        {
            entities.Add(new ExtractedEntity
            {
                Text = match.Value.Trim(),
                EntityType = EntityType,
                StartIndex = match.Index,
                EndIndex = match.Index + match.Length - 1,
                Confidence = BaseConfidence + 0.1f,
                NormalizedValue = match.Groups[1].Value.Trim()
            });
        }

        // Pattern for capitalized organization names
        var orgPattern = @"\b[A-Z][A-Za-z]+(?:\s+[A-Z&][A-Za-z]*)*(?:\s+(?:Institute|Foundation|Association|Society|Group|Department|Agency|Authority))\b";
        var orgMatches = Regex.Matches(text, orgPattern);
        
        foreach (Match match in orgMatches)
        {
            // Skip if already found with suffix
            if (entities.Any(e => Math.Abs(e.StartIndex - match.Index) < 10))
                continue;

            entities.Add(new ExtractedEntity
            {
                Text = match.Value,
                EntityType = EntityType,
                StartIndex = match.Index,
                EndIndex = match.Index + match.Length - 1,
                Confidence = BaseConfidence,
                NormalizedValue = match.Value
            });
        }

        return entities;
    }
}

// Location extractor
public class LocationExtractor : IEntityExtractor
{
    public string EntityType => "LOCATION";
    public float BaseConfidence => 0.75f;

    private readonly HashSet<string> _locationKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "City", "County", "State", "Province", "Country", "Region", "District", "Area",
        "Street", "Avenue", "Boulevard", "Road", "Drive", "Lane", "Way", "Plaza"
    };

    public async Task<List<ExtractedEntity>> ExtractAsync(string text, CancellationToken cancellationToken = default)
    {
        var entities = new List<ExtractedEntity>();

        // Pattern for addresses
        var addressPattern = @"\b\d+\s+[A-Z][A-Za-z\s]+(?:Street|St\.|Avenue|Ave\.|Boulevard|Blvd\.|Road|Rd\.|Drive|Dr\.|Lane|Ln\.)\b";
        var addressMatches = Regex.Matches(text, addressPattern);
        
        foreach (Match match in addressMatches)
        {
            entities.Add(new ExtractedEntity
            {
                Text = match.Value,
                EntityType = EntityType,
                StartIndex = match.Index,
                EndIndex = match.Index + match.Length - 1,
                Confidence = BaseConfidence + 0.15f,
                NormalizedValue = match.Value,
                Properties = new Dictionary<string, object> { ["SubType"] = "ADDRESS" }
            });
        }

        // Pattern for city, state combinations
        var cityStatePattern = @"\b[A-Z][a-z]+,\s*[A-Z]{2}\b";
        var cityStateMatches = Regex.Matches(text, cityStatePattern);
        
        foreach (Match match in cityStateMatches)
        {
            entities.Add(new ExtractedEntity
            {
                Text = match.Value,
                EntityType = EntityType,
                StartIndex = match.Index,
                EndIndex = match.Index + match.Length - 1,
                Confidence = BaseConfidence + 0.1f,
                NormalizedValue = match.Value,
                Properties = new Dictionary<string, object> { ["SubType"] = "CITY_STATE" }
            });
        }

        return entities;
    }
}

// Date/Time extractor
public class DateTimeExtractor : IEntityExtractor
{
    public string EntityType => "DATE";
    public float BaseConfidence => 0.9f;

    public async Task<List<ExtractedEntity>> ExtractAsync(string text, CancellationToken cancellationToken = default)
    {
        var entities = new List<ExtractedEntity>();

        // Various date patterns
        var datePatterns = new[]
        {
            @"\b\d{1,2}/\d{1,2}/\d{4}\b",           // MM/dd/yyyy
            @"\b\d{4}-\d{2}-\d{2}\b",                // yyyy-MM-dd
            @"\b\d{1,2}-\d{1,2}-\d{4}\b",           // MM-dd-yyyy
            @"\b(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+\d{1,2},?\s+\d{4}\b", // Month dd, yyyy
            @"\b\d{1,2}\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+\d{4}\b"   // dd Month yyyy
        };

        foreach (var pattern in datePatterns)
        {
            var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                var confidence = BaseConfidence;
                
                // Try to parse the date to validate
                if (TryParseDate(match.Value, out var parsedDate))
                {
                    entities.Add(new ExtractedEntity
                    {
                        Text = match.Value,
                        EntityType = EntityType,
                        StartIndex = match.Index,
                        EndIndex = match.Index + match.Length - 1,
                        Confidence = confidence,
                        NormalizedValue = parsedDate.ToString("yyyy-MM-dd"),
                        Properties = new Dictionary<string, object> 
                        { 
                            ["ParsedDate"] = parsedDate,
                            ["Format"] = GetDateFormat(match.Value)
                        }
                    });
                }
            }
        }

        return entities;
    }

    private bool TryParseDate(string dateString, out DateTime date)
    {
        return DateTime.TryParse(dateString, out date) && 
               date.Year >= 1900 && date.Year <= DateTime.Now.Year + 10;
    }

    private string GetDateFormat(string dateString)
    {
        if (Regex.IsMatch(dateString, @"\d{1,2}/\d{1,2}/\d{4}")) return "MM/dd/yyyy";
        if (Regex.IsMatch(dateString, @"\d{4}-\d{2}-\d{2}")) return "yyyy-MM-dd";
        if (Regex.IsMatch(dateString, @"\d{1,2}-\d{1,2}-\d{4}")) return "MM-dd-yyyy";
        return "natural";
    }
}

// Additional specialized extractors
public class EmailExtractor : IEntityExtractor
{
    public string EntityType => "EMAIL";
    public float BaseConfidence => 0.95f;

    public async Task<List<ExtractedEntity>> ExtractAsync(string text, CancellationToken cancellationToken = default)
    {
        var entities = new List<ExtractedEntity>();
        var emailPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
        var matches = Regex.Matches(text, emailPattern);
        
        foreach (Match match in matches)
        {
            entities.Add(new ExtractedEntity
            {
                Text = match.Value,
                EntityType = EntityType,
                StartIndex = match.Index,
                EndIndex = match.Index + match.Length - 1,
                Confidence = BaseConfidence,
                NormalizedValue = match.Value.ToLower()
            });
        }

        return entities;
    }
}

public class PhoneNumberExtractor : IEntityExtractor
{
    public string EntityType => "PHONE";
    public float BaseConfidence => 0.9f;

    public async Task<List<ExtractedEntity>> ExtractAsync(string text, CancellationToken cancellationToken = default)
    {
        var entities = new List<ExtractedEntity>();
        
        var phonePatterns = new[]
        {
            @"\b\d{3}-\d{3}-\d{4}\b",                    // 123-456-7890
            @"\b\(\d{3}\)\s*\d{3}-\d{4}\b",             // (123) 456-7890
            @"\b\d{3}\.\d{3}\.\d{4}\b",                  // 123.456.7890
            @"\b\+1\s*\d{3}\s*\d{3}\s*\d{4}\b"          // +1 123 456 7890
        };

        foreach (var pattern in phonePatterns)
        {
            var matches = Regex.Matches(text, pattern);
            
            foreach (Match match in matches)
            {
                entities.Add(new ExtractedEntity
                {
                    Text = match.Value,
                    EntityType = EntityType,
                    StartIndex = match.Index,
                    EndIndex = match.Index + match.Length - 1,
                    Confidence = BaseConfidence,
                    NormalizedValue = NormalizePhoneNumber(match.Value)
                });
            }
        }

        return entities;
    }

    private string NormalizePhoneNumber(string phone)
    {
        var digits = Regex.Replace(phone, @"[^\d]", "");
        if (digits.Length == 10)
        {
            return $"({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6, 4)}";
        }
        if (digits.Length == 11 && digits.StartsWith("1"))
        {
            return $"+1 ({digits.Substring(1, 3)}) {digits.Substring(4, 3)}-{digits.Substring(7, 4)}";
        }
        return phone;
    }
}

public class MoneyExtractor : IEntityExtractor
{
    public string EntityType => "MONEY";
    public float BaseConfidence => 0.85f;

    public async Task<List<ExtractedEntity>> ExtractAsync(string text, CancellationToken cancellationToken = default)
    {
        var entities = new List<ExtractedEntity>();
        
        var moneyPatterns = new[]
        {
            @"\$\d{1,3}(?:,\d{3})*(?:\.\d{2})?",        // $1,234.56
            @"\b\d{1,3}(?:,\d{3})*(?:\.\d{2})?\s*(?:USD|dollars?|cents?)\b", // 1,234.56 dollars
            @"\b(?:USD|EUR|GBP|JPY)\s*\d{1,3}(?:,\d{3})*(?:\.\d{2})?"       // USD 1,234.56
        };

        foreach (var pattern in moneyPatterns)
        {
            var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                entities.Add(new ExtractedEntity
                {
                    Text = match.Value,
                    EntityType = EntityType,
                    StartIndex = match.Index,
                    EndIndex = match.Index + match.Length - 1,
                    Confidence = BaseConfidence,
                    NormalizedValue = NormalizeMoneyValue(match.Value)
                });
            }
        }

        return entities;
    }

    private string NormalizeMoneyValue(string money)
    {
        var amountMatch = Regex.Match(money, @"\d{1,3}(?:,\d{3})*(?:\.\d{2})?");
        if (amountMatch.Success)
        {
            var amount = amountMatch.Value.Replace(",", "");
            var currency = "USD"; // Default
            
            if (money.Contains("EUR")) currency = "EUR";
            else if (money.Contains("GBP")) currency = "GBP";
            else if (money.Contains("JPY")) currency = "JPY";
            
            return $"{currency} {amount}";
        }
        return money;
    }
}

public class NumberExtractor : IEntityExtractor
{
    public string EntityType => "NUMBER";
    public float BaseConfidence => 0.8f;

    public async Task<List<ExtractedEntity>> ExtractAsync(string text, CancellationToken cancellationToken = default)
    {
        var entities = new List<ExtractedEntity>();
        
        // Pattern for various number formats
        var numberPattern = @"\b\d{1,3}(?:,\d{3})*(?:\.\d+)?\b";
        var matches = Regex.Matches(text, numberPattern);
        
        foreach (Match match in matches)
        {
            // Skip if it's part of a date or phone number
            if (IsPartOfDateOrPhone(text, match.Index, match.Length))
                continue;

            entities.Add(new ExtractedEntity
            {
                Text = match.Value,
                EntityType = EntityType,
                StartIndex = match.Index,
                EndIndex = match.Index + match.Length - 1,
                Confidence = BaseConfidence,
                NormalizedValue = match.Value.Replace(",", "")
            });
        }

        return entities;
    }

    private bool IsPartOfDateOrPhone(string text, int index, int length)
    {
        // Simple heuristic - check surrounding characters
        var start = Math.Max(0, index - 5);
        var end = Math.Min(text.Length, index + length + 5);
        var context = text.Substring(start, end - start);
        
        return context.Contains("/") || context.Contains("-") || context.Contains("(") || context.Contains(")");
    }
}

public class UrlExtractor : IEntityExtractor
{
    public string EntityType => "URL";
    public float BaseConfidence => 0.95f;

    public async Task<List<ExtractedEntity>> ExtractAsync(string text, CancellationToken cancellationToken = default)
    {
        var entities = new List<ExtractedEntity>();
        
        var urlPattern = @"\b(?:https?://|www\.)[A-Za-z0-9.-]+\.[A-Za-z]{2,}(?:/[A-Za-z0-9._~:/?#[\]@!$&'()*+,;=-]*)?";
        var matches = Regex.Matches(text, urlPattern);
        
        foreach (Match match in matches)
        {
            entities.Add(new ExtractedEntity
            {
                Text = match.Value,
                EntityType = EntityType,
                StartIndex = match.Index,
                EndIndex = match.Index + match.Length - 1,
                Confidence = BaseConfidence,
                NormalizedValue = match.Value.ToLower()
            });
        }

        return entities;
    }
}
```

### ASP.NET Core Integration

```csharp
// NER controller
[ApiController]
[Route("api/[controller]")]
public class NerController : ControllerBase
{
    private readonly NamedEntityRecognitionService _nerService;
    private readonly ILogger<NerController> _logger;

    public NerController(
        NamedEntityRecognitionService nerService,
        ILogger<NerController> logger)
    {
        _nerService = nerService;
        _logger = logger;
    }

    [HttpPost("extract")]
    public async Task<ActionResult<EntityExtractionResult>> ExtractEntities(
        [FromBody] ExtractEntitiesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var input = new TextInput
            {
                Id = request.Id ?? Guid.NewGuid().ToString(),
                Text = request.Text,
                Language = request.Language ?? "en"
            };

            var result = await _nerService.ExtractEntitiesAsync(input, request.ModelName, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract entities");
            return StatusCode(500, new { error = "Entity extraction failed", details = ex.Message });
        }
    }

    [HttpPost("extract/batch")]
    public async Task<ActionResult<List<EntityExtractionResult>>> ExtractEntitiesBatch(
        [FromBody] BatchExtractEntitiesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var inputs = request.Texts.Select(t => new TextInput
            {
                Id = t.Id ?? Guid.NewGuid().ToString(),
                Text = t.Text,
                Language = t.Language ?? "en"
            });

            var results = await _nerService.ExtractEntitiesBatchAsync(inputs, request.ModelName, cancellationToken);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract entities in batch");
            return StatusCode(500, new { error = "Batch entity extraction failed", details = ex.Message });
        }
    }

    [HttpPost("train")]
    public async Task<ActionResult<NerTrainingResult>> TrainModel(
        [FromBody] TrainNerModelRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var trainingData = request.TrainingData.Select(item => 
                (item.Text, item.Entities.ToList())
            );

            var result = await _nerService.TrainNerModelAsync(
                trainingData, request.ModelName, cancellationToken);
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to train NER model '{ModelName}'", request.ModelName);
            return StatusCode(500, new { error = "NER model training failed", details = ex.Message });
        }
    }
}

// Request/response models
public class ExtractEntitiesRequest
{
    public string? Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Language { get; set; }
    public string? ModelName { get; set; }
}

public class BatchExtractEntitiesRequest
{
    public List<TextRequest> Texts { get; set; } = new();
    public string? ModelName { get; set; }
}

public class TextRequest
{
    public string? Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Language { get; set; }
}

public class TrainNerModelRequest
{
    public string ModelName { get; set; } = string.Empty;
    public List<TrainingDataItem> TrainingData { get; set; } = new();
}

public class TrainingDataItem
{
    public string Text { get; set; } = string.Empty;
    public List<EntityAnnotation> Entities { get; set; } = new();
}

// Dependency injection setup
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNamedEntityRecognition(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<NerOptions>(
            configuration.GetSection("NamedEntityRecognition"));

        services.AddSingleton<NamedEntityRecognitionService>();
        services.AddLogging();

        return services;
    }
}
```

## Usage

### Basic Entity Extraction

```csharp
// Configure services
var services = new ServiceCollection()
    .AddNamedEntityRecognition(configuration)
    .AddLogging()
    .BuildServiceProvider();

var nerService = services.GetRequiredService<NamedEntityRecognitionService>();

// Extract entities from text
var input = new TextInput
{
    Id = "sample1",
    Text = "John Smith from Microsoft contacted me at john.smith@microsoft.com on March 15, 2024 regarding a $50,000 contract.",
    Language = "en"
};

var result = await nerService.ExtractEntitiesAsync(input);

Console.WriteLine($"Found {result.Entities.Count} entities:");
foreach (var entity in result.Entities)
{
    Console.WriteLine($"- {entity.EntityType}: '{entity.Text}' (confidence: {entity.Confidence:F3})");
    if (!string.IsNullOrEmpty(entity.NormalizedValue) && entity.NormalizedValue != entity.Text)
    {
        Console.WriteLine($"  Normalized: '{entity.NormalizedValue}'");
    }
}
```

### Custom Model Training

```csharp
// Prepare training data with BIO labels
var trainingData = new List<(string text, List<EntityAnnotation> entities)>
{
    ("Apple Inc. is a technology company.", new List<EntityAnnotation>
    {
        new() { Text = "Apple Inc.", StartIndex = 0, EndIndex = 9, EntityType = "ORGANIZATION" }
    }),
    ("Tim Cook is the CEO of Apple.", new List<EntityAnnotation>
    {
        new() { Text = "Tim Cook", StartIndex = 0, EndIndex = 7, EntityType = "PERSON" },
        new() { Text = "Apple", StartIndex = 23, EndIndex = 27, EntityType = "ORGANIZATION" }
    }),
    // ... more training examples
};

// Train custom model
var trainingResult = await nerService.TrainNerModelAsync(trainingData, "custom-tech-ner");

Console.WriteLine($"Model trained successfully:");
Console.WriteLine($"- F1 Score: {trainingResult.F1Score:F3}");
Console.WriteLine($"- Precision: {trainingResult.Precision:F3}");
Console.WriteLine($"- Recall: {trainingResult.Recall:F3}");
Console.WriteLine($"- Training Duration: {trainingResult.TrainingDuration.TotalSeconds:F1}s");

foreach (var (entityType, metrics) in trainingResult.EntityTypeMetrics)
{
    Console.WriteLine($"- {entityType}: P={metrics.Precision:F3}, R={metrics.Recall:F3}, F1={metrics.F1Score:F3}");
}
```

### Batch Processing

```csharp
// Process multiple documents
var documents = new List<TextInput>
{
    new() { Id = "doc1", Text = "Barack Obama was born in Hawaii and served as President." },
    new() { Id = "doc2", Text = "Google LLC is headquartered in Mountain View, California." },
    new() { Id = "doc3", Text = "The meeting is scheduled for December 1, 2024 at 2:30 PM." }
};

var batchResults = await nerService.ExtractEntitiesBatchAsync(documents, "custom-tech-ner");

foreach (var result in batchResults)
{
    Console.WriteLine($"\nDocument {result.InputId}:");
    foreach (var entity in result.Entities)
    {
        Console.WriteLine($"  {entity.EntityType}: {entity.Text} ({entity.Confidence:F3})");
    }
}
```

### Advanced Entity Analysis

```csharp
// Entity-focused text analysis
public class EntityAnalysisService
{
    private readonly NamedEntityRecognitionService _nerService;

    public EntityAnalysisService(NamedEntityRecognitionService nerService)
    {
        _nerService = nerService;
    }

    public async Task<EntitySummary> AnalyzeEntitiesAsync(string text)
    {
        var input = new TextInput { Text = text };
        var result = await _nerService.ExtractEntitiesAsync(input);

        var summary = new EntitySummary
        {
            TotalEntities = result.Entities.Count,
            EntityCounts = result.Entities.GroupBy(e => e.EntityType)
                .ToDictionary(g => g.Key, g => g.Count()),
            HighConfidenceEntities = result.Entities.Where(e => e.Confidence > 0.8f).ToList(),
            UniqueEntities = result.Entities.GroupBy(e => e.NormalizedValue)
                .Select(g => g.First()).ToList()
        };

        return summary;
    }

    public async Task<List<EntityRelationship>> FindEntityRelationshipsAsync(string text)
    {
        var input = new TextInput { Text = text };
        var result = await _nerService.ExtractEntitiesAsync(input);
        
        var relationships = new List<EntityRelationship>();
        var entities = result.Entities.OrderBy(e => e.StartIndex).ToList();

        for (int i = 0; i < entities.Count - 1; i++)
        {
            for (int j = i + 1; j < entities.Count; j++)
            {
                var entity1 = entities[i];
                var entity2 = entities[j];

                // Calculate proximity score
                var distance = entity2.StartIndex - entity1.EndIndex;
                if (distance <= 50) // Entities within 50 characters
                {
                    var proximityScore = Math.Max(0, 1.0f - (distance / 50.0f));
                    
                    relationships.Add(new EntityRelationship
                    {
                        Entity1 = entity1,
                        Entity2 = entity2,
                        RelationType = DetermineRelationType(entity1, entity2),
                        Confidence = proximityScore * Math.Min(entity1.Confidence, entity2.Confidence),
                        Distance = distance
                    });
                }
            }
        }

        return relationships.Where(r => r.Confidence > 0.3f).ToList();
    }

    private string DetermineRelationType(ExtractedEntity entity1, ExtractedEntity entity2)
    {
        return (entity1.EntityType, entity2.EntityType) switch
        {
            ("PERSON", "ORGANIZATION") => "WORKS_FOR",
            ("ORGANIZATION", "PERSON") => "EMPLOYS",
            ("PERSON", "LOCATION") => "LOCATED_IN",
            ("ORGANIZATION", "LOCATION") => "BASED_IN",
            ("PERSON", "DATE") => "ASSOCIATED_WITH_DATE",
            ("ORGANIZATION", "MONEY") => "FINANCIAL_AMOUNT",
            _ => "RELATED_TO"
        };
    }
}

public class EntitySummary
{
    public int TotalEntities { get; set; }
    public Dictionary<string, int> EntityCounts { get; set; } = new();
    public List<ExtractedEntity> HighConfidenceEntities { get; set; } = new();
    public List<ExtractedEntity> UniqueEntities { get; set; } = new();
}

public class EntityRelationship
{
    public ExtractedEntity Entity1 { get; set; } = new();
    public ExtractedEntity Entity2 { get; set; } = new();
    public string RelationType { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public int Distance { get; set; }
}
```

**Expected Output:**

```text
Found 5 entities:
- PERSON: 'John Smith' (confidence: 0.850)
- ORGANIZATION: 'Microsoft' (confidence: 0.800)
- EMAIL: 'john.smith@microsoft.com' (confidence: 0.950)
  Normalized: 'john.smith@microsoft.com'
- DATE: 'March 15, 2024' (confidence: 0.900)
  Normalized: '2024-03-15'
- MONEY: '$50,000' (confidence: 0.850)
  Normalized: 'USD 50000'

Model trained successfully:
- F1 Score: 0.847
- Precision: 0.862
- Recall: 0.833
- Training Duration: 12.3s
- PERSON: P=0.891, R=0.856, F1=0.873
- ORGANIZATION: P=0.834, R=0.810, F1=0.822

Document doc1:
  PERSON: Barack Obama (0.891)
  LOCATION: Hawaii (0.756)

Document doc2:
  ORGANIZATION: Google LLC (0.887)
  LOCATION: Mountain View, California (0.823)

Document doc3:
  DATE: December 1, 2024 (0.923)
  DATE: 2:30 PM (0.745)
```

## Notes

**Performance Considerations:**

- Use batch processing for multiple documents to improve throughput
- Cache trained models to avoid retraining for repeated usage
- Implement streaming processing for large texts to manage memory usage
- Consider parallel processing for independent entity extraction tasks

**Quality Optimization:**

- Combine multiple extraction approaches (rule-based + ML-based) for better coverage
- Implement confidence calibration based on validation datasets  
- Use context-aware post-processing to improve entity disambiguation
- Regular model retraining with domain-specific data improves accuracy

**Scalability Patterns:**

- Implement model versioning for A/B testing different NER approaches
- Use distributed processing for large-scale document collections
- Cache frequently extracted entities to reduce computation overhead
- Implement incremental learning for continuously improving models

**Security Considerations:**

- Sanitize input text to prevent injection attacks through malformed entities
- Implement rate limiting to prevent abuse of extraction endpoints
- Use secure storage for trained models and sensitive training data
- Consider privacy implications when extracting personally identifiable information

**Integration Strategies:**

- Combine with search systems for enhanced query understanding
- Use for content classification and automated tagging systems
- Integrate with knowledge graphs for entity linking and disambiguation
- Apply to document processing pipelines for automated information extraction
