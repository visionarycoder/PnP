namespace CSharp.AsyncEnumerable;

public class PagedResponse<T>
{
    public List<T>? Items { get; set; }
    public bool HasNextPage { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}