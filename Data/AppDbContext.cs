using BookLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace BookLibrary.Data
{

    //основен клас за връзка с база данни
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Book> Books { get; set; } //таблица за книги
        public DbSet<User> Users { get; set; } //таблица за потребители
    }
}
