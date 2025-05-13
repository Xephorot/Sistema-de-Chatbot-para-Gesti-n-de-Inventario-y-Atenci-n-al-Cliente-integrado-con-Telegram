using chatbot_api.Configurations;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace chatbot_api.Services
{
    public class MongoService
    {
        public IMongoDatabase Database { get; }

        public MongoService(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            Database = client.GetDatabase(settings.Value.DatabaseName);
        }

        public IMongoCollection<T> GetCollection<T>(string name) => Database.GetCollection<T>(name);
    }
}
