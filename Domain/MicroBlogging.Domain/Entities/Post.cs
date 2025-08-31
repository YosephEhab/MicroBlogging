using MicroBlogging.Domain.Rules;

namespace MicroBlogging.Domain.Entities;

public class Post : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Text { get; private set; }
    public GeoLocation Location { get; private set; }
    public List<ImageAttachment> Images { get; private set; } = [];

    private Post() { }

    public Post(Guid userId, string text, GeoLocation location, IEnumerable<ImageAttachment>? images = null) : base()
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Post text cannot be empty.", nameof(text));

        if (text.Length > PostRules.MaxPostLength)
            throw new ArgumentException($"Post text cannot exceed {PostRules.MaxPostLength} characters.", nameof(text));

        UserId = userId;
        Text = text;
        Location = location;
        Images = images?.ToList() ?? [];
    }

    public void AddImage(ImageAttachment image)
    {
        Images.Add(image ?? throw new ArgumentNullException(nameof(image)));
        MarkUpdated();
    }
}
