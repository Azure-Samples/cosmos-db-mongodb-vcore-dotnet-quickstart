namespace Microsoft.Learn.AzureCosmosDBMongoDBVCoreQuickstart.Api.Factories;

/// <summary>
/// Factory for creating MongoDB clients with Microsoft Entra authentication support using the best-practices from Azure Cosmos DB for MongoDB vCore.
/// </summary>
internal sealed class MongoEntraClientFactory
{
    /// <summary>
    /// This includes the fixed scope for the Azure Cosmos DB for MongoDB vCore service, which is "https://ossrdbms-aad.database.windows.net/.default".
    /// </summary>
    private string[] Scopes { get; } = ["https://ossrdbms-aad.database.windows.net/.default"];

    /// <summary>
    /// Creates a <see cref="IMongoClient"/> client instance with Microsoft Entra authentication support using the provided <see cref="TokenCredential"/>.
    /// </summary>
    /// <param name="endpoint" example="&lt;account-name&gt;.global.mongocluster.cosmos.azure.com">
    /// The endpoint of the Azure Cosmos DB for MongoDB vCore service. This is tpyically in the format "&lt;account-name&gt;.global.mongocluster.cosmos.azure.com".
    /// </param>
    /// <param name="tenantId">
    /// The Microsoft Entra tenant ID to use for authentication. This is the ID of the Microosft Entra tenant that contains the principal that will be used to authenticate.
    /// </param>
    /// <param name="tokenCredential" example="new DefaultAzureCredential()">
    /// The optional <see cref="TokenCredential"/> to use for authentication. This is typically an instance of <see cref="DefaultAzureCredential"/> or a similar credential from the <see cref="Azure.Identity"/> library.
    /// If not provided, an <see cref="DefaultAzureCredential"/> instance will be used.
    /// </param>
    /// <param name="configureSettings">
    /// An optional action to configure the <see cref="MongoClientSettings"/> before creating the client. This can be used to set additional options or modify the default settings.
    /// </param>
    /// <remarks>
    /// The typical credential format for Azure Cosmos DB for MongoDB vCore is "mongodb+srv://&lt;account-name&gt;.global.mongocluster.cosmos.azure.com/?tls=true&amp;authMechanism=MONGODB-OIDC&amp;retrywrites=false&amp;maxIdleTimeMS=120000".
    /// The <see cref="MongoClientSettings"/> are configured with the recommended settings for Azure Cosmos DB for MongoDB vCore, including TLS, retry writes, and connection idle time from this crdential.
    /// </remarks>
    public IMongoClient CreateMongoClient(
        string endpoint,
        string tenantId,
        TokenCredential? tokenCredential = null,
        Action<MongoClientSettings>? configureSettings = null)
    {
        // Creates a new instance of the DefaultAzureCredential class if the credential is not provided.
        tokenCredential ??= new DefaultAzureCredential();

        // Generates the baseline MongoDB connection URL using the specified endpoint.
        MongoUrl url = MongoUrl.Create($"mongodb+srv://{endpoint}/");

        // Creates a new instance of the MongoClientSettings class using the generated URL.
        MongoClientSettings settings = MongoClientSettings.FromUrl(url);

        // Sets the baseline settings for the MongoDB client as recommended by the Azure Cosmos DB for MongoDB vCore product team.
        settings.UseTls = true;
        settings.RetryWrites = false;
        settings.MaxConnectionIdleTime = TimeSpan.FromMinutes(2);

        // Creates a new instance of the AzureIdentityTokenHandler class, which is used to acquire OIDC tokens for authentication.
        AzureIdentityTokenHandler tokenHandler = new(tokenCredential, tenantId, Scopes);

        // Configures the MongoDB client to use OIDC authentication with the provided token handler.
        settings.Credential = MongoCredential.CreateOidcCredential(tokenHandler);

        // Configures any additional or non-default settings using the action if its provided.
        configureSettings?.Invoke(settings);

        // Finalizes the settings to ensure they are immutable and cannot be changed after this point.
        settings.Freeze();

        // Creates and returns a new instance of the MongoClient class using the configured settings.
        // This class implements the IMongoClient interface and provides access to the MongoDB service.
        return new MongoClient(settings);
    }
}