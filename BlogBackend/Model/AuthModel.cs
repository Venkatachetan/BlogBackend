namespace BlogBackend.Model
{
    
    public class MessageResponse
    {
        public string Message { get; set; }
    }

   
    public class LoginResponse
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string AccessToken { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    
    public class AuthCheckResponse
    {
        public string Id { get; set; }
        public string Email { get; set; }
    }


}