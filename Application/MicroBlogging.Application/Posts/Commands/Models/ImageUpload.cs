namespace MicroBlogging.Application.Posts.Commands;

public record ImageUpload(string FileName, string ContentType, Stream Content);
