using System;
using System.Collections.Generic;
using System.Linq;
namespace SV22T1020583.BusinessLayers
{
    public static class Configuration
    {
        private static string _connectionString = "";
        /// <summary>
        /// Khởi tạo cấu hình cho Business Layer
        /// (hàm này phải được gọi trước khi chạy ứng dụng)
        /// </summary>
        /// <param name="connectionString"></param>
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }
        /// <summary>
        /// Lấy chuỗi tham số kết nối đến cơ sở dữ liệu
        /// </summary>
        public static string ConnectionString => _connectionString;
    }
}
