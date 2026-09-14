namespace Fms.Model.DatabaseEntity;

public sealed class User : EntityBase
{
    public string? DisplayName { get; set; }

    public string? Role { get; set; }

    public string? Description { get; set; }
}
