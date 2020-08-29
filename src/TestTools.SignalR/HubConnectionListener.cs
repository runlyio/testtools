using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Runly.TestTools.SignalR
{
	/// <summary>
	/// Synchronizes code that must wait for a method call on a <see cref="HubConnection"/>.
	/// </summary>
	public class HubConnectionListener
	{
		readonly Dictionary<string, WaitCounter> methods = new Dictionary<string, WaitCounter>();

		public int Timeout { get; set; }

		public HubConnectionListener(HubConnection connection, params string[] methodNames)
		{
			_ = connection ?? throw new ArgumentNullException(nameof(connection));
			_ = methodNames ?? throw new ArgumentNullException(nameof(methodNames));

			if (methodNames.Length < 1)
				throw new ArgumentOutOfRangeException(nameof(methodNames), "Must contain at least one method name.");

			foreach (var name in methodNames)
			{
				var counter = new WaitCounter();
				connection.On(name, async () => await counter.Increment());
				methods.Add(name, counter);
			}
		}

		/// <summary>
		/// Sets the default <see cref="Timeout"/> for this <see cref="HubConnectionListener"/>.
		/// </summary>
		/// <param name="seconds">The number of seconds to wait before timing out.</param>
		public HubConnectionListener WithTimeout(int seconds)
		{
			this.Timeout = seconds;
			return this;
		}

		/// <summary>
		/// When awaited, blocks the calling code until the method specified is invoked at least <paramref name="count"/> number of times.
		/// </summary>
		/// <param name="methodName">The method to wait for.</param>
		/// <param name="count">The number of times the method must be called before satisfying this condition.</param>
		/// <returns>A task-like type that can be awaited.</returns>
		public WhenBuilder When(string methodName, int count = 1)
		{
			_ = methodName ?? throw new ArgumentNullException(nameof(methodName));

			var hct = new WhenBuilder(methods, Timeout);
			hct.AddCondition(methodName, count);
			return hct;
		}

		[AsyncMethodBuilder(typeof(AsyncVoidMethodBuilder))]
		public class WhenBuilder
		{
			protected readonly Dictionary<string, WaitCounter> methods;
			private readonly List<Condition> conditions = new List<Condition>();

			public int Timeout { get; set; }

			public WhenBuilder(Dictionary<string, WaitCounter> methods, int timeout) => (this.methods, Timeout) = (methods, timeout);

			public void AddCondition(string methodName, int count)
			{
				if (!methods.ContainsKey(methodName))
					throw new ArgumentException($"The {nameof(HubConnectionListener)} is not listening for {methodName}.", nameof(methodName));

				conditions.Add(new Condition(methodName, count, methods[methodName]));
			}

			public TaskAwaiter GetAwaiter() => Task.WhenAll(conditions.Select(c => c.Counter.ReleaseAt(c.Count, Timeout * 1000))).GetAwaiter();

			/// <summary>
			/// When awaited, blocks the calling code until the method specified is invoked at least <paramref name="count"/> number of times.
			/// </summary>
			/// <param name="methodName">The method to wait for.</param>
			/// <param name="count">The number of times the method must be called before satisfying this condition.</param>
			/// <returns>A task-like type that can be awaited.</returns>
			public WhenBuilder And(string methodName, int count = 1)
			{
				_ = methodName ?? throw new ArgumentNullException(nameof(methodName));

				AddCondition(methodName, count);
				return this;
			}

			/// <summary>
			/// Sets the <see cref="Timeout"/> for this condition.
			/// </summary>
			/// <param name="seconds">The number of seconds to wait before timing out.</param>
			public WhenBuilder WithTimeout(int seconds)
			{
				this.Timeout = seconds;
				return this;
			}
		}

		internal class Condition
		{
			internal string MethodName { get; }
			internal int Count { get; }
			internal WaitCounter Counter { get; }

			internal Condition(string methodName, int count, WaitCounter counter)
			{
				MethodName = methodName;
				Count = count;
				Counter = counter;
			}
		}
	}
}
