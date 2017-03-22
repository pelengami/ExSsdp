using System.Threading.Tasks;

namespace ExSsdp.Http
{
	internal interface IHttpAvailabilityChecker
	{
		Task<bool> Check(string url);
	}
}