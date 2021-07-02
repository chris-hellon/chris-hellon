using System;
using System.Collections.Generic;
using System.Text;
using RestSharp;
using RestSharp.Serializers;
using RestSharp.Deserializers;
using RestSharp.Serialization;

namespace Binance
{
    public static class Extensions
    {
        public static void AddAuthenicationHeader(this RestRequest request, string accessToken, string headerPrefix = "Token token=")
        {
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddHeader("Authorization", headerPrefix + accessToken);
        }

        public static void AddPostBody(this RestRequest request, Dictionary<string, object> bodyParameters)
        {
            StringBuilder stringBuilder = new StringBuilder();

            int index = 0;
            foreach (KeyValuePair<string, object> keyValuePair in bodyParameters)
            {
                if (index == 0) stringBuilder.Append(keyValuePair.Key + "=" + keyValuePair.Value);
                else stringBuilder.Append("&" + keyValuePair.Key + "=" + keyValuePair.Value);

                index++;
            }

            request.AddParameter("application/x-www-form-urlencoded", stringBuilder.ToString(), ParameterType.RequestBody);
        }

        public static DateTime ParseLongToDate(this long date)
        {
            return (new DateTime(1970, 1, 1)).AddMilliseconds(date);
        }
        public static long ParseDateToLong(this DateTime date)
        {
            return (long)(date - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }
    }
}
