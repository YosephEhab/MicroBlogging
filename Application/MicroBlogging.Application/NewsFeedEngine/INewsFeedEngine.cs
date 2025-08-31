using MicroBlogging.Domain.Entities;

namespace MicroBlogging.Application.NewsFeedEngine;

/// <summary>
/// Abstracting the news feed engine so that it can be replaced with a more sophisticated implementation later that shows more user-tailored content.
/// </summary>
public interface INewsFeedEngine
{
    Task<List<Post>> GetFeed(Guid userId, Guid? latestLoadedPostId);
}
