namespace Snippets.DesignPatterns.Structural.Composite;

public class Manager(string name, string position, decimal salary, string department)
    : OrganizationComponent(name, position, salary)
{

    private readonly List<OrganizationComponent> subordinates = [];

    public string Department { get; } = department ?? "General";

    public IReadOnlyList<OrganizationComponent> Subordinates => subordinates.AsReadOnly();

    public override decimal GetTotalSalary() => Salary + subordinates.Sum(s => s.GetTotalSalary());

    public override int GetTeamSize() => 1 + subordinates.Sum(s => s.GetTeamSize());

    public override void DisplayHierarchy(int depth = 0)
    {
        var indent = GetIndentation(depth);
        Console.WriteLine($"{indent}👔 {Name} - {Position} (${Salary:N0}) [{Department}] - Team: {GetTeamSize()}");

        foreach (var subordinate in subordinates)
        {
            subordinate.DisplayHierarchy(depth + 1);
        }
    }

    public override void GiveRaise(decimal percentage)
    {
        Salary *= (1 + percentage / 100);
        Console.WriteLine($"💰 Manager {Name} received a {percentage}% raise. New salary: ${Salary:N0}");
    }

    public void GiveTeamRaise(decimal percentage)
    {
        Console.WriteLine($"🎉 {Name} is giving the entire team a {percentage}% raise:");
        GiveRaise(percentage);

        foreach (var subordinate in subordinates)
        {
            subordinate.GiveRaise(percentage);
        }
    }

    public override void Add(OrganizationComponent component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));

        if (component == this)
            throw new ArgumentException("Cannot add manager to themselves");

        subordinates.Add(component);
    }

    public override void Remove(OrganizationComponent component)
    {
        subordinates.Remove(component);
    }

    public override IEnumerable<OrganizationComponent> GetSubordinates()
    {
        return subordinates.AsEnumerable();
    }
}