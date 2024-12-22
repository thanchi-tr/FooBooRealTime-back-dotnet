using FooBooRealTime_back_dotnet.Interface.GameContext;
using FooBooRealTime_back_dotnet.Model.Domain;
using Microsoft.EntityFrameworkCore;

namespace FooBooRealTime_back_dotnet.Data
{
    public class FooBooDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Game> Games { get; set; }
        public DbSet<Player> Players { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<AbstractSubject>();
            modelBuilder.Entity<Player>()
                    .HasKey(p => p.PlayerId);
            modelBuilder.Entity<Game>()
                    .HasKey(g => g.GameId);
            modelBuilder.Entity<Player>()
                    .Property(p => p.PlayerId)
                    .ValueGeneratedOnAdd();
            modelBuilder.Entity<Player>()
                    .HasMany(p => p.CreatedGames)
                    .WithOne(g => g.Author)
                    .HasForeignKey(g => g.AuthorId)
                    .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Player>().HasData(
                new Player
                {
                    PlayerId = new Guid("09ac5e84-db5c-4131-0d1c-08dd1c5384cf"),
                    Name = "June"
                }

            );
        }
    }
}
