using Microsoft.AspNetCore.Mvc;

namespace CustomerProfitabilityWeb.Controllers
{
    public class GenerateHashController : Controller
    {
        // Truy cập: /GenerateHash/Create
        public IActionResult Create()
        {
            var adminHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
            var managerHash = BCrypt.Net.BCrypt.HashPassword("Manager@123");
            var userHash = BCrypt.Net.BCrypt.HashPassword("User@123");

            return Content($"-- Copy 3 lệnh SQL này và chạy trong SSMS:\n\n" +
                          $"UPDATE Users SET PasswordHash = '{adminHash}' WHERE Username = 'admin';\n" +
                          $"UPDATE Users SET PasswordHash = '{managerHash}' WHERE Username = 'manager1';\n" +
                          $"UPDATE Users SET PasswordHash = '{userHash}' WHERE Username = 'user1';\n\n" +
                          $"-- Test verify:\n" +
                          $"Admin verify: {BCrypt.Net.BCrypt.Verify("Admin@123", adminHash)}");
        }
    }
}
