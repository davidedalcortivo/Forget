using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Benchmarks
{
    [Table("Product", Schema = "dbo")]
    public sealed class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Category { get; set; }
        public bool IsActive { get; set; }
        public decimal Price { get; set; }
        public string? Note { get; set; }
    }

    public sealed class BenchmarkContext(DbContextOptions<BenchmarkContext> options) : DbContext(options)
    {
        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().Property(p => p.Id).ValueGeneratedNever();
        }
    }
}
