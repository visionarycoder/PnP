namespace Snippets.DesignPatterns.Behavioral.State
{
    // ============================================================================
    // STATE PATTERN IMPLEMENTATION
    // ============================================================================

    /// <summary>
    /// Base state machine class with history tracking
    /// </summary>
    public abstract class StateMachine<TState>
        where TState : class, IState<StateMachine<TState>>
    {
        private readonly List<string> stateHistory = [];

        public TState? CurrentState { get; private set; }

        public List<string> GetStateHistory() => [..stateHistory];

        public virtual void TransitionTo(TState newState)
        {
            var previousState = CurrentState?.StateName ?? "None";

            CurrentState?.Exit(this);
            CurrentState = newState;
            CurrentState?.Enter(this);

            stateHistory.Add($"{DateTime.Now:HH:mm:ss} - {previousState} -> {newState.StateName}");
            Console.WriteLine($"State transition: {previousState} -> {newState.StateName}");
        }

        public virtual void Handle()
        {
            CurrentState?.Handle(this);
        }

        protected void SetInitialState(TState initialState)
        {
            CurrentState = initialState;
            CurrentState?.Enter(this);
            stateHistory.Add($"{DateTime.Now:HH:mm:ss} - Initial state: {initialState.StateName}");
        }
    }

    // ============================================================================
    // SIMPLE STATE MACHINE FOR CONCRETE IMPLEMENTATIONS
    // ============================================================================

    // ============================================================================
    // DOCUMENT WORKFLOW EXAMPLE
    // ============================================================================

    // ============================================================================
    // VENDING MACHINE EXAMPLE
    // ============================================================================
}