namespace Snippets.DesignPatterns.Behavioral.State;

/// <summary>
/// Document workflow context managing document lifecycle
/// </summary>
public class DocumentWorkflow : SimpleStateMachine
{
    public string DocumentId { get; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Author { get; }
    public string? Reviewer { get; set; }
    public List<string> Comments { get; } = [];
    public DateTime CreatedAt { get; }
    public DateTime LastModified { get; set; }

    // Available states
    public static readonly DraftState Draft = new();
    public static readonly SubmittedState Submitted = new();
    public static readonly ReviewingState Reviewing = new();
    public static readonly ApprovedState Approved = new();
    public static readonly RejectedState Rejected = new();
    public static readonly PublishedState Published = new();

    public DocumentWorkflow(string documentId, string author, string title)
    {
        DocumentId = documentId;
        Author = author;
        Title = title;
        CreatedAt = DateTime.UtcNow;
        LastModified = DateTime.UtcNow;

        SetInitialState(Draft);
    }

    public void AddComment(string comment)
    {
        Comments.Add($"{DateTime.Now:HH:mm:ss} - {comment}");
        Console.WriteLine($"Comment added: {comment}");
    }

    public void PrintStatus()
    {
        var currentDocState = CurrentState as DocumentState;
        Console.WriteLine($"\n=== Document Status ===");
        Console.WriteLine($"ID: {DocumentId}");
        Console.WriteLine($"Title: {Title}");
        Console.WriteLine($"Author: {Author}");
        Console.WriteLine($"Reviewer: {Reviewer ?? "None"}");
        Console.WriteLine($"State: {CurrentState?.StateName}");
        Console.WriteLine($"Created: {CreatedAt:yyyy-MM-dd HH:mm}");
        Console.WriteLine($"Modified: {LastModified:yyyy-MM-dd HH:mm}");
        Console.WriteLine($"Can Edit: {currentDocState?.CanEdit}");
        Console.WriteLine($"Can Submit: {currentDocState?.CanSubmit}");
        Console.WriteLine($"Can Approve: {currentDocState?.CanApprove}");
        Console.WriteLine($"Can Reject: {currentDocState?.CanReject}");

        if (Comments.Any())
        {
            Console.WriteLine("Recent Comments:");
            foreach (var comment in Comments.TakeLast(3))
            {
                Console.WriteLine($"  - {comment}");
            }
        }
    }

    // Convenience methods that delegate to current state
    public void Submit()
    {
        ((DocumentState)CurrentState!).Submit(this);
    }

    public void Approve()
    {
        ((DocumentState)CurrentState!).Approve(this);
    }

    public void Reject(string reason = "No reason provided")
    {
        ((DocumentState)CurrentState!).Reject(this, reason);
    }

    public void Edit(string content)
    {
        ((DocumentState)CurrentState!).Edit(this, content);
    }
}