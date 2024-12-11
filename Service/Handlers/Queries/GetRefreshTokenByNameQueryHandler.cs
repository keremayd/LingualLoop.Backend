using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using Common.Options;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Postgres.Models;
using Service.DataTransferObjects.Requests;

namespace Service.Handlers.Queries;

public class GetRefreshTokenByNameQueryHandler : IRequestHandler<RefreshTokenRequest, User>
{
    private readonly JwtOptions? _jwtOptions;
    private readonly UserManager<User> _userManager;

    public GetRefreshTokenByNameQueryHandler(IConfiguration configuration,UserManager<User> userManager)
    {
        var configurationSection = configuration.GetSection("JwtOptions");
        _jwtOptions = configurationSection.Get<JwtOptions>();
        _userManager = userManager;
    }
    
    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var secretKey = _jwtOptions!.SecretKey;
        var tokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = false, //TODO true olacak prod çıkışında
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtOptions.ValidIssuer,
            ValidAudience = _jwtOptions.ValidAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken securityToken;

        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);

        var jwtSecurityToken = securityToken as JwtSecurityToken;
        if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token.");

        return principal;
    }
    
    public async Task<User> Handle(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var principal = GetPrincipalFromExpiredToken(request.AccessToken);
        var user = await _userManager.FindByIdAsync(principal.Identity.Name);

        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
            throw new LingualLoopException(ErrorCode.InvalidOrExpiredRefreshToken.CreateMessage(request.RefreshToken),
                ErrorCode.InvalidOrExpiredRefreshToken.GetDescription(request.RefreshToken), HttpStatusCode.Forbidden);

        return user;
    }
}