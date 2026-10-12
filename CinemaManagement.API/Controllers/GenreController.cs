using CinemaManagement.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]   // phải đăng nhập mới xem được danh sách thể loại
    public class GenreController : ControllerBase
    {
        private readonly CinemaDbContext _context;

        public GenreController(CinemaDbContext context)
        {
            _context = context;
        }

        /// <summary>GET /api/genre</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.Genres
                .OrderBy(g => g.GenreName)
                .Select(g => new
                {
                    genreID = g.GenreID,
                    genreName = g.GenreName
                })
                .ToListAsync();

            return Ok(list);
        }
    }
}