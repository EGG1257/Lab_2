
using BookStoreApi.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BookStoreApi.Services;

public class BooksService
{
    private readonly IMongoCollection<Book> _booksCollection;

    public BooksService(IOptions<BookStoreDatabaseSettings> settings)
    {
        var mongoClient = new MongoClient(settings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            settings.Value.DatabaseName);

        _booksCollection = mongoDatabase.GetCollection<Book>(
            settings.Value.BooksCollectionName);
    }

    // Get all books
    public async Task<List<Book>> GetAsync() =>
        await _booksCollection.Find(_ => true).ToListAsync();

    // Get one book by ID
    public async Task<Book?> GetAsync(string id) =>
        await _booksCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    // Add one book
    public async Task CreateAsync(Book newBook) =>
        await _booksCollection.InsertOneAsync(newBook);

    // Update an entire book
    public async Task UpdateAsync(string id, Book updatedBook) =>
        await _booksCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);

    // Delete a book
    public async Task RemoveAsync(string id) =>
        await _booksCollection.DeleteOneAsync(x => x.Id == id);


    // TASK 1: Validate uploaded books
    public List<string> ValidateBooks(List<Book> books)
    {
        var errors = new List<string>();

        for (int i = 0; i < books.Count; i++)
        {
            var book = books[i];

            if (string.IsNullOrWhiteSpace(book.Name))
                errors.Add($"Book {i + 1}: Name is required.");

            if (string.IsNullOrWhiteSpace(book.Category))
                errors.Add($"Book {i + 1}: Category is required.");

            if (string.IsNullOrWhiteSpace(book.Author))
                errors.Add($"Book {i + 1}: Author is required.");

            if (book.Price <= 0)
                errors.Add($"Book {i + 1}: Price must be above 0.");
        }

        return errors;
    }

    // TASK 1: Insert multiple books
    public async Task<List<Book>> CreateManyAsync(List<Book> books)
    {
        await _booksCollection.InsertManyAsync(books);
        return books;
    }

    // TASK 2: Search books
    public async Task<List<Book>> SearchAsync(
        string? category,
        decimal? minPrice,
        decimal? maxPrice)
    {
        var filter = Builders<Book>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(category))
            filter &= Builders<Book>.Filter.Eq(x => x.Category, category);

        if (minPrice.HasValue)
            filter &= Builders<Book>.Filter.Gte(x => x.Price, minPrice.Value);

        if (maxPrice.HasValue)
            filter &= Builders<Book>.Filter.Lte(x => x.Price, maxPrice.Value);

        return await _booksCollection.Find(filter).ToListAsync();
    }

    // TASK 3: Get the most expensive books
    public async Task<List<Book>> GetTopAsync(int count)
    {
        return await _booksCollection
            .Find(_ => true)
            .SortByDescending(x => x.Price)
            .Limit(count)
            .Project(x => new Book
            {
                Name = x.Name,
                Price = x.Price
            })
            .ToListAsync();
    }

    // TASK 4: Update only a book's price
    public async Task<bool> UpdatePriceAsync(string id, decimal price)
    {
        var update = Builders<Book>.Update.Set(x => x.Price, price);

        var result = await _booksCollection.UpdateOneAsync(
            x => x.Id == id,
            update);

        return result.MatchedCount > 0;
    }
}
