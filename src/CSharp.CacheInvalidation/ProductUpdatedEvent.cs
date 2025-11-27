namespace CSharp.CacheInvalidation;

public class ProductUpdatedEvent
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
    public string[] ChangedFields { get; set; } = Array.Empty<string>();
    public DateTime UpdatedAt { get; set; }
}