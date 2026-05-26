using Microsoft.EntityFrameworkCore;
using orders_api.Domain;

namespace orders_api.Persistence;

public sealed class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Code).HasMaxLength(32).IsRequired();
            entity.Property(order => order.CustomerName).HasMaxLength(200).IsRequired();
            entity.Property(order => order.Status).HasMaxLength(50).IsRequired();
            entity.Property(order => order.Total).HasColumnType("decimal(18,2)");
            entity.Property(order => order.CreatedAt).IsRequired();
            entity.HasIndex(order => order.Code).IsUnique();
        });
    }
}
