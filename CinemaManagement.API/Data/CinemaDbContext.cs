using CinemaManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.API.Data
{
    public class CinemaDbContext : DbContext
    {
        public CinemaDbContext(DbContextOptions<CinemaDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<MembershipTier> MembershipTiers => Set<MembershipTier>();
        public DbSet<Member> Members => Set<Member>();
        public DbSet<Genre> Genres => Set<Genre>();
        public DbSet<Movie> Movies => Set<Movie>();
        public DbSet<Hall> Halls => Set<Hall>();
        public DbSet<Seat> Seats => Set<Seat>();
        public DbSet<Showtime> Showtimes => Set<Showtime>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<BookingDetail> BookingDetails => Set<BookingDetail>();
        public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Member>()
                .HasOne(m => m.User)
                .WithOne()
                .HasForeignKey<Member>(m => m.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Member>().HasIndex(m => m.UserID).IsUnique();
            modelBuilder.Entity<Member>().HasIndex(m => m.MembershipCode).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

            modelBuilder.Entity<Member>()
                .HasOne(m => m.Tier)
                .WithMany(t => t.Members)
                .HasForeignKey(m => m.TierID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Member)
                .WithMany(m => m.Bookings)
                .HasForeignKey(b => b.MemberID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Booking>().HasIndex(b => b.BookingCode).IsUnique();

            modelBuilder.Entity<MembershipTier>().Property(t => t.DiscountPercent).HasPrecision(5, 2);
            modelBuilder.Entity<MembershipTier>().Property(t => t.PointRate).HasPrecision(5, 2);
            modelBuilder.Entity<Showtime>().Property(s => s.BasePrice).HasPrecision(10, 2);
            modelBuilder.Entity<Booking>().Property(b => b.TotalAmount).HasPrecision(12, 2);
            modelBuilder.Entity<Booking>().Property(b => b.DiscountAmount).HasPrecision(12, 2);
            modelBuilder.Entity<Booking>().Property(b => b.FinalAmount).HasPrecision(12, 2);
            modelBuilder.Entity<BookingDetail>().Property(d => d.Price).HasPrecision(10, 2);

            modelBuilder.Entity<MembershipTier>().HasData(
                new MembershipTier { TierID = 1, TierName = "Silver", MinPoints = 0, DiscountPercent = 0, PointRate = 1, Description = "Hang co ban" },
                new MembershipTier { TierID = 2, TierName = "Gold", MinPoints = 500, DiscountPercent = 5, PointRate = 1.5m, Description = "Hang vang - giam 5%" },
                new MembershipTier { TierID = 3, TierName = "Platinum", MinPoints = 2000, DiscountPercent = 10, PointRate = 2, Description = "Hang bach kim - giam 10%" }
            );

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserID = 1,
                    Username = "admin",
                    PasswordHash = "$2a$11$7YhtIqvUwLJTDZdlwx9DCOv4So3OEwPCmIsS/hisN/P6HjVigKsRi",
                    FullName = "Quan tri vien",
                    Email = "admin@cinema.local",
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1)
                }
            );

            modelBuilder.Entity<Genre>().HasData(
                new Genre { GenreID = 1, GenreName = "Hanh dong" },
                new Genre { GenreID = 2, GenreName = "Hai" },
                new Genre { GenreID = 3, GenreName = "Tam ly" },
                new Genre { GenreID = 4, GenreName = "Kinh di" }
            );

            modelBuilder.Entity<Hall>().HasData(
                new Hall { HallID = 1, HallName = "Phong 1", TotalSeats = 80, RowsCount = 8, SeatsPerRow = 10 },
                new Hall { HallID = 2, HallName = "Phong 2", TotalSeats = 60, RowsCount = 6, SeatsPerRow = 10 }
            );
        }
    }
}