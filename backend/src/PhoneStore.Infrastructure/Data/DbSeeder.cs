using PhoneStore.Application.Common;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Entities;
using PhoneStore.Domain.Enums;
using PhoneStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Infrastructure.Data;

/// <summary>
/// Seed dữ liệu cửa hàng điện thoại (tĩnh, không crawl) khi DB đang trống.
/// Idempotent: chỉ seed khi bảng Users rỗng. Ảnh dùng placeholder ổn định (không phụ thuộc MinIO).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, SeedSettings settings)
    {
        if (await db.Users.AnyAsync()) return; // đã có dữ liệu

        // ----- Tài khoản bootstrap (lấy từ cấu hình, không hardcode credential) -----
        var admin = new User
        {
            Email = settings.Admin.Email, PasswordHash = hasher.Hash(settings.Admin.Password),
            FullName = settings.Admin.FullName, Role = UserRole.Admin, Phone = settings.Admin.Phone,
            EmailConfirmed = true
        };
        db.Users.Add(admin);
        db.Carts.Add(new Cart { User = admin });

        // Tài khoản khách demo (tùy chọn — bật/tắt qua cấu hình).
        User? customer = null;
        if (settings.SeedDemoCustomer)
        {
            customer = new User
            {
                Email = settings.Customer.Email, PasswordHash = hasher.Hash(settings.Customer.Password),
                FullName = settings.Customer.FullName, Role = UserRole.Customer, Phone = settings.Customer.Phone,
                EmailConfirmed = true
            };
            db.Users.Add(customer);
            db.Carts.Add(new Cart { User = customer });
        }

        // Phương thức vận chuyển: 2 lựa chọn cơ bản.
        db.ShippingMethods.AddRange(
            new ShippingMethod { Name = "Giao tiêu chuẩn", BaseFee = 30000, EstimatedDays = 3 },
            new ShippingMethod { Name = "Giao nhanh", BaseFee = 50000, EstimatedDays = 1 });

        if (!settings.SeedProducts)
        {
            await db.SaveChangesAsync(); // vẫn tạo tài khoản bootstrap + shipping
            return;
        }

        // ----- Thương hiệu (>=6, có SortOrder) -----
        var brands = new Dictionary<string, Brand>();
        var brandSeed = new (string name, int sort)[]
        {
            ("Apple", 1), ("Samsung", 2), ("Xiaomi", 3), ("OPPO", 4), ("vivo", 5), ("realme", 6)
        };
        foreach (var (name, sort) in brandSeed)
        {
            var b = new Brand
            {
                Name = name, Slug = SlugHelper.Generate(name), SortOrder = sort,
                LogoUrl = Placeholder(name),
                Description = $"Sản phẩm chính hãng {name}."
            };
            brands[name] = b;
            db.Brands.Add(b);
        }

        // ----- Danh mục (cây 1-2 cấp) -----
        var root = new Category { Name = "Sản phẩm", Slug = "san-pham" };
        var catPhone = new Category { Name = "Điện thoại", Slug = "dien-thoai", Parent = root };
        var catTablet = new Category { Name = "Máy tính bảng", Slug = "may-tinh-bang", Parent = root };
        var catAccessory = new Category { Name = "Phụ kiện", Slug = "phu-kien", Parent = root };
        var catWatch = new Category { Name = "Đồng hồ thông minh", Slug = "dong-ho-thong-minh", Parent = root };
        db.Categories.AddRange(root, catPhone, catTablet, catAccessory, catWatch);

        var usedSlugs = new HashSet<string>();
        var allVariants = new List<ProductVariant>(); // để sinh đơn hàng thật
        var pIndex = 0;

        // ----- Catalog điện thoại (mô tả tĩnh) -----
        foreach (var sp in PhoneCatalog())
        {
            pIndex++;
            var brand = brands[sp.Brand];
            var minPrice = sp.Variants.Min(v => v.Price);
            var product = new Product
            {
                Name = sp.Name,
                Slug = UniqueSlug(sp.Name, usedSlugs),
                Brand = brand,
                Category = catPhone, // toàn bộ catalog seed là điện thoại
                Description = sp.Description,
                BasePrice = minPrice,
                Status = ProductStatus.Active,
                WarrantyMonths = 12,
                InstallmentAvailable = minPrice >= 8_000_000m, // máy giá cao mới bật trả góp
                CreatedAt = DateTime.UtcNow.AddDays(-(pIndex % 90))
            };

            // Biến thể: tổ hợp Màu × Dung lượng.
            var vi = 0;
            foreach (var v in sp.Variants)
            {
                var variant = new ProductVariant
                {
                    Sku = $"P{pIndex:D4}V{vi++}",
                    Color = v.Color, ColorHex = v.ColorHex, Storage = v.Storage,
                    Price = v.Price, Cost = Math.Round(v.Price * 0.82m), StockQuantity = v.Stock,
                    Product = product
                };
                product.Variants.Add(variant);
                allVariants.Add(variant);
            }

            // Ảnh placeholder ổn định (không phụ thuộc MinIO).
            product.Images.Add(new ProductImage { Url = Placeholder(sp.Name), IsPrimary = true, SortOrder = 0 });

            // Thông số kỹ thuật gom theo nhóm.
            var order = 0;
            foreach (var s in sp.Specs)
                product.Specifications.Add(new ProductSpecification
                {
                    Group = s.Group, Name = s.Name, Value = s.Value, SortOrder = order++
                });

            db.Products.Add(product);
        }

        // ===== Dữ liệu vận hành: coupon + người mua + lịch sử đơn hàng =====
        var rnd = new Random(20240822); // cố định để tái lập
        var coupons = new List<Coupon>
        {
            new() { Code = "WELCOME10", DiscountType = DiscountType.Percentage, DiscountValue = 10, MinOrderAmount = 5_000_000,
                    StartDate = DateTime.UtcNow.AddMonths(-4), EndDate = DateTime.UtcNow.AddMonths(3), UsageLimit = 1000, IsActive = true },
            new() { Code = "SALE500K", DiscountType = DiscountType.FixedAmount, DiscountValue = 500000, MinOrderAmount = 10_000_000,
                    StartDate = DateTime.UtcNow.AddMonths(-4), EndDate = DateTime.UtcNow.AddMonths(2), UsageLimit = 1000, IsActive = true }
        };
        db.Coupons.AddRange(coupons);

        // Người mua với recency đa dạng (để churn/RFM có phân bố thật).
        var buyerHash = hasher.Hash("Buyer@123");
        var buyers = new List<User>();
        if (customer != null) buyers.Add(customer);
        for (var i = 1; i <= 14; i++)
        {
            var u = new User
            {
                Email = $"buyer{i}@phonestore.vn", PasswordHash = buyerHash, FullName = $"Khách hàng {i:D2}",
                Role = UserRole.Customer, Phone = $"09{rnd.Next(10000000, 99999999)}", EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow.AddDays(-rnd.Next(20, 150))
            };
            buyers.Add(u);
            db.Users.Add(u);
        }

        SeedOrders(db, rnd, buyers, coupons, allVariants);

        await db.SaveChangesAsync();

        await SeedFlashSaleAsync(db);
    }

    /// <summary>Seed một chương trình Flash Sale đang chạy từ các sản phẩm bán chạy.</summary>
    private static async Task SeedFlashSaleAsync(AppDbContext db)
    {
        if (await db.FlashSales.AnyAsync()) return;
        var picks = await db.Products.OrderByDescending(p => p.SoldCount).Take(6).ToListAsync();
        if (picks.Count == 0) return;
        var now = DateTime.UtcNow;
        var sale = new FlashSale
        {
            Name = "Flash Sale Giờ Vàng",
            StartAt = now.AddHours(-1),
            EndAt = now.AddDays(1),
            IsActive = true
        };
        foreach (var p in picks)
        {
            sale.Items.Add(new FlashSaleItem
            {
                ProductId = p.Id,
                FlashPrice = Math.Round(p.BasePrice * 0.9m / 1000m) * 1000m,
                QuantityLimit = 50
                // "Đã bán" hiển thị lấy từ Product.SoldCount thật (xem FlashSaleService).
            });
        }
        db.FlashSales.Add(sale);
        await db.SaveChangesAsync();
    }

    /// <summary>Sinh lịch sử đơn hàng thật (rải theo thời gian) để mọi báo cáo tính từ giao dịch thật.</summary>
    private static void SeedOrders(AppDbContext db, Random rnd, List<User> buyers, List<Coupon> coupons, List<ProductVariant> variants)
    {
        var now = DateTime.UtcNow;
        var cancelReasons = new[] { "Khách đổi ý", "Đổi sang máy khác", "Giao hàng chậm", "Tìm được giá tốt hơn", "Đặt nhầm sản phẩm" };
        var seq = 0;
        var imeiSeq = 0;

        for (var n = 0; n < 80; n++)
        {
            var buyer = buyers[rnd.Next(buyers.Count)];
            var daysAgo = rnd.Next(0, 110);
            var createdAt = now.AddDays(-daysAgo).AddMinutes(-rnd.Next(0, 1439));

            var chosen = new HashSet<ProductVariant>();
            var items = new List<OrderItem>();
            decimal subtotal = 0;
            var itemCount = rnd.Next(1, 3); // điện thoại thường mua 1-2 máy
            for (var k = 0; k < itemCount; k++)
            {
                var v = variants[rnd.Next(variants.Count)];
                if (!chosen.Add(v)) continue;
                var qty = rnd.Next(1, 3);
                var info = string.Join(" · ", new[] { v.Color, v.Storage }.Where(s => !string.IsNullOrWhiteSpace(s)));
                subtotal += v.Price * qty;
                items.Add(new OrderItem
                {
                    Variant = v, ProductNameSnapshot = v.Product.Name,
                    VariantInfoSnapshot = string.IsNullOrEmpty(info) ? null : info,
                    PriceSnapshot = v.Price, Quantity = qty
                });
            }
            if (items.Count == 0) continue;

            var shippingFee = rnd.Next(2) == 0 ? 30000m : 50000m;

            Coupon? coupon = null; decimal discount = 0;
            if (rnd.NextDouble() < 0.35)
            {
                var appl = coupons.Where(c => subtotal >= c.MinOrderAmount).ToList();
                if (appl.Count > 0)
                {
                    coupon = appl[rnd.Next(appl.Count)];
                    discount = coupon.DiscountType == DiscountType.Percentage
                        ? Math.Round(subtotal * coupon.DiscountValue / 100m, 0) : coupon.DiscountValue;
                    discount = Math.Min(discount, subtotal);
                }
            }
            var total = subtotal - discount + shippingFee;

            var roll = rnd.NextDouble();
            var status = roll < 0.68 ? OrderStatus.Completed
                : roll < 0.80 ? OrderStatus.Cancelled
                : roll < 0.89 ? OrderStatus.Shipping
                : roll < 0.95 ? OrderStatus.Confirmed : OrderStatus.Pending;
            if (status == OrderStatus.Completed && daysAgo < 3) status = OrderStatus.Shipping;
            var method = rnd.NextDouble() < 0.6 ? PaymentMethod.Cod : PaymentMethod.VnPay;

            // Trả góp: một số đơn giá trị lớn chọn trả góp.
            int? installMonths = null; decimal? installMonthly = null;
            if (total >= 10_000_000m && rnd.NextDouble() < 0.3)
            {
                installMonths = new[] { 6, 9, 12 }[rnd.Next(3)];
                installMonthly = Math.Round(total / installMonths.Value / 1000m) * 1000m; // lãi 0% (demo)
                method = PaymentMethod.Installment;
            }

            var order = new Order
            {
                User = buyer, OrderCode = $"ORD{createdAt:yyMMdd}{++seq:D4}",
                SubTotal = subtotal, DiscountAmount = discount, ShippingFee = shippingFee, TotalAmount = total,
                Status = status, ShippingAddressSnapshot = $"{buyer.FullName} | 0900000000 | Địa chỉ demo, TP.HCM",
                InstallmentMonths = installMonths, InstallmentMonthly = installMonthly,
                CreatedAt = createdAt, UpdatedAt = createdAt, Items = items
            };

            var t = createdAt;
            order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.Pending, Note = "Đơn được tạo.", CreatedAt = t });
            if (status == OrderStatus.Cancelled)
            {
                order.CancelReason = cancelReasons[rnd.Next(cancelReasons.Length)];
                order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.Cancelled, Note = $"Khách hủy: {order.CancelReason}", CreatedAt = t.AddHours(rnd.Next(1, 24)) });
            }
            else
            {
                if (status >= OrderStatus.Confirmed) { t = t.AddHours(rnd.Next(1, 10)); order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.Confirmed, CreatedAt = t }); }
                if (status >= OrderStatus.Shipping) { t = t.AddHours(rnd.Next(6, 36)); order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.Shipping, CreatedAt = t }); }
                if (status == OrderStatus.Completed) { t = t.AddHours(rnd.Next(12, 72)); order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.Completed, CreatedAt = t }); }
            }

            var payStatus = status == OrderStatus.Completed ? PaymentStatus.Paid
                : status == OrderStatus.Cancelled ? PaymentStatus.Failed : PaymentStatus.Pending;
            order.Payment = new Payment { Method = method, Amount = total, Status = payStatus, PaidAt = payStatus == PaymentStatus.Paid ? t : null, CreatedAt = createdAt };

            // Đặc thù điện thoại: khi đã Shipping/Completed thì gán IMEI + tạo phiếu bảo hành.
            if (status == OrderStatus.Shipping || status == OrderStatus.Completed)
            {
                var startDate = t;
                foreach (var it in items)
                {
                    var imei = $"35{++imeiSeq:D13}"; // 15 chữ số giả lập
                    it.Imei = imei;
                    db.WarrantyRecords.Add(new WarrantyRecord
                    {
                        Order = order, OrderItem = it, Imei = imei,
                        ProductNameSnapshot = it.ProductNameSnapshot,
                        StartDate = startDate, EndDate = startDate.AddMonths(12),
                        Status = WarrantyStatus.Active
                    });
                }
            }

            if (status != OrderStatus.Cancelled)
                foreach (var it in items) it.Variant.StockQuantity = Math.Max(0, it.Variant.StockQuantity - it.Quantity);
            if (status == OrderStatus.Completed)
                foreach (var it in items) it.Variant.Product.SoldCount += it.Quantity;
            if (coupon != null && status != OrderStatus.Cancelled)
            {
                coupon.UsedCount++;
                order.OrderCoupons.Add(new OrderCoupon { Coupon = coupon, UserId = buyer.Id });
            }

            db.Orders.Add(order);
        }
    }

    // ---------- helpers ----------
    private static string Placeholder(string text)
        => $"https://dummyimage.com/600x600/eef2ff/1e6fff&text={Uri.EscapeDataString(text)}";

    private static string UniqueSlug(string title, HashSet<string> used)
    {
        var baseSlug = SlugHelper.Generate(title);
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = "san-pham";
        var slug = baseSlug;
        var i = 1;
        while (!used.Add(slug)) slug = $"{baseSlug}-{i++}";
        return slug;
    }

    // ---------- Catalog tĩnh (>=12 máy) ----------
    private record SeedVariant(string Color, string ColorHex, string Storage, decimal Price, int Stock);
    private record SeedSpec(string Group, string Name, string Value);
    private record SeedPhone(string Brand, string Name, string Description,
        List<SeedVariant> Variants, List<SeedSpec> Specs);

    private static SeedSpec S(string group, string name, string value) => new(group, name, value);

    private static List<SeedPhone> PhoneCatalog() => new()
    {
        new("Apple", "iPhone 15 Pro Max",
            "Khung titan, chip A17 Pro, camera tele 5x — flagship mạnh nhất của Apple.",
            new()
            {
                new("Titan Tự Nhiên", "#8f8a80", "256GB", 33_990_000m, 30),
                new("Titan Xanh", "#3a4a5a", "512GB", 39_990_000m, 20),
                new("Titan Đen", "#39393b", "1TB", 45_990_000m, 12),
            },
            new()
            {
                S("Màn hình", "Kích thước", "6.7 inch Super Retina XDR OLED"),
                S("Màn hình", "Tần số quét", "120Hz ProMotion"),
                S("Chip", "Vi xử lý", "Apple A17 Pro"),
                S("Bộ nhớ", "RAM", "8GB"),
                S("Camera", "Camera sau", "48MP + 12MP tele 5x + 12MP siêu rộng"),
                S("Pin & Sạc", "Dung lượng", "4441 mAh, sạc nhanh 27W"),
                S("Kết nối", "SIM", "eSIM + nano SIM, 5G"),
                S("Hệ điều hành", "OS", "iOS 17"),
            }),
        new("Apple", "iPhone 15",
            "iPhone 15 với Dynamic Island, cổng USB-C và camera 48MP.",
            new()
            {
                new("Hồng", "#f7c8d0", "128GB", 21_990_000m, 40),
                new("Xanh Dương", "#a7c7e7", "256GB", 24_990_000m, 25),
            },
            new()
            {
                S("Màn hình", "Kích thước", "6.1 inch Super Retina XDR OLED"),
                S("Chip", "Vi xử lý", "Apple A16 Bionic"),
                S("Bộ nhớ", "RAM", "6GB"),
                S("Camera", "Camera sau", "48MP + 12MP siêu rộng"),
                S("Pin & Sạc", "Dung lượng", "3349 mAh, USB-C"),
                S("Hệ điều hành", "OS", "iOS 17"),
            }),
        new("Apple", "iPhone 14",
            "iPhone 14 chip A15 Bionic, thời lượng pin tốt, giá dễ tiếp cận.",
            new()
            {
                new("Đen", "#1c1c1e", "128GB", 16_990_000m, 35),
                new("Trắng", "#f5f5f0", "256GB", 19_990_000m, 22),
            },
            new()
            {
                S("Màn hình", "Kích thước", "6.1 inch Super Retina XDR OLED"),
                S("Chip", "Vi xử lý", "Apple A15 Bionic"),
                S("Bộ nhớ", "RAM", "6GB"),
                S("Camera", "Camera sau", "12MP + 12MP siêu rộng"),
                S("Pin & Sạc", "Dung lượng", "3279 mAh, Lightning"),
                S("Hệ điều hành", "OS", "iOS 16"),
            }),
        new("Samsung", "Galaxy S24 Ultra",
            "Galaxy S24 Ultra khung titan, bút S Pen, camera 200MP và Galaxy AI.",
            new()
            {
                new("Titan Xám", "#6e6e73", "256GB", 31_990_000m, 25),
                new("Titan Tím", "#8a7fb5", "512GB", 35_990_000m, 15),
                new("Titan Đen", "#2b2b2d", "1TB", 41_990_000m, 8),
            },
            new()
            {
                S("Màn hình", "Kích thước", "6.8 inch Dynamic AMOLED 2X"),
                S("Màn hình", "Tần số quét", "120Hz"),
                S("Chip", "Vi xử lý", "Snapdragon 8 Gen 3 for Galaxy"),
                S("Bộ nhớ", "RAM", "12GB"),
                S("Camera", "Camera sau", "200MP + 50MP tele + 10MP tele + 12MP siêu rộng"),
                S("Pin & Sạc", "Dung lượng", "5000 mAh, sạc 45W"),
                S("Kết nối", "Mạng", "5G, Wi-Fi 7"),
                S("Hệ điều hành", "OS", "Android 14, One UI 6.1"),
            }),
        new("Samsung", "Galaxy S24",
            "Galaxy S24 nhỏ gọn, hiệu năng mạnh và tích hợp Galaxy AI.",
            new()
            {
                new("Vàng", "#e6d3a3", "128GB", 18_990_000m, 30),
                new("Đen", "#2b2b2d", "256GB", 20_990_000m, 20),
            },
            new()
            {
                S("Màn hình", "Kích thước", "6.2 inch Dynamic AMOLED 2X"),
                S("Chip", "Vi xử lý", "Exynos 2400"),
                S("Bộ nhớ", "RAM", "8GB"),
                S("Camera", "Camera sau", "50MP + 10MP tele + 12MP siêu rộng"),
                S("Pin & Sạc", "Dung lượng", "4000 mAh, sạc 25W"),
                S("Hệ điều hành", "OS", "Android 14, One UI 6.1"),
            }),
        new("Samsung", "Galaxy A55 5G",
            "Galaxy A55 khung kim loại, màn hình 120Hz, tầm trung cân đối.",
            new()
            {
                new("Xanh Navy", "#26364f", "128GB", 9_490_000m, 40),
                new("Tím Nhạt", "#c9bfe0", "256GB", 10_990_000m, 25),
            },
            new()
            {
                S("Màn hình", "Kích thước", "6.6 inch Super AMOLED"),
                S("Chip", "Vi xử lý", "Exynos 1480"),
                S("Bộ nhớ", "RAM", "8GB"),
                S("Camera", "Camera sau", "50MP + 12MP siêu rộng + 5MP macro"),
                S("Pin & Sạc", "Dung lượng", "5000 mAh, sạc 25W"),
                S("Hệ điều hành", "OS", "Android 14, One UI 6.1"),
            }),
        new("Xiaomi", "Xiaomi 14",
            "Xiaomi 14 camera Leica, chip Snapdragon 8 Gen 3, sạc siêu nhanh.",
            new()
            {
                new("Đen", "#1c1c1e", "256GB", 22_990_000m, 22),
                new("Trắng", "#f0efe9", "512GB", 25_990_000m, 12),
            },
            new()
            {
                S("Màn hình", "Kích thước", "6.36 inch AMOLED"),
                S("Màn hình", "Tần số quét", "120Hz"),
                S("Chip", "Vi xử lý", "Snapdragon 8 Gen 3"),
                S("Bộ nhớ", "RAM", "12GB"),
                S("Camera", "Camera sau", "50MP Leica + 50MP tele + 50MP siêu rộng"),
                S("Pin & Sạc", "Dung lượng", "4610 mAh, sạc 90W"),
                S("Hệ điều hành", "OS", "Android 14, HyperOS"),
            }),
        new("Xiaomi", "Redmi Note 13 Pro",
            "Redmi Note 13 Pro camera 200MP, màn AMOLED, giá tốt.",
            new()
            {
                new("Xanh Lá", "#3f6f52", "128GB", 6_990_000m, 45),
                new("Đen", "#1c1c1e", "256GB", 7_990_000m, 30),
            },
            new()
            {
                S("Màn hình", "Kích thước", "6.67 inch AMOLED"),
                S("Chip", "Vi xử lý", "Snapdragon 7s Gen 2"),
                S("Bộ nhớ", "RAM", "8GB"),
                S("Camera", "Camera sau", "200MP + 8MP siêu rộng + 2MP macro"),
                S("Pin & Sạc", "Dung lượng", "5100 mAh, sạc 67W"),
                S("Hệ điều hành", "OS", "Android 13, MIUI 14"),
            }),
        new("OPPO", "OPPO Reno11 F 5G",
            "OPPO Reno11 F thiết kế mỏng nhẹ, sạc nhanh SUPERVOOC 67W.",
            new()
            {
                new("Xanh Ngọc", "#2f8f83", "256GB", 8_490_000m, 35),
                new("Đen", "#1c1c1e", "256GB", 8_490_000m, 20),
            },
            new()
            {
                S("Màn hình", "Kích thước", "6.7 inch AMOLED"),
                S("Chip", "Vi xử lý", "MediaTek Dimensity 7050"),
                S("Bộ nhớ", "RAM", "8GB"),
                S("Camera", "Camera sau", "64MP + 8MP siêu rộng + 2MP macro"),
                S("Pin & Sạc", "Dung lượng", "5000 mAh, sạc 67W"),
                S("Hệ điều hành", "OS", "Android 14, ColorOS 14"),
            }),
        new("OPPO", "OPPO Find X7 Ultra",
            "OPPO Find X7 Ultra camera Hasselblad hai tele, flagship nhiếp ảnh.",
            new()
            {
                new("Xanh Dương", "#26507a", "256GB", 24_990_000m, 15),
                new("Nâu", "#6b4f3a", "512GB", 27_990_000m, 8),
            },
            new()
            {
                S("Màn hình", "Kích thước", "6.82 inch AMOLED LTPO"),
                S("Màn hình", "Tần số quét", "120Hz"),
                S("Chip", "Vi xử lý", "Snapdragon 8 Gen 3"),
                S("Bộ nhớ", "RAM", "16GB"),
                S("Camera", "Camera sau", "50MP + 50MP tele 3x + 50MP tele 6x + 50MP siêu rộng"),
                S("Pin & Sạc", "Dung lượng", "5000 mAh, sạc 100W"),
                S("Hệ điều hành", "OS", "Android 14, ColorOS 14"),
            }),
        new("vivo", "vivo V30",
            "vivo V30 màn cong 3D, camera studio Aura Light, pin lớn.",
            new()
            {
                new("Xanh Ngọc", "#5aa9a0", "256GB", 10_990_000m, 30),
                new("Đen", "#1c1c1e", "512GB", 12_490_000m, 15),
            },
            new()
            {
                S("Màn hình", "Kích thước", "6.78 inch AMOLED cong"),
                S("Chip", "Vi xử lý", "Snapdragon 7 Gen 3"),
                S("Bộ nhớ", "RAM", "12GB"),
                S("Camera", "Camera sau", "50MP + 50MP siêu rộng"),
                S("Pin & Sạc", "Dung lượng", "5000 mAh, sạc 80W"),
                S("Hệ điều hành", "OS", "Android 14, Funtouch OS 14"),
            }),
        new("vivo", "vivo Y36",
            "vivo Y36 thiết kế trẻ trung, pin lớn dùng cả ngày.",
            new()
            {
                new("Vàng Gold", "#d8b96a", "128GB", 6_490_000m, 40),
                new("Xanh Dương", "#4a6fa5", "256GB", 7_290_000m, 25),
            },
            new()
            {
                S("Màn hình", "Kích thước", "6.64 inch IPS LCD"),
                S("Chip", "Vi xử lý", "Snapdragon 680"),
                S("Bộ nhớ", "RAM", "8GB"),
                S("Camera", "Camera sau", "50MP + 2MP macro"),
                S("Pin & Sạc", "Dung lượng", "5000 mAh, sạc 44W"),
                S("Hệ điều hành", "OS", "Android 13, Funtouch OS 13"),
            }),
        new("realme", "realme 12 Pro+",
            "realme 12 Pro+ camera tele kính tiềm vọng 64MP, thiết kế cao cấp.",
            new()
            {
                new("Xanh Dương", "#3f6fb0", "256GB", 10_490_000m, 30),
                new("Nâu", "#6b4f3a", "512GB", 11_990_000m, 15),
            },
            new()
            {
                S("Màn hình", "Kích thước", "6.7 inch AMOLED cong"),
                S("Màn hình", "Tần số quét", "120Hz"),
                S("Chip", "Vi xử lý", "Snapdragon 7s Gen 2"),
                S("Bộ nhớ", "RAM", "12GB"),
                S("Camera", "Camera sau", "50MP + 64MP tele periscope + 8MP siêu rộng"),
                S("Pin & Sạc", "Dung lượng", "5000 mAh, sạc 67W"),
                S("Hệ điều hành", "OS", "Android 14, realme UI 5.0"),
            }),
        new("realme", "realme C67",
            "realme C67 camera 108MP, màn 90Hz, tầm giá phổ thông.",
            new()
            {
                new("Đen", "#1c1c1e", "128GB", 5_490_000m, 45),
                new("Xanh Lá", "#3f7a52", "256GB", 6_290_000m, 25),
            },
            new()
            {
                S("Màn hình", "Kích thước", "6.72 inch IPS LCD"),
                S("Màn hình", "Tần số quét", "90Hz"),
                S("Chip", "Vi xử lý", "Snapdragon 685"),
                S("Bộ nhớ", "RAM", "8GB"),
                S("Camera", "Camera sau", "108MP + 2MP"),
                S("Pin & Sạc", "Dung lượng", "5000 mAh, sạc 33W"),
                S("Hệ điều hành", "OS", "Android 14, realme UI 5.0"),
            }),
    };
}
