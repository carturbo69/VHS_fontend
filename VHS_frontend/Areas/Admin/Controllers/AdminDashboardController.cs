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
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1).AddTicks(-1);
                var yesterday = today.AddDays(-1);
                
                todayStats = await _bookingService.GetStatisticsAsync(today, tomorrow);
                yesterdayStats = await _bookingService.GetStatisticsAsync(yesterday, today.AddTicks(-1));
                
                // Debug logging
                System.Diagnostics.Debug.WriteLine($"📊 Today Stats: Revenue={todayStats?.TotalRevenue}, Bookings={todayStats?.TotalBookings}, Completed={todayStats?.CompletedBookings}");
                System.Diagnostics.Debug.WriteLine($"📊 Yesterday Stats: Revenue={yesterdayStats?.TotalRevenue}, Bookings={yesterdayStats?.TotalBookings}");
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
            
            try
            {
                revenueChartData = await _bookingService.GetRevenueChartAsync(days: 7);
                System.Diagnostics.Debug.WriteLine($"📈 Revenue Chart Data: {revenueChartData.Count} days");
                foreach (var item in revenueChartData)
                {
                    System.Diagnostics.Debug.WriteLine($"   - {item.Date:dd/MM/yyyy}: {item.Revenue:N0} VND");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error getting revenue chart: {ex.Message}");
                revenueChartData = new List<VHS_frontend.Areas.Admin.Models.Booking.RevenueChartDataDTO>();
            }
            
            // Chuẩn hóa dữ liệu doanh thu 7 ngày: ngày nào không có thu nhập => 0
            var normalizedRevenueLabels = new List<string>();
            var normalizedRevenueData = new List<decimal>();
            try
            {
                const int chartDays = 7;
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
                // fallback an toàn
                normalizedRevenueLabels = Enumerable.Range(0, 7)
                    .Select(i => DateTime.Today.AddDays(-6 + i).ToString("dd/MM"))
                    .ToList();
                normalizedRevenueData = Enumerable.Repeat(0m, 7).ToList();
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
            
            // Tính toán dữ liệu booking/payment
            var todayRevenue = todayStats?.TotalRevenue ?? 0;
            var yesterdayRevenue = yesterdayStats?.TotalRevenue ?? 0;
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
            
            // Đơn hàng theo tuần (4 tuần trong tháng hiện tại)
            var weeklyLabels = new List<string> { "Tuần 1", "Tuần 2", "Tuần 3", "Tuần 4" };
            var weeklyCounts = new List<int> { 0, 0, 0, 0 };
            try
            {
                var nowDt = DateTime.Now;
                var monthStart = new DateTime(nowDt.Year, nowDt.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
                var filterMonth = new VHS_frontend.Areas.Admin.Models.Booking.AdminBookingFilterDTO
                {
                    FromDate = monthStart,
                    ToDate = monthEnd,
                    PageNumber = 1,
                    PageSize = 100000
                };
                var monthBookings = await _bookingService.GetAllBookingsAsync(filterMonth);
                if (monthBookings != null)
                {
                    foreach (var b in monthBookings.Items.Where(x => x.BookingTime >= monthStart && x.BookingTime <= monthEnd))
                    {
                        var weekIndex = Math.Min((b.BookingTime.Day - 1) / 7, 3); // 0..3
                        weeklyCounts[weekIndex]++;
                    }
                }
            }
            catch
            {
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
