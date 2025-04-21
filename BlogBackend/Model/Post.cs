using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace BlogBackend.Model
{
    public class Post
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string UserId { get; set; }

        public string UserName { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }

        public byte[] ImageBytes { get; set; }
        public List<string> Tags { get; set; } = new List<string>(); 
        public int Likes { get; set; } = 0; 

        public List<Like> LikedBy { get; set; } = new List<Like>();
        public List<Comment> Comments { get; set; } = new List<Comment>(); 
        public DateTime CreatedAt { get; set; }
    }

    public class Comment
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Text { get; set; }

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; }
    }

    public class Like
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime LikedAt { get; set; }
    }

    public class UpdatePostRequest
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public IFormFile Image { get; set; }
        public List<string> Tags { get; set; }
    }
    public class CreatePostRequest
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public IFormFile Image { get; set; }
        public List<string> Tags { get; set; }

        public List<TextFormat> FormattedContent { get; set; } 
    }

    public class TextFormat
    {
        public string Text { get; set; }
        public bool IsBold { get; set; }
        public string Font { get; set; }
        public string Heading { get; set; } 
        public string Alignment { get; set; }
    }

    
    public class CommentRequest
    {
        public string Text { get; set; }
    }

    public class LikeRequest
    {
        public bool IsLike { get; set; }
    }

}