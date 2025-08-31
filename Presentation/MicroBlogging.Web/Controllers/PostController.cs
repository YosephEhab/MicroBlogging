using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MicroBlogging.Application.Posts.Commands;
using MicroBlogging.Domain.Entities;
using MicroBlogging.Web.Helpers;

namespace MicroBlogging.Web.Controllers;

[Authorize]
public class PostController(IMediator mediator) : Controller
{
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string text, double latitude, double longitude, List<IFormFile>? images)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var uploads = new List<ImageUpload>();
        if (images is not null)
        {
            foreach (var file in images)
            {
                if (file.Length > 0)
                {
                    uploads.Add(new ImageUpload(file.FileName, file.ContentType, file.OpenReadStream()));
                }
            }
        }

        var cmd = new CreatePostCommand(userId.Value, text, new GeoLocation(latitude, longitude), uploads.Count != 0 ? uploads : null);

        var postId = await mediator.Send(cmd);

        return RedirectToAction("Index", "Home", new { highlight = postId });
    }
}
