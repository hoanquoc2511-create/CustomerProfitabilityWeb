using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CustomerProfitabilityWeb.Data;

namespace CustomerProfitabilityWeb.Controllers
{
    public class HistoryController : Controller
    {
        private readonly AppDbContext _context;

        public HistoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /History/Index
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.RoleName = HttpContext.Session.GetString("RoleName");

            var batches = await (from b in _context.UploadBatch
                                 where !b.IsDeleted
                                 orderby b.UploadDate descending
                                 select new
                                 {
                                     b.BatchID,
                                     b.FileName,
                                     b.FileSize,
                                     b.UploadDate,
                                     b.TotalProducts,
                                     b.TotalCustomers,
                                     b.TotalTransactions,
                                     b.TotalRevenue,
                                     b.Status,
                                     b.ProcessingTime,
                                     UserFullName = (from u in _context.Users
                                                     where u.UserID == b.UploadedBy
                                                     select u.FullName).FirstOrDefault() ?? "Unknown"
                                 }).ToListAsync();

            return View(batches);
        }

        // GET: /History/Details/5  
        public async Task<IActionResult> Details(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.RoleName = HttpContext.Session.GetString("RoleName");

            try
            {
                // Query đơn giản nhất
                var batchData = await _context.UploadBatch
                    .Where(b => b.BatchID == id)
                    .Select(b => new
                    {
                        b.BatchID,
                        b.BatchName,
                        b.FileName,
                        b.FileSize,
                        b.UploadDate,
                        b.UploadedBy,
                        b.TotalProducts,
                        b.TotalCustomers,
                        b.TotalTransactions,
                        b.TotalRevenue,
                        b.Status,
                        b.ProcessingTime,
                        b.IsDeleted
                    })
                    .FirstOrDefaultAsync();

                if (batchData == null || batchData.IsDeleted)
                {
                    return NotFound();
                }

                // Tạo model mới
                var batch = new CustomerProfitabilityWeb.Models.Entities.UploadBatch
                {
                    BatchID = batchData.BatchID,
                    BatchName = batchData.BatchName ?? "",
                    FileName = batchData.FileName,
                    FileSize = batchData.FileSize,
                    UploadDate = batchData.UploadDate,
                    UploadedBy = batchData.UploadedBy,
                    TotalProducts = batchData.TotalProducts,
                    TotalCustomers = batchData.TotalCustomers,
                    TotalTransactions = batchData.TotalTransactions,
                    TotalRevenue = batchData.TotalRevenue,
                    Status = batchData.Status,
                    ProcessingTime = batchData.ProcessingTime
                };

                // Lấy username
                var userName = await _context.Users
                    .Where(u => u.UserID == batchData.UploadedBy)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync();

                ViewBag.UserName = userName ?? "Unknown";

                return View(batch);
            }
            catch (Exception ex)
            {
                // Log error
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }

        // POST: /History/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var canDelete = HttpContext.Session.GetString("CanDeleteData");
            if (canDelete != "True")
            {
                return Json(new { success = false, message = "Bạn không có quyền xóa dữ liệu" });
            }

            try
            {
                var batch = await _context.UploadBatch.FindAsync(id);
                if (batch == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy batch" });
                }

                batch.IsDeleted = true;
                batch.DeletedBy = userId;
                batch.DeletedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xóa batch thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
    }
}