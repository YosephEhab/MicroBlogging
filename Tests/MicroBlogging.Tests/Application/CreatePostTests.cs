using System.Text;
using FluentValidation.TestHelper;
using MediatR;
using MicroBlogging.Application.Posts.Commands;
using MicroBlogging.Application.Posts.Events;
using MicroBlogging.Domain.Entities;
using MicroBlogging.Domain.Repositories;
using MicroBlogging.Domain.Rules;
using Moq;

namespace MicroBlogging.Tests.Application;

public class CreatePostCommandHandlerTests
{
    private readonly Mock<IRepository<Post>> _mockPostRepository;
    private readonly Mock<IImageStorage> _mockImageStorage;
    private readonly Mock<IMediator> _mockMediator;
    private readonly CreatePostCommandHandler _handler;

    public CreatePostCommandHandlerTests()
    {
        _mockPostRepository = new Mock<IRepository<Post>>();
        _mockImageStorage = new Mock<IImageStorage>();
        _mockMediator = new Mock<IMediator>();
        _handler = new CreatePostCommandHandler(_mockPostRepository.Object, _mockImageStorage.Object, _mockMediator.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommandAndNoImages_ShouldCreatePostAndReturnId()
    {
        // Arrange
        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Test post content",
            new GeoLocation(40.7128, -74.0060),
            null);

        Post capturedPost = null;
        _mockPostRepository
            .Setup(x => x.Add(It.IsAny<Post>()))
            .Callback<Post>(post => capturedPost = post)
            .Returns(Task.CompletedTask);

        _mockPostRepository
            .Setup(x => x.SaveChanges())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(capturedPost);
        Assert.Equal(command.UserId, capturedPost.UserId);
        Assert.Equal(command.Text, capturedPost.Text);
        Assert.Equal(command.Location.Latitude, capturedPost.Location.Latitude);
        Assert.Equal(command.Location.Longitude, capturedPost.Location.Longitude);
        Assert.Empty(capturedPost.Images);

        _mockPostRepository.Verify(x => x.Add(It.IsAny<Post>()), Times.Once);
        _mockPostRepository.Verify(x => x.SaveChanges(), Times.Once);
        _mockMediator.Verify(x => x.Publish(It.IsAny<PostCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommandAndImages_ShouldUploadImagesAndCreatePost()
    {
        // Arrange
        var imageContent1 = new MemoryStream(Encoding.UTF8.GetBytes("fake image data 1"));
        var imageContent2 = new MemoryStream(Encoding.UTF8.GetBytes("fake image data 2"));
        
        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Post with images",
            new GeoLocation(40.7128, -74.0060),
            new List<ImageUpload>
            {
                new("image1.jpg", "image/jpeg", imageContent1),
                new("image2.png", "image/png", imageContent2)
            });

        _mockImageStorage
            .Setup(x => x.UploadImage(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream content, string path, string contentType, CancellationToken ct) => $"https://storage.example.com/{path}");

        Post capturedPost = null;
        _mockPostRepository
            .Setup(x => x.Add(It.IsAny<Post>()))
            .Callback<Post>(post => capturedPost = post)
            .Returns(Task.CompletedTask);

        _mockPostRepository.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(capturedPost);
        Assert.Equal(2, capturedPost.Images.Count);
        Assert.All(capturedPost.Images, img => Assert.StartsWith("https://storage.example.com/", img.OriginalUrl));

        _mockImageStorage.Verify(
            x => x.UploadImage(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WithEmptyImagesList_ShouldCreatePostWithoutImages()
    {
        // Arrange
        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Test post",
            new GeoLocation(40.7128, -74.0060),
            new List<ImageUpload>());

        Post capturedPost = null;
        _mockPostRepository
            .Setup(x => x.Add(It.IsAny<Post>()))
            .Callback<Post>(post => capturedPost = post)
            .Returns(Task.CompletedTask);

        _mockPostRepository.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedPost);
        Assert.Empty(capturedPost.Images);
        _mockImageStorage.Verify(
            x => x.UploadImage(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldPublishPostCreatedEvent()
    {
        // Arrange
        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Test post",
            new GeoLocation(40.7128, -74.0060),
            null);

        PostCreatedEvent capturedEvent = null;
        _mockMediator
            .Setup(x => x.Publish(It.IsAny<PostCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<INotification, CancellationToken>((evt, ct) => capturedEvent = evt as PostCreatedEvent)
            .Returns(Task.CompletedTask);

        _mockPostRepository.Setup(x => x.Add(It.IsAny<Post>())).Returns(Task.CompletedTask);
        _mockPostRepository.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.NotNull(capturedEvent.Post);
        Assert.Equal(command.UserId, capturedEvent.Post.UserId);
        Assert.Equal(command.Text, capturedEvent.Post.Text);
    }

    [Fact]
    public async Task Handle_WhenImageUploadFails_ShouldPropagateException()
    {
        // Arrange
        var imageContent = new MemoryStream(Encoding.UTF8.GetBytes("fake image data"));
        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Post with images",
            new GeoLocation(40.7128, -74.0060),
            new List<ImageUpload> { new("image.jpg", "image/jpeg", imageContent) });

        _mockImageStorage
            .Setup(x => x.UploadImage(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Upload failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        Assert.Equal("Upload failed", exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRepositoryAddFails_ShouldPropagateException()
    {
        // Arrange
        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Test post",
            new GeoLocation(40.7128, -74.0060),
            null);

        _mockPostRepository
            .Setup(x => x.Add(It.IsAny<Post>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        Assert.Equal("Database error", exception.Message);
    }

    [Fact]
    public async Task Handle_WhenSaveChangesFails_ShouldPropagateException()
    {
        // Arrange
        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Test post",
            new GeoLocation(40.7128, -74.0060),
            null);

        _mockPostRepository.Setup(x => x.Add(It.IsAny<Post>())).Returns(Task.CompletedTask);
        _mockPostRepository
            .Setup(x => x.SaveChanges())
            .ThrowsAsync(new InvalidOperationException("Save failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        Assert.Equal("Save failed", exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldUseCorrectImagePath()
    {
        // Arrange
        var imageContent = new MemoryStream(Encoding.UTF8.GetBytes("fake image data"));
        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Post with image",
            new GeoLocation(40.7128, -74.0060),
            new List<ImageUpload> { new("test.jpg", "image/jpeg", imageContent) });

        string capturedPath = null;
        _mockImageStorage
            .Setup(x => x.UploadImage(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, string, string, CancellationToken>((content, path, contentType, ct) => capturedPath = path)
            .ReturnsAsync("https://storage.example.com/uploaded");

        _mockPostRepository.Setup(x => x.Add(It.IsAny<Post>())).Returns(Task.CompletedTask);
        _mockPostRepository.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedPath);
        Assert.Contains("original", capturedPath);
        Assert.EndsWith(".jpg", capturedPath);
    }
}

public class CreatePostCommandValidatorTests
{
    private readonly CreatePostCommandValidator _validator;

    public CreatePostCommandValidatorTests()
    {
        _validator = new CreatePostCommandValidator();
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Valid post text",
            new GeoLocation(40.7128, -74.0060),
            null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Guid.Empty,
            "Valid post text",
            new GeoLocation(40.7128, -74.0060),
            null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithInvalidText_ShouldHaveValidationError(string text)
    {
        // Arrange
        var command = new CreatePostCommand(
            Guid.NewGuid(),
            text,
            new GeoLocation(40.7128, -74.0060),
            null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Text);
    }

    [Fact]
    public void Validate_WithTextExceedingMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        var longText = new string('a', PostRules.MaxPostLength + 1);
        var command = new CreatePostCommand(
            Guid.NewGuid(),
            longText,
            new GeoLocation(40.7128, -74.0060),
            null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Text);
    }

    [Fact]
    public void Validate_WithNullLocation_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Valid post text",
            null,
            null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Location);
    }

    [Fact]
    public void Validate_WithTooManyImages_ShouldHaveValidationError()
    {
        // Arrange
        var images = new List<ImageUpload>();
        for (int i = 0; i < PostRules.MaxImagesPerPost + 1; i++)
        {
            images.Add(new ImageUpload($"image{i}.jpg", "image/jpeg", new MemoryStream()));
        }

        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Post with too many images",
            new GeoLocation(40.7128, -74.0060),
            images);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Images)
            .WithErrorMessage($"A post cannot have more than {PostRules.MaxImagesPerPost} images.");
    }

    [Fact]
    public void Validate_WithNullImageInList_ShouldHaveValidationError()
    {
        // Arrange
        var images = new List<ImageUpload>
        {
            new("valid.jpg", "image/jpeg", new MemoryStream()),
            null
        };

        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Post with null image",
            new GeoLocation(40.7128, -74.0060),
            images);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Images[1]")
            .WithErrorMessage("Image cannot be null.");
    }

    [Fact]
    public void Validate_WithImageExceedingMaxSize_ShouldHaveValidationError()
    {
        // Arrange
        var largeImageContent = new MemoryStream(new byte[PostRules.MaxImageSizeInMB * 1024 * 1024 + 1]);
        var images = new List<ImageUpload>
        {
            new("large.jpg", "image/jpeg", largeImageContent)
        };

        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Post with large image",
            new GeoLocation(40.7128, -74.0060),
            images);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Images[0]")
            .WithErrorMessage($"Image large.jpg exceeds {PostRules.MaxImageSizeInMB}MB.");
    }

    [Theory]
    [InlineData("image.txt", "Invalid extension")]
    [InlineData("image.pdf", "Invalid extension")]
    [InlineData("image.gif", "Valid extension but not in allowed formats")]
    public void Validate_WithInvalidImageExtension_ShouldHaveValidationError(string fileName, string description)
    {
        // Arrange
        var images = new List<ImageUpload>
        {
            new(fileName, "image/jpeg", new MemoryStream(new byte[100]))
        };

        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Post with invalid image",
            new GeoLocation(40.7128, -74.0060),
            images);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        var extension = Path.GetExtension(fileName)?.TrimStart('.').ToLowerInvariant() ?? string.Empty;
        result.ShouldHaveValidationErrorFor("Images[0]")
            .WithErrorMessage($"Image format .{extension} is not allowed.");
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    [InlineData("video/mp4")]
    public void Validate_WithInvalidContentType_ShouldHaveValidationError(string contentType)
    {
        // Arrange
        var images = new List<ImageUpload>
        {
            new("image.jpg", contentType, new MemoryStream(new byte[100]))
        };

        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Post with invalid content type",
            new GeoLocation(40.7128, -74.0060),
            images);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Images[0]")
            .WithErrorMessage($"Content type {contentType} is not allowed.");
    }

    [Theory]
    [InlineData("image.jpg", "image/jpeg")]
    [InlineData("image.jpeg", "image/jpeg")]
    [InlineData("image.png", "image/png")]
    [InlineData("image.webp", "image/webp")]
    public void Validate_WithValidImageFormats_ShouldNotHaveValidationErrors(string fileName, string contentType)
    {
        // Arrange
        var images = new List<ImageUpload>
        {
            new(fileName, contentType, new MemoryStream(new byte[100]))
        };

        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Post with valid image",
            new GeoLocation(40.7128, -74.0060),
            images);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Images);
    }

    [Fact]
    public void Validate_WithExactlyMaxPostLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var maxLengthText = new string('a', PostRules.MaxPostLength);
        var command = new CreatePostCommand(
            Guid.NewGuid(),
            maxLengthText,
            new GeoLocation(40.7128, -74.0060),
            null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Text);
    }

    [Fact]
    public void Validate_WithExactlyMaxImagesPerPost_ShouldNotHaveValidationError()
    {
        // Arrange
        var images = new List<ImageUpload>();
        for (int i = 0; i < PostRules.MaxImagesPerPost; i++)
        {
            images.Add(new ImageUpload($"image{i}.jpg", "image/jpeg", new MemoryStream(new byte[100])));
        }

        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Post with max images",
            new GeoLocation(40.7128, -74.0060),
            images);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Images);
    }

    [Fact]
    public void Validate_WithNonSeekableStream_ShouldSkipSizeValidation()
    {
        // Arrange
        var mockStream = new Mock<Stream>();
        mockStream.Setup(s => s.CanSeek).Returns(false);
        
        var images = new List<ImageUpload>
        {
            new("image.jpg", "image/jpeg", mockStream.Object)
        };

        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Post with non-seekable stream",
            new GeoLocation(40.7128, -74.0060),
            images);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        // Should not have size validation error since stream is not seekable
        result.ShouldNotHaveValidationErrorFor("Images[0]");
    }
}

// Test class for the Command record itself
public class CreatePostCommandTests
{
    [Fact]
    public void CreatePostCommand_ShouldBeRecord()
    {
        // Arrange & Act
        var command1 = new CreatePostCommand(
            Guid.NewGuid(),
            "Test",
            new GeoLocation(0, 0),
            null);

        var command2 = new CreatePostCommand(
            command1.UserId,
            command1.Text,
            command1.Location,
            command1.Images);

        // Assert
        Assert.Equal(command1, command2);
        Assert.Equal(command1.GetHashCode(), command2.GetHashCode());
    }

    [Fact]
    public void CreatePostCommand_ShouldImplementIRequest()
    {
        // Arrange
        var command = new CreatePostCommand(
            Guid.NewGuid(),
            "Test",
            new GeoLocation(0, 0),
            null);

        // Assert
        Assert.IsAssignableFrom<IRequest<Guid>>(command);
    }
}