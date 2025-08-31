using MediatR;
using MicroBlogging.Application.NewsFeedEngine;
using MicroBlogging.Domain.Entities;
using MicroBlogging.Domain.Repositories;

namespace MicroBlogging.Application.Posts.Queries;

public record GetTimelineQuery(Guid UserId, Guid? LatestLoadedPostId, int ScreenWidth) : IRequest<IEnumerable<TimelinePost>>;

public class GetTimelineHandler(INewsFeedEngine newsFeedEngine, IRepository<User> userRepository) : IRequestHandler<GetTimelineQuery, IEnumerable<TimelinePost>>
{
    public async Task<IEnumerable<TimelinePost>> Handle(GetTimelineQuery request, CancellationToken cancellationToken)
    {
        var timeline = await newsFeedEngine.GetFeed(request.UserId, request.LatestLoadedPostId);
        if (timeline is null) return [];

        var users = (await userRepository.GetByIds(timeline.Select(p => p.UserId).Distinct().ToList()))!.ToDictionary(u => u.Id, u => u);

        return timeline.Select(p => new TimelinePost(
            p.Id,
            p.Text,
            p.Images?.Select(i => i.GetBestMatch(request.ScreenWidth)),
            users[p.UserId].Username,
            p.CreatedAt));
    }
}

public record TimelinePost(Guid PostId, string Content, IEnumerable<string>? ImageUrls, string Username, DateTime CreatedAt);