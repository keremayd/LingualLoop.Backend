using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Service.DataTransferObjects.Requests.Authentication;

public class RegisterUserWithPhotoRequest
{
    public string UserNickname { get; set; } = string.Empty;
    [Required(ErrorMessage = "Username is required")]
    public string UserName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public ICollection<string> Roles { get; set; }
    public IFormFile? File { get; set; }
}