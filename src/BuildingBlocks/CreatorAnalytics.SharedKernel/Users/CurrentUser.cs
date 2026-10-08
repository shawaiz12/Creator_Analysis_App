namespace CreatorAnalytics.SharedKernel.Users;

public interface ICurrentUser
{
    Guid? UserId { get; }
}

public sealed class CurrentUser : ICurrentUser
{
    public Guid? UserId { get; private set; }

    public void Set(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty.", nameof(userId));

        if (UserId is not null && UserId != userId)
            throw new InvalidOperationException("The user for this request is already set.");

        UserId = userId;
    }
}