using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020583.DataLayers.Interfaces;
using SV22T1020583.Models.Common;
using SV22T1020583.Models.HR;
using System.Data;

namespace SV22T1020583.DataLayers.SQLServer
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;

        public EmployeeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection OpenConnection()
        {
            return new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Danh sách nhân viên + tìm kiếm + phân trang
        /// </summary>
        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            using var connection = OpenConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@searchValue", input.SearchValue);
            parameters.Add("@offset", input.Offset);
            parameters.Add("@pageSize", input.PageSize);

            string sqlCount = @"
                SELECT COUNT(*)
                FROM Employees
                WHERE (@searchValue = ''
                       OR FullName LIKE '%' + @searchValue + '%'
                       OR Phone LIKE '%' + @searchValue + '%'
                       OR Email LIKE '%' + @searchValue + '%')
            ";

            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, parameters);

            string sqlData;

            if (input.PageSize == 0)
            {
                sqlData = @"
                    SELECT *
                    FROM Employees
                    WHERE (@searchValue = ''
                           OR FullName LIKE '%' + @searchValue + '%'
                           OR Phone LIKE '%' + @searchValue + '%'
                           OR Email LIKE '%' + @searchValue + '%')
                    ORDER BY FullName
                ";
            }
            else
            {
                sqlData = @"
                    SELECT *
                    FROM Employees
                    WHERE (@searchValue = ''
                           OR FullName LIKE '%' + @searchValue + '%'
                           OR Phone LIKE '%' + @searchValue + '%'
                           OR Email LIKE '%' + @searchValue + '%')
                    ORDER BY FullName
                    OFFSET @offset ROWS
                    FETCH NEXT @pageSize ROWS ONLY
                ";
            }

            var data = await connection.QueryAsync<Employee>(sqlData, parameters);

            return new PagedResult<Employee>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = data.ToList()
            };
        }

        /// <summary>
        /// Lấy 1 nhân viên theo ID
        /// </summary>
        public async Task<Employee?> GetAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"
                SELECT *
                FROM Employees
                WHERE EmployeeID = @id
            ";

            return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { id });
        }

        /// <summary>
        /// Thêm nhân viên
        /// </summary>
        public async Task<int> AddAsync(Employee data)
        {
            using var connection = OpenConnection();

            string sql = @"
                INSERT INTO Employees
                (
                    FullName,
                    BirthDate,
                    Address,
                    Phone,
                    Email,
                    Photo,
                    IsWorking
                )
                VALUES
                (
                    @FullName,
                    @BirthDate,
                    @Address,
                    @Phone,
                    @Email,
                    @Photo,
                    @IsWorking
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
            ";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật nhân viên
        /// </summary>
        public async Task<bool> UpdateAsync(Employee data)
        {
            using var connection = OpenConnection();

            string sql = @"
                UPDATE Employees
                SET
                    FullName = @FullName,
                    BirthDate = @BirthDate,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email,
                    Photo = @Photo,
                    IsWorking = @IsWorking
                WHERE EmployeeID = @EmployeeID
            ";

            int rows = await connection.ExecuteAsync(sql, data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa nhân viên
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"
                DELETE FROM Employees
                WHERE EmployeeID = @id
            ";

            int rows = await connection.ExecuteAsync(sql, new { id });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra nhân viên có dữ liệu liên quan không
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"
                SELECT COUNT(*)
                FROM Orders
                WHERE EmployeeID = @id
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
                sql = @"
                    SELECT COUNT(*)
                    FROM Employees
                    WHERE Email = @email
                ";
            }
            else
            {
                sql = @"
                    SELECT COUNT(*)
                    FROM Employees
                    WHERE Email = @email
                    AND EmployeeID <> @id
                ";
            }

            int count = await connection.ExecuteScalarAsync<int>(sql, new { email, id });

            return count == 0;
        }
    }
}