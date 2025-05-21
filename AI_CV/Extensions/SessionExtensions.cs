using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace AI_CV.Extensions
{
    /// <summary>
    /// Các hàm mở rộng để lưu trữ và đọc dữ liệu trong phiên làm việc (session)
    /// </summary>
    public static class SessionExtensions
    {
        /// <summary>
        /// Lưu một đối tượng vào phiên làm việc
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của đối tượng cần lưu</typeparam>
        /// <param name="session">Phiên làm việc hiện tại</param>
        /// <param name="key">Tên để lưu đối tượng</param>
        /// <param name="value">Đối tượng cần lưu</param>
        public static void SetObject<T>(this ISession session, string key, T value)
        {
            // Chuyển đối tượng thành chuỗi JSON và lưu vào phiên
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        /// <summary>
        /// Đọc một đối tượng từ phiên làm việc
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của đối tượng cần đọc</typeparam>
        /// <param name="session">Phiên làm việc hiện tại</param>
        /// <param name="key">Tên của đối tượng cần đọc</param>
        /// <returns>Đối tượng đã đọc, hoặc giá trị mặc định nếu không tìm thấy</returns>
        public static T GetObject<T>(this ISession session, string key)
        {
            // Đọc chuỗi JSON từ phiên
            var value = session.GetString(key);
            // Nếu không có dữ liệu, trả về giá trị mặc định
            // Nếu có, chuyển chuỗi JSON thành đối tượng
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
} 