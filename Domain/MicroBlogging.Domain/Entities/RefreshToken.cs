namespace MicroBlogging.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public string? CreatedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
}
