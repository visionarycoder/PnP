namespace Snippets.DesignPatterns.Structural.Facade;

public class UserRepository(DatabaseConnection db)
{
    public void CreateUser(string email, string name, string hashedPassword)
    {
        var parameters = new Dictionary<string, object>
        {
            ["email"] = email,
            ["name"] = name,
            ["password"] = hashedPassword,
            ["created"] = DateTime.Now
        };

        db.ExecuteQuery(
            "INSERT INTO Users (Email, Name, Password, Created) VALUES (@email, @name, @password, @created)",
            parameters);
    }

    public bool UserExists(string email)
    {
        var parameters = new Dictionary<string, object> { ["email"] = email };
        var count = db.QuerySingle<int>("SELECT COUNT(*) FROM Users WHERE Email = @email", parameters);
        return count > 0;
    }

    public void UpdateLastLogin(string email)
    {
        var parameters = new Dictionary<string, object>
        {
            ["email"] = email,
            ["lastLogin"] = DateTime.Now
        };

        db.ExecuteQuery("UPDATE Users SET LastLogin = @lastLogin WHERE Email = @email", parameters);
    }
}