using System.Net;

namespace FinChain.Model
{
    public class ApiReturnModel<T>
    {
        public bool Success { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
}
