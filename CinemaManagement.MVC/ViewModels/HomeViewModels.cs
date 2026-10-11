namespace CinemaManagement.MVC.ViewModels;

/// <summary>Khớp từng phần tử của GET api/movie (chỉ lấy phần trang chủ cần).</summary>
public class HomeMovieDto
{
    public int MovieID { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string? GenreName { get; set; }
    public string? AgeRating { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Khớp từng phần tử của GET api/showtime.</summary>
public class HomeShowtimeDto
{
    public int ShowtimeID { get; set; }
    public int MovieID { get; set; }
    public string? HallName { get; set; }
    public DateTime StartTime { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Một phim cùng các suất chiếu sắp tới của nó.</summary>
public class HomeMovieCard
{
    public int MovieId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string? GenreName { get; set; }
    public string? AgeRating { get; set; }
    public List<HomeShowtimeDto> Showtimes { get; set; } = new();

    // Màu poster cố định theo tên phim (không dùng string.GetHashCode vì nó đổi mỗi lần chạy lại)
    public int PosterHue
    {
        get
        {
            var h = 17;
            foreach (var c in Title)
                h = unchecked(h * 31 + c);
            return Math.Abs(h % 360);
        }
    }
}

public class HomePageViewModel
{
    public bool IsLoggedIn { get; set; }
    public bool LoadFailed { get; set; }
    public List<HomeMovieCard> Cards { get; set; } = new();
    public HomeMovieCard? NextMovie { get; set; }
    public HomeShowtimeDto? NextShowtime { get; set; }
}