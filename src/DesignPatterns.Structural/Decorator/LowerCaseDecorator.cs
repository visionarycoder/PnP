namespace Snippets.DesignPatterns.Structural.Decorator;

public class LowerCaseDecorator(ITextProcessor textProcessor) : TextDecorator(textProcessor)
{
    public override string Process(string text)
    {
        var processed = base.Process(text);
        return processed.ToLower();
    }

    public override string GetDescription()
    {
        return $"{base.GetDescription()} + LowerCase";
    }

    public override int GetProcessingCost()
    {
        return base.GetProcessingCost() + 2;
    }
}