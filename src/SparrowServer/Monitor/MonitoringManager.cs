using System;
using System.Collections.Generic;
using System.Text;

namespace SparrowServer.Monitor {
	public class MonitoringManager {
		private ComputerState.ISystem m_system = (Environment.OSVersion.Platform == PlatformID.Win32NT ? (ComputerState.ISystem) new ComputerState.Windows () : new ComputerState.Linux ());
		public MonitoringManager () {
		}
	}
}
