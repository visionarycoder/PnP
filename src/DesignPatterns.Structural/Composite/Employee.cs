namespace Snippets.DesignPatterns.Structural.Composite;

public class Employee(string name, string position, decimal salary, string department)
    : OrganizationComponent(name, position, salary)
{

    private readonly List<string> skills = [];

    public string Department { get; } = department ?? "General";

    public IReadOnlyList<string> Skills => skills.AsReadOnly();

    public void AddSkill(string skill)
    {
        if (!string.IsNullOrWhiteSpace(skill) && !skills.Contains(skill))
        {
            skills.Add(skill);
        }
    }

    public override decimal GetTotalSalary() => Salary;

    public override int GetTeamSize() => 1;

    public override void DisplayHierarchy(int depth = 0)
    {
        var indent = GetIndentation(depth);
        Console.WriteLine($"{indent}👤 {Name} - {Position} (${Salary:N0}) [{Department}]");
        if (skills.Count > 0)
        {
            Console.WriteLine($"{new string(' ', depth * 2)}   Skills: {string.Join(", ", skills)}");
        }
    }

    public override void GiveRaise(decimal percentage)
    {
        Salary *= (1 + percentage / 100);
        Console.WriteLine($"💰 {Name} received a {percentage}% raise. New salary: ${Salary:N0}");
    }
}