using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020583.DataLayers.Interfaces;
using SV22T1020583.Models.Common;
using SV22T1020583.Models.Catalog;
using System.Data;

namespace SV22T1020583.DataLayers.SQLServer
{
    public class CategoryRepository : IGenericRepository<Category>
    {
        private readonly string _connectionString;

        public CategoryRepository(string connectionString)
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
        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            using var connection = OpenConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@searchValue", input.SearchValue);
            parameters.Add("@offset", input.Offset);
            parameters.Add("@pageSize", input.PageSize);

            string sqlCount = @"
                SELECT COUNT(*)
                FROM Categories
                WHERE (@searchValue = ''
                       OR CategoryName LIKE '%' + @searchValue + '%')
            ";

            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, parameters);

            string sqlData;

            if (input.PageSize == 0)
            {
                sqlData = @"
                    SELECT *
                    FROM Categories
                    WHERE (@searchValue = ''
                           OR CategoryName LIKE '%' + @searchValue + '%')
                    ORDER BY CategoryName
                ";
            }
            else
            {
                sqlData = @"
                    SELECT *
                    FROM Categories
                    WHERE (@searchValue = ''
                           OR CategoryName LIKE '%' + @searchValue + '%')
                    ORDER BY CategoryName
                    OFFSET @offset ROWS
                    FETCH NEXT @pageSize ROWS ONLY
                ";
            }

            var data = await connection.QueryAsync<Category>(sqlData, parameters);

            return new PagedResult<Category>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = data.ToList()
            };
        }

        /// <summary>
        /// Lấy thông tin 1 category
        /// </summary>
        public async Task<Category?> GetAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"
                SELECT *
                FROM Categories
                WHERE CategoryID = @id
            ";

            return await connection.QueryFirstOrDefaultAsync<Category>(sql, new { id });
        }

        /// <summary>
        /// Thêm category
        /// </summary>
        public async Task<int> AddAsync(Category data)
        {
            using var connection = OpenConnection();

            string sql = @"
                INSERT INTO Categories
                (
                    CategoryName,
                    Description,
                    Photo
                )
                VALUES
                (
                    @CategoryName,
                    @Description,
                    @Photo
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
            ";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật category
        /// </summary>
        public async Task<bool> UpdateAsync(Category data)
        {
            using var connection = OpenConnection();

            string sql = @"
                UPDATE Categories
                SET
                    CategoryName = @CategoryName,
                    Description = @Description,
                    Photo = @Photo
                WHERE CategoryID = @CategoryID
            ";

            int rows = await connection.ExecuteAsync(sql, data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa category
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"
                DELETE FROM Categories
                WHERE CategoryID = @id
            ";

            int rows = await connection.ExecuteAsync(sql, new { id });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra category có đang được sử dụng không
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"
                SELECT COUNT(*)
                FROM Products
                WHERE CategoryID = @id
            ";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });

            return count > 0;
        }
    }
}