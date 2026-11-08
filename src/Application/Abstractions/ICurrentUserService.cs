namespace MediCore.Application.Abstractions;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    Guid? ProfessionalId { get; }
    IReadOnlyList<string> Roles { get; }
}
