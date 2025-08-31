using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroBlogging.Application.Users.Commands;

namespace MicroBlogging.Web.Controllers;

public class AccountController(IMediator mediator) : Controller
{
    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        if (!ModelState.IsValid)
            return View(command);

        try
        {
            var response = await mediator.Send(command);
            // store tokens in cookies
            Response.Cookies.Append("AccessToken", response.AccessToken, new CookieOptions { HttpOnly = true });
            Response.Cookies.Append("RefreshToken", response.RefreshToken, new CookieOptions { HttpOnly = true });

            return RedirectToAction("Index", "Home");
        }
        catch
        {
            ModelState.AddModelError("", "Invalid username or password");
            return View(command);
        }
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(CreateUserCommand command)
    {
        if (!ModelState.IsValid)
            return View(command);

        await mediator.Send(command);
        return RedirectToAction("Login");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        Response.Cookies.Delete("AccessToken");
        Response.Cookies.Delete("RefreshToken");
        return RedirectToAction("Login");
    }
}
