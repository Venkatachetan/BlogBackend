using MongoDB.Driver;
using BlogBackend.Model;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization.IdGenerators;

namespace BlogBackend.Services
{
    public class MongoDbService
    {
        private readonly IMongoCollection<Post> _postsCollection;

        public MongoDbService(IConfiguration configuration)
        {
            
            if (!BsonClassMap.IsClassMapRegistered(typeof(Post)))
            {
                BsonClassMap.RegisterClassMap<Post>(cm => {
                    cm.AutoMap();
                    cm.MapIdMember(c => c.Id)
                      .SetSerializer(new StringSerializer(BsonType.String))
                      .SetIdGenerator(StringObjectIdGenerator.Instance);
                });
            }

            var client = new MongoClient(configuration["MongoDB:ConnectionString"]);
            var database = client.GetDatabase(configuration["MongoDB:DatabaseName"]);
            _postsCollection = database.GetCollection<Post>("posts");
        }

        public async Task CreatePostAsync(Post post)
        {
            
            if (string.IsNullOrEmpty(post.Id))
            {
                post.Id = Guid.NewGuid().ToString();
            }

            await _postsCollection.InsertOneAsync(post);
        }

        
        public async Task<List<Post>> GetPostsAsync()
        {
            return await _postsCollection
                .Find(_ => true)
                .ToListAsync();
        }

        public async Task<Post> GetPostByIdAsync(string id)
        {
            return await _postsCollection
                .Find(p => p.Id == id)
                .FirstOrDefaultAsync();
        }



        public async Task LikePostAsync(string postId, Like like)
        {
            var filter = Builders<Post>.Filter.Eq(p => p.Id, postId);
            var update = Builders<Post>.Update
                .Inc(p => p.Likes, 1)
                .Push(p => p.LikedBy, like);

            await _postsCollection.UpdateOneAsync(filter, update);
        }

        public async Task UnlikePostAsync(string postId, string userId)
        {
            var filter = Builders<Post>.Filter.Eq(p => p.Id, postId);
            var update = Builders<Post>.Update
                .Inc(p => p.Likes, -1)
                .PullFilter(p => p.LikedBy, l => l.UserId == userId); 

            var result = await _postsCollection.UpdateOneAsync(filter, update);
            if (result.MatchedCount == 0)
                throw new InvalidOperationException("Post not found or user hasn't liked it");
        }

        public async Task AddCommentAsync(string postId, Comment comment)
        {
            var filter = Builders<Post>.Filter.Eq(p => p.Id, postId);
            var update = Builders<Post>.Update.Push(p => p.Comments, comment);
            await _postsCollection.UpdateOneAsync(filter, update);
        }

        public async Task DeleteCommentAsync(string postId, string commentId)
        {
            
            var filter = Builders<Post>.Filter.Eq(p => p.Id, postId);
            var update = Builders<Post>.Update.PullFilter(p => p.Comments, c => c.Id == commentId);
            await _postsCollection.UpdateOneAsync(filter, update);
        }

        public async Task DeletePostAsync(string postId)
        {
            var filter = Builders<Post>.Filter.Eq(p => p.Id, postId);
            await _postsCollection.DeleteOneAsync(filter);
        }

        public async Task UpdatePostAsync(string postId, string title, string content, byte[] imageBytes, List<string> tags)
        {
            var filter = Builders<Post>.Filter.Eq(p => p.Id, postId);
            var update = Builders<Post>.Update
                .Set(p => p.Title, title)
                .Set(p => p.Content, content)
                .Set(p => p.ImageBytes, imageBytes)
                .Set(p => p.Tags, tags ?? new List<string>());

            await _postsCollection.UpdateOneAsync(filter, update);
        }
    }
}