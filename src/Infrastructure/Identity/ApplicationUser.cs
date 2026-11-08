using Microsoft.AspNetCore.Identity;

namespace MediCore.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? ProfessionalId { get; set; }
}
