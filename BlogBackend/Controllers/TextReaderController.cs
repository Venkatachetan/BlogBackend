using BlogBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BlogBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TextReaderController : ControllerBase
    {
        private readonly TextReaderService _textReaderService;

        public TextReaderController(TextReaderService textReaderService)
        {
            _textReaderService = textReaderService ?? throw new ArgumentNullException(nameof(textReaderService));
        }

        [HttpGet("read/{postId}")]
        public async Task<IActionResult> ReadPostContent(string postId)
        {
            try
            {
                var audioBytes = await _textReaderService.GenerateAudioFromPostContentAsync(postId);
                return File(audioBytes, "audio/wav", $"post-{postId}-audio.wav");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}