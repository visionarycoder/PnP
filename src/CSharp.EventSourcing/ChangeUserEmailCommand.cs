namespace CSharp.EventSourcing;

public class ChangeUserEmailCommand : Command
{
    public Guid UserId { get; set; }
    public string NewEmail { get; set; } = string.Empty;
}