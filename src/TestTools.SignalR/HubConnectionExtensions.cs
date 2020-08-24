using Microsoft.AspNetCore.SignalR.Client;

namespace Runly.TestTools.SignalR
{
	public static class HubConnectionExtensions
	{
		public static HubConnectionListener ListenFor(this HubConnection connection, params string[] methodNames) =>
			new HubConnectionListener(connection, methodNames);
	}
}
