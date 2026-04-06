using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020583.DataLayers.Interfaces;
using SV22T1020583.Models.Security;
using System.Data;

namespace SV22T1020583.DataLayers.SQLServer
{
    public class UserAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        public UserAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection OpenConnection()
        {
            return new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Kiểm tra đăng nhập cho Khách hàng (Shop)
        /// </summary>
        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using var connection = OpenConnection();

            // Sửa Users thành Customers và map đúng các cột
            string sql = @"
                SELECT 
                    CustomerID as UserID,
                    Email as UserName,
                    CustomerName as DisplayName,
                    Email as Email,
                    N'' as Photo,
                    N'customer' as RoleNames
                FROM Customers
                WHERE Email = @userName
                AND Password = @password
                AND IsLocked = 0
            ";

            return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql,
                new { userName, password });
        }

        /// <summary>
        /// Đổi mật khẩu cho Khách hàng
        /// </summary>
        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using var connection = OpenConnection();

            // Sửa Users thành Customers
            string sql = @"
                UPDATE Customers
                SET Password = @password
                WHERE Email = @userName
            ";

            int rows = await connection.ExecuteAsync(sql,
                new { userName, password });

            return rows > 0;
        }
    }
}