using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Binance.Models
{
    public class FundingRateModel
    {
        [JsonProperty(PropertyName = "symbol")]
        public string Symbol { get; set; }
        
        [JsonProperty(PropertyName = "fundingTime")]
        public long FundingTime { get; set; }

        [JsonProperty(PropertyName = "fundingRate")]
        public decimal FundingRate { get; set; }

        public DateTime FundingTimeParsed { get; set; }

        public FundingRateModel()
        {

        }

    }
}
