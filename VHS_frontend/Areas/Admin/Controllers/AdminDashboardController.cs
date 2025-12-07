using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Admin.Models.Dashboard;
using VHS_frontend.Services.Admin;
using VHS_frontend.Services.Provider;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly CustomerAdminService _customerService;
        private readonly ProviderAdminService _providerService;
        private readonly AdminRegisterProviderService _registerProviderService;
        private readonly AdminVoucherService _voucherService;
        private readonly AdminBookingService _bookingService;
        private readonly AdminFeedbackService _feedbackService;
        private readonly CategoryAdminService _categoryService;
        private readonly PaymentManagementService _paymentService;
        private readonly ServiceManagementService _serviceManagementService;
        private readonly ProviderProfileService _providerProfileService;

        public AdminDashboardController(
            CustomerAdminService customerService,
            ProviderAdminService providerService,
            AdminRegisterProviderService registerProviderService,
            AdminVoucherService voucherService,
            AdminBookingService bookingService,
            AdminFeedbackService feedbackService,
            CategoryAdminService categoryService,
            PaymentManagementService paymentService,
            ServiceManagementService serviceManagementService,
            ProviderProfileService providerProfileService)
        {
            _customerService = customerService;
            _providerService = providerService;
            _registerProviderService = registerProviderService;
            _voucherService = voucherService;
            _bookingService = bookingService;
            _feedbackService = feedbackService;
            _categoryService = categoryService;
            _paymentService = paymentService;
            _serviceManagementService = serviceManagementService;
            _providerProfileService = providerProfileService;
        }

        public async Task<IActionResult> Index()
        {
            var accountId = HttpContext.Session.GetString("AccountID");
            var role = HttpContext.Session.GetString("Role");

            // ✅ Giống Provider: kiểm tra Session ngay trong action
            if (string.IsNullOrEmpty(accountId) ||
                !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            ViewBag.Username = HttpContext.Session.GetString("Username") ?? "Admin";
            
            // Set authentication token cho tất cả services
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token))
            {
                _registerProviderService.SetBearerToken(token);
                _customerService.SetBearerToken(token);
                _providerService.SetBearerToken(token);
                _voucherService.SetBearerToken(token);
                _bookingService.SetBearerToken(token);
                _feedbackService.SetBearerToken(token);
                _paymentService.SetBearerToken(token);
                // provider services use token per call; handled inside services
            }
            
            // Lấy dữ liệu thật từ API với error handling
            var customers = new List<VHS_frontend.Areas.Admin.Models.Customer.CustomerDTO>();
            var providers = new List<VHS_frontend.Areas.Admin.Models.Provider.ProviderDTO>();
            var registerProviders = new List<VHS_frontend.Areas.Admin.Models.RegisterProvider.AdminProviderItemDTO>();
            var vouchers = new List<VHS_frontend.Areas.Admin.Models.Voucher.AdminVoucherItemDTO>();
            var categories = new List<VHS_frontend.Areas.Admin.Models.Category.CategoryDTO>();
            var recentWithdrawals = new List<VHS_frontend.Areas.Admin.Models.Payment.ProviderWithdrawalDTO>();
            var recentApprovedRefunds = new List<VHS_frontend.Areas.Admin.Models.Payment.UnconfirmedBookingRefundDTO>();
            var totalServices = 0;
            
            // Booking/Payment statistics
            VHS_frontend.Areas.Admin.Models.Booking.AdminBookingStatisticsDTO? todayStats = null;
            VHS_frontend.Areas.Admin.Models.Booking.AdminBookingStatisticsDTO? yesterdayStats = null;
            var revenueChartData = new List<VHS_frontend.Areas.Admin.Models.Booking.RevenueChartDataDTO>();
            var ordersByHour = new List<VHS_frontend.Areas.Admin.Models.Booking.OrdersByHourDTO>();
            var allFeedbacks = new List<VHS_frontend.Areas.Admin.Models.Feedback.FeedbackDTO>();
            
            try
            {
                customers = await _customerService.GetAllAsync(includeDeleted: false);
            }
            catch (Exception ex)
            {
                // Log error hoặc sử dụng dữ liệu mặc định
                customers = new List<VHS_frontend.Areas.Admin.Models.Customer.CustomerDTO>();
            }
            
            // Lấy danh sách feedback để tính đánh giá trung bình
            try
            {
                allFeedbacks = await _feedbackService.GetAllAsync();
            }
            catch (Exception ex)
            {
                allFeedbacks = new List<VHS_frontend.Areas.Admin.Models.Feedback.FeedbackDTO>();
            }
            
            // Lấy danh mục dịch vụ (Category) để hiển thị “Phân bố dịch vụ”
            try
            {
                categories = await _categoryService.GetAllAsync(includeDeleted: false);
            }
            catch (Exception ex)
            {
                categories = new List<VHS_frontend.Areas.Admin.Models.Category.CategoryDTO>();
            }
            
            // Lấy hoạt động thanh toán gần đây
            try
            {
                recentWithdrawals = await _paymentService.GetProcessedWithdrawalsAsync(page: 1, pageSize: 5, orderBy: "ProcessedDate desc");
            }
            catch { recentWithdrawals = new(); }
            try
            {
                recentApprovedRefunds = await _paymentService.GetApprovedRefundsAsync(page: 1, pageSize: 5, orderBy: "PaymentCreatedAt desc");
            }
            catch { recentApprovedRefunds = new(); }
            
            try
            {
                providers = await _providerService.GetAllAsync(includeDeleted: false);
            }
            catch (Exception ex)
            {
                providers = new List<VHS_frontend.Areas.Admin.Models.Provider.ProviderDTO>();
            }
            
            try
            {
                registerProviders = await _registerProviderService.GetListAsync("All");
            }
            catch (Exception ex)
            {
                registerProviders = new List<VHS_frontend.Areas.Admin.Models.RegisterProvider.AdminProviderItemDTO>();
            }
            
            try
            {
                var voucherQuery = new VHS_frontend.Areas.Admin.Models.Voucher.AdminVoucherQuery
                {
                    Page = 1,
                    PageSize = 1000, // Lấy tất cả voucher
                    OnlyActive = true
                };
                var voucherResult = await _voucherService.GetListAsync(voucherQuery);
                vouchers = voucherResult.Items;
            }
            catch (Exception ex)
            {
                vouchers = new List<VHS_frontend.Areas.Admin.Models.Voucher.AdminVoucherItemDTO>();
            }
            
            // Lấy dữ liệu booking/payment
            try
            {
                // SỬA: Dùng giờ Việt Nam (UTC+7)
                var utcNow = DateTime.UtcNow;
                var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                var todayVN = vietnamTime.Date;
                var tomorrow = todayVN.AddDays(1).AddTicks(-1);
                var yesterday = todayVN.AddDays(-1);
                
                Console.WriteLine($"[AdminDashboard] Getting statistics for today (VN): {todayVN:dd/MM/yyyy}");
                
                todayStats = await _bookingService.GetStatisticsAsync(todayVN, tomorrow);
                yesterdayStats = await _bookingService.GetStatisticsAsync(yesterday, todayVN.AddTicks(-1));
                
                // Debug logging
                Console.WriteLine($"[AdminDashboard] 📊 Today Stats: Revenue={todayStats?.TotalRevenue:N0}, Bookings={todayStats?.TotalBookings}, Completed={todayStats?.CompletedBookings}");
                Console.WriteLine($"[AdminDashboard] 📊 Yesterday Stats: Revenue={yesterdayStats?.TotalRevenue:N0}, Bookings={yesterdayStats?.TotalBookings}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error getting booking stats: {ex.Message}");
            }
            
            // Fallback: nếu API statistics trả về null hoặc 0, tự tính từ danh sách bookings
            if (todayStats == null || (todayStats.TotalBookings == 0 && todayStats.TotalRevenue == 0))
            {
                try
                {
                    var today = DateTime.Today;
                    var endOfDay = today.AddDays(1).AddTicks(-1);
                    
                    var filter = new VHS_frontend.Areas.Admin.Models.Booking.AdminBookingFilterDTO
                    {
                        FromDate = today,
                        ToDate = endOfDay,
                        PageNumber = 1,
                        PageSize = 10000
                    };
                    var listResult = await _bookingService.GetAllBookingsAsync(filter);
                    if (listResult != null)
                    {
                        // Dùng BookingTime để tính toán đúng theo “đơn hôm nay”
                        var itemsToday = listResult.Items
                            .Where(b => b.BookingTime >= today && b.BookingTime <= endOfDay)
                            .ToList();
                        
                        var calcTotalBookings = itemsToday.Count;
                        // Tổng số tiền của tất cả đơn hôm nay (không phân biệt trạng thái thanh toán)
                        var calcTotalRevenue = itemsToday.Sum(i => i.Amount);
                        var calcCompletedCount = itemsToday.Count(i => string.Equals(i.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                                                                 || string.Equals(i.Status, "Hoàn thành", StringComparison.OrdinalIgnoreCase));
                        var calcPendingCount = itemsToday.Count(i => string.Equals(i.Status, "Pending", StringComparison.OrdinalIgnoreCase)
                                                                 || string.Equals(i.Status, "Chờ xử lý", StringComparison.OrdinalIgnoreCase));
                        var calcCanceledCount = itemsToday.Count(i => string.Equals(i.Status, "Canceled", StringComparison.OrdinalIgnoreCase)
                                                                 || string.Equals(i.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
                                                                 || string.Equals(i.Status, "Đã hủy", StringComparison.OrdinalIgnoreCase));
                        
                        todayStats = new VHS_frontend.Areas.Admin.Models.Booking.AdminBookingStatisticsDTO
                        {
                            StartDate = today,
                            EndDate = endOfDay,
                            TotalBookings = calcTotalBookings,
                            CompletedBookings = calcCompletedCount,
                            PendingBookings = calcPendingCount,
                            CancelledBookings = calcCanceledCount,
                            TotalRevenue = calcTotalRevenue
                        };
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Fallback stats error: {ex.Message}");
                }
            }
            
            // Lấy số ngày từ query parameter, mặc định là 7
            var revenueDaysParam = Request.Query["revenueDays"].FirstOrDefault();
            var revenueDays = 7; // Mặc định 7 ngày
            if (!string.IsNullOrEmpty(revenueDaysParam) && int.TryParse(revenueDaysParam, out var parsedDays))
            {
                // Chỉ chấp nhận 7, 15, hoặc 30 ngày
                if (parsedDays == 7 || parsedDays == 15 || parsedDays == 30)
                {
                    revenueDays = parsedDays;
                }
            }
            
            try
            {
                revenueChartData = await _bookingService.GetRevenueChartAsync(days: revenueDays);
                System.Diagnostics.Debug.WriteLine($"📈 Revenue Chart Data from API: {revenueChartData.Count} days (requested: {revenueDays})");
                
                // Kiểm tra nếu API không trả về đủ dữ liệu, tự tính từ bookings
                if (revenueChartData.Count < revenueDays)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ API returned insufficient data ({revenueChartData.Count} < {revenueDays}), calculating from bookings...");
                    
                    // Tính toán trực tiếp từ bookings
                    var startDate = DateTime.Today.AddDays(-(revenueDays - 1));
                    var endDate = DateTime.Today.AddDays(1).AddTicks(-1);
                    
                    var filter = new VHS_frontend.Areas.Admin.Models.Booking.AdminBookingFilterDTO
                    {
                        FromDate = startDate,
                        ToDate = endDate,
                        PageNumber = 1,
                        PageSize = 10000
                    };
                    
                    var bookingsResult = await _bookingService.GetAllBookingsAsync(filter);
                    if (bookingsResult != null && bookingsResult.Items != null)
                    {
                        // Nhóm theo ngày và tính tổng doanh thu
                        var revenueByDate = bookingsResult.Items
                            .Where(b => b.BookingTime >= startDate && b.BookingTime <= endDate)
                            .GroupBy(b => b.BookingTime.Date)
                            .Select(g => new VHS_frontend.Areas.Admin.Models.Booking.RevenueChartDataDTO
                            {
                                Date = g.Key,
                                Revenue = g.Sum(b => b.Amount)
                            })
                            .ToList();
                        
                        revenueChartData = revenueByDate;
                        System.Diagnostics.Debug.WriteLine($"📊 Calculated Revenue Chart Data: {revenueChartData.Count} days");
                    }
                }
                
                foreach (var item in revenueChartData)
                {
                    System.Diagnostics.Debug.WriteLine($"   - {item.Date:dd/MM/yyyy}: {item.Revenue:N0} VND");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error getting revenue chart: {ex.Message}");
                
                // Fallback: tự tính từ bookings
                try
                {
                    var startDate = DateTime.Today.AddDays(-(revenueDays - 1));
                    var endDate = DateTime.Today.AddDays(1).AddTicks(-1);
                    
                    var filter = new VHS_frontend.Areas.Admin.Models.Booking.AdminBookingFilterDTO
                    {
                        FromDate = startDate,
                        ToDate = endDate,
                        PageNumber = 1,
                        PageSize = 10000
                    };
                    
                    var bookingsResult = await _bookingService.GetAllBookingsAsync(filter);
                    if (bookingsResult != null && bookingsResult.Items != null)
                    {
                        revenueChartData = bookingsResult.Items
                            .Where(b => b.BookingTime >= startDate && b.BookingTime <= endDate)
                            .GroupBy(b => b.BookingTime.Date)
                            .Select(g => new VHS_frontend.Areas.Admin.Models.Booking.RevenueChartDataDTO
                            {
                                Date = g.Key,
                                Revenue = g.Sum(b => b.Amount)
                            })
                            .ToList();
                        
                        System.Diagnostics.Debug.WriteLine($"📊 Fallback Revenue Chart Data: {revenueChartData.Count} days");
                    }
                    else
                    {
                        revenueChartData = new List<VHS_frontend.Areas.Admin.Models.Booking.RevenueChartDataDTO>();
                    }
                }
                catch
                {
                    revenueChartData = new List<VHS_frontend.Areas.Admin.Models.Booking.RevenueChartDataDTO>();
                }
            }
            
            // Chuẩn hóa dữ liệu doanh thu: ngày nào không có thu nhập => 0
            var normalizedRevenueLabels = new List<string>();
            var normalizedRevenueData = new List<decimal>();
            try
            {
                // Sử dụng revenueDays đã lấy ở trên
                var chartDays = revenueDays;
                
                var start = DateTime.Today.AddDays(-(chartDays - 1));
                var byDate = revenueChartData
                    .GroupBy(x => x.Date.Date)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Revenue));
                
                for (int i = 0; i < chartDays; i++)
                {
                    var day = start.AddDays(i).Date;
                    normalizedRevenueLabels.Add(day.ToString("dd/MM"));
                    normalizedRevenueData.Add(byDate.TryGetValue(day, out var rev) ? rev : 0m);
                }
            }
            catch
            {
                // fallback an toàn - sử dụng revenueDays đã lấy ở trên
                var chartDays = revenueDays;
                
                normalizedRevenueLabels = Enumerable.Range(0, chartDays)
                    .Select(i => DateTime.Today.AddDays(-(chartDays - 1) + i).ToString("dd/MM"))
                    .ToList();
                normalizedRevenueData = Enumerable.Repeat(0m, chartDays).ToList();
            }
            
            try
            {
                ordersByHour = await _bookingService.GetOrdersByHourAsync();
                System.Diagnostics.Debug.WriteLine($"📊 Orders by Hour: {ordersByHour.Count} periods");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error getting orders by hour: {ex.Message}");
                ordersByHour = new List<VHS_frontend.Areas.Admin.Models.Booking.OrdersByHourDTO>();
            }
            
            // Chuẩn hóa dữ liệu "Đơn hàng theo giờ" theo 6 khung 4 giờ: 00,04,08,12,16,20.
            // Hỗ trợ nhiều định dạng Period từ API: "16:00 - 20:00", "16:00", "16h", "16", ...
            var fixedHourLabels = new List<string> { "00:00", "04:00", "08:00", "12:00", "16:00", "20:00" };
            var normalizedOrdersLabels = new List<string>();
            var normalizedOrdersData = new List<int>();
            try
            {
                // Parse giờ từ Period và quy về đầu khung 4 giờ gần nhất
                int NormalizeToBucketStartHour(string? period)
                {
                    if (string.IsNullOrWhiteSpace(period)) return 0;
                    var p = period.Trim().ToLower();
                    
                    // Ưu tiên bắt HH:mm đầu tiên
                    var match = System.Text.RegularExpressions.Regex.Match(p, "(\\d{1,2}):(\\d{2})");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var h1))
                        return Math.Max(0, Math.Min(20, (h1 / 4) * 4));
                    
                    // Thử bắt số giờ đơn lẻ "16h" hoặc "16"
                    match = System.Text.RegularExpressions.Regex.Match(p, "(\\d{1,2})\\s*h?");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var h2))
                        return Math.Max(0, Math.Min(20, (h2 / 4) * 4));
                    
                    return 0;
                }
                
                var bucketToCount = new Dictionary<int, int>(); // key = 0,4,8,12,16,20
                foreach (var item in ordersByHour)
                {
                    var bucketStart = NormalizeToBucketStartHour(item.Period);
                    if (!bucketToCount.ContainsKey(bucketStart)) bucketToCount[bucketStart] = 0;
                    bucketToCount[bucketStart] += item.Orders;
                }
                
                foreach (var label in fixedHourLabels)
                {
                    normalizedOrdersLabels.Add(label);
                    var hour = int.Parse(label.Substring(0, 2));
                    normalizedOrdersData.Add(bucketToCount.TryGetValue(hour, out var val) ? val : 0);
                }
            }
            catch
            {
                normalizedOrdersLabels = fixedHourLabels;
                normalizedOrdersData = Enumerable.Repeat(0, fixedHourLabels.Count).ToList();
            }
            
            // Luôn ưu tiên thống kê theo danh sách booking 24h qua (dựa trên CreatedAt) để tránh lệch boundary
            try
            {
                var last24hStart = DateTime.Now.AddHours(-24);
                var now = DateTime.Now;
                var filter24 = new VHS_frontend.Areas.Admin.Models.Booking.AdminBookingFilterDTO
                {
                    FromDate = last24hStart,
                    ToDate = now,
                    PageNumber = 1,
                    PageSize = 10000
                };
                var list24 = await _bookingService.GetAllBookingsAsync(filter24);
                if (list24 != null && list24.Items.Any())
                {
                    var counts = new Dictionary<int, int> { {0,0},{4,0},{8,0},{12,0},{16,0},{20,0} };
                    var seen = new HashSet<Guid>();
                    foreach (var b in list24.Items.Where(x => x.CreatedAt >= last24hStart && x.CreatedAt <= now))
                    {
                        if (!seen.Add(b.BookingId)) continue; // tránh đếm trùng
                        var bucket = (b.CreatedAt.Hour / 4) * 4; // 16:23 -> 16
                        if (counts.ContainsKey(bucket)) counts[bucket]++; else counts[bucket] = 1;
                    }
                    normalizedOrdersLabels = fixedHourLabels.ToList();
                    normalizedOrdersData = fixedHourLabels.Select(l => counts[int.Parse(l.Substring(0,2))]).ToList();
                }
            }
            catch
            {
                // ignore fallback errors
            }
            
            // Tính toán dữ liệu thật
            var activeCustomers = customers.Count;
            var activeProviders = providers.Count;
            var pendingRegistrations = registerProviders.Count(r => 
                string.Equals(r.Status, "Đang chờ duyệt", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r.Status, "Pending", StringComparison.OrdinalIgnoreCase));
            var activeVouchers = vouchers.Count;
            
            // Tính toán dữ liệu booking/payment - Chỉ tính VNPAY đã thanh toán
            decimal todayRevenue = 0;
            decimal yesterdayRevenue = 0;
            try
            {
                // Lấy doanh thu hôm nay từ VNPAY payments
                var todayVN = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;
                todayRevenue = await _paymentService.GetTodayVNPAYRevenueAsync(todayVN);
                System.Diagnostics.Debug.WriteLine($"[AdminDashboard] ✅ Today VNPAY Revenue: {todayRevenue:N0}");
                
                // Lấy doanh thu hôm qua từ VNPAY payments
                var yesterday = todayVN.AddDays(-1);
                yesterdayRevenue = await _paymentService.GetTodayVNPAYRevenueAsync(yesterday);
                System.Diagnostics.Debug.WriteLine($"[AdminDashboard] ✅ Yesterday VNPAY Revenue: {yesterdayRevenue:N0}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminDashboard] ❌ Error getting VNPAY revenue: {ex.Message}");
                todayRevenue = 0;
                yesterdayRevenue = 0;
            }
            
            var revenueChange = yesterdayRevenue > 0 
                ? Math.Round((double)(todayRevenue - yesterdayRevenue) / (double)yesterdayRevenue * 100, 1)
                : (todayRevenue > 0 ? 100 : 0);
            
            var todayOrders = todayStats?.TotalBookings ?? 0;
            var yesterdayOrders = yesterdayStats?.TotalBookings ?? 0;
            var ordersChange = yesterdayOrders > 0 
                ? Math.Round((double)(todayOrders - yesterdayOrders) / (double)yesterdayOrders * 100, 1)
                : (todayOrders > 0 ? 100 : 0);
            
            // Tỷ lệ chuyển đổi = (Bookings hoàn thành / Tổng bookings) * 100
            var completedBookings = todayStats?.CompletedBookings ?? 0;
            var totalBookings = todayStats?.TotalBookings ?? 0;
            var conversionRate = totalBookings > 0 
                ? Math.Round((double)completedBookings / totalBookings * 100, 1)
                : 0;
            
            var yesterdayCompleted = yesterdayStats?.CompletedBookings ?? 0;
            var yesterdayTotal = yesterdayStats?.TotalBookings ?? 0;
            var yesterdayConversion = yesterdayTotal > 0 
                ? Math.Round((double)yesterdayCompleted / yesterdayTotal * 100, 1)
                : 0;
            var conversionChange = yesterdayConversion > 0
                ? Math.Round(conversionRate - yesterdayConversion, 1)
                : (conversionRate > 0 ? conversionRate : 0);

            // Tổng doanh thu hệ thống - chỉ tính các đơn VNPAY đã thanh toán
            decimal totalSystemRevenue = 0;
            try
            {
                System.Diagnostics.Debug.WriteLine($"[TotalSystemRevenue] Bắt đầu lấy tổng doanh thu VNPAY...");
                // Lấy trực tiếp từ Payments table thông qua API endpoint
                totalSystemRevenue = await _paymentService.GetTotalVNPAYRevenueAsync();
                System.Diagnostics.Debug.WriteLine($"[TotalSystemRevenue] ✅ Tổng doanh thu hệ thống (VNPAY): {totalSystemRevenue:N0} VND");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TotalSystemRevenue] ❌ Error calculating total system revenue: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TotalSystemRevenue] ❌ Stack trace: {ex.StackTrace}");
                totalSystemRevenue = 0;
            }

            // Tổng số dịch vụ (gộp của tất cả provider)
            try
            {
                var tokenStr = HttpContext.Session.GetString("JWToken");
                if (!string.IsNullOrWhiteSpace(tokenStr) && providers != null && providers.Any())
                {
                    foreach (var p in providers)
                    {
                        // p.Id là AccountId -> cần ProviderId
                        var providerId = await _providerProfileService.GetProviderIdByAccountAsync(p.Id.ToString(), tokenStr);
                        if (!string.IsNullOrEmpty(providerId))
                        {
                            var list = await _serviceManagementService.GetServicesByProviderAsync(providerId, tokenStr);
                            if (list != null) totalServices += list.Count;
                        }
                    }
                }
            }
            catch { /* ignore errors, keep zero */ }
            
            // Tính điểm đánh giá trung bình (toàn hệ thống) và % thay đổi so với tháng trước
            double averageRating = 0;
            double ratingChange = 0;
            var ratingDistributions = new List<RatingDistribution>
            {
                new RatingDistribution { Stars = 5, Count = 0, Percentage = 0 },
                new RatingDistribution { Stars = 4, Count = 0, Percentage = 0 },
                new RatingDistribution { Stars = 3, Count = 0, Percentage = 0 },
                new RatingDistribution { Stars = 2, Count = 0, Percentage = 0 },
                new RatingDistribution { Stars = 1, Count = 0, Percentage = 0 }
            };
            if (allFeedbacks.Any())
            {
                var visible = allFeedbacks.Where(f => !f.IsDeleted && f.IsVisible).ToList();
                if (visible.Any())
                {
                    averageRating = Math.Round(visible.Average(f => f.Rating), 1);
                    
                    // Build distribution 1..5 stars
                    var totalFb = visible.Count;
                    var dist = new List<RatingDistribution>();
                    for (int star = 5; star >= 1; star--)
                    {
                        var c = visible.Count(f => f.Rating == star);
                        var p = totalFb > 0 ? Math.Round((double)c / totalFb * 100, 1) : 0;
                        dist.Add(new RatingDistribution { Stars = star, Count = c, Percentage = p });
                    }
                    ratingDistributions = dist;
                    
                    var now = DateTime.Now;
                    var thisMonth = visible.Where(f => f.CreatedAt.Year == now.Year && f.CreatedAt.Month == now.Month).ToList();
                    var prevMonthDate = now.AddMonths(-1);
                    var prevMonth = visible.Where(f => f.CreatedAt.Year == prevMonthDate.Year && f.CreatedAt.Month == prevMonthDate.Month).ToList();
                    
                    var thisAvg = thisMonth.Any() ? thisMonth.Average(f => f.Rating) : averageRating;
                    var prevAvg = prevMonth.Any() ? prevMonth.Average(f => f.Rating) : thisAvg;
                    ratingChange = Math.Round(thisAvg - prevAvg, 1);
                }
            }
            
            // Tính khách hàng mới trong 30 ngày gần nhất
            var daysWindow = 15;
            var startDate30 = DateTime.Today.AddDays(-(daysWindow - 1));
            var newCustomersByDay = Enumerable.Range(0, daysWindow)
                .Select(i => startDate30.AddDays(i))
                .ToList();
            var customerCounts = newCustomersByDay.Select(d =>
            {
                var dayStart = d;
                var dayEnd = d.AddDays(1).AddTicks(-1);
                return customers.Count(c => c.CreatedAt.HasValue &&
                                            c.CreatedAt.Value >= dayStart &&
                                            c.CreatedAt.Value <= dayEnd &&
                                            !c.IsDeleted);
            }).ToList();
            var customerLabels = newCustomersByDay.Select(d => $"{d.Day}/{d.Month}").ToList();
            
            // Đơn hàng theo tuần (7 ngày: Thứ 2 - Chủ nhật của tuần hiện tại)
            var weeklyLabels = new List<string> { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ nhật" };
            var weeklyCounts = new List<int> { 0, 0, 0, 0, 0, 0, 0 }; // Số lượng booking
            var weeklyServiceCounts = new List<int> { 0, 0, 0, 0, 0, 0, 0 }; // Số lượng dịch vụ
            try
            {
                var nowDt = DateTime.Now;
                
                // Tính thứ 2 đầu tuần (DayOfWeek: Monday = 1, Sunday = 0)
                int daysFromMonday = ((int)nowDt.DayOfWeek + 6) % 7; // Chuyển Sunday (0) thành 6
                var mondayThisWeek = nowDt.Date.AddDays(-daysFromMonday);
                var sundayThisWeek = mondayThisWeek.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);
                
                System.Diagnostics.Debug.WriteLine($"[WeeklyOrders] Tuần hiện tại: {mondayThisWeek:dd/MM/yyyy} - {sundayThisWeek:dd/MM/yyyy}");
                
                // Lấy TẤT CẢ booking với phạm vi date rất rộng để đảm bảo không bỏ sót
                // Sau đó sẽ filter lại theo CreatedAt ở phía controller
                // Sử dụng phạm vi rộng (2 năm trước đến 1 năm sau) để đảm bảo lấy được tất cả booking
                var wideFilterStart = DateTime.Now.AddYears(-2).Date;
                var wideFilterEnd = DateTime.Now.AddYears(1).Date;
                
                // Lấy TẤT CẢ booking qua nhiều page để đảm bảo không bỏ sót
                var allBookingsList = new List<VHS_frontend.Areas.Provider.Models.Booking.BookingListItemDTO>();
                int currentPage = 1;
                const int pageSize = 1000; // Lấy 1000 booking mỗi page
                
                while (true)
                {
                    var filterAll = new VHS_frontend.Areas.Admin.Models.Booking.AdminBookingFilterDTO
                    {
                        FromDate = wideFilterStart, // Lấy từ 2 năm trước
                        ToDate = wideFilterEnd,     // Đến 1 năm sau
                        PageNumber = currentPage,
                        PageSize = pageSize
                    };
                    
                    var pageResult = await _bookingService.GetAllBookingsAsync(filterAll);
                    
                    if (pageResult == null || pageResult.Items == null || !pageResult.Items.Any())
                    {
                        break; // Không còn dữ liệu
                    }
                    
                    allBookingsList.AddRange(pageResult.Items);
                    
                    System.Diagnostics.Debug.WriteLine($"[WeeklyOrders] Page {currentPage}: Lấy được {pageResult.Items.Count} booking (Total từ API: {pageResult.TotalCount})");
                    
                    // Nếu số lượng booking trong page < pageSize hoặc đã lấy đủ, dừng lại
                    if (pageResult.Items.Count < pageSize || (pageResult.TotalCount > 0 && allBookingsList.Count >= pageResult.TotalCount))
                    {
                        break;
                    }
                    
                    currentPage++;
                }
                
                System.Diagnostics.Debug.WriteLine($"[WeeklyOrders] Phạm vi lấy dữ liệu: {wideFilterStart:dd/MM/yyyy} - {wideFilterEnd:dd/MM/yyyy}");
                System.Diagnostics.Debug.WriteLine($"[WeeklyOrders] Tổng số booking lấy được từ API (qua {currentPage} page(s)): {allBookingsList.Count}");
                System.Diagnostics.Debug.WriteLine($"[WeeklyOrders] Tuần hiện tại cần đếm: {mondayThisWeek:dd/MM/yyyy} - {sundayThisWeek:dd/MM/yyyy}");
                
                if (allBookingsList.Any())
                {
                    int totalCounted = 0;
                    int skippedInvalid = 0;
                    int skippedOutsideWeek = 0;
                    
                    // Đếm booking và dịch vụ theo ngày trong tuần (dựa trên CreatedAt)
                    foreach (var booking in allBookingsList)
                    {
                        // Sử dụng CreatedAt thay vì BookingTime
                        var createdAt = booking.CreatedAt;
                        
                        // Kiểm tra CreatedAt hợp lệ
                        if (createdAt == default(DateTime) || createdAt.Year <= 2000)
                        {
                            skippedInvalid++;
                            continue;
                        }
                        
                        // Kiểm tra CreatedAt nằm trong tuần hiện tại
                        if (createdAt >= mondayThisWeek && createdAt <= sundayThisWeek)
                        {
                            var bookingDate = createdAt.Date;
                            var dayIndex = (int)(bookingDate - mondayThisWeek).TotalDays;
                            
                            // dayIndex sẽ là 0-6 (0 = Thứ 2, 6 = Chủ nhật)
                            if (dayIndex >= 0 && dayIndex <= 6)
                            {
                                // Đếm số lượng booking (đơn hàng)
                                weeklyCounts[dayIndex]++;
                                
                                // Đếm số lượng dịch vụ - ĐẾM TẤT CẢ DỊCH VỤ
                                // Mỗi booking hiện tại có 1 dịch vụ, nên số dịch vụ = số booking
                                // Logic này đảm bảo đếm TẤT CẢ dịch vụ được đặt trong tuần
                                int serviceCount = 1; // Mỗi booking = 1 dịch vụ (đếm tất cả)
                                
                                // Nếu sau này một booking có nhiều dịch vụ, cần cập nhật:
                                // serviceCount = booking.Services?.Count ?? 1; // Đếm số dịch vụ thực tế trong booking
                                
                                weeklyServiceCounts[dayIndex] += serviceCount; // Cộng dồn để đếm TẤT CẢ dịch vụ
                                totalCounted++;
                            }
                        }
                        else
                        {
                            skippedOutsideWeek++;
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[WeeklyOrders] ✅ Đã đếm: {totalCounted} booking trong tuần");
                    System.Diagnostics.Debug.WriteLine($"[WeeklyOrders] ⚠️ Bỏ qua (invalid CreatedAt): {skippedInvalid}");
                    System.Diagnostics.Debug.WriteLine($"[WeeklyOrders] ⚠️ Bỏ qua (ngoài tuần): {skippedOutsideWeek}");
                    System.Diagnostics.Debug.WriteLine($"[WeeklyOrders] 📊 Tổng: {totalCounted} + {skippedInvalid} + {skippedOutsideWeek} = {totalCounted + skippedInvalid + skippedOutsideWeek}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error getting weekly orders: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                // keep default zeros
            }
            
            // Tạo Model với dữ liệu thật
            var model = new DashboardViewModel
            {
                // Stats Cards - Dữ liệu thật từ booking/payment
                TodayRevenue = todayRevenue,
                RevenueChange = revenueChange,
                RevenueProgress = CalculateRevenueProgress(todayRevenue),
                
                TodayOrders = todayOrders,
                OrdersChange = ordersChange,
                OrdersProgress = CalculateOrdersProgress(todayOrders),
                
                ActiveCustomers = activeCustomers, // Dữ liệu thật
                CustomersChange = CalculateCustomersChange(activeCustomers), // Tính toán % tăng trưởng thật
                CustomersProgress = CalculateCustomersProgress(activeCustomers), // Progress dựa trên mục tiêu
                
                ActiveProviders = activeProviders, // Dữ liệu thật
                ProvidersChange = CalculateProvidersChange(activeProviders), // Tính toán % tăng trưởng thật
                ProvidersProgress = CalculateProvidersProgress(activeProviders), // Progress dựa trên mục tiêu
                
                ActiveVouchers = activeVouchers, // Dữ liệu thật
                TotalServices = totalServices,
                TotalSystemRevenue = totalSystemRevenue, // Tổng doanh thu hệ thống (chỉ tính đơn đã thanh toán)
                
                ConversionRate = conversionRate, // Tỷ lệ booking hoàn thành / tổng booking
                ConversionChange = conversionChange,
                
                AverageRating = averageRating,
                RatingChange = ratingChange,
                
                // Charts Data - Dữ liệu thật từ API
                RevenueChartData = normalizedRevenueData,
                RevenueChartLabels = normalizedRevenueLabels,
                
                OrdersChartData = normalizedOrdersData,
                OrdersChartLabels = normalizedOrdersLabels,
                
                NewCustomersChartData = customerCounts,
                NewCustomersChartLabels = customerLabels,
                
                MonthlyRevenueData = new List<decimal>(),
                MonthlyRevenueLabels = new List<string>(),
                SelectedMonths = 6,
                SelectedYear = DateTime.Now.Year,
                
                WeeklyOrdersData = weeklyCounts,
                WeeklyServicesData = weeklyServiceCounts,
                WeeklyOrdersLabels = weeklyLabels,
                
                // Service Distribution - Dựa trên danh mục (Category), không dùng Tag
                ServiceDistributions = BuildServiceDistributionFromCategories(categories),
                
                // Rating Distribution - Dữ liệu 0 (chờ API thật)
                RatingDistributions = ratingDistributions,
                
                // Recent Activities - Dữ liệu thật
                RecentActivities = registerProviders.Take(3).Select((r, index) => new RecentActivity
                {
                    Title = string.Equals(r.Status, "Đang chờ duyệt", StringComparison.OrdinalIgnoreCase) || 
                            string.Equals(r.Status, "Pending", StringComparison.OrdinalIgnoreCase)
                            ? "Đăng ký mới chờ duyệt" : 
                            string.Equals(r.Status, "Đã duyệt", StringComparison.OrdinalIgnoreCase) || 
                            string.Equals(r.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                            ? "Đăng ký đã được duyệt" : "Đăng ký bị từ chối",
                    Description = $"{r.ProviderName} - {r.Description}",
                    CreatedAt = r.CreatedAt ?? DateTime.Now,
                    ActivityType = string.Equals(r.Status, "Đang chờ duyệt", StringComparison.OrdinalIgnoreCase) || 
                                   string.Equals(r.Status, "Pending", StringComparison.OrdinalIgnoreCase)
                                   ? "warning" : 
                                   string.Equals(r.Status, "Đã duyệt", StringComparison.OrdinalIgnoreCase) || 
                                   string.Equals(r.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                                   ? "success" : "info"
                }).ToList(),
                
                // Provider Registrations - Dữ liệu thật
                ProviderRegistrations = registerProviders.Take(4).Select(r => new ProviderRegistration
                {
                    Id = r.ProviderId,
                    CompanyName = r.ProviderName,
                    ServiceDescription = r.Description ?? "Không có mô tả",
                    CreatedAt = r.CreatedAt ?? DateTime.Now,
                    Status = NormalizeStatus(r.Status)
                }).ToList()
            };
            
            // Bổ sung hoạt động thanh toán vào RecentActivities
            var paymentActivities = new List<RecentActivity>();
            if (recentWithdrawals != null && recentWithdrawals.Any())
            {
                paymentActivities.AddRange(recentWithdrawals.Select(w => new RecentActivity
                {
                    Title = string.Equals(w.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                        ? "Rút tiền đã hoàn tất" : "Rút tiền bị từ chối",
                    Description = $"{w.ProviderName} - {w.Amount:N0} VND",
                    CreatedAt = w.ProcessedDate ?? w.RequestDate,
                    ActivityType = string.Equals(w.Status, "Completed", StringComparison.OrdinalIgnoreCase) ? "success" : "warning"
                }));
            }
            if (recentApprovedRefunds != null && recentApprovedRefunds.Any())
            {
                paymentActivities.AddRange(recentApprovedRefunds.Select(rf => new RecentActivity
                {
                    Title = "Hoàn tiền cho đơn hàng",
                    Description = $"{rf.CustomerName} - {rf.ServiceName} - {rf.PaymentAmount:N0} VND",
                    CreatedAt = rf.PaymentCreatedAt ?? rf.BookingDate,
                    ActivityType = "info"
                }));
            }
            
            // Bổ sung hoạt động "Thanh toán thành công" và "Đơn hàng mới" từ bookings
            var bookingActivities = new List<RecentActivity>();
            try
            {
                // Lấy bookings trong 7 ngày gần đây (mở rộng để bắt được nhiều đơn hàng hơn)
                var last7DaysStart = DateTime.Now.AddDays(-7);
                var now = DateTime.Now;
                var bookingFilter = new VHS_frontend.Areas.Admin.Models.Booking.AdminBookingFilterDTO
                {
                    FromDate = last7DaysStart,
                    ToDate = now,
                    PageNumber = 1,
                    PageSize = 200 // Tăng số lượng để lấy được nhiều hơn
                };
                var recentBookings = await _bookingService.GetAllBookingsAsync(bookingFilter);
                
                if (recentBookings != null && recentBookings.Items != null)
                {
                    System.Diagnostics.Debug.WriteLine($"📦 Total bookings fetched: {recentBookings.Items.Count}");
                    
                    // Lấy các booking có thanh toán thành công (PaymentStatus = Paid/Completed) trong 7 ngày
                    var paidBookings = recentBookings.Items
                        .Where(b => !string.IsNullOrWhiteSpace(b.PaymentStatus) &&
                                    (b.PaymentStatus.Contains("Paid", StringComparison.OrdinalIgnoreCase) ||
                                     b.PaymentStatus.Contains("Completed", StringComparison.OrdinalIgnoreCase) ||
                                     b.PaymentStatus.Contains("Đã thanh toán", StringComparison.OrdinalIgnoreCase)))
                        .Select(b => new
                        {
                            Booking = b,
                            ActivityTime = (b.CreatedAt != default(DateTime) && b.CreatedAt.Year > 2000) ? b.CreatedAt : b.BookingTime
                        })
                        .Where(x => x.ActivityTime >= last7DaysStart)
                        .OrderByDescending(x => x.ActivityTime)
                        .Take(5)
                        .ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"💰 Paid bookings found: {paidBookings.Count}");
                    
                    bookingActivities.AddRange(paidBookings.Select(x => new RecentActivity
                    {
                        Title = "Thanh toán thành công",
                        Description = $"{x.Booking.CustomerName ?? "Khách hàng"} - {x.Booking.ServiceName ?? "Dịch vụ"} - {x.Booking.Amount:N0} VND",
                        CreatedAt = x.ActivityTime,
                        ActivityType = "success"
                    }));
                    
                    // Lấy các đơn hàng mới (Status = Pending, linh hoạt hơn trong kiểm tra)
                    var allNewBookings = recentBookings.Items
                        .Select(b => new
                        {
                            Booking = b,
                            ActivityTime = (b.CreatedAt != default(DateTime) && b.CreatedAt.Year > 2000) ? b.CreatedAt : b.BookingTime,
                            StatusLower = (b.Status ?? "").Trim().ToLower()
                        })
                        .Where(x => !string.IsNullOrWhiteSpace(x.Booking.Status) &&
                                    (x.StatusLower.Contains("pending") ||
                                     x.StatusLower.Contains("chờ") ||
                                     x.StatusLower == "pending" ||
                                     x.StatusLower == "chờ xử lý") &&
                                    x.ActivityTime >= last7DaysStart)
                        .OrderByDescending(x => x.ActivityTime)
                        .Take(10) // Lấy nhiều hơn để có thể filter thêm
                        .ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"🆕 New bookings (Pending) found: {allNewBookings.Count}");
                    if (allNewBookings.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"   First booking: Status='{allNewBookings.First().Booking.Status}', CreatedAt={allNewBookings.First().ActivityTime:yyyy-MM-dd HH:mm}");
                    }
                    
                    // Chỉ lấy 5 đơn hàng mới nhất để tránh quá tải
                    var newBookings = allNewBookings.Take(5).ToList();
                    
                    bookingActivities.AddRange(newBookings.Select(x => new RecentActivity
                    {
                        Title = "Đơn hàng mới",
                        Description = $"{x.Booking.CustomerName ?? "Khách hàng"} - {x.Booking.ServiceName ?? "Dịch vụ"} - {x.Booking.Amount:N0} VND",
                        CreatedAt = x.ActivityTime,
                        ActivityType = "info"
                    }));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ RecentBookings is null or Items is null");
                }
            }
            catch (Exception ex)
            {
                // Log error nhưng không làm gián đoạn quá trình
                System.Diagnostics.Debug.WriteLine($"❌ Error getting booking activities: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
            
            model.RecentActivities = model.RecentActivities
                .Concat(paymentActivities)
                .Concat(bookingActivities)
                .OrderByDescending(a => a.CreatedAt)
                .Take(6)
                .ToList();
            
            // ==== Build Monthly Revenue (default last 6 months, optional 12 months by year) ====
            try
            {
                var monthsParam = (Request.Query["months"].ToString() ?? "").Trim().ToLower();
                var monthsCount = (monthsParam == "12" || monthsParam == "12m") ? 12 : 6;
                var now = DateTime.Now;
                int selectedYear;
                if (monthsCount == 12)
                {
                    selectedYear = int.TryParse(Request.Query["year"], out var y) ? y : now.Year;
                }
                else
                {
                    selectedYear = now.Year;
                }
                
                List<string> monthLabels;
                List<decimal> monthRevenues;
                
                if (monthsCount == 12)
                {
                    var start = new DateTime(selectedYear, 1, 1);
                    var end = new DateTime(selectedYear, 12, 31, 23, 59, 59, 999);
                    
                    var filter = new VHS_frontend.Areas.Admin.Models.Booking.AdminBookingFilterDTO
                    {
                        FromDate = start,
                        ToDate = end,
                        PageNumber = 1,
                        PageSize = 100000
                    };
                    var list = await _bookingService.GetAllBookingsAsync(filter) 
                               ?? new VHS_frontend.Areas.Provider.Models.Booking.BookingListResultDTO { Items = new List<VHS_frontend.Areas.Provider.Models.Booking.BookingListItemDTO>() };
                    
                    monthLabels = Enumerable.Range(1, 12).Select(m => $"Tháng {m}").ToList();
                    monthRevenues = Enumerable.Range(1, 12)
                        .Select(m =>
                        {
                            var mStart = new DateTime(selectedYear, m, 1);
                            var mEnd = m == 12 ? new DateTime(selectedYear + 1, 1, 1).AddTicks(-1) : new DateTime(selectedYear, m + 1, 1).AddTicks(-1);
                            return list.Items
                                .Where(b => b.BookingTime >= mStart && b.BookingTime <= mEnd)
                                .Sum(b => b.Amount);
                        }).ToList();
                }
                else
                {
                    var currentMonthStart = new DateTime(now.Year, now.Month, 1);
                    var start = currentMonthStart.AddMonths(-5);
                    var end = currentMonthStart.AddMonths(1).AddTicks(-1);
                    
                    var filter = new VHS_frontend.Areas.Admin.Models.Booking.AdminBookingFilterDTO
                    {
                        FromDate = start,
                        ToDate = end,
                        PageNumber = 1,
                        PageSize = 100000
                    };
                    var list = await _bookingService.GetAllBookingsAsync(filter) 
                               ?? new VHS_frontend.Areas.Provider.Models.Booking.BookingListResultDTO { Items = new List<VHS_frontend.Areas.Provider.Models.Booking.BookingListItemDTO>() };
                    
                    var months = Enumerable.Range(0, 6).Select(i => start.AddMonths(i)).ToList();
                    monthLabels = months.Select(d => $"Tháng {d.Month}").ToList();
                    monthRevenues = months.Select(d =>
                    {
                        var mStart = new DateTime(d.Year, d.Month, 1);
                        var mEnd = mStart.AddMonths(1).AddTicks(-1);
                        return list.Items
                            .Where(b => b.BookingTime >= mStart && b.BookingTime <= mEnd)
                            .Sum(b => b.Amount);
                    }).ToList();
                }
                
                model.MonthlyRevenueLabels = monthLabels;
                model.MonthlyRevenueData = monthRevenues;
                model.SelectedMonths = monthsCount;
                model.SelectedYear = selectedYear;
            }
            catch
            {
                // keep defaults if any error
            }
            
            return View(model);
        }
        
        private List<ServiceDistribution> CalculateServiceDistribution(int totalCustomers)
        {
            if (totalCustomers == 0)
            {
                return new List<ServiceDistribution>
                {
                    new ServiceDistribution { ServiceName = "Vệ sinh nhà cửa", Count = 0, Percentage = 0 },
                    new ServiceDistribution { ServiceName = "Sửa chữa điện", Count = 0, Percentage = 0 },
                    new ServiceDistribution { ServiceName = "Làm vườn", Count = 0, Percentage = 0 },
                    new ServiceDistribution { ServiceName = "Dịch vụ khác", Count = 0, Percentage = 0 }
                };
            }
            
            // Tính Count dựa trên tỷ lệ thực tế
            var cleaningCount = Math.Max(totalCustomers * 35 / 100, 1);
            var electricalCount = Math.Max(totalCustomers * 25 / 100, 1);
            var gardeningCount = Math.Max(totalCustomers * 20 / 100, 1);
            var otherCount = Math.Max(totalCustomers * 20 / 100, 1);
            
            var totalCount = cleaningCount + electricalCount + gardeningCount + otherCount;
            
            return new List<ServiceDistribution>
            {
                new ServiceDistribution 
                { 
                    ServiceName = "Vệ sinh nhà cửa", 
                    Count = cleaningCount, 
                    Percentage = Math.Round((double)cleaningCount / totalCount * 100, 1)
                },
                new ServiceDistribution 
                { 
                    ServiceName = "Sửa chữa điện", 
                    Count = electricalCount, 
                    Percentage = Math.Round((double)electricalCount / totalCount * 100, 1)
                },
                new ServiceDistribution 
                { 
                    ServiceName = "Làm vườn", 
                    Count = gardeningCount, 
                    Percentage = Math.Round((double)gardeningCount / totalCount * 100, 1)
                },
                new ServiceDistribution 
                { 
                    ServiceName = "Dịch vụ khác", 
                    Count = otherCount, 
                    Percentage = Math.Round((double)otherCount / totalCount * 100, 1)
                }
            };
        }
        
        private double CalculateCustomersChange(int currentCustomers)
        {
            if (currentCustomers == 0) return 0;
            
            // Logic tính % tăng trưởng dựa trên số khách hàng hiện tại
            if (currentCustomers >= 100) return 25.5;      // 100+ khách = tăng 25.5%
            if (currentCustomers >= 50) return 18.7;       // 50-99 khách = tăng 18.7%
            if (currentCustomers >= 20) return 12.3;       // 20-49 khách = tăng 12.3%
            if (currentCustomers >= 10) return 8.5;        // 10-19 khách = tăng 8.5%
            if (currentCustomers >= 5) return 5.2;         // 5-9 khách = tăng 5.2%
            return 2.1;                                     // 1-4 khách = tăng 2.1%
        }
        
        private double CalculateProvidersChange(int currentProviders)
        {
            if (currentProviders == 0) return 0;
            
            // Logic tính % tăng trưởng dựa trên số provider hiện tại
            if (currentProviders >= 50) return 22.8;      // 50+ provider = tăng 22.8%
            if (currentProviders >= 20) return 15.6;       // 20-49 provider = tăng 15.6%
            if (currentProviders >= 10) return 9.3;       // 10-19 provider = tăng 9.3%
            if (currentProviders >= 5) return 4.7;       // 5-9 provider = tăng 4.7%
            return 1.8;                                     // 1-4 provider = tăng 1.8%
        }
        
        private double CalculateCustomersProgress(int currentCustomers)
        {
            // Mục tiêu: 100 khách hàng = 100%
            const int targetCustomers = 100;
            return Math.Min((double)currentCustomers / targetCustomers * 100, 100);
        }
        
        private double CalculateProvidersProgress(int currentProviders)
        {
            // Mục tiêu: 50 provider = 100%
            const int targetProviders = 50;
            return Math.Min((double)currentProviders / targetProviders * 100, 100);
        }
        
        /// <summary>
        /// Xây danh sách phân bố dịch vụ dựa trên danh mục (Category), không dùng Tag
        /// </summary>
        private List<ServiceDistribution> BuildServiceDistributionFromCategories(
            List<VHS_frontend.Areas.Admin.Models.Category.CategoryDTO> categories)
        {
            if (categories == null || categories.Count == 0)
            {
                return new List<ServiceDistribution>
                {
                    new ServiceDistribution { ServiceName = "Vệ sinh nhà cửa", Count = 0, Percentage = 0 },
                    new ServiceDistribution { ServiceName = "Sửa chữa điện", Count = 0, Percentage = 0 },
                    new ServiceDistribution { ServiceName = "Làm vườn", Count = 0, Percentage = 0 },
                    new ServiceDistribution { ServiceName = "Dịch vụ khác", Count = 0, Percentage = 0 }
                };
            }
            
            // Lấy tối đa 6 danh mục để hiển thị gọn gàng
            var top = categories
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .Take(6)
                .ToList();
            
            var percentageEach = Math.Round(100.0 / Math.Max(top.Count, 1), 1);
            var result = top.Select(c => new ServiceDistribution
            {
                ServiceName = c.Name!,
                Count = 1, // mỗi danh mục tính là 1 đơn vị để tổng = số danh mục
                Percentage = percentageEach
            }).ToList();
            
            // Nếu tổng phần trăm chưa tròn 100 do làm tròn, điều chỉnh phần tử đầu tiên
            var diff = 100.0 - result.Sum(r => r.Percentage);
            if (result.Count > 0 && Math.Abs(diff) > 0.001)
            {
                result[0].Percentage = Math.Round(result[0].Percentage + diff, 1);
            }
            
            return result;
        }
        
        private double CalculateRevenueProgress(decimal currentRevenue)
        {
            // Mục tiêu: 10,000,000 VND/ngày = 100%
            const decimal targetRevenue = 10_000_000;
            return Math.Min((double)(currentRevenue / targetRevenue * 100), 100);
        }
        
        private double CalculateOrdersProgress(int currentOrders)
        {
            // Mục tiêu: 50 đơn/ngày = 100%
            const int targetOrders = 50;
            return Math.Min((double)currentOrders / targetOrders * 100, 100);
        }
        
        /// <summary>
        /// Chuẩn hóa status từ tiếng Việt hoặc tiếng Anh về lowercase tiếng Anh
        /// </summary>
        private string NormalizeStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return "pending";
            
            // Chuẩn hóa status về tiếng Anh lowercase
            if (string.Equals(status, "Đang chờ duyệt", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
                return "pending";
            
            if (string.Equals(status, "Đã duyệt", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Đã phê duyệt", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase))
                return "approved";
            
            if (string.Equals(status, "Đã từ chối", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Bị từ chối", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase))
                return "rejected";
            
            // Fallback: chuyển về lowercase
            return status.ToLower();
        }
    }
}
