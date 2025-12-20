using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CustomerProfitabilityWeb.Data;
using CustomerProfitabilityWeb.Models.ViewModels;
using CustomerProfitabilityWeb.Helpers;

namespace CustomerProfitabilityWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập → chuyển Dashboard
            if (HttpContext.Session.GetInt32("UserID") != null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Tìm user
                var user = await _context.Users
                    .Where(u => u.Username == model.Username && u.IsActive)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    ModelState.AddModelError("", "Tên đăng nhập không tồn tại");
                    return View(model);
                }

                // Load Role
                var role = await _context.Roles.FindAsync(user.RoleID);

                if (role == null)
                {
                    ModelState.AddModelError("", "Lỗi: Không tìm thấy vai trò");
                    return View(model);
                }

                // TẠM TẮT PASSWORD CHECK
                // if (!PasswordHasher.VerifyPassword(model.Password, user.PasswordHash ?? ""))
                // {
                //     ModelState.AddModelError("", "Mật khẩu không đúng");
                //     return View(model);
                // }

                // Lưu Session
                HttpContext.Session.SetInt32("UserID", user.UserID);
                HttpContext.Session.SetString("Username", user.Username ?? "");
                HttpContext.Session.SetString("FullName", user.FullName ?? "User");
                HttpContext.Session.SetInt32("RoleID", user.RoleID);
                HttpContext.Session.SetString("RoleName", role.RoleName ?? "");

                HttpContext.Session.SetString("CanUploadData", role.CanUploadData.ToString());
                HttpContext.Session.SetString("CanViewAllData", role.CanViewAllData.ToString());
                HttpContext.Session.SetString("CanDeleteData", role.CanDeleteData.ToString());
                HttpContext.Session.SetString("CanManageUsers", role.CanManageUsers.ToString());
                HttpContext.Session.SetString("CanUseAI", role.CanUseAI.ToString());

                user.LastLoginDate = DateTime.Now;
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi: {ex.Message}");
                return View(model);
            }
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
