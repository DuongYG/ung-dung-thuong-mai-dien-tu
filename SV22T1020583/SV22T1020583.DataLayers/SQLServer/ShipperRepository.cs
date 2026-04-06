using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020583.DataLayers.Interfaces;
using SV22T1020583.Models.Common;
using SV22T1020583.Models.Partner;
using System.Data;

namespace SV22T1020583.DataLayers.SQLServer
{
    public class ShipperRepository : IGenericRepository<Shipper>
    {
        private readonly string _connectionString;

        public ShipperRepository(string connectionString)
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
        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            using var connection = OpenConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@searchValue", input.SearchValue);
            parameters.Add("@offset", input.Offset);
            parameters.Add("@pageSize", input.PageSize);

            string sqlCount = @"
                SELECT COUNT(*)
                FROM Shippers
                WHERE (@searchValue = ''
                       OR ShipperName LIKE '%' + @searchValue + '%')
            ";

            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, parameters);

            string sqlData;

            if (input.PageSize == 0)
            {
                sqlData = @"
                    SELECT *
                    FROM Shippers
                    WHERE (@searchValue = ''
                           OR ShipperName LIKE '%' + @searchValue + '%')
                    ORDER BY ShipperName
                ";
            }
            else
            {
                sqlData = @"
                    SELECT *
                    FROM Shippers
                    WHERE (@searchValue = ''
                           OR ShipperName LIKE '%' + @searchValue + '%')
                    ORDER BY ShipperName
                    OFFSET @offset ROWS
                    FETCH NEXT @pageSize ROWS ONLY
                ";
            }

            var data = await connection.QueryAsync<Shipper>(sqlData, parameters);

            return new PagedResult<Shipper>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = data.ToList()
            };
        }

        /// <summary>
        /// Lấy thông tin 1 shipper
        /// </summary>
        public async Task<Shipper?> GetAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"
                SELECT *
                FROM Shippers
                WHERE ShipperID = @id
            ";

            return await connection.QueryFirstOrDefaultAsync<Shipper>(sql, new { id });
        }

        /// <summary>
        /// Thêm shipper
        /// </summary>
        public async Task<int> AddAsync(Shipper data)
        {
            using var connection = OpenConnection();

            string sql = @"
                INSERT INTO Shippers
                (
                    ShipperName,
                    Phone
                )
                VALUES
                (
                    @ShipperName,
                    @Phone
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
            ";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật shipper
        /// </summary>
        public async Task<bool> UpdateAsync(Shipper data)
        {
            using var connection = OpenConnection();

            string sql = @"
                UPDATE Shippers
                SET
                    ShipperName = @ShipperName,
                    Phone = @Phone
                WHERE ShipperID = @ShipperID
            ";

            int rows = await connection.ExecuteAsync(sql, data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa shipper
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"
                DELETE FROM Shippers
                WHERE ShipperID = @id
            ";

            int rows = await connection.ExecuteAsync(sql, new { id });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra shipper có đang được sử dụng không
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"
                SELECT COUNT(*)
                FROM Orders
                WHERE ShipperID = @id
            ";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });

            return count > 0;
        }
    }
}