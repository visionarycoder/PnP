namespace Database.MLDatabaseExamples;

public class ModelPrediction
{
    public string ModelName { get; set; } = string.Empty;
    public Dictionary<string, object> InputFeatures { get; set; } = new();
    public double PredictedValue { get; set; }
    public double ActualValue { get; set; }
    public double Confidence { get; set; }
}