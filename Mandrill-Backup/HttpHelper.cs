using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Mandrill_Backup
{
    internal class HttpHelper
    {
        //some helper methods
        internal async Task<HttpResult> Post<T>(string url, T request)
        {
            //set authorization header to basic clientId:secret
            var client = CreateHttpClient();

            //format message to post
            string requestData = JsonConvert.SerializeObject(request);

            //set posted data to form-urlencoded content
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(requestData, Encoding.UTF8, "application/json")
            };

            return await client.SendAsync(httpRequest).ContinueWith(responseTask =>
            {
                var response = responseTask.Result;

                return new HttpResult
                {
                    Body = response.Content.ReadAsStringAsync().Result, Code = response.StatusCode, StatusDescription = response.ReasonPhrase
                };
            }).ConfigureAwait(false);
        }

        internal HttpClient CreateHttpClient()
        {
            var client = new HttpClient();

            //request json back
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }
    }
}