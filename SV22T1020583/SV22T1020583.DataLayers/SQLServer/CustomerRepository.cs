using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020583.DataLayers.Interfaces;
using SV22T1020583.Models.Common;
using SV22T1020583.Models.Partner;
using System.Data;

namespace SV22T1020583.DataLayers.SQLServer
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        public CustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection OpenConnection()
        {
            return new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Danh sách + tìm kiếm + phân trang
        /// </summary>
        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            using var connection = OpenConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@searchValue", input.SearchValue);
            parameters.Add("@offset", input.Offset);
            parameters.Add("@pageSize", input.PageSize);

            string sqlCount = @"
                SELECT COUNT(*)
                FROM Customers
                WHERE (@searchValue = '' 
                       OR CustomerName LIKE '%' + @searchValue + '%'
                       OR ContactName LIKE '%' + @searchValue + '%'
                       OR Phone LIKE '%' + @searchValue + '%'
                       OR Email LIKE '%' + @searchValue + '%')
            ";

            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, parameters);

            string sqlData;

            if (input.PageSize == 0)
            {
                sqlData = @"
                    SELECT *
                    FROM Customers
                    WHERE (@searchValue = '' 
                           OR CustomerName LIKE '%' + @searchValue + '%'
                           OR ContactName LIKE '%' + @searchValue + '%'
                           OR Phone LIKE '%' + @searchValue + '%'
                           OR Email LIKE '%' + @searchValue + '%')
                    ORDER BY CustomerName
                ";
            }
            else
            {
                sqlData = @"
                    SELECT *
                    FROM Customers
                    WHERE (@searchValue = '' 
                           OR CustomerName LIKE '%' + @searchValue + '%'
                           OR ContactName LIKE '%' + @searchValue + '%'
                           OR Phone LIKE '%' + @searchValue + '%'
                           OR Email LIKE '%' + @searchValue + '%')
                    ORDER BY CustomerName
                    OFFSET @offset ROWS
                    FETCH NEXT @pageSize ROWS ONLY
                ";
            }

            var data = await connection.QueryAsync<Customer>(sqlData, parameters);

            return new PagedResult<Customer>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = data.ToList()
            };
        }

        /// <summary>
        /// Lấy 1 khách hàng theo ID
        /// </summary>
        public async Task<Customer?> GetAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"
                SELECT *
                FROM Customers
                WHERE CustomerID = @id
            ";

            return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { id });
        }

        /// <summary>
        /// Thêm khách hàng
        /// </summary>
        public async Task<int> AddAsync(Customer data)
        {
            using var connection = OpenConnection();

            string sql = @"
                INSERT INTO Customers(CustomerName, ContactName, Province, Address, Phone, Email, Password, IsLocked)
                VALUES(@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @Password, @IsLocked);
                SELECT SCOPE_IDENTITY();
            ";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật khách hàng
        /// </summary>
        public async Task<bool> UpdateAsync(Customer data)
        {
            using var connection = OpenConnection();

            string sql = @"
                UPDATE Customers
                SET CustomerName = @CustomerName,
                    ContactName = @ContactName,
                    Province = @Province,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email,
                    Password = @Password, -- Thêm dòng này nếu cho phép đổi pass ở đây
                    IsLocked = @IsLocked
                WHERE CustomerID = @CustomerID
            ";

            int rows = await connection.ExecuteAsync(sql, data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa khách hàng
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"
                DELETE FROM Customers
                WHERE CustomerID = @id
            ";

            int rows = await connection.ExecuteAsync(sql, new { id });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra khách hàng có dữ liệu liên quan không
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"
                SELECT COUNT(*)
                FROM Orders
                WHERE CustomerID = @id
            ";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });

            return count > 0;
        }

        /// <summary>
        /// Kiểm tra email hợp lệ (không trùng)
        /// </summary>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = OpenConnection();

            string sql;

            if (id == 0)
            {
                //Khách hàng mới
                sql = @"
                    SELECT COUNT(*)
                    FROM Customers
                    WHERE Email = @email
                ";
            }
            else
            {
                //Khách hàng đã tồn tại (update)
                sql = @"
                    SELECT COUNT(*)
                    FROM Customers
                    WHERE Email = @email
                    AND CustomerID <> @id
                ";
            }

            int count = await connection.ExecuteScalarAsync<int>(sql, new { email, id });

            return count == 0;
        }
    }
}