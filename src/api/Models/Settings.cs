namespace Microsoft.Learn.AzureCosmosDBMongoDBVCoreQuickstart.Api.Models;

internal sealed record Settings
{
    public required string Endpoint { get; init; }

    public required string TenantId { get; init; }

    public required string DatabaseName { get; init; }

    public required string CollectionName { get; init; }
}