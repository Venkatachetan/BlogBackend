using BlogBackend.Model;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BlogBackend.Services
{
    public class TextReaderService
    {
        private readonly BlogService _blogService;

        public TextReaderService(BlogService blogService)
        {
            _blogService = blogService ?? throw new ArgumentNullException(nameof(blogService));
        }

        public async Task<byte[]> GenerateAudioFromPostContentAsync(string postId)
        {
            if (string.IsNullOrWhiteSpace(postId))
                throw new ArgumentException("Post ID is required", nameof(postId));

            var post = await _blogService.GetPostByIdAsync(postId);
            if (post == null)
                throw new InvalidOperationException("Post not found");

            if (string.IsNullOrWhiteSpace(post.Content))
                throw new InvalidOperationException("Post content is empty and cannot be read");

            try
            {
                using var synth = new System.Speech.Synthesis.SpeechSynthesizer(); // Fully qualified
                using var stream = new MemoryStream();

                synth.SetOutputToWaveStream(stream);
                synth.SelectVoiceByHints(System.Speech.Synthesis.VoiceGender.Female, System.Speech.Synthesis.VoiceAge.Adult); // Fully qualified enums
                synth.Volume = 100;
                synth.Rate = 0;

                synth.Speak(post.Content);

                stream.Position = 0;
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate audio: {ex.Message}", ex);
            }
        }
    }
}