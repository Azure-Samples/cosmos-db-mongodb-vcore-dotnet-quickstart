WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<Settings>(
    builder.Configuration.GetSection(nameof(Settings))
);

builder.Services.AddSingleton<IMongoClient>((serviceProvider) =>
{
    IOptions<Settings> settingsOptions = serviceProvider.GetRequiredService<IOptions<Settings>>();
    Settings settings = settingsOptions.Value;

    DefaultAzureCredential credential = new();

    MongoEntraClientFactory clientFactory = new();

    IMongoClient client = clientFactory.CreateMongoClient(
        settings.Endpoint,
        settings.TenantId,
        credential
    );

    return client;
});

builder.Services.AddTransient<IMongoDatabase>((serviceProvider) =>
{
    IOptions<Settings> settingsOptions = serviceProvider.GetRequiredService<IOptions<Settings>>();
    Settings settings = settingsOptions.Value;

    IMongoClient client = serviceProvider.GetRequiredService<IMongoClient>();

    IMongoDatabase database = client.GetDatabase(settings.DatabaseName);

    return database;
});

builder.Services.AddTransient<IMongoCollection<Product>>((serviceProvider) =>
{
    IOptions<Settings> settingsOptions = serviceProvider.GetRequiredService<IOptions<Settings>>();
    Settings settings = settingsOptions.Value;

    IMongoDatabase database = serviceProvider.GetRequiredService<IMongoDatabase>();

    IMongoCollection<Product> collection = database.GetCollection<Product>(settings.CollectionName);

    return collection;
});

builder.Services.ActivateSingleton<IMongoClient>();

WebApplication app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/status", async (
    [FromServices] IMongoClient client,
    [FromServices] IMongoDatabase database
) =>
{
    // Get the host name from the MongoDB client settings.
    string host = client.Settings.Server.Host;

    // Create a BSON document representing the "ping command.
    // The command doesn't require a specific numeric value for the "ping" property.
    BsonDocument command = new("ping", default(int));

    // Execute the command against the database to check the connection status.
    // Simply executing with no exception means the connection is healthy.
    await database.RunCommandAsync<BsonDocument>(command);

    // Return a status object indicating the host and that the connection is healthy as a (200) OK response.
    return Results.Ok(new Status { Host = host, IsHealthy = true });
});

app.MapGet("/", async (
    [FromServices] IMongoCollection<Product> collection
) =>
{
    // Create an empty filter to retrieve all documents in the collection.
    // Use the Find method to retrieve all documents in the collection that matches the filter.
    // Then, convert the result to a list asynchronously using ToListAsync.
    List<Product> documents = await collection.Find(
        filter: doc => true
    ).ToListAsync();

    // Return the list of documents as a (200) OK response.
    return Results.Ok(documents);
});

app.MapGet("/category/{category}", async (
    [FromServices] IMongoCollection<Product> collection,
    [FromRoute] string category
) =>
{
    // Create a filter to retrieve all documents with the specified category.
    // Use the Find method to retrieve all documents in the collection that matches the filter.
    // Then, convert the result to a list asynchronously using ToListAsync.
    List<Product> documents = await collection.Find(
        filter: doc => doc.category == category
    ).ToListAsync();

    // Return the list of documents as a (200) OK response.
    return Results.Ok(documents);
});

app.MapPost("/", async (
    [FromServices] IMongoCollection<Product> collection,
    [FromBody] Product document
) =>
{
    // Create a filter to check if a document with the same unique identifier (id) already exists in the collection.
    Expression<Func<Product, bool>> filter = doc => doc.id == document.id;

    // Configure the options to upsert the document (insert if it doesn't exist, replace if it does). 
    ReplaceOptions options = new() { IsUpsert = true };

    // Upsert the document into the collection using the ReplaceOneAsync method.
    await collection.ReplaceOneAsync<Product>(filter, document, options);

    // Return a (201) Created response to indicate that the document was successfully upserted.
    return Results.Created();
});

app.MapGet("/{id}", async (
    [FromServices] IMongoCollection<Product> collection,
    [FromRoute] string id
) =>
{
    // Create a filter to retrieve the document with the specified unique identifier (id).
    Expression<Func<Product, bool>> filter = doc => doc.id == id;

    // Use the Find method to retrieve all documents in the collection that matches the filter.
    // Then, convert the result to a single document asynchronously using SingleOrDefaultAsync.
    // If no document is found, the result will be null.
    Product? document = await collection.Find(filter).SingleOrDefaultAsync();

    // Return the matching document as a (200) OK response.
    // If the document is null, return a (404) Not Found response with a message.
    return Results.Ok(document) ?? Results.NotFound(new { message = "Document not found" });
});

app.MapDelete("/{id}", async (
    [FromServices] IMongoCollection<Product> collection,
    [FromRoute] string id
) =>
{
    // Create a filter to retrieve the document with the specified unique identifier (id).
    Expression<Func<Product, bool>> filter = doc => doc.id == id;

    // Use the DeleteOneAsync method to delete the document in the collection that matches the filter.
    await collection.DeleteOneAsync(filter);

    // Return a (204) NoContent response to indicate that the deletion was successful.
    return Results.NoContent();
});

await app.RunAsync();