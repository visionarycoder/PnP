using System.Text;

namespace Snippets.DesignPatterns.Structural.Decorator;

public class EncryptDecorator(ITextProcessor textProcessor, string key = "DefaultKey123") : TextDecorator(textProcessor)
{
    private readonly string key = key ?? "DefaultKey123";

    public override string Process(string text)
    {
        var processed = base.Process(text);
        return SimpleEncrypt(processed, key);
    }

    public override string GetDescription()
    {
        return $"{base.GetDescription()} + Encrypt";
    }

    public override int GetProcessingCost()
    {
        return base.GetProcessingCost() + 10;
    }

    private string SimpleEncrypt(string text, string key)
    {
        // Simple XOR encryption for demo purposes
        var result = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            result.Append((char)(text[i] ^ key[i % key.Length]));
        }

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(result.ToString()));
    }
}