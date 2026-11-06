using MediatR;
using Service.DataTransferObjects.Responses.Karty;

namespace Service.DataTransferObjects.Requests.Karty;

/// <summary>
/// Sıradaki Karty kartı. Ad tarihî: kart eskiden kullanıcının zorluk
/// puanının bandından seçiliyordu; artık <c>KartyScheduler</c> tekrar
/// vadesi ve ünite sırasıyla seçiyor, puan okunmuyor.
/// </summary>
public class GetKartyByScoreRequest : IRequest<GetKartyByScoreResponse>
{
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Oyunun ünitesi ve destesi (patikadaki şu anki adım). Yeni kelime
    /// yalnız o desteden gelir; ünite verilip deste verilmezse oyun yalnız
    /// tekrardır. Hiçbiri verilmezse eski davranış.
    /// </summary>
    public int? UnitSequence { get; set; }

    public int? Deck { get; set; }

    /// <summary>Oyunda şimdiye kadar verilen tekrar kartı sayısı (payı ölçmek için).</summary>
    public int? Reviews { get; set; }
}