using System;
using System.Collections.Generic;
using System.Text;

namespace Binance.API
{
    public class BaseResponseDTO<T>
    {
        public bool Authorized { get; set; } = true;
        public bool Success { get; set; } = true;
        public T Data { get; set; }
        public System.Net.HttpStatusCode StatusCode { get; set; }
    }
}
