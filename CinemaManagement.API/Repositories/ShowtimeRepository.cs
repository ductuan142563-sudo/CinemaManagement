using CinemaManagement.API.Data;
using CinemaManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.DAL.Repositories
{
    public class ShowtimeRepository : GenericRepository<Showtime>
    {
        public ShowtimeRepository(CinemaDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Lấy các suất chiếu còn trống (StartTime > now) của 1 phim
        /// </summary>
        public IEnumerable<Showtime> GetAvailableByMovie(int movieId)
        {
            return _dbSet
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Where(s => s.MovieID == movieId
                         && s.IsActive
                         && s.StartTime > DateTime.Now)
                .OrderBy(s => s.StartTime)
                .AsNoTracking()
                .ToList();
        }

        public Showtime? GetByIdWithDetails(int showtimeId)
        {
            return _dbSet
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                    .ThenInclude(h => h.Seats)
                .FirstOrDefault(s => s.ShowtimeID == showtimeId);
        }

        public IEnumerable<Showtime> GetUpcoming()
        {
            return _dbSet
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Where(s => s.IsActive && s.StartTime > DateTime.Now)
                .OrderBy(s => s.StartTime)
                .AsNoTracking()
                .ToList();
        }
    }
}