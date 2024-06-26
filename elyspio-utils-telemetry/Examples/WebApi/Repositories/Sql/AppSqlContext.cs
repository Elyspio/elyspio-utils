using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Repositories.Sql;

public class AppSqlContext : DbContext
{
	public AppSqlContext(DbContextOptions<AppSqlContext> options)
		: base(options)
	{
	}

	public required DbSet<UserEntity> Users { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<UserEntity>()
			.Property(u => u.Id)
			.HasDefaultValueSql("NEWID()");
	}
}