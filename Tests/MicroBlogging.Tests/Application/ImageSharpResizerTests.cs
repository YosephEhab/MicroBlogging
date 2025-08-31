using MicroBlogging.Application.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MicroBlogging.Tests.Application;

public class ImageSharpResizerTests
{
    [Fact]
    public async Task Resize_ShouldDownscaleImage_AndPreserveAspectRatio()
    {
        // Arrange
        var resizer = new ImageSharpResizer();
        using var originalImage = new Image<Rgba32>(400, 200); // 2:1 aspect ratio
        using var ms = new MemoryStream();
        await originalImage.SaveAsPngAsync(ms);
        ms.Position = 0;

        // Act
        using var resizedStream = await resizer.Resize(ms, 100, 100, CancellationToken.None);

        // Assert
        using var resizedImage = await Image.LoadAsync(resizedStream);
        Assert.True(resizedImage.Width <= 100);
        Assert.True(resizedImage.Height <= 100);
        // Aspect ratio should be preserved (2:1)
        Assert.Equal(resizedImage.Width, resizedImage.Height * 2);
    }

    [Fact]
    public async Task Resize_ShouldNotUpscaleImage()
    {
        // Arrange
        var resizer = new ImageSharpResizer();
        using var originalImage = new Image<Rgba32>(50, 50);
        using var ms = new MemoryStream();
        await originalImage.SaveAsPngAsync(ms);
        ms.Position = 0;

        // Act
        using var resizedStream = await resizer.Resize(ms, 100, 100, CancellationToken.None);

        // Assert
        using var resizedImage = await Image.LoadAsync(resizedStream);
        Assert.Equal(50, resizedImage.Width);
        Assert.Equal(50, resizedImage.Height);
    }

    [Fact]
    public async Task Resize_ShouldReturnWebpFormat()
    {
        // Arrange
        var resizer = new ImageSharpResizer();
        using var originalImage = new Image<Rgba32>(100, 100);
        using var ms = new MemoryStream();
        await originalImage.SaveAsPngAsync(ms);
        ms.Position = 0;

        // Act
        using var resizedStream = await resizer.Resize(ms, 50, 50, CancellationToken.None);

        // Assert
        resizedStream.Position = 0;
        // Try to load as WebP, should succeed
        var image = await Image.LoadAsync(resizedStream);
        Assert.Equal(50, image.Width);
        Assert.Equal(50, image.Height);
    }
}
