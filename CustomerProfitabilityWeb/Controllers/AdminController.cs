using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CustomerProfitabilityWeb.Data;
using CustomerProfitabilityWeb.Models.Entities;

namespace CustomerProfitabilityWeb.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Users()
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra quyền admin
            var canManageUsers = HttpContext.Session.GetString("CanManageUsers");
            if (canManageUsers != "True")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.RoleName = HttpContext.Session.GetString("RoleName");

            // Lấy danh sách users với role
            var users = await (from u in _context.Users
                               join r in _context.Roles on u.RoleID equals r.RoleID
                               orderby u.CreatedDate descending
                               select new
                               {
                                   u.UserID,
                                   u.Username,
                                   u.FullName,
                                   u.Email,
                                   RoleName = r.RoleName,
                                   u.IsActive,
                                   u.CreatedDate,
                                   LastLogin = u.LastLoginDate
                               }).ToListAsync();

            return View(users);
        }

        // GET: /Admin/UserDetails/5
        public async Task<IActionResult> UserDetails(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.RoleName = HttpContext.Session.GetString("RoleName");

            var user = await (from u in _context.Users
                              join r in _context.Roles on u.RoleID equals r.RoleID
                              where u.UserID == id
                              select new
                              {
                                  u.UserID,
                                  u.Username,
                                  u.FullName,
                                  u.Email,
                                  u.PhoneNumber,
                                  u.Department,
                                  u.RoleID,
                                  RoleName = r.RoleName,
                                  u.IsActive,
                                  u.CreatedDate,
                                  LastLogin = u.LastLoginDate
                              }).FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            // Lấy danh sách roles cho dropdown
            ViewBag.Roles = await _context.Roles.ToListAsync();

            return View(user);
        }

        // POST: /Admin/CreateUser
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var canManageUsers = HttpContext.Session.GetString("CanManageUsers");
            if (canManageUsers != "True")
            {
                return Json(new { success = false, message = "Không có quyền" });
            }

            try
            {
                // Kiểm tra username đã tồn tại
                var exists = await _context.Users.AnyAsync(u => u.Username == model.Username);
                if (exists)
                {
                    return Json(new { success = false, message = "Username đã tồn tại" });
                }

                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = model.Password, // TODO: Hash password
                    FullName = model.FullName,
                    Email = model.Email,
                    RoleID = model.RoleID,
                    CreatedDate = DateTime.Now,
                    CreatedBy = userId.Value,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Tạo user thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // POST: /Admin/UpdateUser
        [HttpPost]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var canManageUsers = HttpContext.Session.GetString("CanManageUsers");
            if (canManageUsers != "True")
            {
                return Json(new { success = false, message = "Không có quyền" });
            }

            try
            {
                var user = await _context.Users.FindAsync(model.UserID);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy user" });
                }

                user.FullName = model.FullName;
                user.Email = model.Email;
                user.PhoneNumber = model.PhoneNumber;
                user.Department = model.Department;
                user.RoleID = model.RoleID;
                user.IsActive = model.IsActive;
                user.ModifiedDate = DateTime.Now;
                user.ModifiedBy = userId.Value;

                // Chỉ update password nếu có
                if (!string.IsNullOrEmpty(model.Password))
                {
                    user.PasswordHash = model.Password; // TODO: Hash password
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // POST: /Admin/DeleteUser/5
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var canManageUsers = HttpContext.Session.GetString("CanManageUsers");
            if (canManageUsers != "True")
            {
                return Json(new { success = false, message = "Không có quyền" });
            }

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy user" });
                }

                // Không cho xóa chính mình
                if (user.UserID == userId)
                {
                    return Json(new { success = false, message = "Không thể xóa chính mình" });
                }

                // Xóa thật luôn (không có soft delete)
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa user thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // POST: /Admin/ToggleUserStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy user" });
                }

                user.IsActive = !user.IsActive;
                user.ModifiedDate = DateTime.Now;
                user.ModifiedBy = userId.Value;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = user.IsActive ? "Đã kích hoạt user" : "Đã vô hiệu hóa user",
                    isActive = user.IsActive
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
    }

    // Models cho API
    public class CreateUserModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int RoleID { get; set; }
    }

    public class UpdateUserModel
    {
        public int UserID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Department { get; set; }
        public int RoleID { get; set; }
        public bool IsActive { get; set; }
        public string Password { get; set; }
    }
}