namespace MicroBlogging.Domain.Repositories;

public interface IImageStorage
{
    Task<string> UploadImage(Stream imageStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task DeleteImage(string fileUrl, CancellationToken cancellationToken = default);
    Task<Stream> DownloadImage(string blocPath, CancellationToken cancellationToken);
}