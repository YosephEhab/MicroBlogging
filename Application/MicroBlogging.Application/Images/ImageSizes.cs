namespace MicroBlogging.Application.Images;

public static class ImageSizes
{
    public static readonly (int Width, int Height, string Label)[] Sizes =
    [
        (200, 200, "thumbnail"),
        (800, 600, "medium"),
        (1600, 1200, "large"),
    ];
}