using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SparrowServer.Monitor {
	internal class MonitoringManager {
		// 系统性能指标
		private ComputerState.ISystem m_system = (Environment.OSVersion.Platform == PlatformID.Win32NT ? (ComputerState.ISystem) new ComputerState.Windows () : new ComputerState.Linux ());

		private StateCache.Guages<double>	m_cpu;
		private StateCache.Guages<long>		m_mem;
		private StateCache.Counter			m_static_request;
		private StateCache.Counter			m_method_request;
		private StateCache.Elapsed			m_Request_elapsed;

		public MonitoringManager () {
			m_cpu = new StateCache.Guages<double> ("CPU Usage", 0, 100, "Program Usage", "Total Usage");
			m_cpu.set_callback (() => {
				var (_cpu_program, _cpu_total) = m_system.CpuUsage;
				return new double [] { _cpu_program, _cpu_total };
			});
			m_mem = new StateCache.Guages<long> ("Memory Usage", 0, m_system.MemCount, "Program Usage", "Total Usage");
			m_mem.set_callback (() => {
				var (_mem_program, _mem_total) = m_system.MemUsage;
				return new long [] { _mem_program, _mem_total };
			});
			m_static_request = new StateCache.Counter ("Static Request", "Total", "Error");
			m_method_request = new StateCache.Counter ("Method Request", "Total", "Error");
			m_Request_elapsed = new StateCache.Elapsed ("Elapsed", "Static", "Method");
		}

		public void OnRequest (bool _static, long _elapsed_ms, bool _error = false) {
			(_static ? m_static_request : m_method_request).Increment (0);
			if (_error)
				(_static ? m_static_request : m_method_request).Increment (1);
			m_Request_elapsed.add_value (_static ? 0 : 1, _elapsed_ms);
		}

		public string get_json (int _count) {
			var _o = new JArray ();
			_o.Add (m_cpu.get_values (_count));
			_o.Add (m_mem.get_values (_count));
			_o.Add (m_static_request.get_values (_count));
			_o.Add (m_method_request.get_values (_count));
			_o.Add (m_Request_elapsed.get_values ());
			return _o.to_json ();
		}
	}
}
