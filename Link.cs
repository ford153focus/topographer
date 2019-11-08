using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace topographer
{
    public class Link
    {
        public int? Id { get; set; }
        [Required] public string Url { get; set; }
        public bool IsParsed { get; set; }
    }

    public class LinkContext : DbContext
    {
        private static LinkContext _instance;

        public static LinkContext GetInstance()
        {
            return _instance ??= new LinkContext();
        }

        public DbSet<Link> Links { get; set; }

        private LinkContext()
        {
            Database.EnsureCreated();
        }

        ~LinkContext()
        {
            Database.EnsureDeleted();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Filename={Environment.CurrentDirectory}{Path.DirectorySeparatorChar}links.sqlite");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Link>().Property(link => link.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<Link>().HasIndex(link => link.Url).IsUnique();
            modelBuilder.Entity<Link>().Property(link => link.IsParsed).HasDefaultValue(false);
        }
    }
}
