namespace CalorieTracker.Identity.Application.Abstractions;

public interface IAccessTokenIssuer
{
    AccessToken Issue(UserAccount user);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt)
{
    public override string ToString() => $"{nameof(AccessToken)} {{ ExpiresAt = {ExpiresAt} }}";
}
