using MongoDB.Driver;
using BlogBackend.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlogBackend.Services
{
    public class BlogService
    {
        private readonly MongoDbService _mongoDbService;

        public BlogService(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        // blogservice.cs
        public async Task<Post> CreatePostAsync(string userId, string userName, string title, string content, byte[] imageBytes, List<string> tags)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required", nameof(userId));
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException("Username is required", nameof(userName));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title is required", nameof(title));
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content is required", nameof(content));

            var post = new Post
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                UserName = userName,
                Title = title,
                Content = content,
                ImageBytes = imageBytes, 
                Tags = tags ?? new List<string>(),
                Likes = 0,
                Comments = new List<Comment>(),
                CreatedAt = DateTime.UtcNow
            };

            await _mongoDbService.CreatePostAsync(post);
            return post;
        }

        public async Task<List<Post>> GetAllPostsAsync()
        {
            return await _mongoDbService.GetPostsAsync();
        }

        public async Task<List<Post>> GetUserPostsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required", nameof(userId));

            var allPosts = await _mongoDbService.GetPostsAsync();
            return allPosts.FindAll(p => p.UserId == userId);
        }



        public async Task<Post> GetPostByIdAsync(string postId)
        {
            if (string.IsNullOrWhiteSpace(postId))
                throw new ArgumentException("Post ID is required", nameof(postId));

            return await _mongoDbService.GetPostByIdAsync(postId);
        }

        public async Task LikePostAsync(string postId, string userId, string userName)
        {
            if (string.IsNullOrWhiteSpace(postId))
                throw new ArgumentException("Post ID is required", nameof(postId));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required", nameof(userId));
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException("User name is required", nameof(userName));

            var post = await _mongoDbService.GetPostByIdAsync(postId);
            if (post == null)
                throw new InvalidOperationException("Post not found");

            
            if (post.LikedBy.Any(l => l.UserId == userId))
                throw new InvalidOperationException("User has already liked this post");

            var like = new Like
            {
                UserId = userId,
                UserName = userName,
                LikedAt = DateTime.UtcNow
            };

            await _mongoDbService.LikePostAsync(postId, like);
        }

        public async Task UnlikePostAsync(string postId, string userId)
        {
            var post = await _mongoDbService.GetPostByIdAsync(postId);
            if (post == null)
                throw new InvalidOperationException("Post not found");

            if (!post.LikedBy.Any(l => l.UserId == userId))
                throw new InvalidOperationException("User hasn't liked this post");

            await _mongoDbService.UnlikePostAsync(postId, userId);
        }

        public async Task AddCommentAsync(string postId, Comment comment)
        {
            if (string.IsNullOrWhiteSpace(postId))
                throw new ArgumentException("Post ID is required", nameof(postId));
            if (comment == null)
                throw new ArgumentNullException(nameof(comment), "Comment cannot be null");
            if (string.IsNullOrWhiteSpace(comment.Text))
                throw new ArgumentException("Comment text is required", nameof(comment.Text));

            var post = await _mongoDbService.GetPostByIdAsync(postId);
            if (post == null)
                throw new InvalidOperationException("Post not found");

            await _mongoDbService.AddCommentAsync(postId, comment);
        }

        public async Task DeleteCommentAsync(string postId, string commentId, string userId)
        {
            if (string.IsNullOrWhiteSpace(postId))
                throw new ArgumentException("Post ID is required", nameof(postId));
            if (string.IsNullOrWhiteSpace(commentId))
                throw new ArgumentException("Comment ID is required", nameof(commentId));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required", nameof(userId));

            var post = await _mongoDbService.GetPostByIdAsync(postId);
            if (post == null)
                throw new InvalidOperationException("Post not found");

            var comment = post.Comments.FirstOrDefault(c => c.Id == commentId);
            if (comment == null)
                throw new InvalidOperationException("Comment not found");

            if (comment.UserId != userId)
                throw new UnauthorizedAccessException("You can only delete your own comments");

            await _mongoDbService.DeleteCommentAsync(postId, commentId);
        }

        public async Task DeletePostAsync(string postId, string userId)
        {
            if (string.IsNullOrWhiteSpace(postId))
                throw new ArgumentException("Post ID is required", nameof(postId));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required", nameof(userId));

            var post = await _mongoDbService.GetPostByIdAsync(postId);
            if (post == null)
                throw new InvalidOperationException("Post not found");
            if (post.UserId != userId)
                throw new UnauthorizedAccessException("You can only delete your own posts");

            await _mongoDbService.DeletePostAsync(postId);
        }

        
        public async Task<Post> UpdatePostAsync(string postId, string userId, string userName,
        string title, string content, byte[] imageBytes, List<string> tags)
        {
            if (string.IsNullOrWhiteSpace(postId))
                throw new ArgumentException("Post ID is required", nameof(postId));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required", nameof(userId));
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException("User name is required", nameof(userName));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title is required", nameof(title));
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content is required", nameof(content));

            
            if (!content.Contains("<") || !content.Contains(">"))
            {
               
                Console.WriteLine("Warning: Content may not contain proper HTML formatting.");
            }

            var post = await _mongoDbService.GetPostByIdAsync(postId);
            if (post == null)
                throw new InvalidOperationException("Post not found");
            if (post.UserId != userId)
                throw new UnauthorizedAccessException("You can only update your own posts");

            
            await _mongoDbService.UpdatePostAsync(postId, title, content, imageBytes, tags);
            return await _mongoDbService.GetPostByIdAsync(postId);
        }

    }
}