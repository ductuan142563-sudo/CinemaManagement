namespace CinemaManagement.MVC.Helpers
{
    public static class SessionHelper
    {
        private const string KeyUserId = "UserId";
        private const string KeyUsername = "Username";
        private const string KeyFullName = "FullName";
        private const string KeyRole = "Role";
        private const string KeyMemberId = "MemberId";

        public static void SetUser(ISession session, int userId, string username, string fullName, string role, int? memberId = null)
        {
            session.SetInt32(KeyUserId, userId);
            session.SetString(KeyUsername, username);
            session.SetString(KeyFullName, fullName ?? "");
            session.SetString(KeyRole, role);
            if (memberId.HasValue)
                session.SetInt32(KeyMemberId, memberId.Value);
        }

        public static void Clear(ISession session)
        {
            session.Clear();
        }

        public static bool IsLoggedIn(ISession session)
            => session.GetInt32(KeyUserId).HasValue;

        public static int? GetUserId(ISession session)
            => session.GetInt32(KeyUserId);

        public static string? GetUsername(ISession session)
            => session.GetString(KeyUsername);

        public static string? GetFullName(ISession session)
            => session.GetString(KeyFullName);

        public static string? GetRole(ISession session)
            => session.GetString(KeyRole);

        public static int? GetMemberId(ISession session)
            => session.GetInt32(KeyMemberId);

        public static bool IsAdmin(ISession session)
            => GetRole(session) == "Admin";

        public static bool IsStaff(ISession session)
            => GetRole(session) == "Staff" || IsAdmin(session);

        public static bool IsMember(ISession session)
            => GetRole(session) == "Member";
    }
}