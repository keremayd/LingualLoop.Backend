namespace Common.Options;

public class HangfireOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public int TimeSpanFromHours { get; set; }
}