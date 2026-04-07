using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020583.DataLayers.Interfaces;
using SV22T1020583.Models.Common;
using SV22T1020583.Models.Sales;
using System.Data;

namespace SV22T1020583.DataLayers.SQLServer
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;

        public OrderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection OpenConnection()
        {
            return new SqlConnection(_connectionString);
        }

        #region Order

        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            using var conn = OpenConnection();

            var p = new DynamicParameters();
            p.Add("@searchValue", input.SearchValue);
            p.Add("@status", (int)input.Status);
            p.Add("@dateFrom", input.DateFrom);
            p.Add("@dateTo", input.DateTo);
            p.Add("@offset", input.Offset);
            p.Add("@pageSize", input.PageSize);

            string where = @"
                WHERE (@searchValue='' OR c.CustomerName LIKE '%' + @searchValue + '%'
                       OR CAST(o.CustomerID AS VARCHAR) = @searchValue)
                AND (@status = 0 OR o.Status=@status)
                AND (@dateFrom IS NULL OR o.OrderTime >= @dateFrom)
                AND (@dateTo IS NULL OR o.OrderTime <= @dateTo)";

            string sqlCount = $@"
                SELECT COUNT(*)
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                {where}";

            int rowCount = await conn.ExecuteScalarAsync<int>(sqlCount, p);

            string sqlData = input.PageSize == 0 ?
            $@"
                SELECT o.*,
                       c.CustomerName,
                       c.ContactName CustomerContactName,
                       c.Email CustomerEmail,
                       c.Phone CustomerPhone,
                       c.Address CustomerAddress,
                       e.FullName EmployeeName,
                       s.ShipperName,
                       s.Phone ShipperPhone
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID=c.CustomerID
                LEFT JOIN Employees e ON o.EmployeeID=e.EmployeeID
                LEFT JOIN Shippers s ON o.ShipperID=s.ShipperID
                {where}
                ORDER BY o.OrderTime DESC"
            :
            $@"
                SELECT o.*,
                       c.CustomerName,
                       c.ContactName CustomerContactName,
                       c.Email CustomerEmail,
                       c.Phone CustomerPhone,
                       c.Address CustomerAddress,
                       e.FullName EmployeeName,
                       s.ShipperName,
                       s.Phone ShipperPhone
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID=c.CustomerID
                LEFT JOIN Employees e ON o.EmployeeID=e.EmployeeID
                LEFT JOIN Shippers s ON o.ShipperID=s.ShipperID
                {where}
                ORDER BY o.OrderTime DESC
                OFFSET @offset ROWS
                FETCH NEXT @pageSize ROWS ONLY";

            var data = await conn.QueryAsync<OrderViewInfo>(sqlData, p);

            return new PagedResult<OrderViewInfo>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = data.ToList()
            };
        }

        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using var conn = OpenConnection();

            string sql = @"
                SELECT o.*,
                       c.CustomerName,
                       c.ContactName CustomerContactName,
                       c.Email CustomerEmail,
                       c.Phone CustomerPhone,
                       c.Address CustomerAddress,
                       e.FullName EmployeeName,
                       s.ShipperName,
                       s.Phone ShipperPhone
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID=c.CustomerID
                LEFT JOIN Employees e ON o.EmployeeID=e.EmployeeID
                LEFT JOIN Shippers s ON o.ShipperID=s.ShipperID
                WHERE o.OrderID=@orderID";

            return await conn.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { orderID });
        }

        public async Task<int> AddAsync(Order data)
        {
            using var conn = OpenConnection();

            string sql = @"
                INSERT INTO Orders
                (
                    CustomerID,
                    OrderTime,
                    DeliveryProvince,
                    DeliveryAddress,
                    EmployeeID,
                    AcceptTime,
                    ShipperID,
                    ShippedTime,
                    FinishedTime,
                    Status
                )
                VALUES
                (
                    @CustomerID,
                    @OrderTime,
                    @DeliveryProvince,
                    @DeliveryAddress,
                    @EmployeeID,
                    @AcceptTime,
                    @ShipperID,
                    @ShippedTime,
                    @FinishedTime,
                    @Status
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await conn.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> UpdateAsync(Order data)
        {
            using var conn = OpenConnection();

            string sql = @"
                UPDATE Orders
                SET
                    CustomerID=@CustomerID,
                    DeliveryProvince=@DeliveryProvince,
                    DeliveryAddress=@DeliveryAddress,
                    EmployeeID=@EmployeeID,
                    AcceptTime=@AcceptTime,
                    ShipperID=@ShipperID,
                    ShippedTime=@ShippedTime,
                    FinishedTime=@FinishedTime,
                    Status=@Status
                WHERE OrderID=@OrderID";

            return await conn.ExecuteAsync(sql, data) > 0;
        }

        public async Task<bool> DeleteAsync(int orderID)
        {
            using var conn = OpenConnection();

            string sql = @"
                DELETE FROM OrderDetails
                WHERE OrderID=@orderID;

                DELETE FROM Orders
                WHERE OrderID=@orderID";

            return await conn.ExecuteAsync(sql, new { orderID }) > 0;
        }

        #endregion

        #region OrderDetails

        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using var conn = OpenConnection();

            string sql = @"
                SELECT d.*,
                       p.ProductName,
                       p.Unit,
                       p.Photo
                FROM OrderDetails d
                JOIN Products p ON d.ProductID=p.ProductID
                WHERE d.OrderID=@orderID";

            var data = await conn.QueryAsync<OrderDetailViewInfo>(sql, new { orderID });

            return data.ToList();
        }

        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using var conn = OpenConnection();

            string sql = @"
                SELECT d.*,
                       p.ProductName,
                       p.Unit,
                       p.Photo
                FROM OrderDetails d
                JOIN Products p ON d.ProductID=p.ProductID
                WHERE d.OrderID=@orderID
                AND d.ProductID=@productID";

            return await conn.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(sql,
                new { orderID, productID });
        }

        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using var conn = OpenConnection();

            string sql = @"
                INSERT INTO OrderDetails
                (
                    OrderID,
                    ProductID,
                    Quantity,
                    SalePrice
                )
                VALUES
                (
                    @OrderID,
                    @ProductID,
                    @Quantity,
                    @SalePrice
                )";

            return await conn.ExecuteAsync(sql, data) > 0;
        }

        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using var conn = OpenConnection();

            string sql = @"
                UPDATE OrderDetails
                SET
                    Quantity=@Quantity,
                    SalePrice=@SalePrice
                WHERE OrderID=@OrderID
                AND ProductID=@ProductID";

            return await conn.ExecuteAsync(sql, data) > 0;
        }

        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using var conn = OpenConnection();

            string sql = @"
                DELETE FROM OrderDetails
                WHERE OrderID=@orderID
                AND ProductID=@productID";

            return await conn.ExecuteAsync(sql, new { orderID, productID }) > 0;
        }

        #endregion

        /// <summary>
        /// Duyêt đơn hàng theo ID đơn hàng và ID nhân viên duyệt. Chỉ duyệt được những đơn có trạng thái "Mới đặt" (1).
        /// </summary>
        public async Task<bool> AcceptAsync(int orderID, int employeeID)
        {
            using var conn = OpenConnection();
            string sql = @"UPDATE Orders
                   SET Status = 2, 
                       EmployeeID = @employeeID, 
                       AcceptTime = GETDATE()
                   WHERE OrderID = @orderID AND Status = 1";
            return await conn.ExecuteAsync(sql, new { orderID, employeeID }) > 0;
        }
        /// <summary>
        /// Từ chối đơn hàng theo ID đơn hàng và ID nhân viên từ chối. Chỉ từ chối được những đơn có trạng thái "Mới đặt" (1). 
        /// Khi từ chối, cập nhật trạng thái thành "Đã hủy" (4) và ghi nhận thời gian từ chối.
        /// </summary>
        public async Task<bool> RejectAsync(int orderID, int employeeID)
        {
            using var conn = OpenConnection();
            string sql = @"UPDATE Orders
                   SET Status = 4, 
                       EmployeeID = @employeeID, 
                       FinishedTime = GETDATE() -- Hoặc dùng một cột ghi chú riêng
                   WHERE OrderID = @orderID AND Status = 1";
            return await conn.ExecuteAsync(sql, new { orderID, employeeID }) > 0;
        }
        /// <summary>
        /// Hủy đơn hàng theo ID đơn hàng. Chỉ hủy được những đơn có trạng thái "Mới đặt" (1), "Đã duyệt" (2) hoặc "Đang giao" (3). 
        /// Khi hủy, cập nhật trạng thái thành "Đã hủy" (4) và ghi nhận thời gian hủy.
        /// </summary>
        public async Task<bool> CancelAsync(int orderID)
        {
            using var conn = OpenConnection();
            string sql = @"UPDATE Orders
                   SET Status = 4, 
                       FinishedTime = GETDATE()
                   WHERE OrderID = @orderID 
                     AND Status IN (1, 2, 3)"; // Có thể hủy khi mới đặt, đã duyệt hoặc đang giao
            return await conn.ExecuteAsync(sql, new { orderID }) > 0;
        }
    }
}