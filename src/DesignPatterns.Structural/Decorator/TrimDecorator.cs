namespace Snippets.DesignPatterns.Structural.Decorator;

public class TrimDecorator(ITextProcessor textProcessor) : TextDecorator(textProcessor)
{
    public override string Process(string text)
    {
        var processed = base.Process(text);
        return processed.Trim();
    }

    public override string GetDescription()
    {
        return $"{base.GetDescription()} + Trim";
    }

    public override int GetProcessingCost()
    {
        return base.GetProcessingCost() + 1;
    }
}