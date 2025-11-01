namespace Snippets.DesignPatterns.Creational.AbstractFactory;

public static class UiFactoryProvider
{
    public static IUiFactory GetFactory(OsType osType)
    {
        return osType switch
        {
            OsType.Windows => new WindowsUiFactory(),
            OsType.MacOs => new MacOsuiFactory(),
            OsType.Linux => new LinuxUiFactory(),
            _ => throw new ArgumentException($"Unsupported OS type: {osType}")
        };
    }

    public static IUiFactory GetFactory()
    {
        // Auto-detect OS (simplified)
        var os = Environment.OSVersion.Platform;
        return os switch
        {
            PlatformID.Win32NT => new WindowsUiFactory(),
            PlatformID.Unix => new LinuxUiFactory(),
            PlatformID.MacOSX => new MacOsuiFactory(),
            _ => new WindowsUiFactory() // Default fallback
        };
    }
}