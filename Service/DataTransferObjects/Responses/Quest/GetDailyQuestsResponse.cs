namespace Service.DataTransferObjects.Responses;

public class GetDailyQuestsResponse
{
    public int DayKey { get; set; }
    public DateTime ResetAtUtc { get; set; }
    public List<DailyQuestResponse> Quests { get; set; } = [];

    /// <summary>Bütün görevler bitince toplanan seri koruması.</summary>
    public QuestFreezeBonusResponse FreezeBonus { get; set; } = new();
}
