using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Postgres.Configurations;
using Postgres.Models;

namespace Postgres;

public class LingualLoopContext : IdentityDbContext<User>
{
    public LingualLoopContext()
    {
        
    }
    
    public LingualLoopContext(DbContextOptions<LingualLoopContext> options) : base(options)
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
    public DbSet<UserKartyHistory> UserKartyHistories { get; set; }
    public DbSet<UserKartyLearning> UserKartyLearnings { get; set; }
    public DbSet<UserDailyActivity> UserDailyActivities { get; set; }
    public DbSet<UserStreak> UserStreaks { get; set; }
    public DbSet<UserLeagueProgress> UserLeagueProgresses { get; set; }
    public DbSet<UserQuestClaim> UserQuestClaims { get; set; }
    public DbSet<UserPremium> UserPremiums { get; set; }
}
