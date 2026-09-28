using Awai.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Awai.Models
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<SiteInfo> SiteInfo { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<PageContent> PageContents { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<SitePhoto> SitePhotos { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<InsuranceApplication> InsuranceApplications { get; set; }
        public DbSet<ApplicationDocument> ApplicationDocuments { get; set; }
        public DbSet<StaffNotification> StaffNotifications { get; set; }
        public DbSet<PaymentIntent> PaymentIntents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entity in new[]
            {
                typeof(SiteInfo), typeof(UserProfile), typeof(Employee), typeof(PageContent),
                typeof(Product), typeof(Post), typeof(SitePhoto), typeof(ContactMessage),
                typeof(InsuranceApplication), typeof(ApplicationDocument), typeof(StaffNotification),
                typeof(PaymentIntent)
            })
            {
                modelBuilder.Entity(entity).Property("Id").HasDefaultValueSql("NEWID()");
            }

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.UserProfile)
                .WithOne(p => p.ApplicationUser)
                .HasForeignKey<UserProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Employee)
                .WithOne(e => e.ApplicationUser)
                .HasForeignKey<Employee>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InsuranceApplication>()
                .HasOne(a => a.Product)
                .WithMany()
                .HasForeignKey(a => a.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApplicationDocument>()
                .HasOne(d => d.InsuranceApplication)
                .WithMany(a => a.Documents)
                .HasForeignKey(d => d.InsuranceApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StaffNotification>()
                .HasOne(n => n.Application)
                .WithMany()
                .HasForeignKey(n => n.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StaffNotification>()
                .HasIndex(n => new { n.IsDismissed, n.Created });

            modelBuilder.Entity<PageContent>()
                .HasIndex(p => p.Key)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PaymentIntent>()
                .Property(p => p.Amount)
                .HasPrecision(18, 3);

            modelBuilder.Entity<PaymentIntent>()
                .HasIndex(p => p.IdempotencyKey)
                .IsUnique();

            modelBuilder.Entity<PaymentIntent>()
                .HasIndex(p => new { p.Status, p.ProcessingStartedUtc });

            modelBuilder.Seed();
        }
    }
}
