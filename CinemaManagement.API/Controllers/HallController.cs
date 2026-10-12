using CinemaManagement.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]   // phải đăng nhập mới xem được danh sách phòng chiếu
    public class HallController : ControllerBase
    {
        private readonly CinemaDbContext _context;

        public HallController(CinemaDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/hall
        /// Trả về { id, name, totalSeats } để khớp class SelectItem (Id, Name) của MVC.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.Halls
                .OrderBy(h => h.HallID)
                .Select(h => new
                {
                    id = h.HallID,
                    name = h.HallName,
                    totalSeats = h.TotalSeats
                })
                .ToListAsync();

            return Ok(list);
        }
    }
}