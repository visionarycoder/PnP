namespace CSharp.ActorModel;

// Supervision strategy
public enum SupervisionDirective
{
    Resume,
    Restart,
    Stop,
    Escalate
}

public interface ISupervisionStrategy
{
    SupervisionDirective Decide(Exception exception);
}

public class OneForOneStrategy : ISupervisionStrategy
{
    private readonly Dictionary<Type, SupervisionDirective> exceptionDirectives;
    private readonly SupervisionDirective defaultDirective;

    public OneForOneStrategy(SupervisionDirective defaultDirective = SupervisionDirective.Restart)
    {
        this.defaultDirective = defaultDirective;
        exceptionDirectives = new();
    }

    public OneForOneStrategy Handle<TException>(SupervisionDirective directive) where TException : Exception
    {
        exceptionDirectives[typeof(TException)] = directive;
        return this;
    }

    public SupervisionDirective Decide(Exception exception)
    {
        var exceptionType = exception.GetType();
        
        // Look for exact match first
        if (exceptionDirectives.TryGetValue(exceptionType, out var directive))
        {
            return directive;
        }

        // Look for base type matches
        foreach (var kvp in exceptionDirectives)
        {
            if (kvp.Key.IsAssignableFrom(exceptionType))
            {
                return kvp.Value;
            }
        }

        return defaultDirective;
    }
}