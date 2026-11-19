using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;
using Service.Helpers;

namespace Service.Handlers.Commands;

/// <summary>
/// Ana dilin kaydı (<c>user_settings</c>). İçeriğin dilini bu satır değil
/// istekteki başlık belirliyor; kayıt, hangi dilden kaç kullanıcı geldiğini
/// görmek ve ileride sunucunun gönderdiği metinler için.
/// </summary>
public class UpdateUserLanguageCommandHandler
    : IRequestHandler<UpdateUserLanguageRequest, UpdateUserLanguageResponse>
{
    private readonly LingualLoopContext _context;

    public UpdateUserLanguageCommandHandler(ILingualLoopGenericRepository<UserSettings> repository)
    {
        _context = repository.GetDbContext();
    }

    public async Task<UpdateUserLanguageResponse> Handle(
        UpdateUserLanguageRequest request,
        CancellationToken cancellationToken)
    {
        var language = KartyTranslationWriter.RequireLocale(request.NativeLanguage);

        await UserSettingsSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(row => row.UserId == request.UserId, cancellationToken);
        if (settings is null)
        {
            _context.UserSettings.Add(new UserSettings
            {
                UserId = request.UserId,
                NativeLanguage = language,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        else if (settings.NativeLanguage != language)
        {
            settings.NativeLanguage = language;
            settings.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return new UpdateUserLanguageResponse { NativeLanguage = language };
    }
}
