using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socket_demo.Utils
{
    public class NetworkStatusChangedArgs : EventArgs
    {
		private bool isAvailable;

		public NetworkStatusChangedArgs(bool isAvailable)
		{
			this.isAvailable = isAvailable;
		}

		public bool IsAvailable
		{
			get { return isAvailable; }
		}
	}

	public delegate void NetworkStatusChangedHandler(object sender, NetworkStatusChangedArgs e);
}
