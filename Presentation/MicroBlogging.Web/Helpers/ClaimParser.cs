using System.Security.Claims;

namespace MicroBlogging.Web.Helpers;

public static class ClaimParser
{
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        string? id = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        return id is null ? null : Guid.Parse(id);
    }
}
