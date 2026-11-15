using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sbz.WebUI.Models;

namespace Sbz.WebUI.Controllers;

[Route("account")]
[AutoValidateAntiforgeryToken]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class AccountController(UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn) : Controller
{
    [HttpGet("register")]
    public IActionResult Register()
        => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Tasks") : View(new RegistrationForm());

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegistrationForm form)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Tasks");
        if (!ModelState.IsValid)
            return View(form);

        var user = new IdentityUser(form.UserName);
        var result = await users.CreateAsync(user, form.Password);
        if (result.Succeeded)
        {
            await signIn.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Tasks");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
        return View(form);
    }

    [HttpGet("login")]
    public IActionResult Login(string returnUrl)
        => User.Identity?.IsAuthenticated == true
            ? RedirectAfterLogin(returnUrl)
            : View(new LoginForm { ReturnUrl = returnUrl });

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectAfterLogin(form.ReturnUrl);
        if (!ModelState.IsValid)
            return View(form);

        var result = await signIn.PasswordSignInAsync(form.UserName, form.Password, isPersistent: false, lockoutOnFailure: true);
        if (result.Succeeded)
            return RedirectAfterLogin(form.ReturnUrl);

        ModelState.AddModelError(string.Empty, "The username or password is incorrect, or the account is unavailable.");
        return View(form);
    }

    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("denied")]
    public IActionResult Denied()
    {
        Response.StatusCode = 403;
        return View();
    }

    private IActionResult RedirectAfterLogin(string returnUrl)
        => Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Tasks");
}
