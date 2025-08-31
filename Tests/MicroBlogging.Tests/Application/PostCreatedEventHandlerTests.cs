using System.Text;
using MicroBlogging.Application.Images;
using MicroBlogging.Application.Posts.Events;
using MicroBlogging.Domain.Entities;
using MicroBlogging.Domain.Repositories;
using Moq;

namespace MicroBlogging.Tests.Application;

public class PostCreatedEventHandlerTests
{
    private readonly Mock<IImageStorage> _mockImageStorage;
    private readonly Mock<IImageResizer> _mockImageResizer;
    private readonly Mock<IRepository<Post>> _mockPostRepository;
    private readonly PostCreatedEventHandler _handler;

    public PostCreatedEventHandlerTests()
    {
        _mockImageStorage = new Mock<IImageStorage>();
        _mockImageResizer = new Mock<IImageResizer>();
        _mockPostRepository = new Mock<IRepository<Post>>();
        _handler = new PostCreatedEventHandler(_mockImageStorage.Object, _mockImageResizer.Object, _mockPostRepository.Object);
    }

    [Fact]
    public async Task Handle_WithPostWithoutImages_ShouldReturnEarlyWithoutProcessing()
    {
        // Arrange
        var post = CreateTestPost("Test post", new List<ImageAttachment>());
        var postCreatedEvent = new PostCreatedEvent(post);

        _mockPostRepository
            .Setup(x => x.GetById(post.Id))
            .ReturnsAsync(post);

        // Act
        await _handler.Handle(postCreatedEvent, CancellationToken.None);

        // Assert
        _mockImageStorage.Verify(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockImageResizer.Verify(x => x.Resize(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockPostRepository.Verify(x => x.SaveChanges(), Times.Never);
    }

    [Fact]
    public async Task Handle_WithPostNotFound_ShouldReturnEarlyWithoutProcessing()
    {
        // Arrange
        var post = CreateTestPost("Test post");
        var postCreatedEvent = new PostCreatedEvent(post);

        _mockPostRepository
            .Setup(x => x.GetById(post.Id))
            .ReturnsAsync((Post?)null);

        // Act
        await _handler.Handle(postCreatedEvent, CancellationToken.None);

        // Assert
        _mockImageStorage.Verify(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockImageResizer.Verify(x => x.Resize(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockPostRepository.Verify(x => x.SaveChanges(), Times.Never);
    }

    [Fact]
    public async Task Handle_WithPostWithImages_ShouldProcessAllImages()
    {
        // Arrange
        var imageAttachments = new List<ImageAttachment>
        {
            new("https://example.com/images/posts/123/image1-original.jpg"),
            new("https://example.com/images/posts/123/image2-original.png")
        };
        var post = CreateTestPost("Test post", imageAttachments);
        var postCreatedEvent = new PostCreatedEvent(post);

        _mockPostRepository
            .Setup(x => x.GetById(post.Id))
            .ReturnsAsync(post);

        var originalImageStream = new MemoryStream(Encoding.UTF8.GetBytes("original image data"));
        _mockImageStorage
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalImageStream);

        var resizedImageStream = new MemoryStream(Encoding.UTF8.GetBytes("resized image data"));
        _mockImageResizer
            .Setup(x => x.Resize(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resizedImageStream);

        _mockImageStorage
            .Setup(x => x.UploadImage(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream stream, string path, string contentType, CancellationToken ct) => $"https://storage.example.com/{path}");

        _mockPostRepository.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(postCreatedEvent, CancellationToken.None);

        // Assert
        var expectedImageProcessingCalls = imageAttachments.Count * ImageSizes.Sizes.Count();
        
        _mockImageStorage.Verify(
            x => x.DownloadImage(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(imageAttachments.Count));

        _mockImageResizer.Verify(
            x => x.Resize(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Exactly(expectedImageProcessingCalls));

        _mockImageStorage.Verify(
            x => x.UploadImage(It.IsAny<Stream>(), It.IsAny<string>(), "image/webp", It.IsAny<CancellationToken>()),
            Times.Exactly(expectedImageProcessingCalls));

        _mockPostRepository.Verify(x => x.SaveChanges(), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldAddVariantsToImageAttachments()
    {
        // Arrange
        var imageAttachment = new ImageAttachment("https://example.com/images/posts/123/image1-original.jpg");
        var post = CreateTestPost("Test post", new List<ImageAttachment> { imageAttachment });
        var postCreatedEvent = new PostCreatedEvent(post);

        _mockPostRepository
            .Setup(x => x.GetById(post.Id))
            .ReturnsAsync(post);

        var originalImageStream = new MemoryStream(Encoding.UTF8.GetBytes("original image data"));
        _mockImageStorage
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalImageStream);

        var resizedImageStream = new MemoryStream(Encoding.UTF8.GetBytes("resized image data"));
        _mockImageResizer
            .Setup(x => x.Resize(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resizedImageStream);

        _mockImageStorage
            .Setup(x => x.UploadImage(It.IsAny<Stream>(), It.IsAny<string>(), "image/webp", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream stream, string path, string contentType, CancellationToken ct) => $"https://storage.example.com/{path}");

        _mockPostRepository.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(postCreatedEvent, CancellationToken.None);

        // Assert
        Assert.Equal(ImageSizes.Sizes.Count(), imageAttachment.Variants.Count);
        Assert.All(imageAttachment.Variants, variant =>
        {
            Assert.StartsWith("https://storage.example.com/", variant.Url);
            Assert.Equal("webp", variant.Format);
            Assert.Contains(ImageSizes.Sizes, size => size.Width == variant.Width && size.Height == variant.Height);
        });
    }

    [Fact]
    public async Task Handle_ShouldUseCorrectImagePaths()
    {
        // Arrange
        var imageAttachment = new ImageAttachment("https://example.com/images/posts/123/image1-original.jpg");
        var post = CreateTestPost("Test post", new List<ImageAttachment> { imageAttachment });
        var postCreatedEvent = new PostCreatedEvent(post);

        var capturedPaths = new List<string>();
        
        _mockPostRepository
            .Setup(x => x.GetById(post.Id))
            .ReturnsAsync(post);

        _mockImageStorage
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("image data")));

        _mockImageResizer
            .Setup(x => x.Resize(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("resized data")));

        _mockImageStorage
            .Setup(x => x.UploadImage(It.IsAny<Stream>(), It.IsAny<string>(), "image/webp", It.IsAny<CancellationToken>()))
            .Callback<Stream, string, string, CancellationToken>((stream, path, contentType, ct) => capturedPaths.Add(path))
            .ReturnsAsync((Stream stream, string path, string contentType, CancellationToken ct) => $"https://storage.example.com/{path}");

        _mockPostRepository.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(postCreatedEvent, CancellationToken.None);

        // Assert
        Assert.Equal(ImageSizes.Sizes.Count(), capturedPaths.Count);
        Assert.All(capturedPaths, path =>
        {
            Assert.StartsWith($"posts/{post.Id}/", path);
            Assert.Contains(imageAttachment.Id.ToString(), path);
            Assert.EndsWith(".webp", path);
        });
    }

    [Fact]
    public async Task Handle_ShouldExtractCorrectPathFromOriginalUrl()
    {
        // Arrange
        var imageAttachment = new ImageAttachment("https://example.com/images/posts/123/image1-original.jpg");
        var post = CreateTestPost("Test post", new List<ImageAttachment> { imageAttachment });
        var postCreatedEvent = new PostCreatedEvent(post);

        string capturedDownloadPath = null;
        
        _mockPostRepository
            .Setup(x => x.GetById(post.Id))
            .ReturnsAsync(post);

        _mockImageStorage
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((path, ct) => capturedDownloadPath = path)
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("image data")));

        _mockImageResizer
            .Setup(x => x.Resize(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("resized data")));

        _mockImageStorage
            .Setup(x => x.UploadImage(It.IsAny<Stream>(), It.IsAny<string>(), "image/webp", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage.example.com/resized");

        _mockPostRepository.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(postCreatedEvent, CancellationToken.None);

        // Assert
        Assert.Equal("posts/123/image1-original.jpg", capturedDownloadPath);
    }

    [Theory]
    [InlineData("https://example.com/images/posts/123/image1-original.jpg", "posts/123/image1-original.jpg")]
    [InlineData("https://cdn.example.com/images/posts/456/image2-original.png", "posts/456/image2-original.png")]
    [InlineData("https://storage.com/images/posts/789/image3-original.webp", "posts/789/image3-original.webp")]
    public async Task Handle_ShouldExtractPathCorrectlyFromVariousUrls(string originalUrl, string expectedPath)
    {
        // Arrange
        var imageAttachment = new ImageAttachment(originalUrl);
        var post = CreateTestPost("Test post", new List<ImageAttachment> { imageAttachment });
        var postCreatedEvent = new PostCreatedEvent(post);

        string capturedDownloadPath = null;
        
        _mockPostRepository
            .Setup(x => x.GetById(post.Id))
            .ReturnsAsync(post);

        _mockImageStorage
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((path, ct) => capturedDownloadPath = path)
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("image data")));

        _mockImageResizer
            .Setup(x => x.Resize(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("resized data")));

        _mockImageStorage
            .Setup(x => x.UploadImage(It.IsAny<Stream>(), It.IsAny<string>(), "image/webp", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage.example.com/resized");

        _mockPostRepository.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(postCreatedEvent, CancellationToken.None);

        // Assert
        Assert.Equal(expectedPath, capturedDownloadPath);
    }

    [Fact]
    public async Task Handle_WithMultipleImages_ShouldProcessAllImages()
    {
        // Arrange
        var imageAttachments = new List<ImageAttachment>
        {
            new("https://example.com/images/posts/123/image1-original.jpg"),
            new("https://example.com/images/posts/123/image2-original.png"),
            new("https://example.com/images/posts/123/image3-original.webp")
        };
        var post = CreateTestPost("Test post", imageAttachments);
        var postCreatedEvent = new PostCreatedEvent(post);

        _mockPostRepository
            .Setup(x => x.GetById(post.Id))
            .ReturnsAsync(post);

        _mockImageStorage
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("image data")));

        _mockImageResizer
            .Setup(x => x.Resize(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("resized data")));

        _mockImageStorage
            .Setup(x => x.UploadImage(It.IsAny<Stream>(), It.IsAny<string>(), "image/webp", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage.example.com/resized");

        _mockPostRepository.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(postCreatedEvent, CancellationToken.None);

        // Assert
        var totalExpectedCalls = imageAttachments.Count * ImageSizes.Sizes.Count();
        
        _mockImageStorage.Verify(
            x => x.DownloadImage(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(imageAttachments.Count));

        _mockImageResizer.Verify(
            x => x.Resize(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Exactly(totalExpectedCalls));

        _mockImageStorage.Verify(
            x => x.UploadImage(It.IsAny<Stream>(), It.IsAny<string>(), "image/webp", It.IsAny<CancellationToken>()),
            Times.Exactly(totalExpectedCalls));

        // Verify that all attachments have variants added
        Assert.All(imageAttachments, attachment =>
            Assert.Equal(ImageSizes.Sizes.Count(), attachment.Variants.Count));
    }

    [Fact]
    public async Task Handle_ShouldCreateVariantsForAllDefinedSizes()
    {
        // Arrange
        var imageAttachment = new ImageAttachment("https://example.com/images/posts/123/image1-original.jpg");
        var post = CreateTestPost("Test post", new List<ImageAttachment> { imageAttachment });
        var postCreatedEvent = new PostCreatedEvent(post);

        _mockPostRepository
            .Setup(x => x.GetById(post.Id))
            .ReturnsAsync(post);

        _mockImageStorage
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("image data")));

        var resizeCallParameters = new List<(int width, int height)>();
        _mockImageResizer
            .Setup(x => x.Resize(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, int, int, CancellationToken>((stream, width, height, ct) =>
                resizeCallParameters.Add((width, height)))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("resized data")));

        _mockImageStorage
            .Setup(x => x.UploadImage(It.IsAny<Stream>(), It.IsAny<string>(), "image/webp", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage.example.com/resized");

        _mockPostRepository.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(postCreatedEvent, CancellationToken.None);

        // Assert
        Assert.Equal(ImageSizes.Sizes.Count(), resizeCallParameters.Count);
        foreach (var (width, height, label) in ImageSizes.Sizes)
        {
            Assert.Contains((width, height), resizeCallParameters);
        }
    }

    [Fact]
    public async Task Handle_WhenImageDownloadFails_ShouldPropagateException()
    {
        // Arrange
        var imageAttachment = new ImageAttachment("https://example.com/images/posts/123/image1-original.jpg");
        var post = CreateTestPost("Test post", new List<ImageAttachment> { imageAttachment });
        var postCreatedEvent = new PostCreatedEvent(post);

        _mockPostRepository
            .Setup(x => x.GetById(post.Id))
            .ReturnsAsync(post);

        _mockImageStorage
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Download failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(postCreatedEvent, CancellationToken.None));
        
        Assert.Equal("Download failed", exception.Message);
    }

    [Fact]
    public async Task Handle_WhenImageResizeFails_ShouldPropagateException()
    {
        // Arrange
        var imageAttachment = new ImageAttachment("https://example.com/images/posts/123/image1-original.jpg");
        var post = CreateTestPost("Test post", new List<ImageAttachment> { imageAttachment });
        var postCreatedEvent = new PostCreatedEvent(post);

        _mockPostRepository
            .Setup(x => x.GetById(post.Id))
            .ReturnsAsync(post);

        _mockImageStorage
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("image data")));

        _mockImageResizer
            .Setup(x => x.Resize(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Resize failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(postCreatedEvent, CancellationToken.None));
        
        Assert.Equal("Resize failed", exception.Message);
    }

    [Fact]
    public async Task Handle_WhenImageUploadFails_ShouldPropagateException()
    {
        // Arrange
        var imageAttachment = new ImageAttachment("https://example.com/images/posts/123/image1-original.jpg");
        var post = CreateTestPost("Test post", new List<ImageAttachment> { imageAttachment });
        var postCreatedEvent = new PostCreatedEvent(post);

        _mockPostRepository
            .Setup(x => x.GetById(post.Id))
            .ReturnsAsync(post);

        _mockImageStorage
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("image data")));

        _mockImageResizer
            .Setup(x => x.Resize(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("resized data")));

        _mockImageStorage
            .Setup(x => x.UploadImage(It.IsAny<Stream>(), It.IsAny<string>(), "image/webp", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Upload failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(postCreatedEvent, CancellationToken.None));
        
        Assert.Equal("Upload failed", exception.Message);
    }

    [Fact]
    public async Task Handle_WhenSaveChangesFails_ShouldPropagateException()
    {
        // Arrange
        var imageAttachment = new ImageAttachment("https://example.com/images/posts/123/image1-original.jpg");
        var post = CreateTestPost("Test post", new List<ImageAttachment> { imageAttachment });
        var postCreatedEvent = new PostCreatedEvent(post);

        _mockPostRepository
            .Setup(x => x.GetById(post.Id))
            .ReturnsAsync(post);

        _mockImageStorage
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("image data")));

        _mockImageResizer
            .Setup(x => x.Resize(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("resized data")));

        _mockImageStorage
            .Setup(x => x.UploadImage(It.IsAny<Stream>(), It.IsAny<string>(), "image/webp", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage.example.com/resized");

        _mockPostRepository
            .Setup(x => x.SaveChanges())
            .ThrowsAsync(new InvalidOperationException("Save failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(postCreatedEvent, CancellationToken.None));
        
        Assert.Equal("Save failed", exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldProcessImagesSynchronously()
    {
        // Arrange
        var imageAttachments = new List<ImageAttachment>
        {
            new("https://example.com/images/posts/123/image1-original.jpg"),
            new("https://example.com/images/posts/123/image2-original.jpg")
        };
        var post = CreateTestPost("Test post", imageAttachments);
        var postCreatedEvent = new PostCreatedEvent(post);

        var processingOrder = new List<string>();
        
        _mockPostRepository
            .Setup(x => x.GetById(post.Id))
            .ReturnsAsync(post);

        _mockImageStorage
            .Setup(x => x.DownloadImage(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((path, ct) => processingOrder.Add($"Download: {path}"))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("image data")));

        _mockImageResizer
            .Setup(x => x.Resize(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("resized data")));

        _mockImageStorage
            .Setup(x => x.UploadImage(It.IsAny<Stream>(), It.IsAny<string>(), "image/webp", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage.example.com/resized");

        _mockPostRepository.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(postCreatedEvent, CancellationToken.None);

        // Assert
        Assert.Equal(2, processingOrder.Count);
        Assert.Contains("Download: posts/123/image1-original.jpg", processingOrder);
        Assert.Contains("Download: posts/123/image2-original.jpg", processingOrder);
    }

    private Post CreateTestPost(string text, List<ImageAttachment>? images = null)
    {
        var post = new Post(Guid.NewGuid(), text, new GeoLocation(40.7128, -74.0060));
        
        if (images != null)
        {
            foreach (var image in images)
            {
                post.AddImage(image);
            }
        }

        return post;
    }
}
