using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Serializers;
using RestSharp.Deserializers;
using RestSharp.Serialization;

namespace Binance.API
{
    public class BaseClient : RestClient
    {
        private readonly IRestClient _client;
        public IDeserializer Serializer { get; set; }
        public BaseClient(string baseUrl)
        {
            Serializer = new JsonDeserializer();
            BaseUrl = new Uri(baseUrl);
            _client = new RestClient(baseUrl);
            _client.AddHandler("application/json", () => Serializer);
            _client.AddHandler("text/json", () => Serializer);
            _client.AddHandler("text/x-json", () => Serializer);
            _client.AddHandler("text/javascript", () => Serializer);
            _client.AddHandler("*+json", () => Serializer);
        }

        private void TimeoutCheck(IRestRequest request, IRestResponse response)
        {
            if (response.StatusCode == 0)
            {
                LogError(BaseUrl, request, response);
            }
        }
        public override IRestResponse Execute(IRestRequest request)
        {
            var response = base.Execute(request);
            TimeoutCheck(request, response);
            return response;
        }
        public override IRestResponse<T> Execute<T>(IRestRequest request)
        {
            var response = base.Execute<T>(request);
            TimeoutCheck(request, response);
            return response;
        }

        public BaseResponseDTO<T> Get<T>(IRestRequest request, IDeserializer serializer) where T : new()
        {
            BaseResponseDTO<T> baseResponseDTO = new BaseResponseDTO<T>();

            var response = Execute<T>(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK) baseResponseDTO.Data = serializer.Deserialize<T>(response);
            else
            {
                LogError(BaseUrl, request, response);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) baseResponseDTO.Authorized = false;
                baseResponseDTO.Success = false;
                baseResponseDTO.Data = default(T);
            }

            baseResponseDTO.StatusCode = response.StatusCode;

            return baseResponseDTO;
        }
        public async Task<BaseResponseDTO<T>> GetAsync<T>(IRestRequest request, IDeserializer serializer) where T : new()
        {
            BaseResponseDTO<T> baseResponseDTO = new BaseResponseDTO<T>();

            var response = await ExecuteAsync<T>(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK) baseResponseDTO.Data = serializer.Deserialize<T>(response);
            else
            {
                LogError(BaseUrl, request, response);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) baseResponseDTO.Authorized = false;
                baseResponseDTO.Success = false;
                baseResponseDTO.Data = default(T);
            }

            baseResponseDTO.StatusCode = response.StatusCode;

            return baseResponseDTO;
        }
        private void LogError(Uri baseUrl, IRestRequest request, IRestResponse response)
        {
            //Get the values of the parameters passed to the API
            string parameters = string.Join(", ", request.Parameters.Select(x => x.Name.ToString() + "=" + ((x.Value == null) ? "NULL" : x.Value)).ToArray());

            //Set up the information message with the URL, 
            //the status code, and the parameters.
            string info = "Request to " + baseUrl.AbsoluteUri
                          + request.Resource + " failed with status code "
                          + response.StatusCode + ", parameters: "
                          + parameters + ", and content: " + response.Content;

            //Acquire the actual exception
            Exception ex;
            if (response != null && response.ErrorException != null)
            {
                ex = response.ErrorException;
            }
            else
            {
                ex = new Exception(info);
                info = string.Empty;
            }

            //Log the exception and info message
            //_errorLogger.LogError(ex, info);

            //if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized) throw ex;
        }
    }
}
