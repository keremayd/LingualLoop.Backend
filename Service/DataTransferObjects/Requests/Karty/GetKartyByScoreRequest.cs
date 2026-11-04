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
}