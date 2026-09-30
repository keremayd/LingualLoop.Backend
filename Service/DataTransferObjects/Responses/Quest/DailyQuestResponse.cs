namespace Service.DataTransferObjects.Responses;

public class DailyQuestResponse
{
    public string QuestKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Target { get; set; }
    public int Progress { get; set; }
    public int RewardTickets { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsClaimed { get; set; }
}
