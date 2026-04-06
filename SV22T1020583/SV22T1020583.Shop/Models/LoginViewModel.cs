namespace SV22T1020583.Shop.Models
{
    public class LoginViewModel
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string ReturnUrl { get; set; } = "/";
    }
}