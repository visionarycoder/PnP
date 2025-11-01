namespace Snippets.DesignPatterns.Structural.Proxy;

public class ImageProxy : IImage
{
    private readonly string filename;
    private HighResolutionImage? realImage;
    private readonly object @lock = new();

    public ImageProxy(string filename)
    {
        this.filename = filename;
        Console.WriteLine($"Image proxy created for: {this.filename}");
    }

    private HighResolutionImage GetRealImage()
    {
        if (realImage == null)
        {
            lock (@lock)
            {
                realImage ??= new HighResolutionImage(filename);
            }
        }

        return realImage;
    }

    public void Display()
    {
        GetRealImage().Display();
    }

    public void Resize(int width, int height)
    {
        GetRealImage().Resize(width, height);
    }

    public string GetMetadata()
    {
        return GetRealImage().GetMetadata();
    }
}