namespace CSharp.EventSourcing;

public class ChangeUserEmailCommandHandler : ICommandHandler<ChangeUserEmailCommand>
{
    private readonly IEventSourcedRepository<User> userRepository;

    public ChangeUserEmailCommandHandler(IEventSourcedRepository<User> userRepository)
    {
        this.userRepository = userRepository;
    }

    public async Task HandleAsync(ChangeUserEmailCommand command, CancellationToken token = default)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, token);
        if (user != null)
        {
            user.ChangeEmail(command.NewEmail);
            await userRepository.SaveAsync(user, token);
        }
    }
}