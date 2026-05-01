using Microsoft.AspNetCore.Identity;

namespace ToolLib.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<Tool> Tools { get; set; } = new List<Tool>();
}
