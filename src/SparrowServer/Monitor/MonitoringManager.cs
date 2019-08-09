using App.Metrics;
using App.Metrics.Gauge;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SparrowServer.Monitor {
	public class MonitoringManager {
		// 系统性能指标
		private ComputerState.ISystem m_system = (Environment.OSVersion.Platform == PlatformID.Win32NT ? (ComputerState.ISystem) new ComputerState.Windows () : new ComputerState.Linux ());

		private StateCache.Guages<double>	m_cpu;
		private StateCache.Guages<long>		m_mem;
		private StateCache.Counter			m_static_request;
		private StateCache.Counter			m_method_request;

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
		}

		public void OnRequest (bool _static, bool _error = false) {
			(_static ? m_static_request : m_method_request).Increment (_error ? 1 : 0);
		}

		public string get_json (int _count) {
			var _o = new JArray ();
			_o.Add (m_cpu.get_values (_count));
			_o.Add (m_mem.get_values (_count));
			_o.Add (m_static_request.get_values (_count));
			_o.Add (m_method_request.get_values (_count));
			return _o.to_json ();
		}
	}
}
