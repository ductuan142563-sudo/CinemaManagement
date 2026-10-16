using Microsoft.AspNetCore.SignalR;

namespace CinemaManagement.MVC.Hubs;

/// <summary>
/// Mỗi suất chiếu là một "phòng" (group). Trình duyệt đang xem suất nào thì vào phòng của suất đó
/// để nhận thông báo ghế vừa được đặt.
/// </summary>
public class SeatHub : Hub
{
    public static string GroupName(int showtimeId) => $"showtime-{showtimeId}";

    public Task JoinShowtime(int showtimeId)
    {
        if (showtimeId <= 0)
            return Task.CompletedTask;

        return Groups.AddToGroupAsync(Context.ConnectionId, GroupName(showtimeId));
    }

    public Task LeaveShowtime(int showtimeId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(showtimeId));
}