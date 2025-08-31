using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MicroBlogging.Web.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using MicroBlogging.Application.Posts.Queries;
using MicroBlogging.Web.Helpers;

namespace MicroBlogging.Web.Controllers;

public class HomeController(IMediator mediator) : Controller
{
    [Authorize]
    public async Task<IActionResult> Index(Guid? latestPostId = null, int screenWidth = 800)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return RedirectToAction("Login", "Account");

        var posts = await mediator.Send(new GetTimelineQuery(
            userId.Value,
            latestPostId,
            screenWidth
        ));

        return View(posts);
    }

    [HttpGet]
    public async Task<IActionResult> LoadMore(Guid latestPostId, int screenWidth)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return RedirectToAction("Login", "Account");

        var posts = await mediator.Send(new GetTimelineQuery(
            userId.Value,
            latestPostId,
            screenWidth
        ));

        return PartialView("_PostList", posts);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
