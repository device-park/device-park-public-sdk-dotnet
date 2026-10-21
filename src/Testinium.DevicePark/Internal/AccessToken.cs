using System;
using System.Runtime.Serialization;

namespace Testinium.DevicePark.Internal;

[DataContract]
internal sealed class AccessTokenResponse
{
    [DataMember(Name = "access_token")]
    public string? AccessToken { get; private set; }

    [DataMember(Name = "token_type")]
    public string? TokenType { get; private set; }

    [DataMember(Name = "expires_in")]
    public long ExpiresIn { get; private set; }
}

internal sealed class AccessToken
{
    private readonly string _value;
    private readonly string _type;
    private readonly long _expiresInSeconds;
    private readonly DateTimeOffset _issuedAt;

    private AccessToken(string value, string type, long expiresInSeconds, DateTimeOffset issuedAt)
    {
        _value = value;
        _type = type;
        _expiresInSeconds = expiresInSeconds;
        _issuedAt = issuedAt;
    }

    internal static AccessToken FromResponse(AccessTokenResponse response, DateTimeOffset issuedAt) =>
        new AccessToken(
            response.AccessToken ?? string.Empty,
            string.IsNullOrEmpty(response.TokenType) ? "Bearer" : response.TokenType!,
            response.ExpiresIn,
            issuedAt);

    internal string AuthorizationHeader => _type + " " + _value;

    internal bool IsValid(TimeSpan safetyMargin, DateTimeOffset now) =>
        now < _issuedAt.AddSeconds(_expiresInSeconds) - safetyMargin;
}
