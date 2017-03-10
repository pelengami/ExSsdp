using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExSsdp.Util
{
	public static class Repeater
	{
		public static void DoInfinityAsync(Action action, TimeSpan interval, CancellationToken cancellationToken)
		{
			var thAction = new Action(async () =>
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					try
					{
						action();
						await Task.Delay(interval, cancellationToken);
					}
					catch (TaskCanceledException)
					{
						//ignore
						return;
					}
					catch (Exception)
					{
						//ignore
						return;
					}
				}
			});

			var thread = ThreadUtil.Create("do infinity async", ThreadPriority.Normal, thAction);
			thread.Start();
			//exit from the thread occurs when the token is canceled
		}
	}
}
