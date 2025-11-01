namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

/// <summary>
/// Abstract AI agent with template method pattern for decision making
/// </summary>
public abstract class AiAgent(string name)
{
    protected string Name = name;
    protected AiStatistics Statistics = new();
    protected List<string> DecisionLog = [];

    /// <summary>
    /// Template method for AI decision making process
    /// </summary>
    public AiDecision MakeDecision(GameContext gameState)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            LogDecision($"🤖 {Name} analyzing game state");

            // Step 1: Analyze current situation
            var situationAnalysis = AnalyzeSituation(gameState);
            LogDecision($"📊 Situation: {situationAnalysis.Summary}");

            // Step 2: Generate possible actions
            var possibleActions = GeneratePossibleActions(gameState);
            LogDecision($"🎯 Generated {possibleActions.Count} possible actions");

            // Step 3: Filter invalid actions
            var validActions = FilterValidActions(possibleActions, gameState);
            LogDecision($"✅ {validActions.Count} valid actions after filtering");

            if (!validActions.Any())
            {
                LogDecision("⚠️ No valid actions available, using fallback");
                var fallbackAction = GetFallbackDecision(gameState);
                return new AiDecision
                {
                    Action = fallbackAction,
                    Confidence = 0.1,
                    ProcessingTime = DateTime.UtcNow - startTime,
                    Reasoning = "No valid actions available",
                    Statistics = Statistics
                };
            }

            // Step 4: Evaluate actions (abstract - must be implemented)
            var evaluatedActions = EvaluateActions(validActions, gameState, situationAnalysis);
            LogDecision($"📈 Actions evaluated with scores");

            // Step 5: Apply strategy modifications (hook method)
            if (ShouldApplyStrategyModifications(gameState))
            {
                evaluatedActions = ApplyStrategyModifications(evaluatedActions, gameState);
                LogDecision($"🔧 Strategy modifications applied");
            }

            // Step 6: Select best action (abstract - must be implemented)
            var selectedAction = SelectBestAction(evaluatedActions, gameState);
            LogDecision($"⭐ Selected action: {selectedAction.Name}");

            // Step 7: Post-decision analysis
            PostDecisionAnalysis(selectedAction, gameState);

            var processingTime = DateTime.UtcNow - startTime;
            Statistics.TotalDecisions++;
            Statistics.AverageDecisionTime = TimeSpan.FromMilliseconds(
                (Statistics.AverageDecisionTime.TotalMilliseconds * (Statistics.TotalDecisions - 1) +
                 processingTime.TotalMilliseconds) / Statistics.TotalDecisions);

            return new AiDecision
            {
                Action = selectedAction,
                Confidence = CalculateConfidence(selectedAction, evaluatedActions),
                ProcessingTime = processingTime,
                Reasoning = string.Join(" → ", DecisionLog.TakeLast(3)),
                Statistics = Statistics
            };
        }
        catch (Exception ex)
        {
            LogDecision($"❌ Error in decision making: {ex.Message}");
            var emergencyAction = GetEmergencyDecision(gameState, ex);
            return new AiDecision
            {
                Action = emergencyAction,
                Confidence = 0.0,
                ProcessingTime = DateTime.UtcNow - startTime,
                Reasoning = $"Emergency fallback due to error: {ex.Message}",
                Statistics = Statistics
            };
        }
    }

    // Abstract methods - must be implemented by AI subclasses
    protected abstract SituationAnalysis AnalyzeSituation(GameContext gameState);

    protected abstract List<GameAction> EvaluateActions(List<GameAction> actions, GameContext gameState,
        SituationAnalysis analysis);

    protected abstract GameAction SelectBestAction(List<GameAction> evaluatedActions, GameContext gameState);

    // Virtual methods with default implementations
    protected virtual List<GameAction> GeneratePossibleActions(GameContext gameState)
    {
        LogDecision("🔍 Generating basic action set");
        return
        [
            new() { Name = "Attack", Type = ActionType.Combat, Priority = 70 },
            new() { Name = "Defend", Type = ActionType.Defense, Priority = 60 },
            new() { Name = "Move", Type = ActionType.Movement, Priority = 50 },
            new() { Name = "UseItem", Type = ActionType.Item, Priority = 40 },
            new() { Name = "Wait", Type = ActionType.Utility, Priority = 10 }
        ];
    }

    protected virtual List<GameAction> FilterValidActions(List<GameAction> actions, GameContext gameState)
    {
        LogDecision("🔍 Filtering valid actions");
        return actions.Where(action => IsActionValid(action, gameState)).ToList();
    }

    protected virtual bool IsActionValid(GameAction action, GameContext gameState)
    {
        // Basic validation - override for game-specific rules
        return gameState.PlayerHealth > 0 &&
               (action.Type != ActionType.Item || gameState.Inventory.Count > 0);
    }

    protected virtual List<GameAction> ApplyStrategyModifications(List<GameAction> actions, GameContext gameState)
    {
        LogDecision("🔧 Applying default strategy modifications");
        // Default: slightly increase defensive actions when health is low
        if (gameState.PlayerHealth < 30)
        {
            foreach (var action in actions.Where(a => a.Type == ActionType.Defense))
            {
                action.Score += 20;
            }
        }

        return actions;
    }

    protected virtual void PostDecisionAnalysis(GameAction selectedAction, GameContext gameState)
    {
        LogDecision($"📋 Post-decision analysis for {selectedAction.Name}");
        Statistics.ActionHistory.Add(selectedAction.Name);

        // Keep only last 10 actions for analysis
        if (Statistics.ActionHistory.Count > 10)
        {
            Statistics.ActionHistory.RemoveAt(0);
        }
    }

    protected virtual GameAction GetFallbackDecision(GameContext gameState)
    {
        LogDecision("🆘 Using fallback decision");
        return new GameAction { Name = "Wait", Type = ActionType.Utility, Score = 0, Priority = 1 };
    }

    protected virtual GameAction GetEmergencyDecision(GameContext gameState, Exception ex)
    {
        LogDecision($"🚨 Emergency decision due to: {ex.Message}");
        Statistics.ErrorCount++;
        return new GameAction { Name = "Emergency_Wait", Type = ActionType.Utility, Score = 0, Priority = 1 };
    }

    protected virtual double CalculateConfidence(GameAction selected, List<GameAction> allActions)
    {
        if (!allActions.Any()) return 0.0;

        var maxScore = allActions.Max(a => a.Score);
        var minScore = allActions.Min(a => a.Score);

        if (maxScore == minScore) return 1.0;

        var normalizedScore = (selected.Score - minScore) / (maxScore - minScore);
        return Math.Max(0.1, Math.Min(1.0, normalizedScore));
    }

    // Hook methods
    protected virtual bool ShouldApplyStrategyModifications(GameContext gameState) => true;

    // Helper methods
    protected void LogDecision(string message)
    {
        var logEntry = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        DecisionLog.Add(logEntry);
        Console.WriteLine(logEntry);

        // Keep log size manageable
        if (DecisionLog.Count > 50)
        {
            DecisionLog.RemoveRange(0, 10);
        }
    }
}