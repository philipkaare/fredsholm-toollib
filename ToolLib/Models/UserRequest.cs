namespace ToolLib.Models;

public enum UserRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public class UserRequest
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Message { get; set; }
    public UserRequestStatus Status { get; set; } = UserRequestStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
