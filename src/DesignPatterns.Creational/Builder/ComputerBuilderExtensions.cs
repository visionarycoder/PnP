namespace Snippets.DesignPatterns.Creational.Builder;

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
            .SetRam(32)
            .SetStorage(1000, "NVMe SSD")
            .AddAccessory("Gaming Keyboard")
            .AddAccessory("Gaming Mouse")
            .SetConnectivity(wifi: true, bluetooth: true);
    }

    public static IComputerBuilder ForOffice(this IComputerBuilder builder)
    {
        return builder
            .SetRam(16)
            .SetStorage(512, "SSD")
            .SetConnectivity(wifi: true, bluetooth: false);
    }
}