namespace Service.DataTransferObjects.Responses.Profile;

public class GetWordCollectionResponse
{
    public int TotalCount { get; set; }
    public int NewCount { get; set; }
    public int DueCount { get; set; }
    public int ArticleCount { get; set; }
    public int FilteredCount { get; set; }
    public int Page { get; set; }
    public bool HasMore { get; set; }
    public List<WordCollectionItemResponse> Items { get; set; } = [];
}

public class WordCollectionItemResponse
{
    public int KartyId { get; set; }
    public string NounText { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public string KartyUrl { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public bool IsNew { get; set; }
    public bool IsDue { get; set; }
    public bool NeedsArticlePractice { get; set; }
}
