namespace TraceCore.Domain.Entities;

// BR-002: Usuários podem participar de mais de um departamento (N:N)
public class UserDepartment
{
    public long UserId { get; set; }
    public long DepartmentId { get; set; }

    public UserDepartment() { }

    public UserDepartment(long userId, long departmentId)
    {
        UserId = userId;
        DepartmentId = departmentId;
    }
}
