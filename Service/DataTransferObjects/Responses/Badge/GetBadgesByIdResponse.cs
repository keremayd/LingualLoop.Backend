using Postgres.Models;

namespace Service.DataTransferObjects.Responses.Badge;

public class GetBadgesByIdResponse
{
    public int UserBadgeId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime EarnedDate { get; set; }
    public Postgres.Models.Badge Badge { get; set; } = new Postgres.Models.Badge();
}