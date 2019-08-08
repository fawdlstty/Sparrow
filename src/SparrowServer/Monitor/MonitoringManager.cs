using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SparrowServer.Monitor {
	public class MonitoringManager {
		//private IMetricsRoot m_metrics = AppMetrics.CreateDefaultBuilder ().OutputMetrics.AsPlainText ().OutputMetrics.AsJson ().Build ();

		// 系统性能指标
		private ComputerState.ISystem m_system = (Environment.OSVersion.Platform == PlatformID.Win32NT ? (ComputerState.ISystem) new ComputerState.Windows () : new ComputerState.Linux ());

		//private GaugeOptions m_cpu = new GaugeOptions () { Name = "CPU Usage" };
		//private MetricTags m_cpu_app = new MetricTags ("cpu_app", "cpu_app_val");
		//private MetricTags m_cpu_total = new MetricTags ("cpu_total", "cpu_total_val");
		//private GaugeOptions m_mem = new GaugeOptions () { Name = "Memory Usage" };
		//private MetricTags m_mem_app = new MetricTags ("mem_app", "mem_app_val");
		//private MetricTags m_mem_total = new MetricTags ("mem_total", "mem_total_val");
		//private GaugeOptions m_disk = new GaugeOptions () { Name = "Disk Usage" };
		//private MetricTags m_disk_total = new MetricTags ("disk_total", "disk_total_val");

		private void counting_sysinfo () {
			//var (_cpu_app, _cpu_total) = m_system.CpuUsage;
			//m_metrics.Measure.Gauge.SetValue (m_cpu, m_cpu_app, _cpu_app);
			//m_metrics.Measure.Gauge.SetValue (m_cpu, m_cpu_total, _cpu_total);
			//var (_mem_app, _mem_total, _mem_count) = m_system.MemUsage;
			//m_metrics.Measure.Gauge.SetValue (m_mem, m_mem_app, _mem_app / (double) _mem_count * 100);
			//m_metrics.Measure.Gauge.SetValue (m_mem, m_mem_total, _mem_total / (double) _mem_count * 100);
			//var (_disk_total, _disk_count) = m_system.CpuUsage;
			//m_metrics.Measure.Gauge.SetValue (m_disk, m_disk_total, _disk_total / _disk_count * 100);
		}

		public async Task<string> get_json () {
			//counting_sysinfo ();
			//var snapshot = m_metrics.Snapshot.Get ();
			//foreach (var formatter in m_metrics.OutputMetricsFormatters) {
			//	if (formatter.MediaType.Format != "json")
			//		continue;
			//	using (var stream = new MemoryStream ()) {
			//		await formatter.WriteAsync (stream, snapshot);
			//		var result = Encoding.UTF8.GetString (stream.ToArray ());
			//		//System.Console.WriteLine (result);
			//		return result;
			//	}
			//}
			//return "";
		}
	}
}
