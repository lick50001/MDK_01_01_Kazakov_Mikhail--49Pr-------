using Microsoft.EntityFrameworkCore;
using RestApiKazakov.Models;

namespace RestApiKazakov.Context
{
    public class AppDbContext : DbContext
    {
        // Все таблицы в одном месте
        public DbSet<Users> Users { get; set; }
        public DbSet<Menus> Menus { get; set; }
        public DbSet<Dishes> Dishes { get; set; }
        public DbSet<Orders> Orders { get; set; }
        public DbSet<OrderItems> OrderItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(
                "server=localhost;uid=root;pwd=;database=Rest",
                new MySqlServerVersion(new Version(8, 0, 11)));
        }

        public AppDbContext() =>
            Database.EnsureCreated();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Users>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<Users>()
                .ToTable("Users")
                .HasIndex(e => e.Username)
                .IsUnique();

            modelBuilder.Entity<Menus>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<Menus>()
                .ToTable("Menus");

            modelBuilder.Entity<Dishes>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<Dishes>()
                .ToTable("Dishes")
                .HasOne(e => e.Menu)
                .WithMany()
                .HasForeignKey(e => e.MenuId);

            modelBuilder.Entity<Orders>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<Orders>()
                .ToTable("Orders")
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId);

            modelBuilder.Entity<OrderItems>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<OrderItems>()
                .ToTable("OrderItems")
                .HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}