#nullable disable
using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;
using CustomerProfitabilityWeb.Data;
using CustomerProfitabilityWeb.Models.Entities;
using OfficeOpenXml;

namespace CustomerProfitabilityWeb.Controllers
{
    public class UploadController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public UploadController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Upload/Index
        public IActionResult Index()
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra quyền upload
            var canUpload = HttpContext.Session.GetString("CanUploadData");
            if (canUpload != "True")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.RoleName = HttpContext.Session.GetString("RoleName");

            return View();
        }

        // POST: /Upload/ProcessFile
        [HttpPost]
        public async Task<IActionResult> ProcessFile(IFormFile file)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn file Excel" });
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls")
            {
                return Json(new { success = false, message = "Chỉ chấp nhận file .xlsx hoặc .xls" });
            }

            // Validate file size (max 10MB)
            if (file.Length > 10 * 1024 * 1024)
            {
                return Json(new { success = false, message = "File không được vượt quá 10MB" });
            }

            try
            {
                // Tạo UploadBatch
                var batch = new UploadBatch
                {
                    BatchName = file.FileName,
                    FileName = file.FileName,
                    FileSize = file.Length,
                    UploadedBy = userId.Value,
                    UploadDate = DateTime.Now,
                    Status = "Processing"
                };

                _context.UploadBatch.Add(batch);
                await _context.SaveChangesAsync();

                // Đọc file Excel
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var stream = file.OpenReadStream())
                using (var package = new ExcelPackage(stream))
                {
                    var workbook = package.Workbook;

                    // Đọc 4 sheets
                    var productSheet = workbook.Worksheets["Products"];
                    var customerSheet = workbook.Worksheets["Customers"];
                    var employeeSheet = workbook.Worksheets["Employees"];
                    var salesSheet = workbook.Worksheets["Sales Transactions"];

                    if (salesSheet == null)
                    {
                        return Json(new { success = false, message = "Thiếu sheet 'Sales Transactions'" });
                    }

                    // Import Products
                    int productCount = await ImportProducts(productSheet, batch.BatchID, userId.Value);

                    // Import Customers
                    int customerCount = await ImportCustomers(customerSheet, batch.BatchID, userId.Value);

                    // Import Employees
                    int employeeCount = await ImportEmployees(employeeSheet, batch.BatchID, userId.Value);

                    // Import Sales Transactions
                    int salesCount = await ImportSales(salesSheet, batch.BatchID, userId.Value);

                    // Tính tổng revenue
                    var totalRevenue = await _context.FactSales
                        .Where(x => x.BatchID == batch.BatchID)
                        .SumAsync(x => (decimal?)x.Revenue) ?? 0;

                    // Update batch
                    batch.TotalProducts = productCount;
                    batch.TotalCustomers = customerCount;
                    batch.TotalTransactions = salesCount;
                    batch.TotalRevenue = totalRevenue;
                    batch.Status = "Success";
                    batch.ProcessingTime = (int)(DateTime.Now - batch.UploadDate).TotalSeconds;

                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Upload thành công!",
                        data = new
                        {
                            batchId = batch.BatchID,
                            products = productCount,
                            customers = customerCount,
                            employees = employeeCount,
                            transactions = salesCount,
                            revenue = totalRevenue.ToString("N0")
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // Import Products
        private async Task<int> ImportProducts(ExcelWorksheet sheet, int batchId, int userId)
        {
            if (sheet == null) return 0;

            int count = 0;
            int rowCount = sheet.Dimension?.Rows ?? 0;

            for (int row = 2; row <= rowCount; row++) // Skip header
            {
                var productId = sheet.Cells[row, 1].Text?.Trim();
                if (string.IsNullOrEmpty(productId)) continue;

                var existing = await _context.DimProduct
                    .FirstOrDefaultAsync(x => x.ProductID == productId);

                if (existing == null)
                {
                    var product = new DimProduct
                    {
                        ProductID = productId,
                        ProductName = sheet.Cells[row, 2].Text?.Trim(),
                        BU = sheet.Cells[row, 3].Text?.Trim(),
                        Division = sheet.Cells[row, 4].Text?.Trim(),
                        Industry = sheet.Cells[row, 5].Text?.Trim(),
                        IsActive = true,
                        BatchID = batchId,
                        CreatedBy = userId,
                        CreatedDate = DateTime.Now
                    };

                    _context.DimProduct.Add(product);
                }
                else
                {
                    // Update existing
                    existing.ProductName = sheet.Cells[row, 2].Text?.Trim();
                    existing.BU = sheet.Cells[row, 3].Text?.Trim();
                    existing.Division = sheet.Cells[row, 4].Text?.Trim();
                    existing.Industry = sheet.Cells[row, 5].Text?.Trim();
                    existing.ModifiedDate = DateTime.Now;
                    existing.ModifiedBy = userId;
                }

                count++; // ← THÊM DÒNG NÀY (count cả insert và update)
            }

            await _context.SaveChangesAsync();
            return count;
        }

        // Import Customers
        private async Task<int> ImportCustomers(ExcelWorksheet sheet, int batchId, int userId)
        {
            if (sheet == null) return 0;

            int count = 0;
            int rowCount = sheet.Dimension?.Rows ?? 0;

            for (int row = 2; row <= rowCount; row++)
            {
                var customerId = sheet.Cells[row, 1].Text?.Trim();
                if (string.IsNullOrEmpty(customerId)) continue;

                var existing = await _context.DimCustomer
                    .FirstOrDefaultAsync(x => x.CustomerID == customerId);

                if (existing == null)
                {
                    var customer = new DimCustomer
                    {
                        CustomerID = customerId,
                        CustomerName = sheet.Cells[row, 2].Text?.Trim(),
                        Region = sheet.Cells[row, 3].Text?.Trim(),
                        Province = sheet.Cells[row, 4].Text?.Trim(),
                        District = sheet.Cells[row, 5].Text?.Trim(),
                        Industry = sheet.Cells[row, 6].Text?.Trim(),
                        ExecutiveName = sheet.Cells[row, 7].Text?.Trim(),
                        Email = sheet.Cells[row, 8].Text?.Trim(),
                        PhoneNumber = sheet.Cells[row, 9].Text?.Trim(),
                        IsActive = true,
                        BatchID = batchId,
                        CreatedBy = userId,
                        CreatedDate = DateTime.Now
                    };

                    _context.DimCustomer.Add(customer);
                }
                else
                {
                    existing.CustomerName = sheet.Cells[row, 2].Text?.Trim();
                    existing.Region = sheet.Cells[row, 3].Text?.Trim();
                    existing.Province = sheet.Cells[row, 4].Text?.Trim();
                    existing.ModifiedDate = DateTime.Now;
                    existing.ModifiedBy = userId;
                }

                count++; // ← THÊM DÒNG NÀY
            }

            await _context.SaveChangesAsync();
            return count;
        }

        // Import Employees
        private async Task<int> ImportEmployees(ExcelWorksheet sheet, int batchId, int userId)
        {
            if (sheet == null) return 0;

            int count = 0;
            int rowCount = sheet.Dimension?.Rows ?? 0;

            for (int row = 2; row <= rowCount; row++)
            {
                var executiveId = sheet.Cells[row, 1].Text?.Trim();
                if (string.IsNullOrEmpty(executiveId)) continue;

                var existing = await _context.DimExecutive
                    .FirstOrDefaultAsync(x => x.ExecutiveID == executiveId);

                if (existing == null)
                {
                    var employee = new DimExecutive
                    {
                        ExecutiveID = executiveId,
                        ExecutiveName = sheet.Cells[row, 2].Text?.Trim(),
                        ExecutiveTitle = sheet.Cells[row, 3].Text?.Trim(),
                        Region = sheet.Cells[row, 4].Text?.Trim(),
                        Email = sheet.Cells[row, 5].Text?.Trim(),
                        PhoneNumber = sheet.Cells[row, 6].Text?.Trim(),
                        BatchID = batchId
                    };

                    _context.DimExecutive.Add(employee);
                }

                count++; // ← THÊM DÒNG NÀY
            }

            await _context.SaveChangesAsync();
            return count;
        }

        // Import Sales
        // Import Sales
        private async Task<int> ImportSales(ExcelWorksheet sheet, int batchId, int userId)
        {
            if (sheet == null) return 0;

            int count = 0;
            int rowCount = sheet.Dimension?.Rows ?? 0;

            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var transactionId = sheet.Cells[row, 1].Text?.Trim();
                    var dateText = sheet.Cells[row, 2].Text?.Trim();
                    var productId = sheet.Cells[row, 3].Text?.Trim();
                    var customerId = sheet.Cells[row, 4].Text?.Trim();
                    var executiveId = sheet.Cells[row, 5].Text?.Trim();
                    var scenarioName = sheet.Cells[row, 6].Text?.Trim();

                    if (string.IsNullOrEmpty(transactionId)) continue;

                    // Parse date
                    if (!DateTime.TryParse(dateText, out DateTime transDate))
                        continue;

                    int dateKey = int.Parse(transDate.ToString("yyyyMMdd"));

                    // Find keys
                    var product = await _context.DimProduct
                        .FirstOrDefaultAsync(x => x.ProductID == productId);
                    var customer = await _context.DimCustomer
                        .FirstOrDefaultAsync(x => x.CustomerID == customerId);
                    var executive = await _context.DimExecutive
                        .FirstOrDefaultAsync(x => x.ExecutiveID == executiveId);
                    var scenario = await _context.DimScenario
                        .FirstOrDefaultAsync(x => x.ScenarioName == scenarioName);

                    if (product == null || customer == null || scenario == null)
                        continue;

                    // Find or create location
                    var location = await _context.DimLocation
                        .FirstOrDefaultAsync(x => x.Province == customer.Province);

                    if (location == null)
                    {
                        location = new DimLocation
                        {
                            Region = customer.Region ?? "Unknown",
                            Province = customer.Province ?? "Unknown",
                            District = customer.District ?? ""
                        };
                        _context.DimLocation.Add(location);
                        await _context.SaveChangesAsync();
                    }

                    // Parse numbers
                    decimal.TryParse(sheet.Cells[row, 7].Text, out decimal quantity);
                    decimal.TryParse(sheet.Cells[row, 8].Text, out decimal unitPrice);
                    decimal.TryParse(sheet.Cells[row, 9].Text, out decimal revenue);
                    decimal.TryParse(sheet.Cells[row, 10].Text, out decimal cogs);

                    var sale = new FactSales
                    {
                        DateKey = dateKey,
                        ProductKey = product.ProductKey,
                        CustomerKey = customer.CustomerKey,
                        ExecutiveKey = (int)(executive?.ExecutiveKey ?? (int?)null),  // Nullable int? - OK
                        LocationKey = location.LocationKey,      // Chắc chắn có giá trị
                        ScenarioKey = scenario.ScenarioKey,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        Revenue = revenue,
                        COGS = cogs,
                        BatchID = batchId,
                        TransactionID = transactionId,
                        CreatedBy = userId,
                        CreatedDate = DateTime.Now
                    };

                    _context.FactSales.Add(sale);
                    count++;
                }
                catch (Exception ex)
                {
                    // Log error nếu cần
                    // Console.WriteLine($"Error row {row}: {ex.Message}");
                    continue;
                }
            }

            await _context.SaveChangesAsync();
            return count;
        }

    }
}