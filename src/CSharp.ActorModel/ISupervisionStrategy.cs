namespace CSharp.ActorModel;

public interface ISupervisionStrategy
{
    SupervisionDirective Decide(Exception exception);
}