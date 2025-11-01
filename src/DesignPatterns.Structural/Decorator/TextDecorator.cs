namespace Snippets.DesignPatterns.Structural.Decorator;

public abstract class TextDecorator(ITextProcessor textProcessor) : ITextProcessor
{
    protected ITextProcessor TextProcessor = textProcessor ?? throw new ArgumentNullException(nameof(textProcessor));

    public virtual string Process(string text)
    {
        return TextProcessor.Process(text);
    }

    public virtual string GetDescription()
    {
        return TextProcessor.GetDescription();
    }

    public virtual int GetProcessingCost()
    {
        return TextProcessor.GetProcessingCost();
    }
}