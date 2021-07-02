
using System;
using System.Collections.Generic;
using System.Text;
using Binance.API;
using RestSharp;
using RestSharp.Serializers;
using RestSharp.Deserializers;
using RestSharp.Serialization;

namespace Binance.API.Futures
{
    public class Client : BaseClient
    { 
        public Client(string baseUrl) : base(baseUrl)
        {

        }
        public BaseResponseDTO<List<Models.FundingRateModel>> GetFundingRates(string symbol, DateTime fromDate, DateTime toDate, int limit = 1000)
        {
            RestRequest request = new RestRequest("fundingRate", Method.GET);
            request.AddParameter("symbol", symbol, ParameterType.QueryString);
            request.AddParameter("startTime", fromDate.ParseDateToLong());
            request.AddParameter("endTime", toDate.ParseDateToLong());
            request.AddParameter("limit", limit);

            return Get<List<Models.FundingRateModel>>(request, Serializer);
        }

        public BaseResponseDTO<Models.ExchangeInfoModel> GetExchangeInfo()
        {
            RestRequest request = new RestRequest("exchangeInfo", Method.GET);

            return Get<Models.ExchangeInfoModel>(request, Serializer);
        }
    }
}
