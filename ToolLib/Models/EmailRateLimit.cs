namespace ToolLib.Models;

public class EmailRateLimit
{
    public DateOnly Date { get; set; }
    public int Count { get; set; }
}
