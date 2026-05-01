using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace ToolLib.Services;

public class MongoImageService
{
    private readonly IGridFSBucket _gridFs;

    public MongoImageService(IConfiguration configuration)
    {
        var connectionString = configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017";
        var databaseName = configuration["MongoDB:DatabaseName"] ?? "toollib";
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _gridFs = new GridFSBucket(database);
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string filename)
    {
        var id = await _gridFs.UploadFromStreamAsync(filename, imageStream);
        return id.ToString();
    }

    public async Task<byte[]?> GetImageAsync(string imageId)
    {
        try
        {
            var objectId = new ObjectId(imageId);
            return await _gridFs.DownloadAsBytesAsync(objectId);
        }
        catch
        {
            return null;
        }
    }

    public async Task DeleteImageAsync(string imageId)
    {
        try
        {
            var objectId = new ObjectId(imageId);
            await _gridFs.DeleteAsync(objectId);
        }
        catch { }
    }
}
