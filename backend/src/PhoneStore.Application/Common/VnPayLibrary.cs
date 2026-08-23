using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace PhoneStore.Application.Common;

/// <summary>
/// Bộ công cụ ký/xác thực chữ ký cho VNPAY theo chuẩn 2.1.0.
/// Tham số phải được sắp xếp theo thứ tự ordinal của key rồi ký HMAC-SHA512.
/// </summary>
public class VnPayLibrary
{
    private readonly SortedList<string, string> _requestData = new(StringComparer.Ordinal);
    private readonly SortedList<string, string> _responseData = new(StringComparer.Ordinal);

    public void AddRequestData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value)) _requestData[key] = value;
    }

    public void AddResponseData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value)) _responseData[key] = value;
    }

    public string GetResponseData(string key) => _responseData.TryGetValue(key, out var v) ? v : string.Empty;

    /// <summary>Tạo URL chuyển hướng sang cổng VNPAY kèm chữ ký.</summary>
    public string CreateRequestUrl(string baseUrl, string hashSecret)
    {
        var signData = BuildQuery(_requestData);
        var secureHash = HmacSha512(hashSecret, signData);
        return $"{baseUrl}?{signData}&vnp_SecureHash={secureHash}";
    }

    /// <summary>So khớp chữ ký VNPAY trả về với chữ ký tự tính từ dữ liệu response.</summary>
    public bool ValidateSignature(string inputHash, string hashSecret)
    {
        _responseData.Remove("vnp_SecureHash");
        _responseData.Remove("vnp_SecureHashType");
        var signData = BuildQuery(_responseData);
        var computed = HmacSha512(hashSecret, signData);
        return computed.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
    }

    private static string BuildQuery(SortedList<string, string> data)
    {
        var sb = new StringBuilder();
        foreach (var (key, value) in data)
        {
            if (string.IsNullOrEmpty(value)) continue;
            if (sb.Length > 0) sb.Append('&');
            sb.Append(WebUtility.UrlEncode(key)).Append('=').Append(WebUtility.UrlEncode(value));
        }
        return sb.ToString();
    }

    private static string HmacSha512(string key, string input)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
