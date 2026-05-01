using Microsoft.EntityFrameworkCore;
using ToolLib.Data;
using ToolLib.Models;

namespace ToolLib.Services;

public class ToolService
{
    private readonly ApplicationDbContext _context;

    public ToolService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Tool>> GetAllToolsAsync()
    {
        return await _context.Tools.Include(t => t.Owner).OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<List<Tool>> GetToolsByOwnerAsync(string ownerId)
    {
        return await _context.Tools.Include(t => t.Owner).Where(t => t.OwnerId == ownerId).OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<Tool?> GetToolByIdAsync(int id)
    {
        return await _context.Tools.Include(t => t.Owner).FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Tool> CreateToolAsync(Tool tool)
    {
        _context.Tools.Add(tool);
        await _context.SaveChangesAsync();
        return tool;
    }

    public async Task UpdateToolAsync(Tool tool)
    {
        _context.Tools.Update(tool);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteToolAsync(int id)
    {
        var tool = await _context.Tools.FindAsync(id);
        if (tool != null)
        {
            _context.Tools.Remove(tool);
            await _context.SaveChangesAsync();
        }
    }
}
