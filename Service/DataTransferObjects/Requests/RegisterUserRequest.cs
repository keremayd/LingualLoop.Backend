using System.ComponentModel.DataAnnotations;
using MediatR;
using Postgres.Models;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class RegisterUserRequest: IRequest<RegisterUserResponse>
{
    public string UserNickname { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Username is required")]
    public string UserName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public ICollection<string> Roles { get; set; }
}