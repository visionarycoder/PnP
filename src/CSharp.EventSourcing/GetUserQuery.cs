namespace CSharp.EventSourcing;

public class GetUserQuery : Query<User?>
{
    public Guid UserId { get; set; }
}