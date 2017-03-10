using System;
using System.Threading;

namespace ExSsdp.Util
{
	public static class ThreadUtil
	{
		public static Thread Create(string name, ThreadPriority threadPriority, Delegate thDelegate)
		{
			var thread = new Thread((ThreadStart)thDelegate)
			{
				Priority = threadPriority,
				Name = name
			};
			return thread;
		}
	}
}