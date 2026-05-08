using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ToolLib.Models;

namespace ToolLib.Controllers;

[Route("[controller]")]
public class AuthController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthController(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [HttpPost("login")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Login(string? email, string? password, string returnUrl = "/vaerktoejer")
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return LocalRedirect("/login?error=1");

        var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: false);
        if (result.Succeeded)
            return LocalRedirect(returnUrl);
        return LocalRedirect("/login?error=1");
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return LocalRedirect("/login");
    }
}
