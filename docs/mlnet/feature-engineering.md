# Feature Engineering for ML.NET

**Description**: Comprehensive text preprocessing and feature transformation patterns for ML.NET applications with advanced vectorization techniques, custom feature extractors, and pipeline optimization strategies.

**Language/Technology**: C#, ML.NET, Text Processing, Feature Engineering

**Code**:

## Text Preprocessing Pipeline

### Advanced Text Preprocessor

```csharp
namespace DocumentProcessor.ML.Features;

using Microsoft.ML;
using Microsoft.ML.Data;
using System.Text.RegularExpressions;
using System.Globalization;

public interface ITextPreprocessor
{
    Task<IEnumerable<string>> PreprocessAsync(string text);
    Task<PreprocessingResult> PreprocessWithMetadataAsync(string text);
    Task<List<PreprocessingResult>> PreprocessBatchAsync(IEnumerable<string> texts);
    Task<TextStatistics> AnalyzeTextAsync(string text);
}

public class TextPreprocessor : ITextPreprocessor
{
    private readonly ILogger<TextPreprocessor> _logger;
    private readonly PreprocessingOptions _options;
    private readonly HashSet<string> _stopWords;
    private readonly Dictionary<string, string> _synonymMap;
    private readonly Regex _urlRegex;
    private readonly Regex _emailRegex;
    private readonly Regex _phoneRegex;
    private readonly Regex _numberRegex;

    public TextPreprocessor(
        ILogger<TextPreprocessor> logger,
        IOptions<PreprocessingOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _stopWords = LoadStopWords(_options.StopWordsLanguage);
        _synonymMap = LoadSynonymMap(_options.SynonymMappingPath);
        
        // Compiled regex patterns for performance
        _urlRegex = new Regex(@"https?://[^\s]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        _emailRegex = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);
        _phoneRegex = new Regex(@"(\+?1[-.\s]?)?(\(?\d{3}\)?[-.\s]?)?\d{3}[-.\s]?\d{4}", RegexOptions.Compiled);
        _numberRegex = new Regex(@"\b\d+\.?\d*\b", RegexOptions.Compiled);
    }

    public async Task<IEnumerable<string>> PreprocessAsync(string text)
    {
        var result = await PreprocessWithMetadataAsync(text);
        return result.ProcessedTokens;
    }

    public async Task<PreprocessingResult> PreprocessWithMetadataAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new PreprocessingResult(
                OriginalText: text ?? string.Empty,
                ProcessedText: string.Empty,
                ProcessedTokens: Array.Empty<string>(),
                ExtractedEntities: new Dictionary<string, List<string>>(),
                Statistics: new TextStatistics(0, 0, 0, 0, 0),
                ProcessingTime: TimeSpan.Zero);
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var entities = new Dictionary<string, List<string>>();

        // Extract and replace entities if configured
        var processedText = text;
        
        if (_options.ExtractUrls)
        {
            var urls = ExtractMatches(_urlRegex, processedText);
            entities["urls"] = urls;
            processedText = _urlRegex.Replace(processedText, " URL_TOKEN ");
        }

        if (_options.ExtractEmails)
        {
            var emails = ExtractMatches(_emailRegex, processedText);
            entities["emails"] = emails;
            processedText = _emailRegex.Replace(processedText, " EMAIL_TOKEN ");
        }

        if (_options.ExtractPhones)
        {
            var phones = ExtractMatches(_phoneRegex, processedText);
            entities["phones"] = phones;
            processedText = _phoneRegex.Replace(processedText, " PHONE_TOKEN ");
        }

        if (_options.ExtractNumbers)
        {
            var numbers = ExtractMatches(_numberRegex, processedText);
            entities["numbers"] = numbers;
            processedText = _numberRegex.Replace(processedText, " NUMBER_TOKEN ");
        }

        // Normalize text
        if (_options.NormalizeCase)
        {
            processedText = processedText.ToLowerInvariant();
        }

        if (_options.RemoveAccents)
        {
            processedText = RemoveAccents(processedText);
        }

        // Remove special characters
        if (_options.RemoveSpecialChars)
        {
            processedText = Regex.Replace(processedText, @"[^\w\s]", " ");
        }

        // Normalize whitespace
        processedText = Regex.Replace(processedText, @"\s+", " ").Trim();

        // Tokenize
        var tokens = processedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Filter tokens
        var filteredTokens = new List<string>();
        
        foreach (var token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token) || 
                token.Length < _options.MinTokenLength || 
                token.Length > _options.MaxTokenLength)
            {
                continue;
            }

            if (_options.RemoveStopWords && _stopWords.Contains(token))
            {
                continue;
            }

            // Apply synonym mapping
            var finalToken = _options.ApplySynonyms && _synonymMap.ContainsKey(token) 
                ? _synonymMap[token] 
                : token;

            filteredTokens.Add(finalToken);
        }

        // Apply stemming/lemmatization if configured
        if (_options.EnableStemming)
        {
            filteredTokens = ApplyStemming(filteredTokens);
        }

        var statistics = await AnalyzeTextAsync(text);
        stopwatch.Stop();

        var result = new PreprocessingResult(
            OriginalText: text,
            ProcessedText: string.Join(" ", filteredTokens),
            ProcessedTokens: filteredTokens.ToArray(),
            ExtractedEntities: entities,
            Statistics: statistics,
            ProcessingTime: stopwatch.Elapsed);

        _logger.LogDebug("Preprocessed text: {OriginalLength} -> {ProcessedLength} chars, {TokenCount} tokens in {Duration}ms",
            text.Length, result.ProcessedText.Length, filteredTokens.Count, stopwatch.ElapsedMilliseconds);

        return result;
    }

    public async Task<List<PreprocessingResult>> PreprocessBatchAsync(IEnumerable<string> texts)
    {
        var tasks = texts.Select(text => PreprocessWithMetadataAsync(text));
        var results = await Task.WhenAll(tasks);
        
        _logger.LogInformation("Preprocessed batch of {Count} texts", results.Length);
        return results.ToList();
    }

    public async Task<TextStatistics> AnalyzeTextAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new TextStatistics(0, 0, 0, 0, 0);
        }

        var characterCount = text.Length;
        var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var sentenceCount = text.Split('.', '!', '?', StringSplitOptions.RemoveEmptyEntries).Length;
        var paragraphCount = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
        
        // Calculate reading complexity (simplified Flesch Reading Ease)
        var averageWordsPerSentence = sentenceCount > 0 ? (double)wordCount / sentenceCount : 0;
        var readabilityScore = 206.835 - (1.015 * averageWordsPerSentence);

        return await Task.FromResult(new TextStatistics(
            CharacterCount: characterCount,
            WordCount: wordCount,
            SentenceCount: sentenceCount,
            ParagraphCount: paragraphCount,
            ReadabilityScore: readabilityScore));
    }

    private List<string> ExtractMatches(Regex regex, string text)
    {
        return regex.Matches(text)
            .Cast<Match>()
            .Select(m => m.Value)
            .Distinct()
            .ToList();
    }

    private string RemoveAccents(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    private List<string> ApplyStemming(List<string> tokens)
    {
        // Simplified Porter Stemmer implementation
        // In practice, you'd use a proper stemming library like Lucene.NET or SharpNLP
        
        return tokens.Select(token =>
        {
            if (token.EndsWith("ing") && token.Length > 5)
                return token[..^3];
            if (token.EndsWith("ed") && token.Length > 4)
                return token[..^2];
            if (token.EndsWith("er") && token.Length > 4)
                return token[..^2];
            if (token.EndsWith("ly") && token.Length > 4)
                return token[..^2];
            
            return token;
        }).ToList();
    }

    private HashSet<string> LoadStopWords(string language)
    {
        // Load stop words from embedded resource or file
        var defaultStopWords = new[]
        {
            "a", "an", "and", "are", "as", "at", "be", "by", "for", "from",
            "has", "he", "in", "is", "it", "its", "of", "on", "that", "the",
            "to", "was", "will", "with", "the", "this", "but", "they", "have",
            "had", "what", "said", "each", "which", "she", "do", "how", "their",
            "if", "up", "out", "many", "then", "them", "these", "so", "some"
        };

        return new HashSet<string>(defaultStopWords, StringComparer.OrdinalIgnoreCase);
    }

    private Dictionary<string, string> LoadSynonymMap(string? mappingPath)
    {
        // Load synonym mappings from file or configuration
        var defaultSynonyms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "good", "positive" },
            { "bad", "negative" },
            { "great", "excellent" },
            { "awful", "terrible" },
            { "ok", "acceptable" },
            { "okay", "acceptable" }
        };

        return defaultSynonyms;
    }
}

public record PreprocessingResult(
    string OriginalText,
    string ProcessedText,
    string[] ProcessedTokens,
    Dictionary<string, List<string>> ExtractedEntities,
    TextStatistics Statistics,
    TimeSpan ProcessingTime);

public record TextStatistics(
    int CharacterCount,
    int WordCount,
    int SentenceCount,
    int ParagraphCount,
    double ReadabilityScore);

public class PreprocessingOptions
{
    public const string SectionName = "Preprocessing";
    
    public bool NormalizeCase { get; set; } = true;
    public bool RemoveAccents { get; set; } = true;
    public bool RemoveSpecialChars { get; set; } = true;
    public bool RemoveStopWords { get; set; } = true;
    public bool ExtractUrls { get; set; } = true;
    public bool ExtractEmails { get; set; } = true;
    public bool ExtractPhones { get; set; } = true;
    public bool ExtractNumbers { get; set; } = true;
    public bool ApplySynonyms { get; set; } = true;
    public bool EnableStemming { get; set; } = false;
    public int MinTokenLength { get; set; } = 2;
    public int MaxTokenLength { get; set; } = 50;
    public string StopWordsLanguage { get; set; } = "en";
    public string? SynonymMappingPath { get; set; }
}
```

## Feature Engineering Pipeline

### Advanced Feature Engineer

```csharp
namespace DocumentProcessor.ML.Features;

using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using Microsoft.ML.Transforms.Text;

public interface IFeatureEngineer
{
    Task<FeatureSet> ExtractFeaturesAsync(string text);
    Task<List<FeatureSet>> ExtractBatchFeaturesAsync(IEnumerable<string> texts);
    IEstimator<ITransformer> BuildFeaturePipeline(MLContext mlContext, FeatureConfiguration config);
    Task<FeatureImportanceResult> AnalyzeFeatureImportanceAsync(IEnumerable<FeatureSet> features, IEnumerable<string> labels);
}

public class FeatureEngineer : IFeatureEngineer
{
    private readonly MLContext _mlContext;
    private readonly ITextPreprocessor _preprocessor;
    private readonly ILogger<FeatureEngineer> _logger;
    private readonly FeatureConfiguration _config;

    public FeatureEngineer(
        MLContext mlContext,
        ITextPreprocessor preprocessor,
        ILogger<FeatureEngineer> logger,
        IOptions<FeatureConfiguration> config)
    {
        _mlContext = mlContext;
        _preprocessor = preprocessor;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<FeatureSet> ExtractFeaturesAsync(string text)
    {
        var preprocessed = await _preprocessor.PreprocessWithMetadataAsync(text);
        
        var features = new FeatureSet
        {
            OriginalText = text,
            ProcessedText = preprocessed.ProcessedText,
            
            // Basic statistical features
            CharacterCount = preprocessed.Statistics.CharacterCount,
            WordCount = preprocessed.Statistics.WordCount,
            SentenceCount = preprocessed.Statistics.SentenceCount,
            AverageWordLength = preprocessed.ProcessedTokens.Length > 0 
                ? preprocessed.ProcessedTokens.Average(t => t.Length) 
                : 0,
            
            // Advanced linguistic features
            UniqueWordRatio = CalculateUniqueWordRatio(preprocessed.ProcessedTokens),
            ReadabilityScore = preprocessed.Statistics.ReadabilityScore,
            
            // N-gram features
            Unigrams = ExtractNGrams(preprocessed.ProcessedTokens, 1),
            Bigrams = ExtractNGrams(preprocessed.ProcessedTokens, 2),
            Trigrams = ExtractNGrams(preprocessed.ProcessedTokens, 3),
            
            // Syntactic features
            CapitalizedWordCount = CountCapitalizedWords(text),
            PunctuationCount = CountPunctuation(text),
            NumberCount = preprocessed.ExtractedEntities.GetValueOrDefault("numbers", new List<string>()).Count,
            
            // Semantic features
            SentimentIndicators = ExtractSentimentIndicators(preprocessed.ProcessedTokens),
            TopicIndicators = ExtractTopicIndicators(preprocessed.ProcessedTokens),
            
            // Entity features
            ExtractedEntities = preprocessed.ExtractedEntities,
            
            ProcessingMetadata = new FeatureMetadata(
                ExtractionTime: DateTime.UtcNow,
                ProcessingDuration: TimeSpan.Zero,
                FeatureCount: 0) // Will be calculated after all features are extracted
        };

        // Calculate TF-IDF vectors if vocabulary is available
        if (_config.EnableTfIdf && _config.Vocabulary?.Any() == true)
        {
            features.TfIdfVector = CalculateTfIdfVector(preprocessed.ProcessedTokens, _config.Vocabulary);
        }

        // Calculate feature count
        var featureCount = CountFeatures(features);
        features.ProcessingMetadata = features.ProcessingMetadata with { FeatureCount = featureCount };

        _logger.LogDebug("Extracted {FeatureCount} features from text with {WordCount} words",
            featureCount, features.WordCount);

        return features;
    }

    public async Task<List<FeatureSet>> ExtractBatchFeaturesAsync(IEnumerable<string> texts)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var tasks = texts.Select(text => ExtractFeaturesAsync(text));
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        _logger.LogInformation("Extracted features from batch of {Count} texts in {Duration}ms",
            results.Length, stopwatch.ElapsedMilliseconds);

        return results.ToList();
    }

    public IEstimator<ITransformer> BuildFeaturePipeline(MLContext mlContext, FeatureConfiguration config)
    {
        var pipeline = mlContext.Transforms.Text.NormalizeText("NormalizedText", "Text", 
            keepDiacritics: !config.RemoveAccents,
            keepPunctuations: !config.RemoveSpecialChars,
            keepNumbers: !config.ExtractNumbers);

        // Tokenization
        pipeline = pipeline.Append(mlContext.Transforms.Text.TokenizeIntoWords("Tokens", "NormalizedText"));

        // Remove stop words if configured
        if (config.RemoveStopWords)
        {
            pipeline = pipeline.Append(mlContext.Transforms.Text.RemoveDefaultStopWords("TokensNoStopWords", "Tokens"));
        }
        else
        {
            pipeline = pipeline.Append(mlContext.Transforms.CopyColumns("TokensNoStopWords", "Tokens"));
        }

        // Generate n-grams
        if (config.EnableNGrams)
        {
            pipeline = pipeline.Append(mlContext.Transforms.Text.ProduceNgrams("NGrams", "TokensNoStopWords",
                ngramLength: config.MaxNGramLength,
                useAllLengths: config.UseAllNGramLengths,
                weighting: config.NGramWeighting));
        }

        // Word embeddings (if available)
        if (config.EnableWordEmbeddings && !string.IsNullOrEmpty(config.WordEmbeddingsPath))
        {
            pipeline = pipeline.Append(mlContext.Transforms.Text.ApplyWordEmbedding("WordEmbeddings", "TokensNoStopWords",
                WordEmbeddingEstimator.PretrainedModelKind.GloVeTwitter25D));
        }

        // TF-IDF vectorization
        if (config.EnableTfIdf)
        {
            pipeline = pipeline.Append(mlContext.Transforms.Text.ProduceTfIdf("TfIdfFeatures", "TokensNoStopWords"));
        }

        // Character n-grams for handling misspellings and OOV words
        if (config.EnableCharNGrams)
        {
            pipeline = pipeline.Append(mlContext.Transforms.Text.TokenizeIntoCharactersAsKeys("CharTokens", "NormalizedText"))
                .Append(mlContext.Transforms.Text.ProduceNgrams("CharNGrams", "CharTokens",
                    ngramLength: config.MaxCharNGramLength,
                    useAllLengths: true,
                    weighting: NgramExtractingEstimator.WeightingCriteria.TfIdf));
        }

        // Combine all features
        var featureColumns = new List<string>();
        
        if (config.EnableNGrams)
            featureColumns.Add("NGrams");
        if (config.EnableTfIdf)
            featureColumns.Add("TfIdfFeatures");
        if (config.EnableWordEmbeddings)
            featureColumns.Add("WordEmbeddings");
        if (config.EnableCharNGrams)
            featureColumns.Add("CharNGrams");

        if (featureColumns.Any())
        {
            pipeline = pipeline.Append(mlContext.Transforms.Concatenate("Features", featureColumns.ToArray()));
        }

        return pipeline;
    }

    public async Task<FeatureImportanceResult> AnalyzeFeatureImportanceAsync(
        IEnumerable<FeatureSet> features, 
        IEnumerable<string> labels)
    {
        var featureList = features.ToList();
        var labelList = labels.ToList();

        if (featureList.Count != labelList.Count)
        {
            throw new ArgumentException("Feature count must match label count");
        }

        var importanceScores = new Dictionary<string, double>();
        var correlations = new Dictionary<string, double>();

        // Calculate mutual information for each feature
        var uniqueLabels = labelList.Distinct().ToList();
        
        // Analyze n-gram importance
        var allUnigrams = featureList.SelectMany(f => f.Unigrams.Keys).Distinct().ToList();
        
        foreach (var unigram in allUnigrams.Take(1000)) // Limit for performance
        {
            var mutualInfo = CalculateMutualInformation(featureList, labelList, unigram, "unigram");
            importanceScores[$"unigram_{unigram}"] = mutualInfo;
        }

        // Analyze statistical feature importance
        var statFeatures = new[] { "WordCount", "CharacterCount", "ReadabilityScore", "UniqueWordRatio" };
        
        foreach (var feature in statFeatures)
        {
            var correlation = CalculateFeatureCorrelation(featureList, labelList, feature);
            correlations[feature] = correlation;
            importanceScores[feature] = Math.Abs(correlation);
        }

        var result = new FeatureImportanceResult(
            ImportanceScores: importanceScores.OrderByDescending(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            Correlations: correlations,
            TopFeatures: importanceScores.OrderByDescending(kvp => kvp.Value).Take(20).Select(kvp => kvp.Key).ToList(),
            AnalysisDate: DateTime.UtcNow,
            SampleCount: featureList.Count);

        _logger.LogInformation("Analyzed feature importance for {FeatureCount} features across {SampleCount} samples",
            importanceScores.Count, featureList.Count);

        return await Task.FromResult(result);
    }

    private double CalculateUniqueWordRatio(string[] tokens)
    {
        if (tokens.Length == 0) return 0;
        return (double)tokens.Distinct().Count() / tokens.Length;
    }

    private Dictionary<string, int> ExtractNGrams(string[] tokens, int n)
    {
        var ngrams = new Dictionary<string, int>();
        
        for (int i = 0; i <= tokens.Length - n; i++)
        {
            var ngram = string.Join(" ", tokens.Skip(i).Take(n));
            ngrams[ngram] = ngrams.GetValueOrDefault(ngram, 0) + 1;
        }

        return ngrams;
    }

    private int CountCapitalizedWords(string text)
    {
        return Regex.Matches(text, @"\b[A-Z][a-z]+\b").Count;
    }

    private int CountPunctuation(string text)
    {
        return text.Count(char.IsPunctuation);
    }

    private Dictionary<string, float> ExtractSentimentIndicators(string[] tokens)
    {
        var positiveWords = new[] { "good", "great", "excellent", "amazing", "wonderful", "fantastic", "love", "perfect" };
        var negativeWords = new[] { "bad", "terrible", "awful", "horrible", "hate", "worst", "disgusting", "pathetic" };

        var indicators = new Dictionary<string, float>
        {
            ["positive_word_count"] = tokens.Count(t => positiveWords.Contains(t, StringComparer.OrdinalIgnoreCase)),
            ["negative_word_count"] = tokens.Count(t => negativeWords.Contains(t, StringComparer.OrdinalIgnoreCase)),
            ["exclamation_sentiment"] = tokens.Count(t => t.Contains('!')),
            ["question_sentiment"] = tokens.Count(t => t.Contains('?'))
        };

        return indicators;
    }

    private Dictionary<string, float> ExtractTopicIndicators(string[] tokens)
    {
        var techWords = new[] { "technology", "software", "computer", "digital", "algorithm", "data", "system" };
        var businessWords = new[] { "business", "market", "customer", "revenue", "profit", "sales", "strategy" };
        var scienceWords = new[] { "research", "study", "analysis", "experiment", "hypothesis", "theory", "method" };

        var indicators = new Dictionary<string, float>
        {
            ["tech_word_density"] = (float)tokens.Count(t => techWords.Contains(t, StringComparer.OrdinalIgnoreCase)) / tokens.Length,
            ["business_word_density"] = (float)tokens.Count(t => businessWords.Contains(t, StringComparer.OrdinalIgnoreCase)) / tokens.Length,
            ["science_word_density"] = (float)tokens.Count(t => scienceWords.Contains(t, StringComparer.OrdinalIgnoreCase)) / tokens.Length
        };

        return indicators;
    }

    private Dictionary<string, float> CalculateTfIdfVector(string[] tokens, Dictionary<string, int> vocabulary)
    {
        var tokenCounts = tokens.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());
        var tfidf = new Dictionary<string, float>();

        foreach (var (term, vocabCount) in vocabulary)
        {
            var tf = tokenCounts.GetValueOrDefault(term, 0);
            var idf = Math.Log((double)vocabulary.Count / (1 + vocabCount));
            tfidf[term] = (float)(tf * idf);
        }

        return tfidf;
    }

    private int CountFeatures(FeatureSet features)
    {
        return features.Unigrams.Count + 
               features.Bigrams.Count + 
               features.Trigrams.Count +
               features.SentimentIndicators.Count +
               features.TopicIndicators.Count +
               (features.TfIdfVector?.Count ?? 0) +
               10; // Base statistical features
    }

    private double CalculateMutualInformation(List<FeatureSet> features, List<string> labels, string feature, string featureType)
    {
        // Simplified mutual information calculation
        // In practice, you'd use a more sophisticated implementation
        
        var featureValues = features.Select(f => featureType switch
        {
            "unigram" => f.Unigrams.GetValueOrDefault(feature, 0),
            _ => 0
        }).ToList();

        var uniqueLabels = labels.Distinct().ToList();
        var uniqueFeatureValues = featureValues.Distinct().ToList();

        double mutualInfo = 0.0;
        var totalSamples = features.Count;

        foreach (var label in uniqueLabels)
        {
            foreach (var featureValue in uniqueFeatureValues)
            {
                var jointCount = features.Zip(labels, (f, l) => new { Feature = f, Label = l })
                    .Count(x => x.Label == label && 
                               (featureType == "unigram" ? x.Feature.Unigrams.GetValueOrDefault(feature, 0) == featureValue : false));

                var labelCount = labels.Count(l => l == label);
                var featureCount = featureValues.Count(fv => fv == featureValue);

                if (jointCount > 0)
                {
                    var jointProb = (double)jointCount / totalSamples;
                    var labelProb = (double)labelCount / totalSamples;
                    var featureProb = (double)featureCount / totalSamples;

                    mutualInfo += jointProb * Math.Log(jointProb / (labelProb * featureProb));
                }
            }
        }

        return mutualInfo;
    }

    private double CalculateFeatureCorrelation(List<FeatureSet> features, List<string> labels, string featureName)
    {
        var featureValues = features.Select(f => featureName switch
        {
            "WordCount" => (double)f.WordCount,
            "CharacterCount" => (double)f.CharacterCount,
            "ReadabilityScore" => f.ReadabilityScore,
            "UniqueWordRatio" => f.UniqueWordRatio,
            _ => 0.0
        }).ToList();

        // Convert labels to numeric (simplified - assumes binary classification)
        var numericLabels = labels.Select(l => l.Equals("positive", StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0).ToList();

        return CalculatePearsonCorrelation(featureValues, numericLabels);
    }

    private double CalculatePearsonCorrelation(List<double> x, List<double> y)
    {
        if (x.Count != y.Count) return 0.0;

        var n = x.Count;
        var sumX = x.Sum();
        var sumY = y.Sum();
        var sumXY = x.Zip(y, (a, b) => a * b).Sum();
        var sumXX = x.Sum(a => a * a);
        var sumYY = y.Sum(b => b * b);

        var numerator = n * sumXY - sumX * sumY;
        var denominator = Math.Sqrt((n * sumXX - sumX * sumX) * (n * sumYY - sumY * sumY));

        return denominator != 0 ? numerator / denominator : 0.0;
    }
}

[Serializable]
public class FeatureSet
{
    public string OriginalText { get; set; } = string.Empty;
    public string ProcessedText { get; set; } = string.Empty;
    
    // Basic statistical features
    public int CharacterCount { get; set; }
    public int WordCount { get; set; }
    public int SentenceCount { get; set; }
    public double AverageWordLength { get; set; }
    public double UniqueWordRatio { get; set; }
    public double ReadabilityScore { get; set; }
    
    // N-gram features
    public Dictionary<string, int> Unigrams { get; set; } = new();
    public Dictionary<string, int> Bigrams { get; set; } = new();
    public Dictionary<string, int> Trigrams { get; set; } = new();
    
    // Syntactic features
    public int CapitalizedWordCount { get; set; }
    public int PunctuationCount { get; set; }
    public int NumberCount { get; set; }
    
    // Semantic features
    public Dictionary<string, float> SentimentIndicators { get; set; } = new();
    public Dictionary<string, float> TopicIndicators { get; set; } = new();
    
    // Entity features
    public Dictionary<string, List<string>> ExtractedEntities { get; set; } = new();
    
    // Vector features
    public Dictionary<string, float>? TfIdfVector { get; set; }
    public float[]? WordEmbeddings { get; set; }
    
    // Metadata
    public FeatureMetadata ProcessingMetadata { get; set; } = new(DateTime.UtcNow, TimeSpan.Zero, 0);
}

public record FeatureMetadata(
    DateTime ExtractionTime,
    TimeSpan ProcessingDuration,
    int FeatureCount);

public record FeatureImportanceResult(
    Dictionary<string, double> ImportanceScores,
    Dictionary<string, double> Correlations,
    List<string> TopFeatures,
    DateTime AnalysisDate,
    int SampleCount);

public class FeatureConfiguration
{
    public const string SectionName = "FeatureEngineering";
    
    // Basic preprocessing
    public bool RemoveAccents { get; set; } = true;
    public bool RemoveSpecialChars { get; set; } = true;
    public bool RemoveStopWords { get; set; } = true;
    public bool ExtractNumbers { get; set; } = true;
    
    // N-gram configuration
    public bool EnableNGrams { get; set; } = true;
    public int MaxNGramLength { get; set; } = 3;
    public bool UseAllNGramLengths { get; set; } = true;
    public NgramExtractingEstimator.WeightingCriteria NGramWeighting { get; set; } = 
        NgramExtractingEstimator.WeightingCriteria.TfIdf;
    
    // Character n-grams
    public bool EnableCharNGrams { get; set; } = false;
    public int MaxCharNGramLength { get; set; } = 4;
    
    // TF-IDF configuration
    public bool EnableTfIdf { get; set; } = true;
    public Dictionary<string, int>? Vocabulary { get; set; }
    
    // Word embeddings
    public bool EnableWordEmbeddings { get; set; } = false;
    public string? WordEmbeddingsPath { get; set; }
    public int EmbeddingDimensions { get; set; } = 100;
}
```

## ASP.NET Core Integration

### Feature Engineering Controller

```csharp
namespace DocumentProcessor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeatureEngineeringController : ControllerBase
{
    private readonly IFeatureEngineer _featureEngineer;
    private readonly ITextPreprocessor _preprocessor;
    private readonly ILogger<FeatureEngineeringController> _logger;

    public FeatureEngineeringController(
        IFeatureEngineer featureEngineer,
        ITextPreprocessor preprocessor,
        ILogger<FeatureEngineeringController> logger)
    {
        _featureEngineer = featureEngineer;
        _preprocessor = preprocessor;
        _logger = logger;
    }

    [HttpPost("extract")]
    public async Task<ActionResult<FeatureExtractionResponse>> ExtractFeatures(
        [FromBody] FeatureExtractionRequest request)
    {
        try
        {
            var features = await _featureEngineer.ExtractFeaturesAsync(request.Text);
            
            var response = new FeatureExtractionResponse(
                Features: features,
                RequestId: Guid.NewGuid().ToString(),
                ProcessedAt: DateTime.UtcNow);

            _logger.LogInformation("Extracted features for text with {WordCount} words", features.WordCount);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting features from text");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("extract/batch")]
    public async Task<ActionResult<BatchFeatureExtractionResponse>> ExtractBatchFeatures(
        [FromBody] BatchFeatureExtractionRequest request)
    {
        try
        {
            if (request.Texts.Count > 1000)
            {
                return BadRequest("Maximum batch size is 1000 texts");
            }

            var features = await _featureEngineer.ExtractBatchFeaturesAsync(request.Texts);
            
            var response = new BatchFeatureExtractionResponse(
                FeatureSets: features,
                RequestId: Guid.NewGuid().ToString(),
                ProcessedAt: DateTime.UtcNow,
                ProcessedCount: features.Count);

            _logger.LogInformation("Extracted features for batch of {Count} texts", features.Count);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting batch features");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("preprocess")]
    public async Task<ActionResult<PreprocessingResponse>> PreprocessText(
        [FromBody] PreprocessingRequest request)
    {
        try
        {
            var result = await _preprocessor.PreprocessWithMetadataAsync(request.Text);
            
            var response = new PreprocessingResponse(
                Result: result,
                RequestId: Guid.NewGuid().ToString(),
                ProcessedAt: DateTime.UtcNow);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preprocessing text");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("importance")]
    public async Task<ActionResult<FeatureImportanceResponse>> AnalyzeFeatureImportance(
        [FromBody] FeatureImportanceRequest request)
    {
        try
        {
            var features = await _featureEngineer.ExtractBatchFeaturesAsync(request.Texts);
            var importance = await _featureEngineer.AnalyzeFeatureImportanceAsync(features, request.Labels);
            
            var response = new FeatureImportanceResponse(
                ImportanceResult: importance,
                RequestId: Guid.NewGuid().ToString(),
                AnalyzedAt: DateTime.UtcNow);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing feature importance");
            return StatusCode(500, "Internal server error");
        }
    }
}

public record FeatureExtractionRequest(string Text);
public record FeatureExtractionResponse(FeatureSet Features, string RequestId, DateTime ProcessedAt);

public record BatchFeatureExtractionRequest(List<string> Texts);
public record BatchFeatureExtractionResponse(List<FeatureSet> FeatureSets, string RequestId, DateTime ProcessedAt, int ProcessedCount);

public record PreprocessingRequest(string Text);
public record PreprocessingResponse(PreprocessingResult Result, string RequestId, DateTime ProcessedAt);

public record FeatureImportanceRequest(List<string> Texts, List<string> Labels);
public record FeatureImportanceResponse(FeatureImportanceResult ImportanceResult, string RequestId, DateTime AnalyzedAt);
```

## Service Registration

### ML.NET Feature Engineering Services

```csharp
namespace DocumentProcessor.Extensions;

public static class FeatureEngineeringServiceCollectionExtensions
{
    public static IServiceCollection AddFeatureEngineering(this IServiceCollection services, IConfiguration configuration)
    {
        // Register preprocessing services
        services.Configure<PreprocessingOptions>(configuration.GetSection(PreprocessingOptions.SectionName));
        services.AddScoped<ITextPreprocessor, TextPreprocessor>();
        
        // Register feature engineering services
        services.Configure<FeatureConfiguration>(configuration.GetSection(FeatureConfiguration.SectionName));
        services.AddScoped<IFeatureEngineer, FeatureEngineer>();
        
        // Add memory cache for performance
        services.AddMemoryCache();
        
        // Add health checks
        services.AddHealthChecks()
            .AddCheck<FeatureEngineeringHealthCheck>("feature-engineering");

        return services;
    }
}

public class FeatureEngineeringHealthCheck : IHealthCheck
{
    private readonly IFeatureEngineer _featureEngineer;
    private readonly ILogger<FeatureEngineeringHealthCheck> _logger;

    public FeatureEngineeringHealthCheck(
        IFeatureEngineer featureEngineer,
        ILogger<FeatureEngineeringHealthCheck> logger)
    {
        _featureEngineer = featureEngineer;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var testText = "This is a test for feature engineering health check.";
            var features = await _featureEngineer.ExtractFeaturesAsync(testText);
            
            if (features.WordCount > 0 && features.ProcessingMetadata.FeatureCount > 0)
            {
                return HealthCheckResult.Healthy("Feature engineering is working correctly");
            }
            
            return HealthCheckResult.Degraded("Feature engineering returned unexpected results");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Feature engineering health check failed");
            return HealthCheckResult.Unhealthy("Feature engineering is not working", ex);
        }
    }
}
```

**Usage**:

```csharp
// Basic feature extraction
var featureEngineer = serviceProvider.GetRequiredService<IFeatureEngineer>();
var features = await featureEngineer.ExtractFeaturesAsync("Sample text for analysis");

Console.WriteLine($"Word Count: {features.WordCount}");
Console.WriteLine($"Readability: {features.ReadabilityScore:F2}");
Console.WriteLine($"Unique Ratio: {features.UniqueWordRatio:F2}");

// Batch processing
var texts = new[] { "Text 1", "Text 2", "Text 3" };
var batchFeatures = await featureEngineer.ExtractBatchFeaturesAsync(texts);

// Feature importance analysis
var labels = new[] { "positive", "negative", "neutral" };
var importance = await featureEngineer.AnalyzeFeatureImportanceAsync(batchFeatures, labels);

foreach (var (feature, score) in importance.TopFeatures.Take(10))
{
    Console.WriteLine($"{feature}: {score:F4}");
}

// Custom preprocessing
var preprocessor = serviceProvider.GetRequiredService<ITextPreprocessor>();
var preprocessed = await preprocessor.PreprocessWithMetadataAsync("Sample text!");

Console.WriteLine($"Original: {preprocessed.OriginalText}");
Console.WriteLine($"Processed: {preprocessed.ProcessedText}");
Console.WriteLine($"Entities: {string.Join(", ", preprocessed.ExtractedEntities.Keys)}");
```

**Notes**:

- **Text Preprocessing**: Comprehensive pipeline with entity extraction, normalization, and tokenization
- **Feature Engineering**: Multi-dimensional feature extraction including statistical, linguistic, and semantic features  
- **N-gram Analysis**: Configurable unigram, bigram, and trigram extraction with frequency counting
- **TF-IDF Vectorization**: Term frequency-inverse document frequency calculation for text similarity
- **Entity Recognition**: Built-in extraction of URLs, emails, phone numbers, and numeric values
- **Feature Importance**: Mutual information and correlation analysis for feature selection
- **Performance Optimization**: Batch processing, caching, and parallel execution for scalability
- **ASP.NET Core Integration**: REST API endpoints for feature extraction with comprehensive error handling

**Performance Considerations**: Implements caching for expensive operations, batch processing for scalability, and configurable feature selection to balance accuracy with computational efficiency.
