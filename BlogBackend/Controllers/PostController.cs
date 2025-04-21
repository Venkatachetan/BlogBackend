using Microsoft.AspNetCore.Mvc;
using BlogBackend.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BlogBackend.Model;
using System.IO;
using System.Text.Json;

namespace BlogBackend.Controllers
{
    [ApiController]
    [Route("api/blog")]
    public class BlogController : ControllerBase
    {
        private readonly BlogService _blogService;
        private readonly JwtService _jwtService;

        public BlogController(BlogService blogService, JwtService jwtService)
        {
            _blogService = blogService;
            _jwtService = jwtService;
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.FindFirst("name")?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName))
                {
                    Console.WriteLine("Claims available: ");
                    foreach (var claim in User.Claims)
                    {
                        Console.WriteLine($"{claim.Type}: {claim.Value}");
                    }
                    return Unauthorized(new { Message = "User ID or username is missing from JWT claims." });
                }

                if (string.IsNullOrWhiteSpace(request.Title))
                    return BadRequest(new { Message = "Title is required." });
                if (string.IsNullOrWhiteSpace(request.Content))
                    return BadRequest(new { Message = "Content is required." });

                // Optional: Basic validation to ensure content contains valid HTML
                if (!request.Content.Contains("<") || !request.Content.Contains(">"))
                {
                    // You could enforce stricter HTML validation here if needed
                    // For simplicity, just checking for tags
                    Console.WriteLine("Warning: Content may not contain proper HTML formatting.");
                }

                byte[] imageBytes = null;
                if (request.Image != null && request.Image.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await request.Image.CopyToAsync(memoryStream);
                        imageBytes = memoryStream.ToArray();
                    }
                }

                var post = await _blogService.CreatePostAsync(
                    userId,
                    userName,
                    request.Title,
                    request.Content, // Pass HTML-formatted content
                    imageBytes,
                    request.Tags
                );

                var response = new
                {
                    Post = post,
                    ImageBase64 = imageBytes != null ? Convert.ToBase64String(imageBytes) : null
                };

                return CreatedAtAction(nameof(GetUserPosts), new { userId }, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error creating post: {ex.Message}" });
            }
        }
        [HttpGet("all")]
        public async Task<IActionResult> GetAllPosts()
        {
            var posts = await _blogService.GetAllPostsAsync();
            // Optionally convert ImageBytes to Base64 for all posts if needed
            var response = posts.Select(post => new
            {
                Post = post,
                ImageBase64 = post.ImageBytes != null ? Convert.ToBase64String(post.ImageBytes) : null
            });
            return Ok(response);
        }

        [HttpGet("{postId}")]
        public async Task<IActionResult> GetPost(string postId)
        {
            try
            {
                var post = await _blogService.GetPostByIdAsync(postId);
                if (post == null)
                    throw new InvalidOperationException("Post not found");

                var response = new
                {
                    Post = post,
                    ImageBase64 = post.ImageBytes != null ? Convert.ToBase64String(post.ImageBytes) : null
                };

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserPosts(string userId)
        {
            try
            {
                var posts = await _blogService.GetUserPostsAsync(userId);
                var response = posts.Select(post => new
                {
                    Post = post,
                    ImageBase64 = post.ImageBytes != null ? Convert.ToBase64String(post.ImageBytes) : null
                });
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("like/{postId}")]
        [Authorize]
        public async Task<IActionResult> LikePost(string postId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.FindFirst("name")?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName))
                    return Unauthorized(new { Message = "User ID or username is missing from JWT claims." });

                await _blogService.LikePostAsync(postId, userId, userName);

                var updatedPost = await _blogService.GetPostByIdAsync(postId);

                return Ok(new
                {
                    Message = "Post liked successfully",
                    LikedBy = userName,
                    TotalLikes = updatedPost.Likes,
                    Likers = updatedPost.LikedBy.Select(l => new { l.UserId, l.UserName, l.LikedAt })
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("unlike/{postId}")]
        [Authorize]
        public async Task<IActionResult> UnlikePost(string postId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "User ID is missing from JWT claims." });

                await _blogService.UnlikePostAsync(postId, userId);
                var updatedPost = await _blogService.GetPostByIdAsync(postId);

                return Ok(new
                {
                    Message = "Post unliked successfully",
                    TotalLikes = updatedPost.Likes
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("comment/{postId}")]
        [Authorize]
        public async Task<IActionResult> AddComment(string postId, [FromBody] CommentRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.FindFirst("name")?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName))
                    return Unauthorized(new { Message = "User ID or username is missing from JWT claims." });

                if (string.IsNullOrWhiteSpace(request.Text))
                    return BadRequest(new { Message = "Comment text is required." });

                var post = await _blogService.GetPostByIdAsync(postId);
                if (post == null)
                    throw new InvalidOperationException("Post not found");

                var comment = new Comment
                {
                    UserId = userId,
                    UserName = userName,
                    Text = request.Text,
                    CreatedAt = DateTime.UtcNow
                };

                await _blogService.AddCommentAsync(postId, comment);
                return Ok(new { Message = "Comment added successfully", Commenter = userName, CommentText = request.Text });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [HttpDelete("{postId}/comment/{commentId}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(string postId, string commentId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "User ID is missing from JWT claims." });

                await _blogService.DeleteCommentAsync(postId, commentId, userId);
                return Ok(new { Message = "Comment deleted successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [HttpDelete("{postId}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(string postId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "User ID is missing from JWT claims." });

                await _blogService.DeletePostAsync(postId, userId);
                return Ok(new { Message = "Post deleted successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [HttpPut("{postId}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost(string postId, [FromForm] UpdatePostRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.FindFirst("name")?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName))
                    return Unauthorized(new { Message = "User ID or username is missing from JWT claims." });

                if (string.IsNullOrWhiteSpace(request.Title))
                    return BadRequest(new { Message = "Title is required." });
                if (string.IsNullOrWhiteSpace(request.Content))
                    return BadRequest(new { Message = "Content is required." });

               
                if (!request.Content.Contains("<") || !request.Content.Contains(">"))
                {
                    
                    Console.WriteLine("Warning: Content may not contain proper HTML formatting.");
                }

                byte[] imageBytes = null;
                if (request.Image != null && request.Image.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await request.Image.CopyToAsync(memoryStream);
                        imageBytes = memoryStream.ToArray();
                    }
                }

                var updatedPost = await _blogService.UpdatePostAsync(
                    postId,
                    userId,
                    userName,
                    request.Title,
                    request.Content, 
                    imageBytes,
                    request.Tags
                );

                var response = new
                {
                    Post = updatedPost,
                    ImageBase64 = imageBytes != null ? Convert.ToBase64String(imageBytes) : null
                };

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }
    }

    // Request models (unchanged from previous versions)
    public class CreatePostRequest
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public IFormFile Image { get; set; }
        public List<string> Tags { get; set; }
    }

    public class UpdatePostRequest
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public IFormFile Image { get; set; }
        public List<string> Tags { get; set; }
    }

    public class CommentRequest
    {
        public string Text { get; set; }
    }
}