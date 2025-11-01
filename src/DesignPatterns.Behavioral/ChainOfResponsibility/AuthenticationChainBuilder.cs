namespace Snippets.DesignPatterns.Behavioral.ChainOfResponsibility;

public class AuthenticationChainBuilder
{
    private IRequestHandler<AuthRequest, AuthResponse>? firstHandler;
    private IRequestHandler<AuthRequest, AuthResponse>? currentHandler;

    public AuthenticationChainBuilder AddHandler(IRequestHandler<AuthRequest, AuthResponse> handler)
    {
        if (firstHandler == null)
        {
            firstHandler = handler;
            currentHandler = handler;
        }
        else
        {
            currentHandler!.NextHandler = handler;
            currentHandler = handler;
        }

        return this;
    }

    public IRequestHandler<AuthRequest, AuthResponse> Build()
    {
        return firstHandler ?? throw new InvalidOperationException("No handlers added to chain");
    }
}