using System.Text;

namespace Snippets.DesignPatterns.Structural.Facade;

public class PasswordHasher
{
    public string HashPassword(string password)
    {
        Console.WriteLine("🔒 Hashing password");
        // Simulate complex hashing (BCrypt, Argon2, etc.)
        Thread.Sleep(200);
        var hash = Convert.ToBase64String(Encoding.UTF8.GetBytes($"hashed_{password}_{DateTime.Now.Ticks}"));
        Console.WriteLine("✅ Password hashed");
        return hash;
    }

    public bool VerifyPassword(string password, string hash)
    {
        Console.WriteLine("🔍 Verifying password");
        Thread.Sleep(200);
        // Simulate verification
        return hash.Contains("hashed_");
    }
}