using SV22T1020583.DataLayers.Interfaces;
using SV22T1020583.DataLayers.SQLServer;
using SV22T1020583.Models.Security;
using System.Security.Cryptography;
using System.Text;

namespace SV22T1020583.BusinessLayers
{
    /// <summary>
    /// Lớp cung cấp các chức năng liên quan đến bảo mật hệ thống
    /// </summary>
    public static class SecurityDataService
    {
        private static readonly IUserAccountRepository accountDB;

        static SecurityDataService()
        {
            accountDB = new UserAccountRepository(Configuration.ConnectionString);
        }

        /// <summary>
        /// Hàm mã hóa MD5
        /// </summary>
        public static string GetMD5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Kiểm tra đăng nhập
        /// </summary>
        public static async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            // mã hóa password trước khi kiểm tra
            password = GetMD5(password);

            return await accountDB.AuthorizeAsync(userName, password);
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        public static async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            // mã hóa password trước khi lưu
            password = GetMD5(password);

            return await accountDB.ChangePasswordAsync(userName, password);
        }
    }
}