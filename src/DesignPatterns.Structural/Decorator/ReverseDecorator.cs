namespace Snippets.DesignPatterns.Structural.Decorator;

public class ReverseDecorator(ITextProcessor textProcessor) : TextDecorator(textProcessor)
{
    public override string Process(string text)
    {
        var processed = base.Process(text);
        var charArray = processed.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    public override string GetDescription()
    {
        return $"{base.GetDescription()} + Reverse";
    }

    public override int GetProcessingCost()
    {
        return base.GetProcessingCost() + 3;
    }
}