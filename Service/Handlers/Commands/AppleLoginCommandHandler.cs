using System.Net;
using Common.Enums;
using Common.Errors;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Requests.Authentication;
using Service.DataTransferObjects.Responses;
using Service.Helpers;

namespace Service.Handlers.Commands;

/// <summary>
/// Apple ile giriş. Hesap üç yoldan bulunur:
///
/// 1. Apple kimliği daha önce bir hesaba bağlandıysa o hesap. Kimlik değişmez;
///    kullanıcı Apple'daki e-postasını değiştirse de aynı hesap açılır.
/// 2. Yoksa aynı e-postayla açılmış hesap (e-posta ya da Google ile) ve Apple
///    kimliği ona bağlanır. Apple e-postayı doğrulanmış verir; Google girişi
///    de e-postayla eşleştiriyor.
/// 3. O da yoksa yeni hesap. Apple adı yalnız ilk girişte verir, hesap
///    istekteki adla açılır. Fotoğraf yok: kullanıcı varsayılan görselle başlar.
///
/// "E-postamı gizle" seçen kullanıcıya Apple bir aktarma adresi verir; o adres
/// başka hesapla eşleşmez, yeni hesap açılır.
/// </summary>
public class AppleLoginCommandHandler : IRequestHandler<AppleLoginRequest, AppleLoginResponse>
{
    public const string LoginProvider = "Apple";

    private readonly AppleIdentityValidator _validator;
    private readonly UserManager<User> _userManager;
    private readonly IMediator _mediator;

    public AppleLoginCommandHandler(
        AppleIdentityValidator validator,
        UserManager<User> userManager,
        IMediator mediator)
    {
        _validator = validator;
        _userManager = userManager;
        _mediator = mediator;
    }

    public async Task<AppleLoginResponse> Handle(AppleLoginRequest request, CancellationToken cancellationToken)
    {
        var identity = await _validator.ValidateAsync(request.IdentityToken, request.RawNonce, cancellationToken);

        var user = await _userManager.FindByLoginAsync(LoginProvider, identity.Subject);
        if (user != null) return new AppleLoginResponse { User = user };

        if (string.IsNullOrEmpty(identity.Email))
        {
            // Apple e-postayı yalnız ilk yetkilendirmede vermeyi bırakmış
            // olabilir; hesap e-postasız açılamıyor (RequireUniqueEmail).
            throw new LingualLoopException(
                ErrorCode.AppleIdentityInvalid.CreateMessage("e-posta yok"),
                ErrorCode.AppleIdentityInvalid.GetDescription("e-posta yok"),
                HttpStatusCode.BadRequest);
        }

        user = await _userManager.FindByEmailAsync(identity.Email);
        if (user == null)
        {
            var name = $"{request.GivenName} {request.FamilyName}".Trim();
            var slug = (name.Length > 0 ? name : identity.Email.Split('@')[0]).ToSluggedUsername();
            var registered = await _mediator.Send(new RegisterUserRequest
            {
                Email = identity.Email,
                UserName = slug,
                UserNickname = slug,
                FirstName = request.GivenName?.Trim() ?? string.Empty,
                LastName = request.FamilyName?.Trim() ?? string.Empty,
                Roles = new List<string> { "User" },
            }, cancellationToken);
            user = registered.User!;
        }

        var linked = await _userManager.AddLoginAsync(
            user,
            new UserLoginInfo(LoginProvider, identity.Subject, LoginProvider));
        if (!linked.Succeeded)
        {
            var errorList = linked.Errors.Select(error => new ErrorList
            {
                ErrorCode = error.Code,
                ErrorDescription = error.Description,
            }).ToList();
            throw new LingualLoopException(
                ErrorCode.TheUserNotCreatedInDatabase.CreateMessage(user.UserName),
                errorList,
                ErrorCode.TheUserNotCreatedInDatabase.GetDescription(),
                HttpStatusCode.BadRequest);
        }

        return new AppleLoginResponse { User = user };
    }
}
