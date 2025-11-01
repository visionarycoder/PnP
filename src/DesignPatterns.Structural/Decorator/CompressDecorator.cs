using System.IO.Compression;
using System.Text;

namespace Snippets.DesignPatterns.Structural.Decorator;

public class CompressDecorator(ITextProcessor textProcessor) : TextDecorator(textProcessor)
{
    public override string Process(string text)
    {
        var processed = base.Process(text);
        return Compress(processed);
    }

    public override string GetDescription()
    {
        return $"{base.GetDescription()} + Compress";
    }

    public override int GetProcessingCost()
    {
        return base.GetProcessingCost() + 8;
    }

    private string Compress(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }

        return Convert.ToBase64String(output.ToArray());
    }
}