using System.Linq.Expressions;
using MicroBlogging.Application.NewsFeedEngine;
using MicroBlogging.Domain.Entities;
using MicroBlogging.Domain.Repositories;
using Moq;

namespace MicroBlogging.Tests.Application;

public class SimpleNewsFeedEngineTests
{
    private readonly Mock<IPostRepository> _mockPostRepository;
    private readonly SimpleNewsFeedEngine _newsFeedEngine;
    private readonly Guid _testUserId = Guid.NewGuid();

    public SimpleNewsFeedEngineTests()
    {
        _mockPostRepository = new Mock<IPostRepository>();
        _newsFeedEngine = new SimpleNewsFeedEngine(_mockPostRepository.Object);
    }

    [Fact]
    public async Task GetFeed_WhenLatestLoadedPostIdIsNull_ShouldReturnFirstPageOfPosts()
    {
        // Arrange
        var expectedPosts = CreateTestPosts(10);
        _mockPostRepository
            .Setup(x => x.GetNextN(It.IsAny<Expression<Func<Post, bool>>>(), 10))
            .ReturnsAsync(expectedPosts);

        // Act
        var result = await _newsFeedEngine.GetFeed(_testUserId, null);

        // Assert
        Assert.Equal(expectedPosts, result);
        _mockPostRepository.Verify(
            x => x.GetNextN(It.IsAny<Expression<Func<Post, bool>>>(), 10),
            Times.Once);
    }

    [Fact]
    public async Task GetFeed_WhenLatestLoadedPostIdIsProvided_ShouldReturnPostsAfterLatestLoaded()
    {
        // Arrange
        var latestLoadedPostId = Guid.NewGuid();
        var latestLoadedPost = CreateTestPost(_testUserId, "Latest post", DateTime.UtcNow.AddHours(-1));
        var expectedPosts = CreateTestPosts(10, DateTime.UtcNow);

        _mockPostRepository
            .Setup(x => x.GetById(latestLoadedPostId))
            .ReturnsAsync(latestLoadedPost);

        _mockPostRepository
            .Setup(x => x.GetNextN(It.IsAny<Expression<Func<Post, bool>>>(), 10))
            .ReturnsAsync(expectedPosts);

        // Act
        var result = await _newsFeedEngine.GetFeed(_testUserId, latestLoadedPostId);

        // Assert
        Assert.Equal(expectedPosts, result);
        _mockPostRepository.Verify(x => x.GetById(latestLoadedPostId), Times.Once);
        _mockPostRepository.Verify(
            x => x.GetNextN(It.IsAny<Expression<Func<Post, bool>>>(), 10),
            Times.Once);
    }

    [Fact]
    public async Task GetFeed_WhenLatestLoadedPostIdIsProvided_ShouldFilterPostsByCreatedAtTime()
    {
        // Arrange
        var latestLoadedPostId = Guid.NewGuid();
        var latestLoadedPost = CreateTestPost(_testUserId, "Latest post", DateTime.UtcNow.AddHours(-2));
        var expectedPosts = CreateTestPosts(5, DateTime.UtcNow.AddHours(-1));

        _mockPostRepository
            .Setup(x => x.GetById(latestLoadedPostId))
            .ReturnsAsync(latestLoadedPost);

        _mockPostRepository
            .Setup(x => x.GetNextN(
                It.Is<Expression<Func<Post, bool>>>(expr => expr != null),
                10))
            .ReturnsAsync(expectedPosts)
            .Callback<Expression<Func<Post, bool>>, int>((predicate, pageSize) =>
            {
                // Verify that the predicate correctly filters by CreatedAt
                var compiledPredicate = predicate.Compile();
                var testPost = CreateTestPost(_testUserId, "Test", latestLoadedPost.CreatedAt.AddMinutes(1));
                var olderPost = CreateTestPost(_testUserId, "Test", latestLoadedPost.CreatedAt.AddMinutes(-1));

                Assert.True(compiledPredicate(testPost));
                Assert.False(compiledPredicate(olderPost));
            });

        // Act
        var result = await _newsFeedEngine.GetFeed(_testUserId, latestLoadedPostId);

        // Assert
        Assert.Equal(expectedPosts, result);
    }

    [Fact]
    public async Task GetFeed_WhenRepositoryReturnsEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        _mockPostRepository
            .Setup(x => x.GetNextN(It.IsAny<Expression<Func<Post, bool>>>(), 10))
            .ReturnsAsync(new List<Post>());

        // Act
        var result = await _newsFeedEngine.GetFeed(_testUserId, null);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFeed_WhenRepositoryReturnsFewerThanPageSize_ShouldReturnAllAvailablePosts()
    {
        // Arrange
        var expectedPosts = CreateTestPosts(5); // Less than page size of 10
        _mockPostRepository
            .Setup(x => x.GetNextN(It.IsAny<Expression<Func<Post, bool>>>(), 10))
            .ReturnsAsync(expectedPosts);

        // Act
        var result = await _newsFeedEngine.GetFeed(_testUserId, null);

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal(expectedPosts, result);
    }

    [Fact]
    public async Task GetFeed_ShouldAlwaysRequestExactlyPageSizeFromRepository()
    {
        // Arrange
        var expectedPosts = CreateTestPosts(10);
        _mockPostRepository
            .Setup(x => x.GetNextN(It.IsAny<Expression<Func<Post, bool>>>(), It.IsAny<int>()))
            .ReturnsAsync(expectedPosts);

        // Act
        await _newsFeedEngine.GetFeed(_testUserId, null);

        // Assert
        _mockPostRepository.Verify(
            x => x.GetNextN(It.IsAny<Expression<Func<Post, bool>>>(), 10),
            Times.Once);
    }

    [Fact]
    public async Task GetFeed_WhenCalledMultipleTimesWithSameParameters_ShouldCallRepositoryEachTime()
    {
        // Arrange
        var expectedPosts = CreateTestPosts(10);
        _mockPostRepository
            .Setup(x => x.GetNextN(It.IsAny<Expression<Func<Post, bool>>>(), 10))
            .ReturnsAsync(expectedPosts);

        // Act
        await _newsFeedEngine.GetFeed(_testUserId, null);
        await _newsFeedEngine.GetFeed(_testUserId, null);

        // Assert
        _mockPostRepository.Verify(
            x => x.GetNextN(It.IsAny<Expression<Func<Post, bool>>>(), 10),
            Times.Exactly(2));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("550e8400-e29b-41d4-a716-446655440000")]
    public async Task GetFeed_WithDifferentLatestLoadedPostIds_ShouldHandleBothScenarios(string? latestLoadedPostIdString)
    {
        // Arrange
        Guid? latestLoadedPostId = latestLoadedPostIdString != null ? Guid.Parse(latestLoadedPostIdString) : null;
        var expectedPosts = CreateTestPosts(10);

        if (latestLoadedPostId.HasValue)
        {
            var latestPost = CreateTestPost(_testUserId, "Latest", DateTime.UtcNow.AddHours(-1));
            _mockPostRepository
                .Setup(x => x.GetById(latestLoadedPostId.Value))
                .ReturnsAsync(latestPost);
        }

        _mockPostRepository
            .Setup(x => x.GetNextN(It.IsAny<Expression<Func<Post, bool>>>(), 10))
            .ReturnsAsync(expectedPosts);

        // Act
        var result = await _newsFeedEngine.GetFeed(_testUserId, latestLoadedPostId);

        // Assert
        Assert.Equal(expectedPosts, result);
        Assert.Equal(10, result.Count);
    }

    [Fact]
    public async Task GetFeed_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        _mockPostRepository
            .Setup(x => x.GetNextN(It.IsAny<Expression<Func<Post, bool>>>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _newsFeedEngine.GetFeed(_testUserId, null));

        Assert.Equal("Database connection failed", exception.Message);
    }

    [Fact]
    public async Task GetFeed_WhenGetByIdThrowsException_ShouldPropagateException()
    {
        // Arrange
        var latestLoadedPostId = Guid.NewGuid();
        _mockPostRepository
            .Setup(x => x.GetById(latestLoadedPostId))
            .ThrowsAsync(new InvalidOperationException("Post not found"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _newsFeedEngine.GetFeed(_testUserId, latestLoadedPostId));

        Assert.Equal("Post not found", exception.Message);
    }

    [Fact]
    public async Task GetFeed_WithRealWorldScenario_ShouldHandlePaginationCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow;

        // First page of posts
        var firstPagePosts = new List<Post>
        {
            CreatePostWithTime(userId, "Post 1", baseTime.AddMinutes(-1)),
            CreatePostWithTime(userId, "Post 2", baseTime.AddMinutes(-2)),
            CreatePostWithTime(userId, "Post 3", baseTime.AddMinutes(-3))
        };

        // Second page of posts (after latest loaded)
        var latestLoadedPost = firstPagePosts.Last();
        var secondPagePosts = new List<Post>
        {
            CreatePostWithTime(userId, "Post 4", baseTime.AddMinutes(-4)),
            CreatePostWithTime(userId, "Post 5", baseTime.AddMinutes(-5))
        };

        _mockPostRepository
            .Setup(x => x.GetNextN(It.IsAny<Expression<Func<Post, bool>>>(), 10))
            .ReturnsAsync((Expression<Func<Post, bool>> predicate, int count) =>
            {
                var compiledPredicate = predicate.Compile();
                // Simulate filtering behavior
                return compiledPredicate(CreatePostWithTime(userId, "Test", baseTime)) ?
                    firstPagePosts : secondPagePosts;
            });

        _mockPostRepository
            .Setup(x => x.GetById(latestLoadedPost.Id))
            .ReturnsAsync(latestLoadedPost);

        // Act - First call (initial load)
        var firstResult = await _newsFeedEngine.GetFeed(userId, null);

        // Act - Second call (pagination)
        var secondResult = await _newsFeedEngine.GetFeed(userId, latestLoadedPost.Id);

        // Assert
        Assert.Equal(3, firstResult.Count);
        Assert.Equal(3, secondResult.Count);
        _mockPostRepository.Verify(x => x.GetById(latestLoadedPost.Id), Times.Once);
    }

    [Fact]
    public async Task GetFeed_WithValidLatestLoadedPostId_ShouldVerifyCorrectFilterPredicate()
    {
        // Arrange
        var latestLoadedPostId = Guid.NewGuid();
        var latestLoadedPostTime = DateTime.UtcNow.AddHours(-1);
        var latestLoadedPost = CreatePostWithTime(_testUserId, "Latest", latestLoadedPostTime);

        Expression<Func<Post, bool>>? capturedPredicate = null;

        _mockPostRepository
            .Setup(x => x.GetById(latestLoadedPostId))
            .ReturnsAsync(latestLoadedPost);

        _mockPostRepository
            .Setup(x => x.GetNextN(It.IsAny<Expression<Func<Post, bool>>>(), 10))
            .ReturnsAsync(new List<Post>())
            .Callback<Expression<Func<Post, bool>>, int>((predicate, count) =>
            {
                capturedPredicate = predicate;
            });

        // Act
        await _newsFeedEngine.GetFeed(_testUserId, latestLoadedPostId);

        // Assert
        Assert.NotNull(capturedPredicate);

        var compiledPredicate = capturedPredicate.Compile();
        var newerPost = CreatePostWithTime(_testUserId, "Newer", latestLoadedPostTime.AddMinutes(1));
        var olderPost = CreatePostWithTime(_testUserId, "Older", latestLoadedPostTime.AddMinutes(-1));
        var sameTimePost = CreatePostWithTime(_testUserId, "Same", latestLoadedPostTime);

        Assert.True(compiledPredicate(newerPost));
        Assert.False(compiledPredicate(olderPost));
        Assert.False(compiledPredicate(sameTimePost));
    }

    [Fact]
    public async Task GetFeed_ConstantPageSize_ShouldAlwaysUse10()
    {
        // Arrange
        _mockPostRepository
            .Setup(x => x.GetNextN(It.IsAny<Expression<Func<Post, bool>>>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Post>());

        // Act
        await _newsFeedEngine.GetFeed(_testUserId, null);

        // Assert
        _mockPostRepository.Verify(
            x => x.GetNextN(It.IsAny<Expression<Func<Post, bool>>>(), 10),
            Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    public async Task GetFeed_WithVariousPostCounts_ShouldReturnExactCountFromRepository(int postCount)
    {
        // Arrange
        var posts = CreateTestPosts(postCount);
        _mockPostRepository
            .Setup(x => x.GetNextN(It.IsAny<Expression<Func<Post, bool>>>(), 10))
            .ReturnsAsync(posts);

        // Act
        var result = await _newsFeedEngine.GetFeed(_testUserId, null);

        // Assert
        Assert.Equal(postCount, result.Count);
        Assert.Equal(posts, result);
    }

    private Post CreatePostWithTime(Guid userId, string text, DateTime createdAt)
    {
        var post = new Post(userId, text, new GeoLocation(0, 0));

        // Use reflection to set the protected CreatedAt property
        var createdAtProperty = typeof(BaseEntity).GetProperty(nameof(BaseEntity.CreatedAt));
        createdAtProperty?.SetValue(post, createdAt);

        return post;
    }

    #region Helper Methods

    private List<Post> CreateTestPosts(int count, DateTime? baseDateTime = null)
    {
        var posts = new List<Post>();
        var baseTime = baseDateTime ?? DateTime.UtcNow;

        for (int i = 0; i < count; i++)
        {
            posts.Add(CreateTestPost(
                Guid.NewGuid(),
                $"Test post {i + 1}",
                baseTime.AddMinutes(-i)));
        }

        return posts;
    }

    private Post CreateTestPost(Guid userId, string text, DateTime createdAt)
    {
        var post = new Post(userId, text, new GeoLocation(0, 0));

        // Use reflection to set the CreatedAt property since it's protected
        var createdAtProperty = typeof(BaseEntity).GetProperty(nameof(BaseEntity.CreatedAt));
        createdAtProperty?.SetValue(post, createdAt);

        return post;
    }

    #endregion
}
