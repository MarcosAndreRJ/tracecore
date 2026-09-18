namespace TraceCore.Domain.Entities;

public class Role
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Role() { }

    public Role(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
