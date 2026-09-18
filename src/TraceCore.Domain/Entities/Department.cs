namespace TraceCore.Domain.Entities;

public class Department
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";

    public Department() { }

    public Department(string name, string description, string status = "Active")
    {
        Name = name.Trim();
        Description = description;
        Status = status;
    }
}
