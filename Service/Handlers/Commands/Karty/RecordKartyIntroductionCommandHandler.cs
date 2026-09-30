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
/// ## Günlük tavan neden burada uygulanıyor
///
/// <see cref="KartyScheduler"/> tavanı kart **seçilirken** kontrol ediyor ama
/// satır burada yazılıyor; arada istemcinin 3 kartlık destesi var. Sekizinci
/// karta "Anladım" denirken dokuzuncu ve onuncu kart çoktan çekilmiş
/// oluyordu — ikisi de kota dolmadan önce alındığı için tanışma modunda
/// geliyor ve tavanı **iki fazlayla** deliyorlardı (gözlemlenen: 8 yerine 10).
///
/// Taşmanın büyüklüğü deste derinliğine eşit, yani istemci ayarına bağlı bir
/// sayı. Bu yüzden çözüm desteyi küçültmek değil, tavanı **yazma anında**
/// uygulamak: kararın gerçekten bağlandığı tek nokta burası ve buradaki
/// kontrol istemci ne yaparsa yapsın geçerli.
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

        var (dayStartUtc, dayEndUtc) = IstanbulDay.UtcRangeOf(now);
        var introducedToday = await _context.UserKartyLearnings
            .CountAsync(
                item =>
                    item.UserId == request.UserId &&
                    item.IntroducedDate >= dayStartUtc &&
                    item.IntroducedDate < dayEndUtc,
                cancellationToken);

        // Kota dolu ve bu kart yeni → kaydetme. Kullanıcı kartı gördü, bu bir
        // kayıp değil; kelime yarın usulünce tanıştırılacak. Alternatif —
        // kaydedip tavanı esnetmek — tavanı bir temenniye çevirirdi.
        if (learning is null && introducedToday >= KartyLeitner.DailyNewWordLimit)
        {
            await StreakActivityRecorder.MarkPlayedAsync(
                _context, request.UserId, now, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return new RecordKartyIntroductionResponse
            {
                KartyId = request.KartyId,
                IntroducedDate = now,
                IntroducedToday = introducedToday,
                DailyLimit = KartyLeitner.DailyNewWordLimit,
                Recorded = false,
            };
        }

        if (learning is null)
        {
            introducedToday += 1;
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
            IntroducedToday = introducedToday,
            DailyLimit = KartyLeitner.DailyNewWordLimit,
            Recorded = true,
        };
    }
}
