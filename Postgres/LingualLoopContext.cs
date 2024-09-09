using Microsoft.EntityFrameworkCore;
using Postgres.Models;

namespace Postgres;

public class LingualLoopContext : DbContext
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
}