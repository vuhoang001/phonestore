namespace PhoneStore.Application.Common;

/// <summary>Cấu hình lưu trữ file trên MinIO (tương thích S3).</summary>
public class MinioSettings
{
    /// <summary>Địa chỉ MinIO nội bộ (vd "minio:9000" trong docker, "localhost:9000" khi chạy local).</summary>
    public string Endpoint { get; set; } = "localhost:9000";
    /// <summary>Địa chỉ công khai để dựng URL trình duyệt tải ảnh (vd "http://localhost:9000").</summary>
    public string PublicEndpoint { get; set; } = "http://localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string Bucket { get; set; } = "phonestore";
    public bool UseSsl { get; set; }
}
