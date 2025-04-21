using Microsoft.AspNetCore.Mvc;
using BlogBackend.Services;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace BlogBackend.Controllers
{
    [ApiController]
    [Route("api/ai-content")]
    public class AIContentController : ControllerBase
    {
        private readonly AIContentService _aiContentService;

        public AIContentController(AIContentService aiContentService)
        {
            _aiContentService = aiContentService;
        }

        [HttpPost("generate")]
        [Authorize] 
        public async Task<IActionResult> GenerateContent([FromBody] GenerateContentRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Title))
                    return BadRequest(new { Message = "Title is required." });

                var generatedContent = await _aiContentService.GenerateContentAsync(request.Title);

                return Ok(new
                {
                    Title = request.Title,
                    GeneratedContent = generatedContent
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, new { Message = $"Error generating content: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Unexpected error: {ex.Message}" });
            }
        }
    }

    public class GenerateContentRequest
    {
        public string Title { get; set; }
    }
}