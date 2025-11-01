namespace Snippets.DesignPatterns.Creational.Builder;

/// <summary>
/// Director class that knows how to build specific computer configurations
/// </summary>
public class ComputerDirector(IComputerBuilder builder)
{
    public Computer BuildGamingComputer()
    {
        return builder
            .SetCpu("Intel i9-13900K")
            .SetGpu("NVIDIA RTX 4080")
            .SetRam(32)
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
        return builder
            .SetCpu("Intel i5-13400")
            .SetGpu("Integrated Graphics")
            .SetRam(16)
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
        return builder
            .SetCpu("AMD Ryzen 5 5600")
            .SetGpu("AMD RX 6600")
            .SetRam(16)
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