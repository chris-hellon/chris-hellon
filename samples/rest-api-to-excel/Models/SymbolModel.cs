using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Binance.Models
{
    public class SymbolModel
    {
        [JsonProperty(PropertyName = "symbol")]
        public string Symbol { get; set; }

        [JsonProperty(PropertyName = "pair")]
        public string Pair { get; set; }

        [JsonProperty(PropertyName = "contractType")]
        public string ContractType { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        public SymbolModel()
        {

        }
    }
}
