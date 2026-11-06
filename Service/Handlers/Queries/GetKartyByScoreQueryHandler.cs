using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Service.DataTransferObjects.Requests.Karty;
using Service.DataTransferObjects.Responses.Karty;
using Service.Helpers;
using KartyModel = Postgres.Models.Karty;

namespace Service.Handlers.Queries;

public class GetKartyByScoreQueryHandler : IRequestHandler<GetKartyByScoreRequest, GetKartyByScoreResponse>
{
    private readonly LingualLoopContext _context;

    public GetKartyByScoreQueryHandler(ILingualLoopGenericRepository<KartyModel> kartyRepository, ILingualLoopGenericRepository<Postgres.Models.UserKartyHistory> ukhRepository)
    {
        _context = kartyRepository.GetDbContext();
    }

    public async Task<GetKartyByScoreResponse> Handle(GetKartyByScoreRequest request, CancellationToken cancellationToken)
    {
        await KartyLearningSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);
        await KartyAudioSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        // Kart puan bandından rastgele seçilmiyor: vadesi gelmiş tekrar (oyunun
        // payı kadar, yeni kelimelerle karışık), oyunun destesinden yeni
        // kelime, sonra pratik. Sıra `KartyScheduler`'da.
        var game = request.UnitSequence is int unit
            ? new KartyGameKey(
                unit,
                request.Deck is int deck && deck > 0 ? deck : null,
                Math.Max(0, request.Reviews ?? 0))
            : null;
        var pick = await KartyScheduler.SelectAsync(
            _context, request.UserId, DateTime.UtcNow, game, cancellationToken);
        var selectedKartyId = pick.KartyId;

        var karty = selectedKartyId is null
            ? null
            : await _context.Karty
                .Where(q => q.KartyId == selectedKartyId)
                .Select(q => new
                {
                    q.KartyId,
                    q.NounText,
                    q.Article,
                    q.KartyUrl,
                    q.AudioUrl,
                    q.MinScore,
                    q.MaxScore,
                })
                .FirstOrDefaultAsync(cancellationToken);

        if (karty is null)
        {
            throw new LingualLoopException(ErrorCode.NoDataFoundInKarty.CreateMessage(request.UserId),
                ErrorCode.NoDataFoundInKarty.GetDescription(request.UserId), HttpStatusCode.BadRequest);
        }

        // Kart seçildi; **nasıl sunulacağı** ayrı bir karar ve kullanıcının o
        // kelimeyle geçmişine bakıyor.
        var learning = await _context.UserKartyLearnings
            .FirstOrDefaultAsync(
                item => item.UserId == request.UserId && item.KartyId == karty.KartyId,
                cancellationToken);

        var mode = KartyPresentationPolicy.Decide(learning);

        var response = new GetKartyByScoreResponse
        {
            KartyId = karty.KartyId,
            Mode = mode,
            Stage = pick.Stage,
            Article = karty.Article,
            KartyUrl = karty.KartyUrl,
            AudioUrl = karty.AudioUrl,
            MinScore = karty.MinScore,
            MaxScore = karty.MaxScore,
        };

        if (mode == KartyCardMode.Introduce)
        {
            // Tanışmada soru yok: kelime **doğru** yazımıyla gösterilir.
            // `KartySpellingChallengeBuilder` burada hiç çağrılmıyor, çünkü
            // %50 ihtimalle bozuk yazım üretiyor ve ilk temas onunla olamaz.
            response.QuestionText = karty.NounText;
            response.CorrectText = karty.NounText;
            response.IsCorrect = true;
            return response;
        }

        var challenge = KartySpellingChallengeBuilder.Build(karty.NounText);
        response.QuestionText = challenge.DisplayText;
        response.CorrectText = challenge.CorrectText;
        response.IsCorrect = challenge.IsCorrect;
        return response;
    }
}
