using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text;

namespace SparrowServer.Monitor.ComputerState {
	internal class Windows : ISystem {
		// ref: https://github.com/dotnet/orleans/blob/master/src/TelemetryConsumers/Orleans.TelemetryConsumers.Counters/Statistics/PerfCounterEnvironmentStatistics.cs
		private Process m_cur_process = Process.GetCurrentProcess ();
		~Windows () {
			if (m_cur_process != null) {
				m_cur_process.Close ();
				m_cur_process.Dispose ();
			}
			if (m_cpu_total != null) {
				m_cpu_total.Close ();
				m_cpu_total.Dispose ();
			}
			if (m_mem_avail != null) {
				m_mem_avail.Close ();
				m_mem_avail.Dispose ();
			}
		}

		// 获取当前进程占用cpu比例及cpu总使用率
		private TimeSpan m_cpu_last_cur = TimeSpan.Zero;
		private PerformanceCounter m_cpu_total = new PerformanceCounter ("Processor", "% Processor Time", "_Total", true);

		public (double, double) CpuUsage {
			get {
				var _total_time = m_cur_process.TotalProcessorTime;
				var _current = (_total_time - m_cpu_last_cur).TotalMilliseconds / Environment.ProcessorCount / 1000 * 100;
				m_cpu_last_cur = _total_time;
				return (_current, m_cpu_total.NextValue ());
			}
		}

		// 获取当前进程占用内存、内存总使用率及内存总空间
		private PerformanceCounter m_mem_avail = new PerformanceCounter ("Memory", "Available Bytes", true);
		private long m_mem_total_byte = 0;

		public long MemCount {
			get {
				if (m_mem_total_byte <= 0) {
					m_mem_total_byte = 0;
					var _wmi_searcher = new ManagementObjectSearcher ("select Capacity from Win32_PhysicalMemory");
					foreach (ManagementObject _wmi_object in _wmi_searcher.Get ())
						m_mem_total_byte += Convert.ToInt64 (_wmi_object.Properties ["Capacity"].Value);
					if (m_mem_total_byte <= 0)
						throw new Exception ("No physical ram installed on machine?");
				}
				return m_mem_total_byte;
			}
		}
		public (long, long) MemUsage {
			get {
				return (m_cur_process.WorkingSet64, m_mem_total_byte - m_mem_avail.NextValue ().to_long ());
			}
		}

		// 获取硬盘总使用量及硬盘总大小
		public (double, double) DiskUsage {
			get {
				long _disk_total = 0, _free = 0;
				var _wmi_searcher = new ManagementObjectSearcher ("select * from Win32_LogicalDisk");
				foreach (ManagementObject _wmi_object in _wmi_searcher.Get ()) {
					if (DriveType.Fixed.to_int () == _wmi_object ["DriveType"].to_int ()) {
						_free += _wmi_object ["FreeSpace"].to_long ();
						_disk_total += _wmi_object ["Size"].to_long ();
					}
				}
				return (_disk_total - _free, _disk_total);
			}
		}
	}
}
