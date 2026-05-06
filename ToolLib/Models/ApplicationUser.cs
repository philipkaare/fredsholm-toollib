using Microsoft.AspNetCore.Identity;

namespace ToolLib.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? Address { get; set; }
    public ICollection<Tool> Tools { get; set; } = new List<Tool>();
}
