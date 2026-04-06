using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020583.DataLayers.Interfaces;
using SV22T1020583.Models.Common;
using SV22T1020583.Models.Partner;
using System.Data;

namespace SV22T1020583.DataLayers.SQLServer
{
    public class SupplierRepository : IGenericRepository<Supplier>
    {
        private readonly string _connectionString;

        public SupplierRepository(string connectionString)
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
        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            using var connection = OpenConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@searchValue", input.SearchValue);
            parameters.Add("@offset", input.Offset);
            parameters.Add("@pageSize", input.PageSize);

            string sqlCount = @"
                SELECT COUNT(*)
                FROM Suppliers
                WHERE (@searchValue = '' 
                       OR SupplierName LIKE '%' + @searchValue + '%'
                       OR ContactName LIKE '%' + @searchValue + '%')
            ";

            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, parameters);

            string sqlData;

            if (input.PageSize == 0)
            {
                sqlData = @"
                    SELECT *
                    FROM Suppliers
                    WHERE (@searchValue = '' 
                           OR SupplierName LIKE '%' + @searchValue + '%'
                           OR ContactName LIKE '%' + @searchValue + '%')
                    ORDER BY SupplierName
                ";
            }
            else
            {
                sqlData = @"
                    SELECT *
                    FROM Suppliers
                    WHERE (@searchValue = '' 
                           OR SupplierName LIKE '%' + @searchValue + '%'
                           OR ContactName LIKE '%' + @searchValue + '%')
                    ORDER BY SupplierName
                    OFFSET @offset ROWS
                    FETCH NEXT @pageSize ROWS ONLY
                ";
            }

            var data = await connection.QueryAsync<Supplier>(sqlData, parameters);

            return new PagedResult<Supplier>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = data.ToList()
            };
        }

        /// <summary>
        /// Lấy 1 supplier theo ID
        /// </summary>
        public async Task<Supplier?> GetAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"
                SELECT *
                FROM Suppliers
                WHERE SupplierID = @id
            ";

            return await connection.QueryFirstOrDefaultAsync<Supplier>(sql, new { id });
        }

        /// <summary>
        /// Thêm supplier
        /// </summary>
        public async Task<int> AddAsync(Supplier data)
        {
            using var connection = OpenConnection();

            string sql = @"
                INSERT INTO Suppliers
                (
                    SupplierName,
                    ContactName,
                    Province,
                    Address,
                    Phone,
                    Email
                )
                VALUES
                (
                    @SupplierName,
                    @ContactName,
                    @Province,
                    @Address,
                    @Phone,
                    @Email
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
            ";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật supplier
        /// </summary>
        public async Task<bool> UpdateAsync(Supplier data)
        {
            using var connection = OpenConnection();

            string sql = @"
                UPDATE Suppliers
                SET
                    SupplierName = @SupplierName,
                    ContactName = @ContactName,
                    Province = @Province,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email
                WHERE SupplierID = @SupplierID
            ";

            int rows = await connection.ExecuteAsync(sql, data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa supplier
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"
                DELETE FROM Suppliers
                WHERE SupplierID = @id
            ";

            int rows = await connection.ExecuteAsync(sql, new { id });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra supplier có đang được sử dụng không
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"
                SELECT COUNT(*)
                FROM Products
                WHERE SupplierID = @id
            ";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });

            return count > 0;
        }
    }
}