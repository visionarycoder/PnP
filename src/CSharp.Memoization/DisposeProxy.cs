namespace CSharp.Memoization;

/// <summary>
/// Base class for disposable proxy objects.
/// </summary>
public abstract class DisposeProxy : IDisposable
{
    private bool disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        disposed = true;
    }

    ~DisposeProxy()
    {
        Dispose(false);
    }
}