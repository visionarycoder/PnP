namespace CSharp.DistributedCache;

public class UserService
{
    public async Task<User> GetUserAsync(int id)
    {
        // Simulate database call delay
        await Task.Delay(10);
        
        return new User
        {
            Id = id,
            Name = $"User {id}",
            Email = $"user{id}@example.com"
        };
    }
}