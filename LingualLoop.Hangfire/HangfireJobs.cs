using Hangfire;
using LingualLoop.Hangfire.Jobs;

namespace LingualLoop.Hangfire;

public class HangfireJobs
{
    public static void ConfigureJobs()
    {
        RecurringJob.AddOrUpdate<UpdateLivesSyncJob>(nameof(UpdateLivesSyncJob), x => x.Execute(), "* * * * *");
    }
}