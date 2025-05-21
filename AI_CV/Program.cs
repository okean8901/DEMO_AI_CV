using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AI_CV.Services;
using AI_CV.Models;
using System;

namespace AI_CV
{
    /// <summary>
    /// Lớp chính của ứng dụng, chứa điểm khởi đầu và cấu hình
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Điểm khởi đầu của ứng dụng
        /// </summary>
        public static void Main(string[] args)
        {
            // Tạo builder để cấu hình ứng dụng
            var builder = WebApplication.CreateBuilder(args);

            // Cấu hình các dịch vụ cần thiết cho ứng dụng
            // 1. Thêm hỗ trợ MVC (Model-View-Controller)
            builder.Services.AddControllersWithViews();

            // 2. Cấu hình Session để lưu trữ thông tin người dùng
            builder.Services.AddSession(options =>
            {
                // Session sẽ hết hạn sau 30 phút không hoạt động
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                // Cookie chỉ có thể truy cập qua HTTP, không qua JavaScript
                options.Cookie.HttpOnly = true;
                // Cookie này là bắt buộc cho ứng dụng
                options.Cookie.IsEssential = true;
            });

            // 3. Đăng ký dịch vụ phân tích CV
            // Dịch vụ này sử dụng các API của Azure để phân tích CV
            builder.Services.AddScoped<ICVAnalysisService>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                return new CVAnalysisService(
                    // Cấu hình các thông tin xác thực cho các dịch vụ Azure
                    configuration["Azure:ComputerVision:SubscriptionKey"],
                    configuration["Azure:ComputerVision:Endpoint"],
                    configuration["Azure:FormRecognizer:Endpoint"],
                    configuration["Azure:FormRecognizer:Key"],
                    configuration["Azure:TextAnalytics:Endpoint"],
                    configuration["Azure:TextAnalytics:Key"]
                );
            });

            // Xây dựng ứng dụng
            var app = builder.Build();

            // Cấu hình pipeline xử lý HTTP request
            if (!app.Environment.IsDevelopment())
            {
                // Trong môi trường production:
                // 1. Xử lý lỗi bằng trang Error
                app.UseExceptionHandler("/Home/Error");
                // 2. Bật HSTS (HTTP Strict Transport Security)
                app.UseHsts();
            }

            // Cấu hình các middleware
            // 1. Chuyển hướng HTTP sang HTTPS
            app.UseHttpsRedirection();
            // 2. Cho phép truy cập các file tĩnh (CSS, JS, images)
            app.UseStaticFiles();
            // 3. Bật routing
            app.UseRouting();
            // 4. Bật xác thực và phân quyền
            app.UseAuthorization();
            // 5. Bật session
            app.UseSession();

            // Cấu hình routing mặc định
            // Pattern: /Controller/Action/Id
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Chạy ứng dụng
            app.Run();
        }
    }
}
