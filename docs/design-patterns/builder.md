# Builder Pattern

**Description**: Constructs complex objects step by step, allowing different representations of the same construction process. Useful for objects with many optional parameters or complex initialization.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Complex object to be built
/// </summary>
public class Computer
{
    public string CPU { get; set; }
    public string GPU { get; set; }
    public int RAM { get; set; } // in GB
    public int Storage { get; set; } // in GB
    public string StorageType { get; set; }
    public string MotherBoard { get; set; }
    public string PowerSupply { get; set; }
    public string Case { get; set; }
    public List<string> Accessories { get; set; } = new();
    public bool HasWiFi { get; set; }
    public bool HasBluetooth { get; set; }
    public string OperatingSystem { get; set; }
    public decimal Price { get; set; }
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Computer Configuration:");
        sb.AppendLine($"  CPU: {CPU}");
        sb.AppendLine($"  GPU: {GPU}");
        sb.AppendLine($"  RAM: {RAM}GB");
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

/// <summary>
/// Abstract builder interface
/// </summary>
public interface IComputerBuilder
{
    IComputerBuilder SetCPU(string cpu);
    IComputerBuilder SetGPU(string gpu);
    IComputerBuilder SetRAM(int ramGB);
    IComputerBuilder SetStorage(int storageGB, string storageType = "SSD");
    IComputerBuilder SetMotherBoard(string motherBoard);
    IComputerBuilder SetPowerSupply(string powerSupply);
    IComputerBuilder SetCase(string computerCase);
    IComputerBuilder AddAccessory(string accessory);
    IComputerBuilder SetConnectivity(bool wifi = true, bool bluetooth = true);
    IComputerBuilder SetOperatingSystem(string os);
    IComputerBuilder SetPrice(decimal price);
    Computer Build();
    void Reset();
}

/// <summary>
/// Concrete builder implementation
/// </summary>
public class ComputerBuilder : IComputerBuilder
{
    private Computer _computer;
    
    public ComputerBuilder()
    {
        Reset();
    }
    
    public void Reset()
    {
        _computer = new Computer();
    }
    
    public IComputerBuilder SetCPU(string cpu)
    {
        _computer.CPU = cpu;
        return this;
    }
    
    public IComputerBuilder SetGPU(string gpu)
    {
        _computer.GPU = gpu;
        return this;
    }
    
    public IComputerBuilder SetRAM(int ramGB)
    {
        _computer.RAM = ramGB;
        return this;
    }
    
    public IComputerBuilder SetStorage(int storageGB, string storageType = "SSD")
    {
        _computer.Storage = storageGB;
        _computer.StorageType = storageType;
        return this;
    }
    
    public IComputerBuilder SetMotherBoard(string motherBoard)
    {
        _computer.MotherBoard = motherBoard;
        return this;
    }
    
    public IComputerBuilder SetPowerSupply(string powerSupply)
    {
        _computer.PowerSupply = powerSupply;
        return this;
    }
    
    public IComputerBuilder SetCase(string computerCase)
    {
        _computer.Case = computerCase;
        return this;
    }
    
    public IComputerBuilder AddAccessory(string accessory)
    {
        _computer.Accessories.Add(accessory);
        return this;
    }
    
    public IComputerBuilder SetConnectivity(bool wifi = true, bool bluetooth = true)
    {
        _computer.HasWiFi = wifi;
        _computer.HasBluetooth = bluetooth;
        return this;
    }
    
    public IComputerBuilder SetOperatingSystem(string os)
    {
        _computer.OperatingSystem = os;
        return this;
    }
    
    public IComputerBuilder SetPrice(decimal price)
    {
        _computer.Price = price;
        return this;
    }
    
    public Computer Build()
    {
        var result = _computer;
        Reset(); // Prepare for next build
        return result;
    }
}

/// <summary>
/// Director class that knows how to build specific computer configurations
/// </summary>
public class ComputerDirector
{
    private readonly IComputerBuilder _builder;
    
    public ComputerDirector(IComputerBuilder builder)
    {
        _builder = builder;
    }
    
    public Computer BuildGamingComputer()
    {
        return _builder
            .SetCPU("Intel i9-13900K")
            .SetGPU("NVIDIA RTX 4080")
            .SetRAM(32)
            .SetStorage(1000, "NVMe SSD")
            .SetMotherBoard("ASUS ROG Maximus Z790")
            .SetPowerSupply("850W 80+ Gold")
            .SetCase("NZXT H7 Elite")
            .AddAccessory("Gaming Keyboard")
            .AddAccessory("Gaming Mouse")
            .AddAccessory("RGB LED Strips")
            .SetConnectivity(wifi: true, bluetooth: true)
            .SetOperatingSystem("Windows 11 Pro")
            .SetPrice(2499.99m)
            .Build();
    }
    
    public Computer BuildOfficeComputer()
    {
        return _builder
            .SetCPU("Intel i5-13400")
            .SetGPU("Integrated Graphics")
            .SetRAM(16)
            .SetStorage(512, "SSD")
            .SetMotherBoard("MSI B660M")
            .SetPowerSupply("450W 80+ Bronze")
            .SetCase("Fractal Design Core 1000")
            .SetConnectivity(wifi: true, bluetooth: false)
            .SetOperatingSystem("Windows 11")
            .SetPrice(799.99m)
            .Build();
    }
    
    public Computer BuildBudgetComputer()
    {
        return _builder
            .SetCPU("AMD Ryzen 5 5600")
            .SetGPU("AMD RX 6600")
            .SetRAM(16)
            .SetStorage(500, "SSD")
            .SetMotherBoard("ASRock B450M")
            .SetPowerSupply("500W 80+ Bronze")
            .SetCase("Cooler Master MasterBox Q300L")
            .SetConnectivity(wifi: false, bluetooth: false)
            .SetOperatingSystem("Windows 11 Home")
            .SetPrice(699.99m)
            .Build();
    }
}

/// <summary>
/// Fluent builder extension for more natural syntax
/// </summary>
public static class ComputerBuilderExtensions
{
    public static IComputerBuilder WithMultipleAccessories(this IComputerBuilder builder, params string[] accessories)
    {
        foreach (var accessory in accessories)
        {
            builder.AddAccessory(accessory);
        }
        return builder;
    }
    
    public static IComputerBuilder ForGaming(this IComputerBuilder builder)
    {
        return builder
            .SetRAM(32)
            .SetStorage(1000, "NVMe SSD")
            .AddAccessory("Gaming Keyboard")
            .AddAccessory("Gaming Mouse")
            .SetConnectivity(wifi: true, bluetooth: true);
    }
    
    public static IComputerBuilder ForOffice(this IComputerBuilder builder)
    {
        return builder
            .SetRAM(16)
            .SetStorage(512, "SSD")
            .SetConnectivity(wifi: true, bluetooth: false);
    }
}
```

**Usage**:

```csharp
using System;

class Program
{
    static void Main()
    {
        // Direct builder usage
        var builder = new ComputerBuilder();
        
        var customComputer = builder
            .SetCPU("AMD Ryzen 7 7700X")
            .SetGPU("NVIDIA RTX 4070")
            .SetRAM(32)
            .SetStorage(1000, "NVMe SSD")
            .SetMotherBoard("MSI X670E")
            .SetPowerSupply("750W 80+ Gold")
            .SetCase("Fractal Design Define 7")
            .AddAccessory("Wireless Keyboard")
            .AddAccessory("Wireless Mouse")
            .SetConnectivity(wifi: true, bluetooth: true)
            .SetOperatingSystem("Windows 11 Pro")
            .SetPrice(1899.99m)
            .Build();
        
        Console.WriteLine("=== Custom Computer ===");
        Console.WriteLine(customComputer);
        
        // Using Director for predefined configurations
        var director = new ComputerDirector(new ComputerBuilder());
        
        var gamingComputer = director.BuildGamingComputer();
        Console.WriteLine("=== Gaming Computer ===");
        Console.WriteLine(gamingComputer);
        
        var officeComputer = director.BuildOfficeComputer();
        Console.WriteLine("=== Office Computer ===");
        Console.WriteLine(officeComputer);
        
        var budgetComputer = director.BuildBudgetComputer();
        Console.WriteLine("=== Budget Computer ===");
        Console.WriteLine(budgetComputer);
        
        // Using extension methods for fluent building
        var fluentComputer = new ComputerBuilder()
            .SetCPU("Intel i7-13700K")
            .SetGPU("AMD RX 7800 XT")
            .ForGaming() // Extension method
            .WithMultipleAccessories("Mechanical Keyboard", "Gaming Headset", "Webcam")
            .SetOperatingSystem("Windows 11 Pro")
            .SetPrice(1699.99m)
            .Build();
        
        Console.WriteLine("=== Fluent Builder Computer ===");
        Console.WriteLine(fluentComputer);
        
        // Building multiple computers with same builder
        var quickBuilder = new ComputerBuilder();
        
        var computer1 = quickBuilder
            .SetCPU("Intel i5-12400")
            .SetRAM(16)
            .SetPrice(999.99m)
            .Build();
        
        var computer2 = quickBuilder // Builder is reset after Build()
            .SetCPU("AMD Ryzen 5 7600")
            .SetRAM(32)
            .SetPrice(1199.99m)
            .Build();
        
        Console.WriteLine("=== Multiple Builds ===");
        Console.WriteLine($"Computer 1 CPU: {computer1.CPU}, RAM: {computer1.RAM}GB");
        Console.WriteLine($"Computer 2 CPU: {computer2.CPU}, RAM: {computer2.RAM}GB");
    }
}
```

**Notes**:

- Separates complex object construction from its representation
- Fluent interface makes the code more readable and chainable
- Director class encapsulates common building procedures
- Builder automatically resets after each Build() call
- Extension methods provide additional fluent syntax options
- Useful for objects with many optional parameters (alternative to telescoping constructors)
- Makes code more maintainable when object construction is complex
- Can create different representations of the same object
- Related patterns: [Factory Method](factory-method.md), [Abstract Factory](abstract-factory.md)

**Prerequisites**:

- .NET Framework 2.0+ or .NET Core
- Understanding of method chaining and fluent interfaces

**References**:

- Gang of Four Design Patterns book
- [Microsoft Docs: Builder Pattern](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/builder)

---

*Created: 2025-10-31*  
*Last Updated: 2025-10-31*  
*Tags: #builder #creational #fluent-interface #complex-objects*
