using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Common.Options;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;

namespace Service.Handlers.Commands;

public class CreateTokenCommandHandler : IRequestHandler<CreateTokenRequest, string>
{
    private readonly UserManager<User> _userManager;
    private readonly JwtOptions? _jwtOptions;

    public CreateTokenCommandHandler(IConfiguration configuration, UserManager<User> userManager)
    {
        var configurationSection = configuration.GetSection("JwtOptions");
        _jwtOptions = configurationSection.Get<JwtOptions>();
        _userManager = userManager;
    }

    private SigningCredentials GetSigninCredentials()
    {
        var key = Encoding.UTF8.GetBytes(_jwtOptions!.SecretKey);
        var secret = new SymmetricSecurityKey(key);

        return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
    }
    
    private async Task<List<Claim>> GetClaims(CreateTokenRequest request)
    {
        User u = new User()
        {
            UserName = request.UserName,
            PasswordHash = request.Password
        };
        
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.Name, request.UserName)
        };

        var roles = await _userManager.GetRolesAsync(u);

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return claims;
    }
    
    private JwtSecurityToken GenerateTokenOptions(SigningCredentials signinCredentials, List<Claim> claims)
    {
        var tokenOptions = new JwtSecurityToken(
            issuer: _jwtOptions!.ValidIssuer,
            audience: _jwtOptions.ValidAudience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(_jwtOptions.Expires)),
            signingCredentials: signinCredentials);

        return tokenOptions;
    }

    public async Task<string> Handle(CreateTokenRequest request, CancellationToken cancellationToken)
    {
        var signinCredentials = GetSigninCredentials();
        var claims = await GetClaims(request);
        var tokenOptions = GenerateTokenOptions(signinCredentials, claims);

        return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
    }
}