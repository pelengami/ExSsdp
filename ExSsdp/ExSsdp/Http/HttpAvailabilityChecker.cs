using System.Net;

namespace ExSsdp.Http
{
	internal class HttpAvailabilityChecker : IHttpAvailabilityChecker
	{
		const int RequestTimeoutMs = 500;

		public bool Check(string url)
		{
			var request = (HttpWebRequest)WebRequest.Create(url);
			request.Timeout = RequestTimeoutMs;

			var response = (HttpWebResponse)request.GetResponse();

			return response.StatusCode != HttpStatusCode.OK;
		}
	}
}
