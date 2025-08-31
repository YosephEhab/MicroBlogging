using MicroBlogging.Application.Posts.Helpers;

namespace MicroBlogging.Tests.Application;

public class BlobPathBuilderTests
{
    [Fact]
    public void BuildPostImagePath_ShouldReturnCorrectFormat()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var size = "800x600";
        var extension = ".webp";

        // Act
        var result = BlobPathBuilder.BuildPostImagePath(postId, imageId, size, extension);

        // Assert
        var expected = $"posts/{postId}/{imageId}-{size}{extension}";
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("thumbnail", ".jpg")]
    [InlineData("1920x1080", ".webp")]
    [InlineData("original", ".png")]
    public void BuildPostImagePath_WithVariousParameters_ShouldReturnCorrectFormat(string size, string extension)
    {
        // Arrange
        var postId = Guid.NewGuid();
        var imageId = Guid.NewGuid();

        // Act
        var result = BlobPathBuilder.BuildPostImagePath(postId, imageId, size, extension);

        // Assert
        Assert.StartsWith($"posts/{postId}/", result);
        Assert.Contains($"{imageId}-{size}", result);
        Assert.EndsWith(extension, result);
    }

    [Fact]
    public void ReplaceSize_ShouldReplaceLastSizeSegment()
    {
        // Arrange
        var originalPath = "posts/123/image-456-800x600.webp";
        var newSize = "1920x1080";
        var extension = ".webp";

        // Act
        var result = BlobPathBuilder.ReplaceSize(originalPath, newSize, extension);

        // Assert
        Assert.Equal("posts/123/image-456-1920x1080.webp", result);
    }

    [Fact]
    public void ReplaceSize_WithPathWithoutDashes_ShouldReturnOriginalPath()
    {
        // Arrange
        var originalPath = "posts/123/image.webp";
        var newSize = "1920x1080";
        var extension = ".webp";

        // Act
        var result = BlobPathBuilder.ReplaceSize(originalPath, newSize, extension);

        // Assert
        Assert.Equal(originalPath, result);
    }

    [Theory]
    [InlineData("posts/123/image-456-800x600.webp", "thumbnail", ".jpg", "posts/123/image-456-thumbnail.jpg")]
    [InlineData("images/test-image-original.png", "small", ".webp", "images/test-image-small.webp")]
    public void ReplaceSize_WithVariousInputs_ShouldProduceExpectedOutput(string originalPath, string newSize, string extension, string expected)
    {
        // Act
        var result = BlobPathBuilder.ReplaceSize(originalPath, newSize, extension);

        // Assert
        Assert.Equal(expected, result);
    }
}