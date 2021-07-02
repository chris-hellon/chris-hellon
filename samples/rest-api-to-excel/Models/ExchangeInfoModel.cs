using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Binance.Models
{
    public class ExchangeInfoModel
    {
        [JsonProperty(PropertyName = "timeZone")]
        public string TimeZone { get; set; }

        [JsonProperty(PropertyName = "serverTime")]
        public long ServerTime { get; set; }

        [JsonProperty(PropertyName = "futuresType")]
        public string FuturesType { get; set; }

        [JsonProperty(PropertyName = "symbols")]
        public List<SymbolModel> Symbols { get; set; }

        public ExchangeInfoModel()
        {

        }
    }
}
