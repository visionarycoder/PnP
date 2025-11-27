namespace CSharp.CacheInvalidation;

public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public int[] ProductIds { get; set; } = Array.Empty<int>();
    public DateTime CreatedAt { get; set; }
}