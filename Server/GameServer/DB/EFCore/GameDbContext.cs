using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameServer
{
	public class GameDbContext : DbContext
	{
		public DbSet<HeroDb> Heroes { get; set; }
		public DbSet<ItemDb> Items { get; set; }

		static readonly ILoggerFactory _logger = LoggerFactory.Create(builder => { builder.AddConsole(); });

		public GameDbContext()
		{
		}

		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
			ConfigManager.LoadConfig();

			options.UseLoggerFactory(_logger).UseSqlServer(ConfigManager.Config.connectionString);
		}

		protected override void OnModelCreating(ModelBuilder builder)
		{
			// AccountDbId에 인덱스 걸어준다
			builder.Entity<HeroDb>().HasIndex(t => t.AccountDbId);

			builder.Entity<HeroDb>().Property(nameof(HeroDb.CreatedDate)).HasDefaultValueSql("CURRENT_TIMESTAMP");
           
			builder.Entity<HeroDb>().HasIndex(t => t.Name);

            builder.Entity<ItemDb>().HasOne(e => e.OwnerDb).WithMany(e => e.Items).HasForeignKey(e => e.OwnerDbId).IsRequired();
		}
	}
}