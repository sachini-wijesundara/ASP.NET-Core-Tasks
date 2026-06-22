using Microsoft.EntityFrameworkCore;
using ASP.NET_Core_Tasks.Models;
using Task = ASP.NET_Core_Tasks.Models.Task;

namespace ASP.NET_Core_Tasks.Data
{
    public class TaskDbContext : DbContext
    {
        public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options)
        {
        }

        public DbSet<Task> Tasks => Set<Task>();
        public DbSet<User> Users => Set<User>();
    }
}
