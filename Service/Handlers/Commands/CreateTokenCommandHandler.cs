using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Common.Options;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;

namespace Service.Handlers.Commands;

public class CreateTokenCommandHandler : IRequestHandler<CreateTokenRequest, CreateTokenResponse>
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
            UserName = request.Id,
            PasswordHash = request.Password
        };
        
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.Name, request.Id)
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

    public static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

    public async Task<CreateTokenResponse> Handle(CreateTokenRequest request, CancellationToken cancellationToken)
    {
        var signinCredentials = GetSigninCredentials();
        var claims = await GetClaims(request);
        var tokenOptions = GenerateTokenOptions(signinCredentials, claims);

        User user = await _userManager.FindByIdAsync(request.Id);

        if (request.PopulateExp)
        {
            user.RefreshToken = GenerateRefreshToken();
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        }

        await _userManager.UpdateAsync(user);
        
        var accessToken = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

        return new CreateTokenResponse()
        {
            AccessToken = accessToken,
            RefreshToken = user.RefreshToken
        };
    }
}