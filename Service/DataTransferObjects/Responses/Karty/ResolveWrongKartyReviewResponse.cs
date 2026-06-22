namespace Service.DataTransferObjects.Responses.Karty;

public class ResolveWrongKartyReviewResponse
{
    public string UserId { get; set; } = string.Empty;
    public int KartyId { get; set; }
    public bool IsMastered { get; set; }
    public int WrongCount { get; set; }
    public DateTime? ReviewedDate { get; set; }
}
