using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Runly.TestTools.SignalR
{
	public class HubConnectionListener
	{
		readonly Dictionary<string, WaitCounter> methods = new Dictionary<string, WaitCounter>();

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

		public WhenBuilder When(string methodName, int count = 1)
		{
			_ = methodName ?? throw new ArgumentNullException(nameof(methodName));

			var hct = new WhenBuilder(methods);
			hct.AddCondition(methodName, count);
			return hct;
		}

		[AsyncMethodBuilder(typeof(AsyncVoidMethodBuilder))]
		public class WhenBuilder
		{
			protected readonly Dictionary<string, WaitCounter> methods;
			private readonly List<Condition> conditions = new List<Condition>();

			public int Timeout { get; set; }

			public WhenBuilder(Dictionary<string, WaitCounter> methods) => this.methods = methods;

			public void AddCondition(string methodName, int count)
			{
				if (!methods.ContainsKey(methodName))
					throw new ArgumentException($"The {nameof(HubConnectionListener)} is not listening for {methodName}.", nameof(methodName));

				conditions.Add(new Condition(methodName, count, methods[methodName]));
			}

			public TaskAwaiter GetAwaiter() => Task.WhenAll(conditions.Select(c => c.Counter.ReleaseAt(c.Count, Timeout * 1000))).GetAwaiter();

			public WhenBuilder And(string methodName, int count = 1)
			{
				_ = methodName ?? throw new ArgumentNullException(nameof(methodName));

				AddCondition(methodName, count);
				return this;
			}

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
