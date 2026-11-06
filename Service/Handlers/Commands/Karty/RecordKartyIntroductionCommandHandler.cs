using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests.Karty;
using Service.DataTransferObjects.Responses.Karty;
using Service.Helpers;

namespace Service.Handlers.Commands.Karty;

/// <summary>
/// Kullanıcı tanışma kartındaki "Anladım"a bastığında çağrılır.
///
/// Kayıt **sunumda değil onayda** yapılıyor: kart ekrana geldiği anda
/// işaretlenseydi, oyunu o kartta bırakan kullanıcı hiç okumadığı bir
/// kelimeyle "tanışmış" sayılırdı ve bir daha tanıştırılmazdı.
///
/// Günlük yeni kelime tavanı bir dönem burada, yazma anında uygulanıyordu
/// (istemcinin önden çektiği kartlar tavanı deliyordu). Tavan deste modeline
/// geçişte kalktı (<see cref="KartyDeckRules"/>): kelime seçici yalnız
/// oturumun destesinden yeni kelime veriyor, önden çekilen kartlar da o
/// destenin kelimeleri.
/// </summary>
public class RecordKartyIntroductionCommandHandler
    : IRequestHandler<RecordKartyIntroductionRequest, RecordKartyIntroductionResponse>
{
    private readonly LingualLoopContext _context;

    public RecordKartyIntroductionCommandHandler(
        ILingualLoopGenericRepository<UserKartyLearning> learningRepository)
    {
        _context = learningRepository.GetDbContext();
    }

    public async Task<RecordKartyIntroductionResponse> Handle(
        RecordKartyIntroductionRequest request,
        CancellationToken cancellationToken)
    {
        await KartyLearningSchemaHelper.EnsureCreatedAsync(_context, cancellationToken);

        var now = DateTime.UtcNow;
        var learning = await _context.UserKartyLearnings
            .FirstOrDefaultAsync(
                item => item.UserId == request.UserId && item.KartyId == request.KartyId,
                cancellationToken);

        if (learning is null)
        {
            learning = new UserKartyLearning
            {
                UserId = request.UserId,
                KartyId = request.KartyId,
                // **Sıfır**, bir değil: tanışmak bilmek değildir. Satırı
                // `KartyLearningRecorder` oluşturduğunda 1 yazıyor çünkü orası
                // doğru cevabı kaydediyor; burası yalnız karşılaşmayı.
                CorrectCount = 0,
                IntroducedDate = now,
                FirstLearnedDate = now,
                LastCorrectDate = now,
            };
            _context.UserKartyLearnings.Add(learning);
        }
        else
        {
            // Tekrarlanabilir: ilk tanışmanın damgası korunur.
            learning.IntroducedDate ??= now;
        }

        // Yeni bir kelimeyle tanışmak da oynamaktır.
        await StreakActivityRecorder.MarkPlayedAsync(
            _context, request.UserId, now, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new RecordKartyIntroductionResponse
        {
            KartyId = learning.KartyId,
            IntroducedDate = learning.IntroducedDate ?? now,
            Recorded = true,
        };
    }
}
