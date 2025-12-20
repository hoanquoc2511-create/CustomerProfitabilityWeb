using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CustomerProfitabilityWeb.Data;

namespace CustomerProfitabilityWeb.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Dashboard/Index
        public async Task<IActionResult> Index()
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy thông tin từ Session
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.RoleName = HttpContext.Session.GetString("RoleName");

            // ========== TÍNH TOÁN KPI ==========

            // 1. Tổng số khách hàng
            var totalCustomers = await _context.DimCustomer
                .Where(x => x.IsActive)
                .CountAsync();

            // 2. Tổng doanh thu (Actual) - DÙNG JOIN
            var totalRevenue = await (from fs in _context.FactSales
                                      join s in _context.DimScenario on fs.ScenarioKey equals s.ScenarioKey
                                      where s.ScenarioName == "Actual"
                                      select fs.Revenue).SumAsync();

            // 3. Biên lợi nhuận trung bình - DÙNG JOIN
            var avgMargin = await (from fs in _context.FactSales
                                   join s in _context.DimScenario on fs.ScenarioKey equals s.ScenarioKey
                                   where s.ScenarioName == "Actual" && fs.Revenue > 0
                                   select fs.GrossProfitMarginPct ?? 0).AverageAsync();

            // 4. Tổng số giao dịch - DÙNG JOIN
            var totalTransactions = await (from fs in _context.FactSales
                                           join s in _context.DimScenario on fs.ScenarioKey equals s.ScenarioKey
                                           where s.ScenarioName == "Actual"
                                           select fs).CountAsync();

            // 5. Doanh thu Budget - DÙNG JOIN
            var budgetRevenue = await (from fs in _context.FactSales
                                       join s in _context.DimScenario on fs.ScenarioKey equals s.ScenarioKey
                                       where s.ScenarioName == "Budget"
                                       select fs.Revenue).SumAsync();

            // 6. YoY Growth (giả sử so với năm trước)
            var currentYearRevenue = totalRevenue;
            var lastYearRevenue = currentYearRevenue * 0.8m; // Giả sử năm trước thấp hơn 20%
            var yoyGrowth = lastYearRevenue > 0
                ? ((currentYearRevenue - lastYearRevenue) / lastYearRevenue) * 100
                : 0;

            // Truyền KPI vào ViewBag
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.AvgMargin = avgMargin;
            ViewBag.TotalTransactions = totalTransactions;
            ViewBag.BudgetRevenue = budgetRevenue;
            ViewBag.YoYGrowth = yoyGrowth;

            return View();
        }

        // API: Lấy dữ liệu cho biểu đồ
        [HttpGet]
        public async Task<IActionResult> GetChartData(string chartType)
        {
            try
            {
                switch (chartType)
                {
                    case "revenue-by-product":
                        return Json(await GetRevenueByProduct());

                    case "revenue-by-month":
                        return Json(await GetRevenueByMonth());

                    case "margin-by-product":
                        return Json(await GetMarginByProduct());

                    case "revenue-by-region":
                        return Json(await GetRevenueByRegion());

                    case "revenue-by-province":  // ← THÊM DÒNG NÀY
                        return await GetRevenueByProvince();  // ← VÀ DÒNG NÀY

                    default:
                        return Json(new { success = false, message = "Invalid chart type" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Doanh thu theo sản phẩm 
        private async Task<object> GetRevenueByProduct()
        {
            // Tính tổng doanh thu cho MỖI sản phẩm (không phân biệt Actual/Budget)
            var productTotals = await (from fs in _context.FactSales
                                       join p in _context.DimProduct on fs.ProductKey equals p.ProductKey
                                       group fs by p.ProductName into g
                                       select new
                                       {
                                           ProductName = g.Key,
                                           TotalRevenue = g.Sum(x => x.Revenue)
                                       })
                                      .OrderByDescending(x => x.TotalRevenue)
                                      .Take(10)
                                      .ToListAsync();

            var topProductNames = productTotals.Select(x => x.ProductName).ToList();

            //Lấy chi tiết Actual & Budget cho 10 sản phẩm đó
            var data = await (from fs in _context.FactSales
                              join p in _context.DimProduct on fs.ProductKey equals p.ProductKey
                              join s in _context.DimScenario on fs.ScenarioKey equals s.ScenarioKey
                              where topProductNames.Contains(p.ProductName)
                              group fs by new { p.ProductName, s.ScenarioName } into g
                              select new
                              {
                                  ProductName = g.Key.ProductName,
                                  Scenario = g.Key.ScenarioName,
                                  Revenue = g.Sum(x => x.Revenue)
                              })
                             .ToListAsync();

            // Đảm bảo mỗi sản phẩm có CẢ Actual VÀ Budget (điền 0 nếu thiếu)
            var result = new List<object>();
            foreach (var productName in topProductNames)
            {
                var actualRevenue = data.FirstOrDefault(x => x.ProductName == productName && x.Scenario == "Actual")?.Revenue ?? 0;
                var budgetRevenue = data.FirstOrDefault(x => x.ProductName == productName && x.Scenario == "Budget")?.Revenue ?? 0;

                result.Add(new
                {
                    ProductName = productName,
                    Scenario = "Actual",
                    Revenue = actualRevenue
                });

                result.Add(new
                {
                    ProductName = productName,
                    Scenario = "Budget",
                    Revenue = budgetRevenue
                });
            }

            return new { success = true, data = result };
        }

        // Doanh thu theo tháng
        private async Task<object> GetRevenueByMonth()
        {
            var data = await (from fs in _context.FactSales
                              join s in _context.DimScenario on fs.ScenarioKey equals s.ScenarioKey
                              group fs by new
                              {
                                  Year = fs.DateKey / 10000,
                                  Month = (fs.DateKey % 10000) / 100,
                                  s.ScenarioName
                              } into g
                              select new
                              {
                                  Year = g.Key.Year,
                                  Month = g.Key.Month,
                                  Scenario = g.Key.ScenarioName,
                                  Revenue = g.Sum(x => x.Revenue)
                              })
                              .OrderBy(x => x.Year)
                              .ThenBy(x => x.Month)
                              .ToListAsync();

            return new { success = true, data };
        }

        // Biên lợi nhuận theo sản phẩm
        private async Task<object> GetMarginByProduct()
        {
            var data = await (from fs in _context.FactSales
                              join p in _context.DimProduct on fs.ProductKey equals p.ProductKey
                              join s in _context.DimScenario on fs.ScenarioKey equals s.ScenarioKey
                              where s.ScenarioName == "Actual"
                              group fs by p.ProductName into g
                              select new
                              {
                                  ProductName = g.Key,
                                  AvgMargin = g.Average(x => x.GrossProfitMarginPct ?? 0)
                              })
                              .OrderByDescending(x => x.AvgMargin)
                              .Take(10)
                              .ToListAsync();

            return new { success = true, data };
        }

        // Doanh thu theo khu vực
        private async Task<object> GetRevenueByRegion()
        {
            var data = await (from fs in _context.FactSales
                              join l in _context.DimLocation on fs.LocationKey equals l.LocationKey
                              join s in _context.DimScenario on fs.ScenarioKey equals s.ScenarioKey
                              where s.ScenarioName == "Actual"
                              group fs by l.Region into g
                              select new
                              {
                                  Region = g.Key,
                                  Revenue = g.Sum(x => x.Revenue)
                              })
                              .OrderByDescending(x => x.Revenue)
                              .ToListAsync();

            return new { success = true, data };
        }

        // Chart 5: Revenue by Province (Top 20)
        private async Task<IActionResult> GetRevenueByProvince()
        {
            var data = await (from fs in _context.FactSales
                              join l in _context.DimLocation on fs.LocationKey equals l.LocationKey
                              group fs by l.Province into g
                              select new
                              {
                                  province = g.Key,
                                  revenue = g.Sum(x => x.Revenue)
                              })
                             .OrderByDescending(x => x.revenue)
                             .Take(20)
                             .ToListAsync();

            return Json(new { success = true, data });
        }

        // GET: /Dashboard/GetInsights
        public async Task<IActionResult> GetInsights(string chartType)
        {
            try
            {
                string insights = "";

                switch (chartType)
                {
                    case "revenue-by-region":
                        insights = await GenerateRegionInsights();
                        break;

                    case "revenue-by-product":
                        insights = await GenerateProductInsights();
                        break;

                    case "margin-by-product":
                        insights = await GenerateMarginInsights();
                        break;

                    case "revenue-by-month":
                        insights = await GenerateMonthlyInsights();
                        break;

                    default:
                        insights = "Chưa có phân tích cho biểu đồ này.";
                        break;
                }

                return Json(new { success = true, insights });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Phân tích theo Khu vực
        private async Task<string> GenerateRegionInsights()
        {
            var regionData = await (from fs in _context.FactSales
                                    join l in _context.DimLocation on fs.LocationKey equals l.LocationKey
                                    join s in _context.DimScenario on fs.ScenarioKey equals s.ScenarioKey
                                    where s.ScenarioName == "Actual"
                                    group fs by l.Region into g
                                    select new
                                    {
                                        Region = g.Key,
                                        Revenue = g.Sum(x => x.Revenue),
                                        Transactions = g.Count(),
                                        AvgMargin = g.Average(x => x.GrossProfitMarginPct ?? 0)
                                    })
                                   .OrderByDescending(x => x.Revenue)
                                   .ToListAsync();

            if (!regionData.Any()) return "Chưa có dữ liệu để phân tích.";

            var best = regionData.First();
            var worst = regionData.Last();
            var total = regionData.Sum(x => x.Revenue);
            var bestPercent = (best.Revenue / total * 100);
            var worstPercent = (worst.Revenue / total * 100);

            return $@"
 <strong>Phân tích Khu vực:</strong>

 <strong class='text-success'>Khu vực tốt nhất: {best.Region}</strong>
- Doanh thu: {(best.Revenue / 1_000_000_000):N2} tỷ VNĐ ({bestPercent:N1}% tổng doanh thu)
- Số giao dịch: {best.Transactions:N0} đơn hàng
- Biên lợi nhuận: {best.AvgMargin:N1}%

<strong>Tại sao thành công?</strong>
✓ Mật độ dân cư cao, thị trường lớn
✓ Hệ thống phân phối tốt
✓ Sức mua cao, nhu cầu ổn định

<strong>Đề xuất để tăng trưởng:</strong>
→ Mở rộng mạng lưới cửa hàng/đại lý
→ Đầu tư marketing địa phương
→ Phát triển sản phẩm cao cấp cho phân khúc này

 <strong class='text-warning'>Khu vực cần cải thiện: {worst.Region}</strong>
- Doanh thu: {(worst.Revenue / 1_000_000_000):N2} tỷ VNĐ (chỉ {worstPercent:N1}% tổng doanh thu)
- Số giao dịch: {worst.Transactions:N0} đơn hàng

<strong>Nguyên nhân tiềm ẩn:</strong>
- Hệ thống phân phối chưa rộng khắp
- Nhận diện thương hiệu thấp
- Cạnh tranh cao từ đối thủ địa phương

<strong>Chiến lược đột phá:</strong>
→ Khảo sát nhu cầu địa phương chi tiết
→ Hợp tác với nhà phân phối/đại lý địa phương
→ Chương trình khuyến mãi đặc biệt để tăng nhận diện
→ Điều chỉnh sản phẩm phù hợp với thị hiếu địa phương
→ Đào tạo đội ngũ bán hàng am hiểu thị trường này
";
        }

        // Phân tích theo Sản phẩm
        private async Task<string> GenerateProductInsights()
        {
            var productData = await (from fs in _context.FactSales
                                     join p in _context.DimProduct on fs.ProductKey equals p.ProductKey
                                     join s in _context.DimScenario on fs.ScenarioKey equals s.ScenarioKey
                                     where s.ScenarioName == "Actual"
                                     group fs by p.ProductName into g
                                     select new
                                     {
                                         Product = g.Key,
                                         Revenue = g.Sum(x => x.Revenue),
                                         Quantity = g.Count(),
                                         AvgMargin = g.Average(x => x.GrossProfitMarginPct ?? 0)
                                     })
                                    .OrderByDescending(x => x.Revenue)
                                    .Take(10)
                                    .ToListAsync();

            if (!productData.Any()) return "Chưa có dữ liệu để phân tích.";

            var top = productData.First();
            var total = productData.Sum(x => x.Revenue);
            var topPercent = (top.Revenue / total * 100);

            return $@"
 <strong>Phân tích Sản phẩm:</strong>

 <strong class='text-success'>Sản phẩm bán chạy nhất: {top.Product}</strong>
- Doanh thu: {(top.Revenue / 1_000_000):N0}M VNĐ ({topPercent:N1}% trong top 10)
- Số lượng bán: {top.Quantity:N0} đơn vị
- Biên lợi nhuận: {top.AvgMargin:N1}%

<strong>Yếu tố thành công:</strong>
✓ Đáp ứng nhu cầu thị trường
✓ Chất lượng tốt, giá cạnh tranh
✓ Có lợi thế so với đối thủ

<strong>Đề xuất tăng trưởng:</strong>
→ Tăng tồn kho để tránh hết hàng
→ Bundle với sản phẩm bổ trợ
→ Cross-selling với sản phẩm khác
→ Phát triển phiên bản cao cấp/giá rẻ hơn

 <strong>Cơ hội từ sản phẩm khác:</strong>
→ Phân tích sản phẩm có margin cao nhưng doanh thu thấp
→ Đầu tư marketing cho sản phẩm tiềm năng
→ Tối ưu giá bán và chi phí sản xuất
";
        }

        // Phân tích Biên lợi nhuận
        private async Task<string> GenerateMarginInsights()
        {
            var marginData = await (from fs in _context.FactSales
                                    join p in _context.DimProduct on fs.ProductKey equals p.ProductKey
                                    where fs.Revenue > 0
                                    group fs by p.ProductName into g
                                    select new
                                    {
                                        Product = g.Key,
                                        AvgMargin = g.Average(x => x.GrossProfitMarginPct ?? 0),
                                        Revenue = g.Sum(x => x.Revenue)
                                    })
                                   .OrderByDescending(x => x.AvgMargin)
                                   .Take(10)
                                   .ToListAsync();

            if (!marginData.Any()) return "Chưa có dữ liệu để phân tích.";

            var bestMargin = marginData.First();
            var worstMargin = marginData.Last();

            return $@"
 <strong>Phân tích Biên lợi nhuận:</strong>

 <strong class='text-success'>Sản phẩm có margin tốt nhất: {bestMargin.Product}</strong>
- Biên lợi nhuận: {bestMargin.AvgMargin:N1}%
- Doanh thu: {(bestMargin.Revenue / 1_000_000):N0}M VNĐ

<strong>Chiến lược tối ưu:</strong>
→ Đây là sản phẩm ""ngôi sao"" - ưu tiên đẩy mạnh bán hàng
→ Tăng volume để tối đa hóa lợi nhuận
→ Giữ vững chất lượng và giá

<strong class='text-warning'>Sản phẩm margin thấp: {worstMargin.Product}</strong>
- Biên lợi nhuận: {worstMargin.AvgMargin:N1}%
- Doanh thu: {(worstMargin.Revenue / 1_000_000):N0}M VNĐ

<strong>Hành động cần làm:</strong>
→ Đàm phán lại giá với nhà cung cấp
→ Tối ưu chi phí vận hành
→ Cân nhắc tăng giá bán nếu thị trường chấp nhận
→ Nếu không cải thiện được: loại bỏ hoặc chỉ bán theo đơn đặt hàng

 <strong>Cơ hội tăng lợi nhuận tổng thể:</strong>
→ Tập trung nguồn lực vào top 3 sản phẩm margin cao
→ Phát triển sản phẩm tương tự với margin tốt
→ Giảm tỷ trọng sản phẩm margin kém
";
        }

        // Phân tích theo Tháng
        private async Task<string> GenerateMonthlyInsights()
        {
            var monthlyData = await (from fs in _context.FactSales
                                     join d in _context.DimDate on fs.DateKey equals d.DateKey
                                     join s in _context.DimScenario on fs.ScenarioKey equals s.ScenarioKey
                                     where s.ScenarioName == "Actual"
                                     group fs by new { d.Year, d.Month } into g
                                     select new
                                     {
                                         Year = g.Key.Year,
                                         Month = g.Key.Month,
                                         Revenue = g.Sum(x => x.Revenue)
                                     })
                                    .OrderBy(x => x.Year)
                                    .ThenBy(x => x.Month)
                                    .ToListAsync();

            if (monthlyData.Count < 2) return "Chưa đủ dữ liệu để phân tích xu hướng.";

            var latest = monthlyData.Last();
            var previous = monthlyData[monthlyData.Count - 2];
            var growth = ((latest.Revenue - previous.Revenue) / previous.Revenue * 100);

            var bestMonth = monthlyData.OrderByDescending(x => x.Revenue).First();
            var worstMonth = monthlyData.OrderBy(x => x.Revenue).First();

            return $@"
 <strong>Phân tích Xu hướng Doanh thu:</strong>

 <strong>Tháng gần nhất ({latest.Month}/{latest.Year}):</strong>
- Doanh thu: {(latest.Revenue / 1_000_000):N0}M VNĐ
- So với tháng trước: {(growth >= 0 ? "+" : "")}{growth:N1}%

 <strong class='text-success'>Tháng tốt nhất: {bestMonth.Month}/{bestMonth.Year}</strong>
- Doanh thu: {(bestMonth.Revenue / 1_000_000):N0}M VNĐ

 <strong class='text-warning'>Tháng thấp nhất: {worstMonth.Month}/{worstMonth.Year}</strong>
- Doanh thu: {(worstMonth.Revenue / 1_000_000):N0}M VNĐ

<strong>Xu hướng & Dự báo:</strong>
{(growth > 5 ? "✓ Tăng trưởng tích cực - Duy trì momentum" :
          growth > 0 ? "→ Tăng trưởng ổn định - Cần đẩy mạnh hơn" :
          " Doanh thu giảm - Cần hành động khẩn cấp")}

<strong>Đề xuất hành động:</strong>
→ Phân tích nguyên nhân các tháng cao điểm/thấp điểm
→ Chuẩn bị chiến dịch marketing cho mùa cao điểm
→ Tối ưu tồn kho theo chu kỳ mùa vụ
→ Xây dựng kế hoạch khuyến mãi cho tháng yếu
";
        }
    }
}