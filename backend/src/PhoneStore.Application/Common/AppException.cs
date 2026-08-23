namespace PhoneStore.Application.Common;

/// <summary>Lỗi nghiệp vụ đã biết, được middleware map sang HTTP status tương ứng.</summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }

    public static AppException NotFound(string message) => new(message, 404);
    public static AppException Unauthorized(string message) => new(message, 401);
    public static AppException Forbidden(string message) => new(message, 403);
    public static AppException Conflict(string message) => new(message, 409);
}
