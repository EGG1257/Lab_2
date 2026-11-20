using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BookStoreApi.Models;

public class Book
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string? Name { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Price { get; set; }

    public string? Category { get; set; }

    public string? Author { get; set; }
}
