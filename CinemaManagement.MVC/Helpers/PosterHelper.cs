namespace CinemaManagement.MVC.Helpers;

public static class PosterHelper
{
    /// <summary>
    /// Trả về địa chỉ đầy đủ của ảnh poster (ghép với ApiSettings:BaseUrl).
    /// Trả null nếu phim chưa có ảnh.
    /// </summary>
    public static string? Resolve(IConfiguration config, string? posterUrl)
    {
        if (string.IsNullOrWhiteSpace(posterUrl))
            return null;

        if (posterUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            posterUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return posterUrl;

        var baseUrl = config["ApiSettings:BaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
            return null;

        return baseUrl + (posterUrl.StartsWith('/') ? posterUrl : "/" + posterUrl);
    }

    /// <summary>
    /// Màu cố định (0-359) theo tên phim, dùng cho poster chưa có ảnh.
    /// Không dùng string.GetHashCode vì giá trị đó đổi mỗi lần chạy lại ứng dụng.
    /// </summary>
    public static int Hue(string? title)
    {
        var h = 17;
        foreach (var c in title ?? string.Empty)
            h = unchecked(h * 31 + c);
        return Math.Abs(h % 360);
    }
}