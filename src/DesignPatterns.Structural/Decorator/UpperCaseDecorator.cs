namespace Snippets.DesignPatterns.Structural.Decorator;

public class UpperCaseDecorator(ITextProcessor textProcessor) : TextDecorator(textProcessor)
{
    public override string Process(string text)
    {
        var processed = base.Process(text);
        return processed.ToUpper();
    }

    public override string GetDescription()
    {
        return $"{base.GetDescription()} + UpperCase";
    }

    public override int GetProcessingCost()
    {
        return base.GetProcessingCost() + 2;
    }
}