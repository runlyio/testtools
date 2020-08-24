using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Diag = System.Diagnostics;

namespace Runly.TestTools.SignalR
{
	public class WaitCounter
	{
		private readonly ConcurrentDictionary<int, TaskCompletionSource<bool>> tasks = new ConcurrentDictionary<int, TaskCompletionSource<bool>>();
		private readonly SemaphoreSlim sync = new SemaphoreSlim(1, 1);

		public int Count { get; private set; }

		public WaitCounter() { }

		public async Task Increment()
		{
			// Using a write lock because we don't want to increment the count after ReleaseAt checks for a count
			// and before it creates a new TaskCompletionSource. This would cause the task to never be complete.
			await sync.WaitAsync();
			try
			{
				Count++;

				if (tasks.TryGetValue(Count, out TaskCompletionSource<bool> tcs))
					tcs.SetResult(true);
			}
			finally
			{
				sync.Release();
			}
		}

		/// <summary>
		/// Blocks the caller until <see cref="Count"/> to reach <paramref name="count"/> or the <paramref name="timeout"/>, if any, expires.
		/// </summary>
		/// <param name="count">The <see cref="Count"/> at which to release the caller.</param>
		/// <param name="timeout">The number of seconds to wait before returning, regardless of whether the count has been reached.</param>
		/// <exception cref="TimeoutException">When <paramref name="timeout"/> is greater than zero and the timeout expires before the <paramref name="count"/> is reached.</exception>
		public async Task ReleaseAt(int count, int timeout = 0)
		{
			TaskCompletionSource<bool> tcs;

			await sync.WaitAsync();
			try
			{
				if (count <= Count)
					return;

				tcs = tasks.GetOrAdd(count, i => new TaskCompletionSource<bool>(i));
			}
			finally
			{
				sync.Release();
			}

			if (timeout <= 0)
			{
				await tcs.Task;
			}
			else
			{
				await Task.WhenAny(tcs.Task, Task.Delay(timeout * 1000));

				if (!tcs.Task.IsCompleted)
					throw new TimeoutException($"The count ({Count}) failed to reach {count} before the timeout.");
			}
		}
	}
}
