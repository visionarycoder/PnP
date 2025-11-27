namespace CSharp.CacheAside;

public record UserData(string Id, string Name, string Email)
{
    public override string ToString() => $"User({Id}, {Name}, {Email})";
}