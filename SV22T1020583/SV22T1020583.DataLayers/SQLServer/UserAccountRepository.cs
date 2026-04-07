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
            string sql = @"
                SELECT CAST(EmployeeID as nvarchar) as UserID, Email as UserName, 
                       FullName as DisplayName, Email, Photo, RoleNames
                FROM Employees
                WHERE Email = @userName AND Password = @password AND IsWorking = 1
                UNION
                SELECT CAST(CustomerID as nvarchar) as UserID, Email as UserName, 
                       CustomerName as DisplayName, Email, N'' as Photo, N'customer' as RoleNames
                FROM Customers
                WHERE Email = @userName AND Password = @password AND IsLocked = 0";

            return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new { userName, password });
        }

        /// <summary>
        /// Đổi mật khẩu cho Tài khoản
        /// </summary>
        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using var connection = OpenConnection();

            // Thử cập nhật cho bảng Employees trước
            string sqlEmployee = @"UPDATE Employees SET Password = @password WHERE Email = @userName";
            int rows = await connection.ExecuteAsync(sqlEmployee, new { userName, password });

            // Nếu không tìm thấy Email đó trong Employees (rows == 0), thử cập nhật cho Customers
            if (rows == 0)
            {
                string sqlCustomer = @"UPDATE Customers SET Password = @password WHERE Email = @userName";
                rows = await connection.ExecuteAsync(sqlCustomer, new { userName, password });
            }

            return rows > 0;
        }
        /// <summary>
        /// Đổi vai trò của tài khoản
        /// </summary>
        public async Task<bool> SaveRoleAsync(string userName, string roles)
        {
            using var connection = OpenConnection();
            string sql = @"UPDATE Employees 
                   SET RoleNames = @roles 
                   WHERE Email = @userName";
            int rows = await connection.ExecuteAsync(sql, new { roles, userName });
            return rows > 0;
        }
    }
}