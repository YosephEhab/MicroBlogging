namespace MicroBlogging.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }

    private User() { }

    public User(string username, string passwordHash)
    {
        Username = username ?? throw new ArgumentNullException(nameof(username));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
    }
}
