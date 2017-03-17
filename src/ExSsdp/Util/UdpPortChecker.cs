using System.Linq;

namespace ExSsdp.Util
{
    public static class UdpPortChecker
    {
        public static bool TryGetFirstAvailableUdpPort(out int port)
        {
            var startingAtPort = 1024;
            var maxNumberOfPortsToCheck = 500;
            var range = Enumerable.Range(startingAtPort, maxNumberOfPortsToCheck).ToList();

            var ipGlobalProperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
            var activeUdpListeners = ipGlobalProperties.GetActiveUdpListeners();

            var portsInUse =
                from p in range
                join used in activeUdpListeners
                on p equals used.Port
                select p;

            var firstFreeUdpPortInRange = range.Except(portsInUse).FirstOrDefault();
            if (firstFreeUdpPortInRange > 0)
            {
                port = firstFreeUdpPortInRange;
                return true;
            }

            port = -1;
            return false;
        }
    }
}
