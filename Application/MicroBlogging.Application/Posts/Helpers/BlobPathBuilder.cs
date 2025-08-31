namespace MicroBlogging.Application.Posts.Helpers;

public static class BlobPathBuilder
{
    public static string BuildPostImagePath(Guid postId, Guid imageId, string size, string extension) => $"posts/{postId}/{imageId}-{size}{extension}";

    public static string ReplaceSize(string originalPath, string newSize, string extension)
    {
        var parts = originalPath.Split('-');
        if (parts.Length < 2) return originalPath;
        var prefix = string.Join("-", parts.Take(parts.Length - 1));
        return $"{prefix}-{newSize}{extension}";
    }
}
