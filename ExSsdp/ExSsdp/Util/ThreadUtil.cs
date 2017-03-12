using System;
using System.Threading;

namespace ExSsdp.Util
{
	public static class ThreadUtil
	{
		public static Thread Create(string name, ThreadPriority threadPriority, Action thAction)
		{
			var thread = new Thread(new ThreadStart(thAction))
			{
				Priority = threadPriority,
				Name = name
			};

			return thread;
		}
	}
}