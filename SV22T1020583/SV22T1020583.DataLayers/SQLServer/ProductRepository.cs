using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020583.DataLayers.Interfaces;
using SV22T1020583.Models.Catalog;
using SV22T1020583.Models.Common;
using System.Data;

namespace SV22T1020583.DataLayers.SQLServer
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection OpenConnection() => new SqlConnection(_connectionString);

        #region Product

        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            using var conn = OpenConnection();

            var p = new DynamicParameters();
            p.Add("@searchValue", input.SearchValue);
            p.Add("@categoryID", input.CategoryID);
            p.Add("@supplierID", input.SupplierID);
            p.Add("@minPrice", input.MinPrice);
            p.Add("@maxPrice", input.MaxPrice);
            p.Add("@offset", input.Offset);
            p.Add("@pageSize", input.PageSize);

            string sqlCount = @"
                SELECT COUNT(*)
                FROM Products
                WHERE (@searchValue='' OR ProductName LIKE '%' + @searchValue + '%')
                  AND (@categoryID=0 OR CategoryID=@categoryID)
                  AND (@supplierID=0 OR SupplierID=@supplierID)
                  AND (Price >= @minPrice) 
                  AND (@maxPrice <= 0 OR Price <= @maxPrice)";

            int rowCount = await conn.ExecuteScalarAsync<int>(sqlCount, p);

            string sqlData = input.PageSize == 0 ?
            @"
                SELECT *
                FROM Products
                WHERE (@searchValue='' OR ProductName LIKE '%' + @searchValue + '%')
                  AND (@categoryID=0 OR CategoryID=@categoryID)
                  AND (@supplierID=0 OR SupplierID=@supplierID)
                  AND (Price >= @minPrice) 
                  AND (@maxPrice <= 0 OR Price <= @maxPrice) 
                ORDER BY ProductName"
            :
            @"
                SELECT *
                FROM Products
                WHERE (@searchValue='' OR ProductName LIKE '%' + @searchValue + '%')
                  AND (@categoryID=0 OR CategoryID=@categoryID)
                  AND (@supplierID=0 OR SupplierID=@supplierID)
                  AND (Price >= @minPrice) 
                  AND (@maxPrice <= 0 OR Price <= @maxPrice) 
                ORDER BY ProductName
                OFFSET @offset ROWS
                FETCH NEXT @pageSize ROWS ONLY";

            var data = await conn.QueryAsync<Product>(sqlData, p);

            return new PagedResult<Product>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = data.ToList()
            };
        }

        public async Task<Product?> GetAsync(int productID)
        {
            using var conn = OpenConnection();

            string sql = @"SELECT * FROM Products WHERE ProductID=@productID";

            return await conn.QueryFirstOrDefaultAsync<Product>(sql, new { productID });
        }

        public async Task<int> AddAsync(Product data)
        {
            using var conn = OpenConnection();

            string sql = @"
                INSERT INTO Products
                (
                    ProductName,
                    ProductDescription,
                    SupplierID,
                    CategoryID,
                    Unit,
                    Price,
                    Photo,
                    IsSelling
                )
                VALUES
                (
                    @ProductName,
                    @ProductDescription,
                    @SupplierID,
                    @CategoryID,
                    @Unit,
                    @Price,
                    @Photo,
                    @IsSelling
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await conn.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> UpdateAsync(Product data)
        {
            using var conn = OpenConnection();

            string sql = @"
                UPDATE Products
                SET
                    ProductName=@ProductName,
                    ProductDescription=@ProductDescription,
                    SupplierID=@SupplierID,
                    CategoryID=@CategoryID,
                    Unit=@Unit,
                    Price=@Price,
                    Photo=@Photo,
                    IsSelling=@IsSelling
                WHERE ProductID=@ProductID";

            return await conn.ExecuteAsync(sql, data) > 0;
        }

        public async Task<bool> DeleteAsync(int productID)
        {
            using var conn = OpenConnection();

            string sql = @"DELETE FROM Products WHERE ProductID=@productID";

            return await conn.ExecuteAsync(sql, new { productID }) > 0;
        }

        public async Task<bool> IsUsedAsync(int productID)
        {
            using var conn = OpenConnection();

            string sql = @"SELECT COUNT(*) FROM OrderDetails WHERE ProductID=@productID";

            return await conn.ExecuteScalarAsync<int>(sql, new { productID }) > 0;
        }

        #endregion

        #region Attributes

        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using var conn = OpenConnection();

            string sql = @"SELECT * FROM ProductAttributes
                           WHERE ProductID=@productID
                           ORDER BY DisplayOrder";

            var data = await conn.QueryAsync<ProductAttribute>(sql, new { productID });
            return data.ToList();
        }

        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using var conn = OpenConnection();

            string sql = @"SELECT * FROM ProductAttributes WHERE AttributeID=@attributeID";

            return await conn.QueryFirstOrDefaultAsync<ProductAttribute>(sql, new { attributeID });
        }

        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using var conn = OpenConnection();

            string sql = @"
                INSERT INTO ProductAttributes
                (
                    ProductID,
                    AttributeName,
                    AttributeValue,
                    DisplayOrder
                )
                VALUES
                (
                    @ProductID,
                    @AttributeName,
                    @AttributeValue,
                    @DisplayOrder
                );

                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            return await conn.ExecuteScalarAsync<long>(sql, data);
        }

        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using var conn = OpenConnection();

            string sql = @"
                UPDATE ProductAttributes
                SET
                    AttributeName=@AttributeName,
                    AttributeValue=@AttributeValue,
                    DisplayOrder=@DisplayOrder
                WHERE AttributeID=@AttributeID";

            return await conn.ExecuteAsync(sql, data) > 0;
        }

        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using var conn = OpenConnection();

            string sql = @"DELETE FROM ProductAttributes WHERE AttributeID=@attributeID";

            return await conn.ExecuteAsync(sql, new { attributeID }) > 0;
        }

        #endregion

        #region Photos

        public async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            using var conn = OpenConnection();

            string sql = @"SELECT * FROM ProductPhotos
                           WHERE ProductID=@productID
                           ORDER BY DisplayOrder";

            var data = await conn.QueryAsync<ProductPhoto>(sql, new { productID });
            return data.ToList();
        }

        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using var conn = OpenConnection();

            string sql = @"SELECT * FROM ProductPhotos WHERE PhotoID=@photoID";

            return await conn.QueryFirstOrDefaultAsync<ProductPhoto>(sql, new { photoID });
        }

        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using var conn = OpenConnection();

            string sql = @"
                INSERT INTO ProductPhotos
                (
                    ProductID,
                    Photo,
                    Description,
                    DisplayOrder,
                    IsHidden
                )
                VALUES
                (
                    @ProductID,
                    @Photo,
                    @Description,
                    @DisplayOrder,
                    @IsHidden
                );

                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            return await conn.ExecuteScalarAsync<long>(sql, data);
        }

        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using var conn = OpenConnection();

            string sql = @"
                UPDATE ProductPhotos
                SET
                    Photo=@Photo,
                    Description=@Description,
                    DisplayOrder=@DisplayOrder,
                    IsHidden=@IsHidden
                WHERE PhotoID=@PhotoID";

            return await conn.ExecuteAsync(sql, data) > 0;
        }

        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using var conn = OpenConnection();

            string sql = @"DELETE FROM ProductPhotos WHERE PhotoID=@photoID";

            return await conn.ExecuteAsync(sql, new { photoID }) > 0;
        }

        #endregion
    }
}