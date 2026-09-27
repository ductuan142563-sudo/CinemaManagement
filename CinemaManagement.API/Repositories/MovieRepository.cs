using CinemaManagement.API.Data;
using CinemaManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.DAL.Repositories
{
    public class MovieRepository : GenericRepository<Movie>
    {
        public MovieRepository(CinemaDbContext context) : base(context)
        {
        }

        public IEnumerable<Movie> GetActiveMovies()
        {
            return _dbSet
                .Include(m => m.Genre)
                .Where(m => m.IsActive)
                .AsNoTracking()
                .ToList();
        }

        public Movie? GetByIdWithGenre(int movieId)
        {
            return _dbSet
                .Include(m => m.Genre)
                .FirstOrDefault(m => m.MovieID == movieId);
        }

        public IEnumerable<Movie> SearchByTitle(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return GetActiveMovies();

            keyword = keyword.Trim().ToLower();
            return _dbSet
                .Include(m => m.Genre)
                .Where(m => m.IsActive && m.Title.ToLower().Contains(keyword))
                .AsNoTracking()
                .ToList();
        }
    }
}