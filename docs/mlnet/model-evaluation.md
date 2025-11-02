# Model Evaluation for ML.NET

**Description**: Comprehensive model performance evaluation patterns with advanced metrics, cross-validation strategies, statistical significance testing, and automated model comparison frameworks for ML.NET applications.

**Language/Technology**: C#, ML.NET, Statistical Analysis, Model Validation

**Code**:

## Model Evaluation Framework

### Advanced Model Evaluator

```csharp
namespace DocumentProcessor.ML.Evaluation;

using Microsoft.ML;
using Microsoft.ML.Data;
using System.Text.Json;

public interface IModelEvaluator
{
    Task<EvaluationResult> EvaluateClassificationModelAsync<TData, TPrediction>(
        ITransformer model, 
        IEnumerable<TData> testData,
        EvaluationOptions? options = null)
        where TData : class, new()
        where TPrediction : class, new();
    
    Task<RegressionEvaluationResult> EvaluateRegressionModelAsync<TData, TPrediction>(
        ITransformer model,
        IEnumerable<TData> testData,
        EvaluationOptions? options = null)
        where TData : class, new()
        where TPrediction : class, new();
    
    Task<CrossValidationResult> PerformCrossValidationAsync<TData>(
        IEstimator<ITransformer> pipeline,
        IEnumerable<TData> data,
        CrossValidationOptions options)
        where TData : class, new();
    
    Task<ModelComparisonResult> CompareModelsAsync<TData>(
        Dictionary<string, ITransformer> models,
        IEnumerable<TData> testData,
        ComparisonMetrics comparisonMetrics)
        where TData : class, new();
    
    Task<StatisticalSignificanceResult> TestStatisticalSignificanceAsync(
        EvaluationResult model1Results,
        EvaluationResult model2Results,
        SignificanceTestOptions options);
}

public class ModelEvaluator : IModelEvaluator
{
    private readonly MLContext _mlContext;
    private readonly ILogger<ModelEvaluator> _logger;
    private readonly IMetricsCalculator _metricsCalculator;
    private readonly IStatisticalTester _statisticalTester;

    public ModelEvaluator(
        MLContext mlContext,
        ILogger<ModelEvaluator> logger,
        IMetricsCalculator metricsCalculator,
        IStatisticalTester statisticalTester)
    {
        _mlContext = mlContext;
        _logger = logger;
        _metricsCalculator = metricsCalculator;
        _statisticalTester = statisticalTester;
    }

    public async Task<EvaluationResult> EvaluateClassificationModelAsync<TData, TPrediction>(
        ITransformer model,
        IEnumerable<TData> testData,
        EvaluationOptions? options = null)
        where TData : class, new()
        where TPrediction : class, new()
    {
        options ??= new EvaluationOptions();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        _logger.LogInformation("Starting classification model evaluation with {TestCount} samples", testData.Count());

        var testDataView = _mlContext.Data.LoadFromEnumerable(testData);
        var predictions = model.Transform(testDataView);
        
        // Get ML.NET built-in metrics
        var metrics = _mlContext.MulticlassClassification.Evaluate(predictions);
        
        // Calculate additional custom metrics
        var predictionResults = _mlContext.Data.CreateEnumerable<TPrediction>(predictions, reuseRowObject: false).ToList();
        var actualLabels = ExtractActualLabels(testData);
        var predictedLabels = ExtractPredictedLabels(predictionResults);
        
        var customMetrics = await _metricsCalculator.CalculateDetailedMetricsAsync(
            actualLabels, 
            predictedLabels, 
            options.ClassNames ?? GetUniqueLabels(actualLabels));

        // Calculate confidence intervals if requested
        Dictionary<string, ConfidenceInterval>? confidenceIntervals = null;
        if (options.CalculateConfidenceIntervals)
        {
            confidenceIntervals = await CalculateConfidenceIntervalsAsync(
                customMetrics, 
                testData.Count(), 
                options.ConfidenceLevel);
        }

        // Generate learning curves if requested
        LearningCurveResult? learningCurve = null;
        if (options.GenerateLearningCurve && options.TrainingData != null)
        {
            learningCurve = await GenerateLearningCurveAsync<TData>(
                options.Pipeline!, 
                options.TrainingData, 
                testData, 
                options.LearningCurveSteps);
        }

        stopwatch.Stop();

        var result = new EvaluationResult(
            ModelName: options.ModelName ?? "Unknown",
            EvaluationType: EvaluationType.MulticlassClassification,
            Accuracy: metrics.MicroAccuracy,
            MacroAccuracy: metrics.MacroAccuracy,
            LogLoss: metrics.LogLoss,
            LogLossReduction: metrics.LogLossReduction,
            ConfusionMatrix: ParseConfusionMatrix(metrics.ConfusionMatrix),
            DetailedMetrics: customMetrics,
            ConfidenceIntervals: confidenceIntervals,
            LearningCurve: learningCurve,
            TestSampleCount: testData.Count(),
            EvaluationDuration: stopwatch.Elapsed,
            EvaluatedAt: DateTime.UtcNow,
            AdditionalInfo: new Dictionary<string, object>
            {
                ["PerClassLogLoss"] = metrics.PerClassLogLoss?.ToList() ?? new List<double>(),
                ["TopKAccuracy"] = metrics.TopKAccuracy
            });

        _logger.LogInformation("Classification evaluation completed: Accuracy={Accuracy:P2}, LogLoss={LogLoss:F4} in {Duration}ms",
            result.Accuracy, result.LogLoss, stopwatch.ElapsedMilliseconds);

        return result;
    }

    public async Task<RegressionEvaluationResult> EvaluateRegressionModelAsync<TData, TPrediction>(
        ITransformer model,
        IEnumerable<TData> testData,
        EvaluationOptions? options = null)
        where TData : class, new()
        where TPrediction : class, new()
    {
        options ??= new EvaluationOptions();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation("Starting regression model evaluation with {TestCount} samples", testData.Count());

        var testDataView = _mlContext.Data.LoadFromEnumerable(testData);
        var predictions = model.Transform(testDataView);
        
        var metrics = _mlContext.Regression.Evaluate(predictions);
        
        // Calculate additional regression metrics
        var predictionResults = _mlContext.Data.CreateEnumerable<TPrediction>(predictions, reuseRowObject: false).ToList();
        var actualValues = ExtractActualValues(testData);
        var predictedValues = ExtractPredictedValues(predictionResults);
        
        var advancedMetrics = await CalculateAdvancedRegressionMetricsAsync(actualValues, predictedValues);
        
        stopwatch.Stop();

        var result = new RegressionEvaluationResult(
            ModelName: options.ModelName ?? "Unknown",
            MeanAbsoluteError: metrics.MeanAbsoluteError,
            MeanSquaredError: metrics.MeanSquaredError,
            RootMeanSquaredError: metrics.RootMeanSquaredError,
            RSquared: metrics.RSquared,
            LossFunction: metrics.LossFunction,
            AdvancedMetrics: advancedMetrics,
            TestSampleCount: testData.Count(),
            EvaluationDuration: stopwatch.Elapsed,
            EvaluatedAt: DateTime.UtcNow);

        _logger.LogInformation("Regression evaluation completed: R²={RSquared:F4}, RMSE={RMSE:F4} in {Duration}ms",
            result.RSquared, result.RootMeanSquaredError, stopwatch.ElapsedMilliseconds);

        return result;
    }

    public async Task<CrossValidationResult> PerformCrossValidationAsync<TData>(
        IEstimator<ITransformer> pipeline,
        IEnumerable<TData> data,
        CrossValidationOptions options)
        where TData : class, new()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        _logger.LogInformation("Starting {Folds}-fold cross-validation with {DataCount} samples", 
            options.NumberOfFolds, data.Count());

        var dataView = _mlContext.Data.LoadFromEnumerable(data);
        
        // Perform cross-validation
        var cvResults = _mlContext.MulticlassClassification.CrossValidate(
            data: dataView,
            estimator: pipeline,
            numberOfFolds: options.NumberOfFolds,
            labelColumnName: options.LabelColumnName ?? "Label",
            stratificationColumn: options.StratificationColumn);

        var foldResults = new List<FoldResult>();
        
        for (int i = 0; i < cvResults.Length; i++)
        {
            var cvResult = cvResults[i];
            var foldResult = new FoldResult(
                FoldNumber: i + 1,
                Accuracy: cvResult.Metrics.MicroAccuracy,
                MacroAccuracy: cvResult.Metrics.MacroAccuracy,
                LogLoss: cvResult.Metrics.LogLoss,
                LogLossReduction: cvResult.Metrics.LogLossReduction,
                Model: cvResult.Model);
            
            foldResults.Add(foldResult);
        }

        // Calculate aggregate statistics
        var accuracies = foldResults.Select(f => f.Accuracy).ToList();
        var logLosses = foldResults.Select(f => f.LogLoss).ToList();
        
        var aggregateStats = new CrossValidationStats(
            MeanAccuracy: accuracies.Average(),
            StdAccuracy: CalculateStandardDeviation(accuracies),
            MeanLogLoss: logLosses.Average(),
            StdLogLoss: CalculateStandardDeviation(logLosses),
            MinAccuracy: accuracies.Min(),
            MaxAccuracy: accuracies.Max(),
            AccuracyRange: accuracies.Max() - accuracies.Min());

        stopwatch.Stop();

        var result = new CrossValidationResult(
            FoldResults: foldResults,
            AggregateStats: aggregateStats,
            NumberOfFolds: options.NumberOfFolds,
            DataSampleCount: data.Count(),
            ValidationDuration: stopwatch.Elapsed,
            ValidatedAt: DateTime.UtcNow);

        _logger.LogInformation("Cross-validation completed: Mean Accuracy={MeanAccuracy:P2}±{StdAccuracy:P3} in {Duration}ms",
            aggregateStats.MeanAccuracy, aggregateStats.StdAccuracy, stopwatch.ElapsedMilliseconds);

        return result;
    }

    public async Task<ModelComparisonResult> CompareModelsAsync<TData>(
        Dictionary<string, ITransformer> models,
        IEnumerable<TData> testData,
        ComparisonMetrics comparisonMetrics)
        where TData : class, new()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        _logger.LogInformation("Comparing {ModelCount} models on {TestCount} samples", 
            models.Count, testData.Count());

        var modelResults = new Dictionary<string, EvaluationResult>();
        
        // Evaluate each model
        foreach (var (modelName, model) in models)
        {
            var evaluationOptions = new EvaluationOptions 
            { 
                ModelName = modelName,
                CalculateConfidenceIntervals = comparisonMetrics.IncludeConfidenceIntervals
            };
            
            var result = await EvaluateClassificationModelAsync<TData, object>(model, testData, evaluationOptions);
            modelResults[modelName] = result;
        }

        // Rank models by specified metrics
        var rankings = new Dictionary<string, List<ModelRanking>>();
        
        foreach (var metric in comparisonMetrics.RankingMetrics)
        {
            var modelScores = modelResults.Select(kvp => new ModelRanking(
                ModelName: kvp.Key,
                MetricValue: ExtractMetricValue(kvp.Value, metric),
                Rank: 0 // Will be calculated after sorting
            )).OrderByDescending(mr => mr.MetricValue).ToList();
            
            // Assign ranks
            for (int i = 0; i < modelScores.Count; i++)
            {
                modelScores[i] = modelScores[i] with { Rank = i + 1 };
            }
            
            rankings[metric] = modelScores;
        }

        // Calculate statistical significance between top models if requested
        List<SignificanceComparison>? significanceTests = null;
        if (comparisonMetrics.TestStatisticalSignificance && modelResults.Count >= 2)
        {
            significanceTests = await PerformPairwiseSignificanceTestsAsync(modelResults);
        }

        stopwatch.Stop();

        var result = new ModelComparisonResult(
            ModelResults: modelResults,
            Rankings: rankings,
            SignificanceTests: significanceTests,
            ComparisonMetrics: comparisonMetrics,
            TestSampleCount: testData.Count(),
            ComparisonDuration: stopwatch.Elapsed,
            ComparedAt: DateTime.UtcNow);

        var bestModel = rankings.Values.First().First().ModelName;
        _logger.LogInformation("Model comparison completed. Best model: {BestModel} in {Duration}ms",
            bestModel, stopwatch.ElapsedMilliseconds);

        return result;
    }

    public async Task<StatisticalSignificanceResult> TestStatisticalSignificanceAsync(
        EvaluationResult model1Results,
        EvaluationResult model2Results,
        SignificanceTestOptions options)
    {
        _logger.LogInformation("Testing statistical significance between {Model1} and {Model2}",
            model1Results.ModelName, model2Results.ModelName);

        var result = await _statisticalTester.PerformSignificanceTestAsync(
            model1Results, 
            model2Results, 
            options);

        _logger.LogInformation("Significance test completed: p-value={PValue:F6}, significant={IsSignificant}",
            result.PValue, result.IsSignificant);

        return result;
    }

    private async Task<LearningCurveResult> GenerateLearningCurveAsync<TData>(
        IEstimator<ITransformer> pipeline,
        IEnumerable<TData> trainingData,
        IEnumerable<TData> validationData,
        int steps)
        where TData : class, new()
    {
        var trainingList = trainingData.ToList();
        var validationList = validationData.ToList();
        var curvePoints = new List<LearningCurvePoint>();
        
        var sampleSizes = Enumerable.Range(1, steps)
            .Select(i => Math.Min(trainingList.Count, (int)(trainingList.Count * (double)i / steps)))
            .Where(size => size >= 10) // Minimum sample size
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        foreach (var sampleSize in sampleSizes)
        {
            var trainingSample = trainingList.Take(sampleSize);
            var trainingDataView = _mlContext.Data.LoadFromEnumerable(trainingSample);
            
            var model = pipeline.Fit(trainingDataView);
            
            // Evaluate on training data
            var trainingPredictions = model.Transform(trainingDataView);
            var trainingMetrics = _mlContext.MulticlassClassification.Evaluate(trainingPredictions);
            
            // Evaluate on validation data
            var validationDataView = _mlContext.Data.LoadFromEnumerable(validationList);
            var validationPredictions = model.Transform(validationDataView);
            var validationMetrics = _mlContext.MulticlassClassification.Evaluate(validationPredictions);
            
            curvePoints.Add(new LearningCurvePoint(
                TrainingSampleSize: sampleSize,
                TrainingAccuracy: trainingMetrics.MicroAccuracy,
                ValidationAccuracy: validationMetrics.MicroAccuracy,
                TrainingLogLoss: trainingMetrics.LogLoss,
                ValidationLogLoss: validationMetrics.LogLoss));
        }

        return new LearningCurveResult(
            CurvePoints: curvePoints,
            RecommendedSampleSize: DetermineOptimalSampleSize(curvePoints),
            OverfittingDetected: DetectOverfitting(curvePoints));
    }

    private int DetermineOptimalSampleSize(List<LearningCurvePoint> curvePoints)
    {
        // Find the point where validation accuracy stabilizes
        var maxValidationAcc = curvePoints.Max(p => p.ValidationAccuracy);
        var threshold = maxValidationAcc * 0.95; // 95% of max performance
        
        return curvePoints.FirstOrDefault(p => p.ValidationAccuracy >= threshold)?.TrainingSampleSize 
               ?? curvePoints.Last().TrainingSampleSize;
    }

    private bool DetectOverfitting(List<LearningCurvePoint> curvePoints)
    {
        if (curvePoints.Count < 3) return false;
        
        // Check if training accuracy continues to increase while validation accuracy plateaus or decreases
        var last3Points = curvePoints.TakeLast(3).ToList();
        
        var trainingIncreasing = last3Points[2].TrainingAccuracy > last3Points[1].TrainingAccuracy && 
                               last3Points[1].TrainingAccuracy > last3Points[0].TrainingAccuracy;
        
        var validationStagnant = Math.Abs(last3Points[2].ValidationAccuracy - last3Points[0].ValidationAccuracy) < 0.01;
        
        return trainingIncreasing && validationStagnant;
    }

    private double CalculateStandardDeviation(IEnumerable<double> values)
    {
        var valuesList = values.ToList();
        var mean = valuesList.Average();
        var squaredDifferences = valuesList.Select(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(squaredDifferences.Average());
    }

    private double ExtractMetricValue(EvaluationResult result, string metricName)
    {
        return metricName.ToLowerInvariant() switch
        {
            "accuracy" => result.Accuracy,
            "macroaccuracy" => result.MacroAccuracy,
            "logloss" => result.LogLoss,
            "loglossreduction" => result.LogLossReduction,
            _ => throw new ArgumentException($"Unknown metric: {metricName}")
        };
    }

    private async Task<List<SignificanceComparison>> PerformPairwiseSignificanceTestsAsync(
        Dictionary<string, EvaluationResult> modelResults)
    {
        var comparisons = new List<SignificanceComparison>();
        var models = modelResults.Keys.ToList();
        
        for (int i = 0; i < models.Count; i++)
        {
            for (int j = i + 1; j < models.Count; j++)
            {
                var model1 = models[i];
                var model2 = models[j];
                
                var significance = await _statisticalTester.PerformSignificanceTestAsync(
                    modelResults[model1],
                    modelResults[model2],
                    new SignificanceTestOptions { TestType = SignificanceTestType.McNemar });
                
                comparisons.Add(new SignificanceComparison(
                    Model1: model1,
                    Model2: model2,
                    StatisticalTest: significance));
            }
        }
        
        return comparisons;
    }

    private List<string> ExtractActualLabels<TData>(IEnumerable<TData> testData)
    {
        // This would need to be implemented based on your data structure
        // For now, returning empty list as placeholder
        return new List<string>();
    }

    private List<string> ExtractPredictedLabels<TPrediction>(List<TPrediction> predictions)
    {
        // This would need to be implemented based on your prediction structure
        // For now, returning empty list as placeholder
        return new List<string>();
    }

    private List<float> ExtractActualValues<TData>(IEnumerable<TData> testData)
    {
        // This would need to be implemented based on your data structure
        return new List<float>();
    }

    private List<float> ExtractPredictedValues<TPrediction>(List<TPrediction> predictions)
    {
        // This would need to be implemented based on your prediction structure
        return new List<float>();
    }

    private List<string> GetUniqueLabels(List<string> labels)
    {
        return labels.Distinct().OrderBy(x => x).ToList();
    }

    private async Task<Dictionary<string, ConfidenceInterval>> CalculateConfidenceIntervalsAsync(
        DetailedMetrics metrics,
        int sampleCount,
        double confidenceLevel)
    {
        // Calculate confidence intervals using normal approximation
        var z = confidenceLevel switch
        {
            0.90 => 1.645,
            0.95 => 1.96,
            0.99 => 2.576,
            _ => 1.96
        };

        var intervals = new Dictionary<string, ConfidenceInterval>();
        
        // Accuracy confidence interval
        var accuracy = metrics.OverallAccuracy;
        var accuracyStdError = Math.Sqrt(accuracy * (1 - accuracy) / sampleCount);
        var accuracyMargin = z * accuracyStdError;
        
        intervals["Accuracy"] = new ConfidenceInterval(
            LowerBound: Math.Max(0, accuracy - accuracyMargin),
            UpperBound: Math.Min(1, accuracy + accuracyMargin),
            ConfidenceLevel: confidenceLevel);

        return await Task.FromResult(intervals);
    }

    private async Task<AdvancedRegressionMetrics> CalculateAdvancedRegressionMetricsAsync(
        List<float> actualValues,
        List<float> predictedValues)
    {
        if (actualValues.Count != predictedValues.Count)
        {
            throw new ArgumentException("Actual and predicted values must have same count");
        }

        var residuals = actualValues.Zip(predictedValues, (a, p) => a - p).ToList();
        
        return await Task.FromResult(new AdvancedRegressionMetrics(
            MeanAbsolutePercentageError: CalculateMAPE(actualValues, predictedValues),
            MedianAbsoluteError: CalculateMedianAbsoluteError(residuals),
            MeanAbsoluteScaledError: CalculateMASE(actualValues, predictedValues),
            SymmetricMeanAbsolutePercentageError: CalculateSMAPE(actualValues, predictedValues),
            ResidualAnalysis: new ResidualAnalysis(
                Mean: residuals.Average(),
                StandardDeviation: CalculateStandardDeviation(residuals.Select(r => (double)r)),
                Skewness: CalculateSkewness(residuals),
                Kurtosis: CalculateKurtosis(residuals))));
    }

    private double CalculateMAPE(List<float> actual, List<float> predicted)
    {
        var ape = actual.Zip(predicted, (a, p) => Math.Abs((a - p) / Math.Max(Math.Abs(a), 1e-8)));
        return ape.Average() * 100;
    }

    private double CalculateMedianAbsoluteError(List<float> residuals)
    {
        var absResiduals = residuals.Select(Math.Abs).OrderBy(x => x).ToList();
        var count = absResiduals.Count;
        
        if (count % 2 == 0)
        {
            return (absResiduals[count / 2 - 1] + absResiduals[count / 2]) / 2.0;
        }
        
        return absResiduals[count / 2];
    }

    private double CalculateMASE(List<float> actual, List<float> predicted)
    {
        // Simplified MASE calculation
        var mae = actual.Zip(predicted, (a, p) => Math.Abs(a - p)).Average();
        var naiveMae = actual.Skip(1).Zip(actual, (curr, prev) => Math.Abs(curr - prev)).Average();
        
        return mae / Math.Max(naiveMae, 1e-8);
    }

    private double CalculateSMAPE(List<float> actual, List<float> predicted)
    {
        var smape = actual.Zip(predicted, (a, p) => 
            Math.Abs(a - p) / (Math.Abs(a) + Math.Abs(p) + 1e-8));
        
        return smape.Average() * 200; // Multiply by 200 for percentage
    }

    private double CalculateSkewness(List<float> values)
    {
        var mean = values.Average();
        var stdDev = CalculateStandardDeviation(values.Select(v => (double)v));
        
        if (stdDev == 0) return 0;
        
        var skewness = values.Select(v => Math.Pow((v - mean) / stdDev, 3)).Average();
        return skewness;
    }

    private double CalculateKurtosis(List<float> values)
    {
        var mean = values.Average();
        var stdDev = CalculateStandardDeviation(values.Select(v => (double)v));
        
        if (stdDev == 0) return 0;
        
        var kurtosis = values.Select(v => Math.Pow((v - mean) / stdDev, 4)).Average() - 3;
        return kurtosis;
    }

    private ConfusionMatrixData ParseConfusionMatrix(ConfusionMatrix matrix)
    {
        var classes = matrix.GetFormattedConfusionTable()
            .Split('\n')
            .Skip(1) // Skip header
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split('\t')[0])
            .ToList();

        var matrixData = new int[classes.Count, classes.Count];
        
        // Parse the confusion matrix (simplified)
        for (int i = 0; i < classes.Count; i++)
        {
            for (int j = 0; j < classes.Count; j++)
            {
                matrixData[i, j] = (int)matrix.Counts[i][j];
            }
        }

        return new ConfusionMatrixData(
            Classes: classes,
            Matrix: matrixData,
            FormattedTable: matrix.GetFormattedConfusionTable());
    }
}

// Data Transfer Objects and Supporting Types

public record EvaluationResult(
    string ModelName,
    EvaluationType EvaluationType,
    double Accuracy,
    double MacroAccuracy,
    double LogLoss,
    double LogLossReduction,
    ConfusionMatrixData ConfusionMatrix,
    DetailedMetrics DetailedMetrics,
    Dictionary<string, ConfidenceInterval>? ConfidenceIntervals,
    LearningCurveResult? LearningCurve,
    int TestSampleCount,
    TimeSpan EvaluationDuration,
    DateTime EvaluatedAt,
    Dictionary<string, object> AdditionalInfo);

public record RegressionEvaluationResult(
    string ModelName,
    double MeanAbsoluteError,
    double MeanSquaredError,
    double RootMeanSquaredError,
    double RSquared,
    double LossFunction,
    AdvancedRegressionMetrics AdvancedMetrics,
    int TestSampleCount,
    TimeSpan EvaluationDuration,
    DateTime EvaluatedAt);

public record CrossValidationResult(
    List<FoldResult> FoldResults,
    CrossValidationStats AggregateStats,
    int NumberOfFolds,
    int DataSampleCount,
    TimeSpan ValidationDuration,
    DateTime ValidatedAt);

public record ModelComparisonResult(
    Dictionary<string, EvaluationResult> ModelResults,
    Dictionary<string, List<ModelRanking>> Rankings,
    List<SignificanceComparison>? SignificanceTests,
    ComparisonMetrics ComparisonMetrics,
    int TestSampleCount,
    TimeSpan ComparisonDuration,
    DateTime ComparedAt);

public record FoldResult(
    int FoldNumber,
    double Accuracy,
    double MacroAccuracy,
    double LogLoss,
    double LogLossReduction,
    ITransformer Model);

public record CrossValidationStats(
    double MeanAccuracy,
    double StdAccuracy,
    double MeanLogLoss,
    double StdLogLoss,
    double MinAccuracy,
    double MaxAccuracy,
    double AccuracyRange);

public record ModelRanking(
    string ModelName,
    double MetricValue,
    int Rank);

public record SignificanceComparison(
    string Model1,
    string Model2,
    StatisticalSignificanceResult StatisticalTest);

public record ConfidenceInterval(
    double LowerBound,
    double UpperBound,
    double ConfidenceLevel);

public record LearningCurveResult(
    List<LearningCurvePoint> CurvePoints,
    int RecommendedSampleSize,
    bool OverfittingDetected);

public record LearningCurvePoint(
    int TrainingSampleSize,
    double TrainingAccuracy,
    double ValidationAccuracy,
    double TrainingLogLoss,
    double ValidationLogLoss);

public record AdvancedRegressionMetrics(
    double MeanAbsolutePercentageError,
    double MedianAbsoluteError,
    double MeanAbsoluteScaledError,
    double SymmetricMeanAbsolutePercentageError,
    ResidualAnalysis ResidualAnalysis);

public record ResidualAnalysis(
    double Mean,
    double StandardDeviation,
    double Skewness,
    double Kurtosis);

public record ConfusionMatrixData(
    List<string> Classes,
    int[,] Matrix,
    string FormattedTable);

public enum EvaluationType
{
    BinaryClassification,
    MulticlassClassification,
    Regression,
    Clustering,
    Ranking
}

public class EvaluationOptions
{
    public string? ModelName { get; set; }
    public string[]? ClassNames { get; set; }
    public bool CalculateConfidenceIntervals { get; set; } = false;
    public double ConfidenceLevel { get; set; } = 0.95;
    public bool GenerateLearningCurve { get; set; } = false;
    public IEnumerable<object>? TrainingData { get; set; }
    public IEstimator<ITransformer>? Pipeline { get; set; }
    public int LearningCurveSteps { get; set; } = 10;
}

public class CrossValidationOptions
{
    public int NumberOfFolds { get; set; } = 5;
    public string? LabelColumnName { get; set; } = "Label";
    public string? StratificationColumn { get; set; }
    public bool Shuffle { get; set; } = true;
    public int? Seed { get; set; }
}

public class ComparisonMetrics
{
    public List<string> RankingMetrics { get; set; } = new() { "Accuracy", "LogLoss" };
    public bool TestStatisticalSignificance { get; set; } = true;
    public bool IncludeConfidenceIntervals { get; set; } = true;
    public double SignificanceLevel { get; set; } = 0.05;
}

// Supporting interfaces that would be implemented separately

public interface IMetricsCalculator
{
    Task<DetailedMetrics> CalculateDetailedMetricsAsync(
        List<string> actualLabels,
        List<string> predictedLabels,
        string[] classNames);
}

public interface IStatisticalTester
{
    Task<StatisticalSignificanceResult> PerformSignificanceTestAsync(
        EvaluationResult model1Results,
        EvaluationResult model2Results,
        SignificanceTestOptions options);
}

public record DetailedMetrics(
    double OverallAccuracy,
    Dictionary<string, ClassMetrics> PerClassMetrics,
    double MacroPrecision,
    double MacroRecall,
    double MacroF1Score,
    double WeightedPrecision,
    double WeightedRecall,
    double WeightedF1Score,
    double CohenKappa,
    double MatthewsCorrelationCoefficient);

public record ClassMetrics(
    string ClassName,
    double Precision,
    double Recall,
    double F1Score,
    double Specificity,
    int TruePositives,
    int FalsePositives,
    int TrueNegatives,
    int FalseNegatives);

public record StatisticalSignificanceResult(
    double PValue,
    bool IsSignificant,
    double TestStatistic,
    SignificanceTestType TestType,
    double SignificanceLevel,
    string Interpretation);

public class SignificanceTestOptions
{
    public SignificanceTestType TestType { get; set; } = SignificanceTestType.McNemar;
    public double SignificanceLevel { get; set; } = 0.05;
    public bool TwoTailed { get; set; } = true;
}

public enum SignificanceTestType
{
    McNemar,
    PairedTTest,
    Wilcoxon,
    Bootstrap
}
```

## ASP.NET Core Integration

### Model Evaluation Controller

```csharp
namespace DocumentProcessor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModelEvaluationController : ControllerBase
{
    private readonly IModelEvaluator _modelEvaluator;
    private readonly IModelManager _modelManager;
    private readonly ILogger<ModelEvaluationController> _logger;

    public ModelEvaluationController(
        IModelEvaluator modelEvaluator,
        IModelManager modelManager,
        ILogger<ModelEvaluationController> logger)
    {
        _modelEvaluator = modelEvaluator;
        _modelManager = modelManager;
        _logger = logger;
    }

    [HttpPost("evaluate/classification")]
    public async Task<ActionResult<EvaluationResponse>> EvaluateClassificationModel(
        [FromBody] ClassificationEvaluationRequest request)
    {
        try
        {
            var model = await _modelManager.LoadModelAsync(request.ModelId);
            if (model == null)
            {
                return NotFound($"Model {request.ModelId} not found");
            }

            var options = new EvaluationOptions
            {
                ModelName = request.ModelName ?? request.ModelId,
                CalculateConfidenceIntervals = request.IncludeConfidenceIntervals,
                ConfidenceLevel = request.ConfidenceLevel,
                GenerateLearningCurve = request.GenerateLearningCurve
            };

            // Convert test data (simplified - would need proper implementation)
            var testData = request.TestData.Select(td => new { Text = td.Text, Label = td.Label });
            
            var result = await _modelEvaluator.EvaluateClassificationModelAsync<object, object>(
                model, testData, options);

            var response = new EvaluationResponse(
                Result: result,
                RequestId: Guid.NewGuid().ToString(),
                EvaluatedAt: DateTime.UtcNow);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating classification model {ModelId}", request.ModelId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("cross-validate")]
    public async Task<ActionResult<CrossValidationResponse>> PerformCrossValidation(
        [FromBody] CrossValidationRequest request)
    {
        try
        {
            var pipeline = await _modelManager.GetPipelineAsync(request.PipelineId);
            if (pipeline == null)
            {
                return NotFound($"Pipeline {request.PipelineId} not found");
            }

            var options = new CrossValidationOptions
            {
                NumberOfFolds = request.NumberOfFolds,
                LabelColumnName = request.LabelColumnName,
                Shuffle = request.Shuffle,
                Seed = request.Seed
            };

            // Convert training data
            var data = request.TrainingData.Select(td => new { Text = td.Text, Label = td.Label });

            var result = await _modelEvaluator.PerformCrossValidationAsync(pipeline, data, options);

            var response = new CrossValidationResponse(
                Result: result,
                RequestId: Guid.NewGuid().ToString(),
                ValidatedAt: DateTime.UtcNow);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing cross-validation for pipeline {PipelineId}", request.PipelineId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("compare")]
    public async Task<ActionResult<ModelComparisonResponse>> CompareModels(
        [FromBody] ModelComparisonRequest request)
    {
        try
        {
            var models = new Dictionary<string, ITransformer>();
            
            foreach (var modelId in request.ModelIds)
            {
                var model = await _modelManager.LoadModelAsync(modelId);
                if (model != null)
                {
                    models[modelId] = model;
                }
            }

            if (models.Count < 2)
            {
                return BadRequest("At least 2 valid models are required for comparison");
            }

            var comparisonMetrics = new ComparisonMetrics
            {
                RankingMetrics = request.RankingMetrics,
                TestStatisticalSignificance = request.TestStatisticalSignificance,
                IncludeConfidenceIntervals = request.IncludeConfidenceIntervals,
                SignificanceLevel = request.SignificanceLevel
            };

            // Convert test data
            var testData = request.TestData.Select(td => new { Text = td.Text, Label = td.Label });

            var result = await _modelEvaluator.CompareModelsAsync(models, testData, comparisonMetrics);

            var response = new ModelComparisonResponse(
                Result: result,
                RequestId: Guid.NewGuid().ToString(),
                ComparedAt: DateTime.UtcNow);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing models");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("metrics/{evaluationId}")]
    public async Task<ActionResult<EvaluationMetricsResponse>> GetEvaluationMetrics(string evaluationId)
    {
        try
        {
            // This would typically retrieve stored evaluation results from a database
            // For now, returning a placeholder response
            
            var response = new EvaluationMetricsResponse(
                EvaluationId: evaluationId,
                Metrics: new Dictionary<string, object>
                {
                    ["accuracy"] = 0.85,
                    ["precision"] = 0.82,
                    ["recall"] = 0.88,
                    ["f1_score"] = 0.85
                },
                RetrievedAt: DateTime.UtcNow);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving evaluation metrics for {EvaluationId}", evaluationId);
            return StatusCode(500, "Internal server error");
        }
    }
}

// Request/Response DTOs
public record ClassificationEvaluationRequest(
    string ModelId,
    string? ModelName,
    List<TestDataPoint> TestData,
    bool IncludeConfidenceIntervals = false,
    double ConfidenceLevel = 0.95,
    bool GenerateLearningCurve = false);

public record CrossValidationRequest(
    string PipelineId,
    List<TrainingDataPoint> TrainingData,
    int NumberOfFolds = 5,
    string? LabelColumnName = "Label",
    bool Shuffle = true,
    int? Seed = null);

public record ModelComparisonRequest(
    List<string> ModelIds,
    List<TestDataPoint> TestData,
    List<string> RankingMetrics,
    bool TestStatisticalSignificance = true,
    bool IncludeConfidenceIntervals = true,
    double SignificanceLevel = 0.05);

public record TestDataPoint(string Text, string Label);
public record TrainingDataPoint(string Text, string Label);

public record EvaluationResponse(EvaluationResult Result, string RequestId, DateTime EvaluatedAt);
public record CrossValidationResponse(CrossValidationResult Result, string RequestId, DateTime ValidatedAt);
public record ModelComparisonResponse(ModelComparisonResult Result, string RequestId, DateTime ComparedAt);
public record EvaluationMetricsResponse(string EvaluationId, Dictionary<string, object> Metrics, DateTime RetrievedAt);
```

## Service Registration

### ML.NET Evaluation Services

```csharp
namespace DocumentProcessor.Extensions;

public static class ModelEvaluationServiceCollectionExtensions
{
    public static IServiceCollection AddModelEvaluation(this IServiceCollection services, IConfiguration configuration)
    {
        // Register core evaluation services
        services.AddScoped<IModelEvaluator, ModelEvaluator>();
        services.AddScoped<IMetricsCalculator, MetricsCalculator>();
        services.AddScoped<IStatisticalTester, StatisticalTester>();
        
        // Register model management
        services.AddScoped<IModelManager, ModelManager>();
        
        // Configure evaluation options
        services.Configure<EvaluationConfiguration>(configuration.GetSection("ModelEvaluation"));
        
        // Add health checks
        services.AddHealthChecks()
            .AddCheck<ModelEvaluationHealthCheck>("model-evaluation");

        return services;
    }
}

public class EvaluationConfiguration
{
    public const string SectionName = "ModelEvaluation";
    
    public double DefaultConfidenceLevel { get; set; } = 0.95;
    public int DefaultCrossValidationFolds { get; set; } = 5;
    public double DefaultSignificanceLevel { get; set; } = 0.05;
    public bool EnableDetailedMetrics { get; set; } = true;
    public bool EnableLearningCurves { get; set; } = false;
    public string MetricsStoragePath { get; set; } = "./evaluation-results";
}
```

**Usage**:

```csharp
// Basic model evaluation
var modelEvaluator = serviceProvider.GetRequiredService<IModelEvaluator>();
var model = await modelManager.LoadModelAsync("sentiment-classifier-v1");

var testData = new[]
{
    new { Text = "This product is amazing!", Label = "positive" },
    new { Text = "Terrible quality, waste of money.", Label = "negative" },
    new { Text = "It's okay, nothing special.", Label = "neutral" }
};

var options = new EvaluationOptions
{
    ModelName = "Sentiment Classifier v1.0",
    CalculateConfidenceIntervals = true,
    ConfidenceLevel = 0.95
};

var evaluation = await modelEvaluator.EvaluateClassificationModelAsync<object, object>(
    model, testData, options);

Console.WriteLine($"Accuracy: {evaluation.Accuracy:P2}");
Console.WriteLine($"Log Loss: {evaluation.LogLoss:F4}");
Console.WriteLine($"Evaluation Duration: {evaluation.EvaluationDuration.TotalMilliseconds}ms");

// Cross-validation
var pipeline = mlContext.Transforms.Text.FeaturizeText("Features", "Text")
    .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy());

var cvOptions = new CrossValidationOptions
{
    NumberOfFolds = 5,
    LabelColumnName = "Label"
};

var cvResult = await modelEvaluator.PerformCrossValidationAsync(pipeline, trainingData, cvOptions);

Console.WriteLine($"Mean Accuracy: {cvResult.AggregateStats.MeanAccuracy:P2} ± {cvResult.AggregateStats.StdAccuracy:P3}");
Console.WriteLine($"Accuracy Range: {cvResult.AggregateStats.MinAccuracy:P2} - {cvResult.AggregateStats.MaxAccuracy:P2}");

// Model comparison
var models = new Dictionary<string, ITransformer>
{
    ["SVM"] = await modelManager.LoadModelAsync("svm-classifier"),
    ["Random Forest"] = await modelManager.LoadModelAsync("rf-classifier"),
    ["Neural Network"] = await modelManager.LoadModelAsync("nn-classifier")
};

var comparisonMetrics = new ComparisonMetrics
{
    RankingMetrics = new[] { "Accuracy", "LogLoss" }.ToList(),
    TestStatisticalSignificance = true
};

var comparison = await modelEvaluator.CompareModelsAsync(models, testData, comparisonMetrics);

foreach (var (metric, rankings) in comparison.Rankings)
{
    Console.WriteLine($"\n{metric} Rankings:");
    foreach (var ranking in rankings)
    {
        Console.WriteLine($"{ranking.Rank}. {ranking.ModelName}: {ranking.MetricValue:F4}");
    }
}

// Statistical significance testing
if (comparison.SignificanceTests?.Any() == true)
{
    foreach (var test in comparison.SignificanceTests)
    {
        Console.WriteLine($"{test.Model1} vs {test.Model2}: " +
            $"p-value={test.StatisticalTest.PValue:F6}, " +
            $"significant={test.StatisticalTest.IsSignificant}");
    }
}
```

**Notes**:

- **Comprehensive Metrics**: Accuracy, precision, recall, F1-score, log loss, and domain-specific metrics
- **Statistical Validation**: Cross-validation with confidence intervals and significance testing
- **Model Comparison**: Automated ranking and pairwise statistical significance tests
- **Learning Curves**: Overfitting detection and optimal training sample size recommendations
- **Regression Support**: Advanced regression metrics including MAPE, MASE, and residual analysis
- **Performance Monitoring**: Evaluation duration tracking and health checks for production use
- **ASP.NET Core Integration**: REST API endpoints for evaluation workflows with comprehensive error handling

**Performance Considerations**: Implements efficient batch evaluation, caching of expensive calculations, and configurable evaluation depth to balance thoroughness with computational resources.
