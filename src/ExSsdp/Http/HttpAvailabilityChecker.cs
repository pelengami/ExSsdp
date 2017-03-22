using System;
using System.Net;
using System.Threading.Tasks;

namespace ExSsdp.Http
{
    internal sealed class HttpAvailabilityChecker : IHttpAvailabilityChecker
    {
        private const int RequestTimeoutMs = 1000;

        public async Task<bool> Check(string url)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = RequestTimeoutMs;

                var response = (HttpWebResponse)await request.GetResponseAsync();

                return response.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return false;
            }
        }
    }
}
