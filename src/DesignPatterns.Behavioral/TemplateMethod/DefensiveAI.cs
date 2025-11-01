namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

/// <summary>
/// Defensive AI that prioritizes safety and survival
/// </summary>
public class DefensiveAi() : AiAgent("Defensive AI")
{
    protected override SituationAnalysis AnalyzeSituation(GameContext gameState)
    {
        LogDecision("🛡️ Analyzing situation with defensive mindset");

        var dangerLevel = CalculateDangerLevel(gameState);
        var safetyLevel = 100 - dangerLevel;

        return new SituationAnalysis
        {
            Summary = $"Danger: {dangerLevel}%, Safety: {safetyLevel}%",
            ThreatLevel = dangerLevel,
            OpportunityLevel = safetyLevel,
            RecommendedStrategy = dangerLevel > 60 ? "Full defense" : "Cautious approach"
        };
    }

    protected override List<GameAction> EvaluateActions(List<GameAction> actions, GameContext gameState,
        SituationAnalysis analysis)
    {
        LogDecision("🛡️ Evaluating actions with defensive priority");

        foreach (var action in actions)
        {
            action.Score = action.Priority;

            // Defensive AI bonuses
            switch (action.Type)
            {
                case ActionType.Defense:
                    action.Score += 50; // High bonus for defensive actions
                    break;

                case ActionType.Combat:
                    action.Score -= 30; // Penalty for aggressive actions
                    if (gameState.PlayerHealth > gameState.EnemyHealth * 2)
                        action.Score += 20; // Exception when significantly stronger
                    break;

                case ActionType.Movement:
                    action.Score += gameState.EnemyDistance < 3 ? 20 : -5; // Retreat when close
                    break;

                case ActionType.Item:
                    action.Score += action.Name.Contains("Heal") || action.Name.Contains("Shield") ? 30 : 0;
                    break;

                case ActionType.Utility:
                    action.Score += 10; // Slight bonus for utility actions
                    break;
            }

            // Adjust based on health
            if (gameState.PlayerHealth < 30)
            {
                if (action.Type == ActionType.Defense || action.Name.Contains("Heal"))
                    action.Score += 40;
            }
        }

        return actions;
    }

    protected override GameAction SelectBestAction(List<GameAction> evaluatedActions, GameContext gameState)
    {
        LogDecision("🛡️ Selecting safest action");

        // Prioritize healing when health is critical
        if (gameState.PlayerHealth < 20)
        {
            var healActions = evaluatedActions.Where(a => a.Name.Contains("Heal")).ToList();
            if (healActions.Any())
                return healActions.OrderByDescending(a => a.Score).First();
        }

        return evaluatedActions.OrderByDescending(a => a.Score).First();
    }

    private int CalculateDangerLevel(GameContext gameState)
    {
        var danger = 0;
        if (gameState.PlayerHealth < 25) danger += 50;
        else if (gameState.PlayerHealth < 50) danger += 25;

        if (gameState.EnemyHealth > gameState.PlayerHealth) danger += 20;
        if (gameState.EnemyDistance < 2) danger += 30;
        if (!gameState.Inventory.Any()) danger += 10;

        return Math.Min(100, danger);
    }
}