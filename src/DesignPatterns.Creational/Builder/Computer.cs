using System.Text;

namespace Snippets.DesignPatterns.Creational.Builder;

/// <summary>
/// Complex object to be built
/// </summary>
public class Computer
{
    public string Cpu { get; set; } = string.Empty;
    public string Gpu { get; set; } = string.Empty;
    public int Ram { get; set; } // in GB
    public int Storage { get; set; } // in GB
    public string StorageType { get; set; } = string.Empty;
    public string MotherBoard { get; set; } = string.Empty;
    public string PowerSupply { get; set; } = string.Empty;
    public string Case { get; set; } = string.Empty;
    public List<string> Accessories { get; set; } = [];
    public bool HasWiFi { get; set; }
    public bool HasBluetooth { get; set; }
    public string OperatingSystem { get; set; } = string.Empty;
    public decimal Price { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Computer Configuration:");
        sb.AppendLine($"  CPU: {Cpu}");
        sb.AppendLine($"  GPU: {Gpu}");
        sb.AppendLine($"  RAM: {Ram}GB");
        sb.AppendLine($"  Storage: {Storage}GB {StorageType}");
        sb.AppendLine($"  Motherboard: {MotherBoard}");
        sb.AppendLine($"  Power Supply: {PowerSupply}");
        sb.AppendLine($"  Case: {Case}");
        sb.AppendLine($"  WiFi: {HasWiFi}");
        sb.AppendLine($"  Bluetooth: {HasBluetooth}");
        sb.AppendLine($"  OS: {OperatingSystem}");
        sb.AppendLine($"  Price: ${Price:F2}");
        if (Accessories.Count > 0)
        {
            sb.AppendLine($"  Accessories: {string.Join(", ", Accessories)}");
        }

        return sb.ToString();
    }
}