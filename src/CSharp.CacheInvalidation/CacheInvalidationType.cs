namespace CSharp.CacheInvalidation;

public enum CacheInvalidationType
{
    Direct,
    Bulk,
    Pattern,
    Tag,
    Dependency,
    Hierarchy,
    TimeBased,
    EventDriven
}