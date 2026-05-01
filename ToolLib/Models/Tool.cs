namespace ToolLib.Models;

public class Tool
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser? Owner { get; set; }
    public string? ImageId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
