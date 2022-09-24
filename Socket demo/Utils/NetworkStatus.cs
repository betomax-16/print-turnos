using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Socket_demo.Utils
{
    class NetworkStatus
    {
		private static bool isAvailable;
		private static NetworkStatusChangedHandler handler;

		static NetworkStatus()
		{
			isAvailable = IsNetworkAvailable();
		}

		public static event NetworkStatusChangedHandler AvailabilityChanged
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			add
			{
				if (handler == null)
				{
					NetworkChange.NetworkAvailabilityChanged += new NetworkAvailabilityChangedEventHandler(DoNetworkAvailabilityChanged);

					NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(DoNetworkAddressChanged);
				}

				handler = (NetworkStatusChangedHandler)Delegate.Combine(handler, value);
			}

			[MethodImpl(MethodImplOptions.Synchronized)]
			remove
			{
				handler = (NetworkStatusChangedHandler)Delegate.Remove(handler, value);

				if (handler == null)
				{
					NetworkChange.NetworkAvailabilityChanged
						-= new NetworkAvailabilityChangedEventHandler(DoNetworkAvailabilityChanged);

					NetworkChange.NetworkAddressChanged
						-= new NetworkAddressChangedEventHandler(DoNetworkAddressChanged);
				}
			}
		}

		public static bool IsAvailable
		{
			get { return isAvailable; }
		}

		private static bool IsNetworkAvailable()
		{
			if (NetworkInterface.GetIsNetworkAvailable())
			{
				NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
				foreach (NetworkInterface face in interfaces)
				{
					if (face.OperationalStatus == OperationalStatus.Up)
					{
						if ((face.NetworkInterfaceType != NetworkInterfaceType.Tunnel) &&
							(face.NetworkInterfaceType != NetworkInterfaceType.Loopback))
						{
							IPv4InterfaceStatistics statistics = face.GetIPv4Statistics();

							if ((statistics.BytesReceived > 0) &&
								(statistics.BytesSent > 0))
							{
								return true;
							}
						}
					}
				}
			}

			return false;
		}


		private static void DoNetworkAddressChanged(object sender, EventArgs e)
		{
			SignalAvailabilityChange(sender);
		}


		private static void DoNetworkAvailabilityChanged(
			object sender, NetworkAvailabilityEventArgs e)
		{
			SignalAvailabilityChange(sender);
		}


		private static void SignalAvailabilityChange(object sender)
		{
			bool change = IsNetworkAvailable();

			if (change != isAvailable)
			{
				isAvailable = change;

				if (handler != null)
				{
					handler(sender, new NetworkStatusChangedArgs(isAvailable));
				}
			}
		}
	}
}
