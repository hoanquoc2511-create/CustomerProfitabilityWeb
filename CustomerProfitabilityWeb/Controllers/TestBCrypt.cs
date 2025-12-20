using Microsoft.AspNetCore.Mvc;

namespace CustomerProfitabilityWeb.Controllers
{
    public class TestBCryptController : Controller
    {
        // Truy cập: /TestBCrypt/Check
        public IActionResult Check()
        {
            string password = "Admin@123";
            string hash = "$2a$11$zQpY8Z.H3D/P0G5XvW8k1OeJy9K7L5M3N1O2P4Q6R8S0T2U4V6W8Y";

            try
            {
                // Test hash mới
                string newHash = BCrypt.Net.BCrypt.HashPassword(password);

                // Test verify với hash database
                bool isValid = BCrypt.Net.BCrypt.Verify(password, hash);

                return Content($"Password: {password}\n" +
                              $"Hash từ DB: {hash}\n" +
                              $"Hash mới tạo: {newHash}\n\n" +
                              $"Verify kết quả: {isValid}\n\n" +
                              $"BCrypt version: {typeof(BCrypt.Net.BCrypt).Assembly.GetName().Version}");
            }
            catch (Exception ex)
            {
                return Content($"LỖI: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
