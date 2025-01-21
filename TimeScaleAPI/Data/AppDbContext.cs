using Microsoft.EntityFrameworkCore;
using TimeScaleAPI.Models;

namespace TimeScaleAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
            Database.EnsureCreated();
        }

        public DbSet<ValueData> ValueDatas { get; set; }
        public DbSet<ResultData> ResultDatas { get; set; }  
    }
}
