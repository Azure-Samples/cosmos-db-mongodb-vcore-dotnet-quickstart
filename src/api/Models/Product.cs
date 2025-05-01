namespace Microsoft.Learn.AzureCosmosDBMongoDBVCoreQuickstart.Api.Models;

internal sealed record Product(
    string id,
    string category,
    string name,
    int quantity,
    decimal price,
    bool clearance
);