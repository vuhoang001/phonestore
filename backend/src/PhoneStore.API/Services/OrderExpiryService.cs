using PhoneStore.Application.Interfaces;

namespace PhoneStore.API.Services;

/// <summary>
/// Job nền: định kỳ tự hủy các đơn Pending quá hạn chưa thanh toán,
/// hoàn kho & hoàn lượt coupon để không giữ tồn kho vô thời hạn.
/// </summary>
public class OrderExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderExpiryService> _logger;
    private readonly TimeSpan _expiry;
    private readonly TimeSpan _interval;

    public OrderExpiryService(IServiceScopeFactory scopeFactory, ILogger<OrderExpiryService> logger, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _expiry = TimeSpan.FromHours(config.GetValue<double?>("Orders:PaymentExpiryHours") ?? 24);
        _interval = TimeSpan.FromMinutes(config.GetValue<double?>("Orders:ExpiryScanMinutes") ?? 15);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // OrderService là scoped → phải tạo scope riêng cho mỗi lần quét.
                using var scope = _scopeFactory.CreateScope();
                var orders = scope.ServiceProvider.GetRequiredService<IOrderService>();
                var count = await orders.CancelExpiredAsync(_expiry);
                if (count > 0)
                    _logger.LogInformation("Đã tự hủy {Count} đơn quá hạn thanh toán.", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi quét đơn quá hạn thanh toán.");
            }
            await Task.Delay(_interval, stoppingToken);
        }
    }
}
