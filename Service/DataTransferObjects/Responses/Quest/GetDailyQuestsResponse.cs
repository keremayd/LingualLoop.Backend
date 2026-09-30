namespace Service.DataTransferObjects.Responses;

public class GetDailyQuestsResponse
{
    public int DayKey { get; set; }
    public DateTime ResetAtUtc { get; set; }
    public List<DailyQuestResponse> Quests { get; set; } = [];
}
