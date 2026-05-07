namespace Microsoft.Learn.AzureCosmosDBMongoDBVCoreQuickstart.Api.Handlers;

/// <summary>
/// Gets an OIDC access token using an <see cref="TokenCredential"/> from the <see cref="Azure.Identity"/> library.
/// </summary>
/// <param name="credential" example="new DefaultAzureCredential()">
/// The <see cref="TokenCredential"/> to use for authentication. This is typically an instance of <see cref="DefaultAzureCredential"/> or a similar credential from the <see cref="Azure.Identity"/> library.
/// </param>
/// <param name="tenantId">
/// The Microsoft Entra tenant ID to use for authentication. This is the ID of the Microosft Entra tenant that contains the principal that will be used to authenticate.
/// </param>
/// <param name="scopes">
/// The scopes to request when acquiring the token. This should typically be set to the fixed scope for the Azure DocumentDB (with MongoDB compatibility) service, which is "https://ossrdbms-aad.database.windows.net/.default".
/// </param>
/// <remarks>
/// This class implements the <see cref="IOidcCallback"/> interface, which is used by the <see cref="MongoDB.Driver"/> to acquire OIDC tokens for authentication.
/// This handler is intended to be used with the <see cref="MongoEntraClientFactory"/> class, which creates MongoDB clients with Microsoft Entra authentication support.
/// </remarks>
internal sealed class AzureIdentityTokenHandler(
    TokenCredential credential,
    string tenantId,
    string[] scopes
) : IOidcCallback
{
    /// <inheritdoc />
    public OidcAccessToken GetOidcAccessToken(OidcCallbackParameters parameters, CancellationToken cancellationToken)
    {
        // The OIDC token is acquired using the provided TokenCredential and the specified scopes.
        AccessToken token = credential.GetToken(
            new TokenRequestContext(scopes, tenantId: tenantId),
            cancellationToken
        );

        // The token is returned as an OidcAccessToken object, which contains the token string and its expiration time.
        return new OidcAccessToken(token.Token, token.ExpiresOn - DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public async Task<OidcAccessToken> GetOidcAccessTokenAsync(OidcCallbackParameters parameters, CancellationToken cancellationToken)
    {
        // The OIDC token is acquired asynchronously using the provided TokenCredential and the specified scopes.
        AccessToken token = await credential.GetTokenAsync(
            new TokenRequestContext(scopes, parentRequestId: null, tenantId: tenantId),
            cancellationToken
        );

        // The token is returned as an OidcAccessToken object, which contains the token string and its expiration time.
        return new OidcAccessToken(token.Token, token.ExpiresOn - DateTimeOffset.UtcNow);
    }
}