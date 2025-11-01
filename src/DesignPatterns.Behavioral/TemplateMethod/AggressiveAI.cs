namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

/// <summary>
/// Aggressive AI that prioritizes offensive actions
/// </summary>
public class AggressiveAi() : AiAgent("Aggressive AI")
{
    protected override SituationAnalysis AnalyzeSituation(GameContext gameState)
    {
        LogDecision("⚔️ Analyzing situation with aggressive mindset");

        var threatLevel = CalculateThreatLevel(gameState);
        var opportunityLevel = CalculateOpportunityLevel(gameState);

        return new SituationAnalysis
        {
            Summary = $"Threat: {threatLevel}%, Opportunity: {opportunityLevel}%",
            ThreatLevel = threatLevel,
            OpportunityLevel = opportunityLevel,
            RecommendedStrategy = threatLevel > 70 ? "All-out attack" : "Aggressive offense"
        };
    }

    protected override List<GameAction> EvaluateActions(List<GameAction> actions, GameContext gameState,
        SituationAnalysis analysis)
    {
        LogDecision("⚔️ Evaluating actions with aggressive bias");

        foreach (var action in actions)
        {
            // Base score from priority
            action.Score = action.Priority;

            // Aggressive AI bonuses
            switch (action.Type)
            {
                case ActionType.Combat:
                    action.Score += 40; // High bonus for combat
                    if (gameState.EnemyHealth > gameState.PlayerHealth)
                        action.Score += 20; // Extra bonus when behind
                    break;

                case ActionType.Defense:
                    action.Score -= 20; // Penalty for defensive actions
                    if (gameState.PlayerHealth < 20)
                        action.Score += 30; // Exception when critically low
                    break;

                case ActionType.Movement:
                    action.Score += gameState.PlayerHealth > 50 ? 10 : -10;
                    break;

                case ActionType.Item:
                    action.Score += action.Name.Contains("Weapon") ? 25 : 5;
                    break;
            }

            // Situation-based adjustments
            if (analysis.OpportunityLevel > 60)
                action.Score += action.Type == ActionType.Combat ? 15 : -5;
        }

        return actions;
    }

    protected override GameAction SelectBestAction(List<GameAction> evaluatedActions, GameContext gameState)
    {
        LogDecision("⚔️ Selecting most aggressive action");

        // Prefer combat actions, then highest scoring
        var combatActions = evaluatedActions.Where(a => a.Type == ActionType.Combat).ToList();
        if (combatActions.Any())
        {
            return combatActions.OrderByDescending(a => a.Score).First();
        }

        return evaluatedActions.OrderByDescending(a => a.Score).First();
    }

    private int CalculateThreatLevel(GameContext gameState)
    {
        var threat = 0;
        if (gameState.EnemyHealth > gameState.PlayerHealth) threat += 30;
        if (gameState.PlayerHealth < 50) threat += 40;
        if (gameState.EnemyDistance < 3) threat += 30;
        return Math.Min(100, threat);
    }

    private int CalculateOpportunityLevel(GameContext gameState)
    {
        var opportunity = 0;
        if (gameState.PlayerHealth > gameState.EnemyHealth) opportunity += 40;
        if (gameState.EnemyDistance < 2) opportunity += 30;
        if (gameState.Inventory.Any(i => i.Contains("Weapon"))) opportunity += 30;
        return Math.Min(100, opportunity);
    }
}