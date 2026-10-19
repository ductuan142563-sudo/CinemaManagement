using System.Globalization;
using System.Text.RegularExpressions;

namespace CinemaManagement.MVC.Helpers;

/// <summary>Thông tin để hiển thị mã QR chuyển khoản cho một đơn.</summary>
public class PaymentQrInfo
{
    public string ImageUrl { get; init; } = string.Empty;
    public string BankId { get; init; } = string.Empty;
    public string AccountNo { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string TransferContent { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}

/// <summary>
/// Tạo mã VietQR bằng Quick Link của VietQR (img.vietqr.io).
/// Mã nhận tiền lấy từ appsettings.json, mục Payment.
/// </summary>
public static class PaymentQrHelper
{
    private static readonly Regex BankIdPattern = new("^[A-Za-z0-9]{2,20}$", RegexOptions.Compiled);
    private static readonly Regex AccountPattern = new("^[0-9]{6,20}$", RegexOptions.Compiled);

    public static bool IsConfigured(IConfiguration config) =>
        TryReadBank(config, out _, out _, out _);

    public static PaymentQrInfo? Build(IConfiguration config, string orderCode, decimal amount)
    {
        if (!TryReadBank(config, out var bankId, out var accountNo, out var accountName))
            return null;

        // Nội dung chuyển khoản: mã đơn bỏ dấu gạch (nhiều ngân hàng loại bỏ ký tự đặc biệt)
        var content = orderCode.Replace("-", string.Empty);
        var money = Math.Round(amount, 0).ToString("0", CultureInfo.InvariantCulture);

        var url = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.png" +
                  $"?amount={money}" +
                  $"&addInfo={Uri.EscapeDataString(content)}" +
                  $"&accountName={Uri.EscapeDataString(accountName)}";

        return new PaymentQrInfo
        {
            ImageUrl = url,
            BankId = bankId,
            AccountNo = accountNo,
            AccountName = accountName,
            TransferContent = content,
            Amount = amount
        };
    }

    // Chỉ chấp nhận định dạng hợp lệ để giá trị cấu hình không thể làm hỏng đường dẫn ảnh
    private static bool TryReadBank(IConfiguration config, out string bankId, out string accountNo, out string accountName)
    {
        bankId = (config["Payment:BankId"] ?? string.Empty).Trim();
        accountNo = (config["Payment:AccountNo"] ?? string.Empty).Trim();
        accountName = (config["Payment:AccountName"] ?? string.Empty).Trim();

        return BankIdPattern.IsMatch(bankId)
               && AccountPattern.IsMatch(accountNo)
               && accountName.Length > 0;
    }
}