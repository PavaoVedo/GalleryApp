using GalleryApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GalleryApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Photo> Photos => Set<Photo>();
        public DbSet<Hashtag> Hashtags => Set<Hashtag>();
        public DbSet<PhotoHashtag> PhotoHashtags => Set<PhotoHashtag>();
        public DbSet<ActionLog> ActionLogs => Set<ActionLog>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Hashtag>()
                .HasIndex(x => x.Tag)
                .IsUnique();

            builder.Entity<PhotoHashtag>()
                .HasKey(x => new { x.PhotoId, x.HashtagId });

            builder.Entity<PhotoHashtag>()
                .HasOne(x => x.Photo)
                .WithMany(p => p.PhotoHashtags)
                .HasForeignKey(x => x.PhotoId);

            builder.Entity<PhotoHashtag>()
                .HasOne(x => x.Hashtag)
                .WithMany(h => h.PhotoHashtags)
                .HasForeignKey(x => x.HashtagId);
        }
    }
}
