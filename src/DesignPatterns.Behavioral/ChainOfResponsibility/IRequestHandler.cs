namespace Snippets.DesignPatterns.Behavioral.ChainOfResponsibility;

public interface IRequestHandler<TRequest, TResponse>
{
    IRequestHandler<TRequest, TResponse>? NextHandler { get; set; }
    Task<TResponse?> HandleAsync(TRequest request);
}