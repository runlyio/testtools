using Microsoft.AspNetCore.SignalR.Client;

namespace Runly.TestTools.SignalR
{
	public static class HubConnectionExtensions
	{
		/// <summary>
		/// Creates a <see cref="HubConnectionListener"/> that counts each method call for the <paramref name="methodNames"/> specified.
		/// </summary>
		/// <param name="connection">The <see cref="HubConnection"/> to monitor.</param>
		/// <param name="methodNames">The method names to track.</param>
		/// <returns>A <see cref="HubConnectionListener"/>.</returns>
		public static HubConnectionListener ListenFor(this HubConnection connection, params string[] methodNames) =>
			new HubConnectionListener(connection, methodNames);
	}
}
