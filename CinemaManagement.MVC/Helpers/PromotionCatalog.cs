namespace CinemaManagement.MVC.Helpers;

/// <summary>Một chương trình khuyến mãi / ưu đãi.</summary>
/// <param name="Code">Mã ngắn, cũng là tên file ảnh banner (ví dụ weekend.jpg).</param>
/// <param name="Rail">"left" hoặc "right": banner hiện ở rìa trái hay phải.</param>
/// <param name="IsLive">false = chưa áp dụng thật, hiện nhãn "Sắp áp dụng".</param>
public record Promotion(
    string Code,
    string Badge,
    string BigText,
    string Title,
    string Summary,
    string[] Details,
    string Validity,
    int Hue,
    bool IsLive,
    string Rail);

public static class PromotionCatalog
{
    public static readonly IReadOnlyList<Promotion> All = new List<Promotion>
    {
        new(
            Code: "weekend",
            Badge: "CUỐI TUẦN",
            BigText: "-10%",
            Title: "Cuối tuần giảm 10%",
            Summary: "Giảm 10% tiền vé cho các suất chiếu vào thứ Bảy và Chủ nhật.",
            Details: new[]
            {
                "Áp dụng cho suất chiếu bắt đầu vào thứ Bảy hoặc Chủ nhật.",
                "Chỉ giảm trên tiền vé, không áp dụng cho bắp nước.",
                "Thành viên được hưởng mức giảm cao hơn giữa ưu đãi cuối tuần và ưu đãi theo hạng."
            },
            Validity: "Áp dụng quanh năm",
            Hue: 38,
            IsLive: false,
            Rail: "left"),

        new(
            Code: "combo",
            Badge: "BẮP NƯỚC",
            BigText: "COMBO",
            Title: "Combo bắp nước",
            Summary: "Đặt bắp và nước ngay khi chọn ghế, nhận tại quầy trước giờ chiếu.",
            Details: new[]
            {
                "Chọn bắp, nước hoặc combo ngay ở bước chọn ghế.",
                "Thanh toán một lần cùng tiền vé.",
                "Nhận tại quầy bắp nước trước giờ chiếu."
            },
            Validity: "Áp dụng cho mọi suất chiếu",
            Hue: 8,
            IsLive: false,
            Rail: "right"),

        new(
            Code: "member-tier",
            Badge: "THÀNH VIÊN",
            BigText: "đến 10%",
            Title: "Ưu đãi theo hạng thành viên",
            Summary: "Hạng càng cao, mức giảm trên giá vé càng lớn.",
            Details: new[]
            {
                "Silver giảm 0%, Gold giảm 5% (từ 500 điểm), Platinum giảm 10% (từ 2.000 điểm).",
                "Mức giảm tự động áp dụng khi đặt vé bằng tài khoản thành viên."
            },
            Validity: "Áp dụng quanh năm",
            Hue: 265,
            IsLive: true,
            Rail: "left"),

        new(
            Code: "points",
            Badge: "TÍCH ĐIỂM",
            BigText: "x2",
            Title: "Tích điểm nhân hệ số",
            Summary: "Hạng càng cao, mỗi vé tích được càng nhiều điểm.",
            Details: new[]
            {
                "Hệ số tích điểm: Silver x1, Gold x1,5, Platinum x2.",
                "Điểm được cộng ngay sau khi thanh toán thành công.",
                "Dùng điểm để đổi trong mục Hồ sơ."
            },
            Validity: "Áp dụng quanh năm",
            Hue: 160,
            IsLive: true,
            Rail: "right")
    };

    /// <summary>Các banner hiện ở một rìa trang ("left" hoặc "right").</summary>
    public static IEnumerable<Promotion> ForRail(string? side) =>
        All.Where(p => string.Equals(p.Rail, side, StringComparison.OrdinalIgnoreCase));
}