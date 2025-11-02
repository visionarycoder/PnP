namespace CSharp.FunctionalLinq;

/// <summary>
/// Demo record types for functional programming demonstrations
/// </summary>
public record Customer(string FirstName, string LastName, Address? Address);
public record Address(string Street, string? City);