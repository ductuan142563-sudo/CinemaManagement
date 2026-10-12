using CinemaManagement.MVC.Helpers;

namespace CinemaManagement.MVC.ViewModels;

/// <summary>Khớp response của GET api/movie và GET api/movie/{id}.</summary>
public class BookingMovieDto
{
    public int MovieID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Director { get; set; }
    public int Duration { get; set; }
    public string? GenreName { get; set; }
    public string? AgeRating { get; set; }
    public string? Description { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string? PosterUrl { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Khớp từng phần tử của GET api/showtime.</summary>
public class BookingShowtimeDto
{
    public int ShowtimeID { get; set; }
    public int MovieID { get; set; }
    public string? HallName { get; set; }
    public DateTime StartTime { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Một phim trong lưới ở trang Đặt vé.</summary>
public class BookingMovieCard
{
    public BookingMovieDto Movie { get; set; } = new();
    public bool HasShowtimes { get; set; }
    public int PosterHue => PosterHelper.Hue(Movie.Title);
}

public class BookingIndexViewModel
{
    public List<BookingMovieCard> Items { get; set; } = new();
}

/// <summary>Trang riêng của một phim: thông tin phim và các suất chiếu sắp tới.</summary>
public class BookingMoviePageViewModel
{
    public BookingMovieDto Movie { get; set; } = new();
    public List<BookingShowtimeDto> Showtimes { get; set; } = new();
    public int PosterHue => PosterHelper.Hue(Movie.Title);
}