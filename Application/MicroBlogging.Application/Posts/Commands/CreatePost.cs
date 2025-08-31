using FluentValidation;
using MediatR;
using MicroBlogging.Application.Posts.Events;
using MicroBlogging.Application.Posts.Helpers;
using MicroBlogging.Domain.Entities;
using MicroBlogging.Domain.Repositories;
using MicroBlogging.Domain.Rules;

namespace MicroBlogging.Application.Posts.Commands;

public sealed record CreatePostCommand(Guid UserId, string Text, GeoLocation Location, List<ImageUpload>? Images) : IRequest<Guid>;

public sealed class CreatePostCommandHandler(IRepository<Post> posts, IImageStorage imageStorage, IMediator mediator) : IRequestHandler<CreatePostCommand, Guid>
{
    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var post = new Post(request.UserId, request.Text, request.Location);

        if (request.Images is not null && request.Images.Count != 0)
        {
            foreach (var image in request.Images)
            {
                string url = await UploadImage(post.Id, image, cancellationToken);
                post.AddImage(new ImageAttachment(url));
            }
        }

        await posts.Add(post);
        await posts.SaveChanges();

        // Fire event for resizing
        await mediator.Publish(new PostCreatedEvent(post), cancellationToken);

        return post.Id;
    }

    private async Task<string> UploadImage(Guid postId, ImageUpload image, CancellationToken cancellationToken)
    {
        var imageId = Guid.NewGuid();
        var extension = Path.GetExtension(image.FileName);
        var path = BlobPathBuilder.BuildPostImagePath(postId, imageId, "original", extension);

        return await imageStorage.UploadImage(image.Content, path, image.ContentType, cancellationToken);
    }
}

public sealed class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Text)
            .NotEmpty()
            .MaximumLength(PostRules.MaxPostLength);

        RuleFor(x => x.Location)
            .NotNull();

        RuleFor(x => x.Images)
            .Must(images => images == null || images.Count <= PostRules.MaxImagesPerPost)
            .WithMessage($"A post cannot have more than {PostRules.MaxImagesPerPost} images.");
        
        RuleForEach(x => x.Images).Custom((file, ctx) =>
        {
            if (file is null)
            {
                ctx.AddFailure("Image cannot be null.");
                return;
            }

            // Validate size
            if (file.Content.CanSeek)
            {
                if (file.Content.Length > PostRules.MaxImageSizeInMB * 1024 * 1024)
                    ctx.AddFailure($"Image {file.FileName} exceeds {PostRules.MaxImageSizeInMB}MB.");
            }

            // Validate extension
            var ext = Path.GetExtension(file.FileName)?.TrimStart('.').ToLowerInvariant() ?? string.Empty;
            if (!PostRules.AllowedImageFormats.Contains(ext, StringComparer.OrdinalIgnoreCase))
                ctx.AddFailure($"Image format .{ext} is not allowed.");

            // Validate content type
            if (!PostRules.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                ctx.AddFailure($"Content type {file.ContentType} is not allowed.");
        });
    }
}
