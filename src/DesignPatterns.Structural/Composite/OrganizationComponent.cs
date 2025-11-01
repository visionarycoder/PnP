namespace Snippets.DesignPatterns.Structural.Composite;

public abstract class OrganizationComponent(string name, string position, decimal salary)
{

    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
    public string Position { get; } = position ?? throw new ArgumentNullException(nameof(position));
    public decimal Salary { get; protected set; } = salary;
    public DateTime HireDate { get; } = DateTime.Now;

    public abstract decimal GetTotalSalary();
    public abstract int GetTeamSize();
    public abstract void DisplayHierarchy(int depth = 0);
    public abstract void GiveRaise(decimal percentage);

    public virtual void Add(OrganizationComponent component)
    {
        throw new NotSupportedException($"Cannot add subordinates to {GetType().Name}");
    }

    public virtual void Remove(OrganizationComponent component)
    {
        throw new NotSupportedException($"Cannot remove subordinates from {GetType().Name}");
    }

    public virtual IEnumerable<OrganizationComponent> GetSubordinates()
    {
        return [];
    }

    protected string GetIndentation(int depth)
    {
        return new string('│', depth) + (depth > 0 ? "├─ " : "");
    }

}