namespace ExSsdp.Http
{
	internal interface IHttpAvailabilityChecker
	{
		bool Check(string url);
	}
}