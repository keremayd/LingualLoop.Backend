using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Postgres.Configurations;
using Postgres.Models;

namespace Postgres;

public class StelindaContext : IdentityDbContext<User>
{
    public StelindaContext()
    {
        
    }
    
    public StelindaContext(DbContextOptions<StelindaContext> options) : base(options)
    {
    }
    
    public DbSet<Answer> Answers { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserScore> UserScores { get; set; }
    public DbSet<Video> Videos { get; set; }
    public DbSet<UserLives> UserLives { get; set; }
    public DbSet<UserBadge> UserBadges { get; set; }
    public DbSet<UserVideo> UserVideos { get; set; }
    public DbSet<Karty> Karty { get; set; }
    public DbSet<KartyUnit> KartyUnits { get; set; }
    public DbSet<KartyUnitTranslation> KartyUnitTranslations { get; set; }
    public DbSet<KartyTranslation> KartyTranslations { get; set; }
    public DbSet<UserKartyUnitState> UserKartyUnitStates { get; set; }
    public DbSet<UserKartyHistory> UserKartyHistories { get; set; }
    public DbSet<UserKartyLearning> UserKartyLearnings { get; set; }
    public DbSet<UserDailyActivity> UserDailyActivities { get; set; }
    public DbSet<UserStreak> UserStreaks { get; set; }
    public DbSet<UserLeagueProgress> UserLeagueProgresses { get; set; }
    public DbSet<UserQuestClaim> UserQuestClaims { get; set; }
    public DbSet<UserBlock> UserBlocks { get; set; }
    public DbSet<UserReport> UserReports { get; set; }
    public DbSet<AppleSubscription> AppleSubscriptions { get; set; }
    public DbSet<UserPremium> UserPremiums { get; set; }
    public DbSet<UserSettings> UserSettings { get; set; }
}
