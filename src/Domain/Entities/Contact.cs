namespace MediCore.Domain.Entities;

public class Contact
{
    public string MobilePhone { get; set; } = string.Empty;
    public string? LandlinePhone { get; set; }
    public string? Email { get; set; }
}
