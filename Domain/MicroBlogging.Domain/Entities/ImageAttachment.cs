namespace MicroBlogging.Domain.Entities;

public class ImageAttachment : BaseEntity
{
    public Guid PostId { get; private set; }
    public string OriginalUrl { get; private set; }
    public List<ImageVariant> Variants { get; private set; } = [];

    private ImageAttachment() { }

    public ImageAttachment(string originalUrl)
    {
        OriginalUrl = originalUrl ?? throw new ArgumentNullException(nameof(originalUrl));
    }

    public void AddVariant(ImageVariant variant)
    {
        Variants.Add(variant ?? throw new ArgumentNullException(nameof(variant)));
        MarkUpdated();
    }

    public string GetBestMatch(int screenWidth)
    {
        var best = Variants.OrderBy(v => Math.Abs(v.Width - screenWidth)).FirstOrDefault();
        return best?.Url ?? OriginalUrl;
    }
}
