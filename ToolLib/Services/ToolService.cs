using Microsoft.EntityFrameworkCore;
using ToolLib.Data;
using ToolLib.Models;

namespace ToolLib.Services;

public class ToolService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public ToolService(IDbContextFactory<ApplicationDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<Tool>> GetAllToolsAsync()
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.Tools.Include(t => t.Owner).OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<List<Tool>> GetToolsByOwnerAsync(string ownerId)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.Tools.Include(t => t.Owner).Where(t => t.OwnerId == ownerId).OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<Tool?> GetToolByIdAsync(int id)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.Tools.Include(t => t.Owner).FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Tool> CreateToolAsync(Tool tool)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        ctx.Tools.Add(tool);
        await ctx.SaveChangesAsync();
        return tool;
    }

    public async Task UpdateToolAsync(Tool tool)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        ctx.Tools.Update(tool);
        await ctx.SaveChangesAsync();
    }

    public async Task DeleteToolAsync(int id)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var tool = await ctx.Tools.FindAsync(id);
        if (tool != null)
        {
            ctx.Tools.Remove(tool);
            await ctx.SaveChangesAsync();
        }
    }
}
