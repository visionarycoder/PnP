namespace CSharp.AsyncEnumerable;

public record SensorReading
{
    public DateTime Timestamp { get; init; }
    public double Temperature { get; init; }
    public double Humidity { get; init; }
    public double Pressure { get; init; }
}