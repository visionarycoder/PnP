namespace Snippets.DesignPatterns.Creational.Prototype;

/// <summary>
/// Prototype registry for managing prototype instances
/// </summary>
public class DocumentPrototypeRegistry
{
    private readonly Dictionary<string, Document> prototypes = new();

    public void RegisterPrototype(string key, Document prototype)
    {
        prototypes[key] = prototype;
    }

    public Document CreateDocument(string prototypeKey)
    {
        if (prototypes.TryGetValue(prototypeKey, out var prototype))
        {
            return prototype.Clone();
        }

        throw new ArgumentException($"No prototype found for key: {prototypeKey}");
    }

    public IEnumerable<string> GetAvailablePrototypes()
    {
        return prototypes.Keys;
    }

    public void RemovePrototype(string key)
    {
        prototypes.Remove(key);
    }
}