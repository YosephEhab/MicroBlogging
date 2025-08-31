using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace MicroBlogging.Application.Images;

public sealed class ImageSharpResizer : IImageResizer
{
    public async Task<Stream> Resize(Stream original, int targetWidth, int targetHeight, CancellationToken cancellationToken)
    {
        original.Position = 0;
        using var image = await Image.LoadAsync(original, cancellationToken);

        // calculate new size while preserving aspect ratio
        var ratioX = (double)targetWidth / image.Width;
        var ratioY = (double)targetHeight / image.Height;
        var ratio = Math.Min(ratioX, ratioY);

        // only allow downscaling
        if (ratio > 1.0)
            ratio = 1.0;

        var newWidth = (int)(image.Width * ratio);
        var newHeight = (int)(image.Height * ratio);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(newWidth, newHeight),
            Mode = ResizeMode.Max // maintains aspect ratio
        }));

        var output = new MemoryStream();
        var encoder = new WebpEncoder
        {
            Quality = 80,
            FileFormat = WebpFileFormatType.Lossy
        };

        await image.SaveAsync(output, encoder, cancellationToken);
        output.Position = 0;

        return output;
    }
}
