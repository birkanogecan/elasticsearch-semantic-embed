using Microsoft.EntityFrameworkCore;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Elasticsearch_SemanticSearch.Data
{
    public class AppDbContext : DbContext
    {
        private const string ConnectionString = "Server=.;Database=CrawlProduct;User Id=SA;Password=;TrustServerCertificate=True";

        public DbSet<ProductTY> ProductTYs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(ConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductTY>().ToTable("ProductTY");
        }
    }
}
