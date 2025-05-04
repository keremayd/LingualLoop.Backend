namespace Service.DataTransferObjects.Responses.Badge;

public class EarnBadgeByIdResponse
{
    public int BadgeId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime EarnedDate { get; set; }
}