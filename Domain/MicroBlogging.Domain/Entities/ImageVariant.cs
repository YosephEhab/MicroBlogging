namespace MicroBlogging.Domain.Entities;

public class ImageVariant
{
    public string Url { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string Format { get; private set; }

    private ImageVariant() { } // EF

    public ImageVariant(string url, int width, int height, string format = "webp")
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
        Width = width;
        Height = height;
        Format = format;
    }
}