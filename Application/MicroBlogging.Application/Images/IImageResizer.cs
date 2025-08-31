namespace MicroBlogging.Application.Images;

public interface IImageResizer
{
    Task<Stream> Resize(Stream original, int width, int height, CancellationToken cancellationToken);
}