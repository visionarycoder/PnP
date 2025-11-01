namespace Snippets.DesignPatterns.Structural.Decorator;

public interface ITextProcessor
{
    string Process(string text);
    string GetDescription();
    int GetProcessingCost();
}