namespace Snippets.DesignPatterns.Structural.Proxy;

public interface IImage
{
    void Display();
    void Resize(int width, int height);
    string GetMetadata();
}