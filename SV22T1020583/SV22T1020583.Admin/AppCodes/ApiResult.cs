namespace SV22T1020583.Admin
{
    /// <summary>
    /// Biểu diễn kết quả trả về của các API (dạng JSON)
    /// </summary>
    public class ApiResult
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="code"></param>
        /// <param name="mesage"></param>
        public ApiResult(int code, string message = "") 
        {
            Code = code;
            Message = message;
        }
        /// <summary>
        /// Mã thông báo kết quả (nếu là 0 tức là lỗi hoặc không thành công)f
        /// </summary>
        public int Code { get; set; }
        /// <summary>
        /// Thông báo lỗi (nếu có)
        /// </summary>
        public string Message { get; set; } = "";
    }
}
