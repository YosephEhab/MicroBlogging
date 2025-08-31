namespace MicroBlogging.Domain.Rules;

public static class PostRules
{
    public const int MaxPostLength = 140;
    public const int MaxImageSizeInMB = 2;
    public const int MaxImagesPerPost = 4;

    public static readonly string[] AllowedImageFormats = ["jpg", "jpeg", "png", "webp"];
    // Allowed MIME types
    public static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
}