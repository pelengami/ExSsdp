using System;
using System.Net;

namespace ExSsdp.Http
{
	internal sealed class HttpAvailabilityChecker : IHttpAvailabilityChecker
	{
		private const int RequestTimeoutMs = 1000;

		public bool Check(string url)
		{
			try
			{
				var request = (HttpWebRequest)WebRequest.Create(url);
				request.Timeout = RequestTimeoutMs;

				var response = (HttpWebResponse)request.GetResponse();

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
