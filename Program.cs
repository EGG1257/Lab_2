
using BookStoreApi.Models;
using BookStoreApi.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BookStoreDatabaseSettings>(
    builder.Configuration.GetSection("BookStoreDatabase"));

builder.Services.AddSingleton<BooksService>();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(
    options =>
    {
        options.SerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "BookStore API");
    });
}

var books = app.MapGroup("/books").WithTags("Books");

books.MapGet("/", async (BooksService service) =>
    await service.GetAsync());

books.MapGet("/{id:length(24)}", async (string id, BooksService service) =>
    await service.GetAsync(id) is Book book
        ? Results.Ok(book)
        : Results.NotFound());

books.MapPost("/", async (Book newBook, BooksService service) =>
{
    await service.CreateAsync(newBook);
    return Results.Created($"/books/{newBook.Id}", newBook);
});

books.MapPut("/{id:length(24)}", async (
    string id, Book updatedBook, BooksService service) =>
{
    var book = await service.GetAsync(id);

    if (book is null)
        return Results.NotFound();

    updatedBook.Id = book.Id;

    await service.UpdateAsync(id, updatedBook);

    return Results.NoContent();
});

books.MapDelete("/{id:length(24)}", async (
    string id, BooksService service) =>
{
    var book = await service.GetAsync(id);

    if (book is null)
        return Results.NotFound();

    await service.RemoveAsync(id);

    return Results.NoContent();
});

books.MapPost("/upload", async (
    IFormFile? file, BooksService service) =>
{
    if (file is null || file.Length == 0)
        return Results.BadRequest(new { message = "Please upload a file." });

    if (!Path.GetExtension(file.FileName)
        .Equals(".json", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new
        {
            message = "Only .json files are accepted."
        });
    }

    List<Book>? uploadedBooks;

    try
    {
        using var stream = file.OpenReadStream();

        uploadedBooks = await JsonSerializer.DeserializeAsync<List<Book>>(
            stream,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
    }
    catch (JsonException)
    {
        return Results.BadRequest(new
        {
            message = "The file contains invalid JSON."
        });
    }

    if (uploadedBooks is null || uploadedBooks.Count == 0)
    {
        return Results.BadRequest(new
        {
            message = "The JSON file must contain an array of books."
        });
    }

    var errors = service.ValidateBooks(uploadedBooks);

    if (errors.Count > 0)
    {
        return Results.BadRequest(new
        {
            message = "Some books are invalid. Nothing was inserted.",
            invalidBooks = errors
        });
    }

    var insertedBooks = await service.CreateManyAsync(uploadedBooks);

    return Results.Ok(new
    {
        insertedCount = insertedBooks.Count,
        ids = insertedBooks.Select(book => book.Id)
    });
})
.Accepts<IFormFile>("multipart/form-data")
.DisableAntiforgery();

books.MapGet("/search", async (
    string? category,
    decimal? minPrice,
    decimal? maxPrice,
    BooksService service) =>
{
    var results = await service.SearchAsync(
        category, minPrice, maxPrice);

    return Results.Ok(results);
});

books.MapGet("/top", async (
    int? count,
    BooksService service) =>
{
    int requestedCount = count ?? 3;

    if (requestedCount <= 0)
    {
        return Results.BadRequest(new
        {
            message = "Count must be greater than 0."
        });
    }

    var results = await service.GetTopAsync(requestedCount);

    return Results.Ok(results);
});

books.MapPatch("/{id:length(24)}/price", async (
    string id,
    PriceUpdate request,
    BooksService service) =>
{
    if (request.Price <= 0)
        return Results.BadRequest(new { message = "Price must be above 0." });

    bool updated = await service.UpdatePriceAsync(id, request.Price);

    if (!updated)
        return Results.NotFound();

    return Results.NoContent();
});

app.Run();

public record PriceUpdate(decimal Price);
