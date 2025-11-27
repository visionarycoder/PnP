namespace CSharp.DistributedCache;

public class UserKeyGenerator : IKeyGenerator<int>
{
    public string GenerateKey(int key) => $"user:{key}";
}