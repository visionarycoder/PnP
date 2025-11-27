namespace CSharp.ActorModel;

public interface IActorSystem : IDisposable
{
    string Name { get; }
    Task<IActorRef> ActorOf<T>(string? name = null) where T : ActorBase, new();
    IActorRef? GetActor(string path);
    Task Stop(IActorRef actor);
    Task Shutdown();
    event EventHandler<ActorSystemEventArgs>? ActorSystemEvent;
}