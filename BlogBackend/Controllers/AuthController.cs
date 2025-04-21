using Microsoft.AspNetCore.Mvc;
using Supabase;
using System.Security.Claims;
using System.Text.Json;
using Supabase.Gotrue;
using System.Text;
using BlogBackend.Model;
using BlogBackend.Services;


namespace BlogBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;
        private readonly Supabase.Client _supabaseClient;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            JwtService jwtService,
            Supabase.Client supabaseClient,
            ILogger<AuthController> logger)
        {
            _jwtService = jwtService;
            _supabaseClient = supabaseClient;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromQuery] string email, [FromQuery] string password)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("Login attempt with missing email or password");
                    return BadRequest(new MessageResponse { Message = "Email and password are required" });
                }

                var response = await _supabaseClient.Auth.SignIn(email, password);
                if (response?.User == null)
                {
                    _logger.LogWarning($"Login failed for email: {email} - Invalid credentials");
                    return Unauthorized(new MessageResponse { Message = "Invalid credentials" });
                }

                // Extract name from UserMetadata, with fallback
                string name = response.User.UserMetadata?.GetValueOrDefault("name")?.ToString() ??
                              response.User.UserMetadata?.GetValueOrDefault("display_name")?.ToString() ?? "";
                _logger.LogInformation($"Extracted name from metadata: {name}");

                // Pass the full metadata to the token if desired
                var metadata = response.User.UserMetadata ?? new Dictionary<string, object>();
                var accessToken = _jwtService.GenerateToken(response.User.Id, response.User.Email, name, metadata);

                _logger.LogInformation($"Login successful for email: {email}");
                return Ok(new LoginResponse
                {
                    Id = response.User.Id,
                    Email = response.User.Email,
                    AccessToken = accessToken,
                    Metadata = metadata
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during login for email: {email}");
                return StatusCode(500, new MessageResponse { Message = $"Login failed: {ex.Message}" });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromQuery] string email, [FromQuery] string password, [FromQuery] string name)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("Register attempt with missing email or password");
                    return BadRequest(new MessageResponse { Message = "Email and password are required" });
                }

                var options = new SignUpOptions
                {
                    Data = new Dictionary<string, object>
            {
                { "name", name ?? "" },
                { "display_name", name ?? "" }
            }
                };

                var signUpResponse = await _supabaseClient.Auth.SignUp(email, password, options);
                if (signUpResponse?.User == null)
                {
                    _logger.LogWarning($"Registration failed for email: {email}");
                    return BadRequest(new MessageResponse { Message = "Unable to register user" });
                }

                _logger.LogInformation($"User registered successfully: {email}");
                return Ok(new MessageResponse { Message = "User registered successfully. Please log in." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during registration for email: {email}");
                return StatusCode(500, new MessageResponse { Message = $"Registration failed: {ex.Message}" });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _supabaseClient.Auth.SignOut();
                _logger.LogInformation("User logged out successfully");
                return Ok(new MessageResponse { Message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new MessageResponse { Message = $"Logout failed: {ex.Message}" });
            }
        }

        [HttpGet("check")]
        public IActionResult CheckAuthentication([FromQuery] string accessToken)
        {
            try
            {
                _logger.LogInformation($"Checking token: {accessToken ?? "null"}");
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("No access token provided");
                    return Unauthorized(new MessageResponse { Message = "No access token provided" });
                }

                var principal = _jwtService.ValidateToken(accessToken);
                if (principal == null)
                {
                    _logger.LogWarning("Invalid or expired token");
                    return Unauthorized(new MessageResponse { Message = "Invalid or expired token" });
                }

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                _logger.LogInformation($"Token validated for user: {email}");
                return Ok(new AuthCheckResponse { Id = userId, Email = email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking authentication");
                return StatusCode(500, new MessageResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Forgot password attempt with missing email");
                    return BadRequest(new MessageResponse { Message = "Email is required" });
                }

                await _supabaseClient.Auth.ResetPasswordForEmail(email);

                _logger.LogInformation($"Password reset email sent successfully for: {email}");
                return Ok(new MessageResponse
                {
                    Message = "Password reset email sent. Please check your inbox."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during forgot password for email: {email}");
                return StatusCode(500, new MessageResponse
                {
                    Message = $"Password reset request failed: {ex.Message}"
                });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
            [FromQuery] string accessToken,
            [FromQuery] string newPassword,
            [FromQuery] string confirmPassword,
            [FromServices] IHttpClientFactory httpClientFactory)
        {
            try
            {
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("Reset password attempt with missing token");
                    return BadRequest(new MessageResponse { Message = "Reset token is required" });
                }

                if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
                {
                    _logger.LogWarning("Reset password attempt with missing password fields");
                    return BadRequest(new MessageResponse { Message = "New password and confirmation are required" });
                }

                if (newPassword != confirmPassword)
                {
                    _logger.LogWarning("Reset password attempt with mismatched passwords");
                    return BadRequest(new MessageResponse { Message = "Passwords do not match" });
                }

                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri("https://oofxnanzbztrizraqmss.supabase.co");
                httpClient.DefaultRequestHeaders.Add("apikey", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im9vZnhuYW56Ynp0cml6cmFxbXNzIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDI4MTA4NDksImV4cCI6MjA1ODM4Njg0OX0.xUWwGrbCIHgNDY1la3fqCc6ZR7OR6QYiJTFlqRT4834"); // Your API key
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var updateBody = new { password = newPassword };
                var updateContent = new StringContent(JsonSerializer.Serialize(updateBody), Encoding.UTF8, "application/json");
                var updateResponse = await httpClient.PutAsync("/auth/v1/user", updateContent);
                if (!updateResponse.IsSuccessStatusCode)
                {
                    var errorContent = await updateResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Password update failed: {errorContent}");
                    return BadRequest(new MessageResponse { Message = "Failed to reset password." });
                }

                _logger.LogInformation($"Password reset successfully for user");
                return Ok(new MessageResponse { Message = "Password reset successfully. Please log in with your new password." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                return StatusCode(500, new MessageResponse
                {
                    Message = $"Password reset failed: {ex.Message}"
                });
            }
        }
    }
}