using MicroBlogging.Domain.Entities;
using MicroBlogging.Domain.Repositories;

namespace MicroBlogging.Application.NewsFeedEngine;

public class SimpleNewsFeedEngine(IPostRepository postRepository) : INewsFeedEngine
{
    private const int FeedPageSize = 10;

    public async Task<List<Post>> GetFeed(Guid userId, Guid? latestLoadedPostId)
    {
        if (latestLoadedPostId is null)
            return await postRepository.GetNextN(p => true, FeedPageSize);

        var latestLoadedPost = await postRepository.GetById(latestLoadedPostId.Value);
        return await postRepository.GetNextN(p => p.CreatedAt > latestLoadedPost!.CreatedAt, FeedPageSize);
    }
}
