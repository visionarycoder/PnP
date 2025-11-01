namespace Snippets.DesignPatterns.Structural.Proxy;

// Subject interface

// Real subject - expensive to create
public class HighResolutionImage : IImage
{
    private readonly string filename;
    private byte[] imageData;
    private int width, height;

    public HighResolutionImage(string filename)
    {
        this.filename = filename;
        imageData = [];
        LoadImage(); // Expensive operation
    }

    private void LoadImage()
    {
        Console.WriteLine($"Loading high-resolution image: {filename}");
        // Simulate expensive loading operation
        Thread.Sleep(2000);
        imageData = new byte[10_000_000]; // 10MB image
        width = 4096;
        height = 2160;
        Console.WriteLine("Image loaded into memory");
    }

    public void Display()
    {
        Console.WriteLine($"Displaying {filename} ({width}x{height})");
    }

    public void Resize(int width, int height)
    {
        this.width = width;
        this.height = height;
        Console.WriteLine($"Image resized to {this.width}x{this.height}");
    }

    public string GetMetadata()
    {
        return $"File: {filename}, Size: {width}x{height}, Data: {imageData.Length} bytes";
    }
}

// Virtual Proxy - delays creation until needed

// Protection Proxy Example

// Real subject

// Protection Proxy - controls access based on user permissions

// Caching Proxy

// Logging Proxy