using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MicroBlogging.Domain.Repositories;

namespace MicroBlogging.Persistence.Repositories;

public class AzureBlobImageStorage : IImageStorage
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobImageStorage(string connectionString, string containerName)
    {
        var serviceClient = new BlobServiceClient(connectionString);
        _containerClient = serviceClient.GetBlobContainerClient(containerName);
        _containerClient.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<string> UploadImage(Stream imageStream, string blobPath, string contentType, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(blobPath);
        await blobClient.UploadAsync(imageStream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);
        return blobClient.Uri.ToString();
    }

    public async Task DeleteImage(string blobPath, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(blobPath);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<Stream> DownloadImage(string blobPath, CancellationToken cancellationToken)
    {
        var blobClient = _containerClient.GetBlobClient(blobPath);

        if (!await blobClient.ExistsAsync(cancellationToken))
            throw new FileNotFoundException($"Blob not found: {blobPath}");

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var memoryStream = new MemoryStream();
        await response.Value.Content.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        return memoryStream;
    }
}