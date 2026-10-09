namespace CinemaManagement.MVC.Helpers;

public static class SessionMemberExtensions
{
    /// <summary>
    /// Đọc MemberId từ Session, chịu được cả 2 cách lưu (SetString hoặc SetInt32).
    /// </summary>
    public static int? GetMemberIdOrNull(this ISession session)
    {
        // Trường hợp lưu bằng SetString("MemberId", "1")
        if (int.TryParse(session.GetString("MemberId"), out var fromString))
            return fromString;

        // Trường hợp lưu bằng SetInt32("MemberId", 1)
        var fromInt = session.GetInt32("MemberId");
        if (fromInt.HasValue)
            return fromInt;

        return null;
    }
}