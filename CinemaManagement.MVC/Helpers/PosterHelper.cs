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
}