namespace CSharp.CacheInvalidation;

public class UserUpdatedEvent
{
    public int UserId { get; set; }
    public string[] ChangedFields { get; set; } = Array.Empty<string>();
    public DateTime UpdatedAt { get; set; }
}