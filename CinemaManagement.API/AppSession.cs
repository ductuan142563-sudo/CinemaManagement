using CinemaManagement.API.Models;

namespace CinemaManagement.Common
{
    public static class AppSession
    {
        public static User? CurrentUser { get; set; }
        public static Member? CurrentMember { get; set; }

        public static bool IsLoggedIn => CurrentUser != null;

        public static bool IsAdmin => CurrentUser?.Role == "Admin";
        public static bool IsStaff => CurrentUser?.Role == "Staff";
        public static bool IsMember => CurrentUser?.Role == "Member";

        public static void Clear()
        {
            CurrentUser = null;
            CurrentMember = null;
        }
    }
}