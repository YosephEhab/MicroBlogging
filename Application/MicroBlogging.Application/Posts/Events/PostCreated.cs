using MicroBlogging.Domain.Entities;
using MediatR;
using MicroBlogging.Domain.Repositories;
using MicroBlogging.Application.Images;
using MicroBlogging.Application.Posts.Helpers;

namespace MicroBlogging.Application.Posts.Events;

public record PostCreatedEvent(Post Post) : INotification;

public sealed class PostCreatedEventHandler(IImageStorage imageStorage, IImageResizer resizer, IRepository<Post> postRepository) : INotificationHandler<PostCreatedEvent>
{
    public async Task Handle(PostCreatedEvent notification, CancellationToken cancellationToken)
    {
        var post = await postRepository.GetById(notification.Post.Id);
        if (post == null || post.Images.Count == 0) return;
        await ResizeAndUploadPostImages(post, cancellationToken);

        await postRepository.SaveChanges();
    }

    private async Task ResizeAndUploadPostImages(Post post, CancellationToken cancellationToken)
    {
        foreach (var attachment in post.Images)
        {
            // Define the base path to remove
            const string basePath = "/images/";
            var originalPath = new Uri(attachment.OriginalUrl).AbsolutePath;
            var index = originalPath.IndexOf(basePath, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                originalPath = originalPath.Substring(index + basePath.Length);
            }
            using var stream = await imageStorage.DownloadImage(originalPath, cancellationToken);

            foreach (var (width, height, label) in ImageSizes.Sizes)
            {
                using var resized = await resizer.Resize(stream, width, height, cancellationToken);

                // Path: /posts/{postId}/{imageId}-{width}x{height}.webp
                var imageId = attachment.Id;
                var path = BlobPathBuilder.BuildPostImagePath(post.Id, imageId, $"{width}x{height}", ".webp");
                string url = await imageStorage.UploadImage(resized, path, "image/webp", cancellationToken);
                attachment.AddVariant(new ImageVariant(url, width, height, "webp"));
            }
        }
    }
}
