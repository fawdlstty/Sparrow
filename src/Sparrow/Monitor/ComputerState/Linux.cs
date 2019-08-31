using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sparrow.Monitor.ComputerState {
	internal class Linux : ISystem {
		// ref: https://github.com/dotnet/orleans/blob/master/src/TelemetryConsumers/Orleans.TelemetryConsumers.Linux/LinuxEnvironmentStatistics.cs
		private Process m_cur_process = Process.GetCurrentProcess ();

		~Linux () {
			if (m_cur_process != null) {
				m_cur_process.Close ();
				m_cur_process.Dispose ();
			}
		}
		private static async Task<string> _ReadLineWithStartingAsync (string _path, string _start_with) {
			using (var fs = new FileStream (_path, FileMode.Open, FileAccess.Read, FileShare.Read, 512, FileOptions.SequentialScan | FileOptions.Asynchronous)) {
				using (var r = new StreamReader (fs, Encoding.ASCII)) {
					string _line;
					while ((_line = await r.ReadLineAsync ()) != null) {
						if (_line.left_is (_start_with))
							return _line;
					}
				}
			}
			return "";
		}

		// 获取当前进程占用cpu比例及cpu总使用率
		private TimeSpan m_cpu_last_cur = TimeSpan.Zero;
		private long m_prevIdleTime;
		private long m_prevTotalTime;

		public (double, double) CpuUsage {
			get {
				var _total_time = m_cur_process.TotalProcessorTime;
				var _current = (_total_time - m_cpu_last_cur).TotalMilliseconds / Environment.ProcessorCount / 1000 * 100;
				m_cpu_last_cur = _total_time;
				//
				double _cpu_total = 0;
				var _cpu_usage = _ReadLineWithStartingAsync ("/proc/stat", "cpu  ").Result;
				if (!_cpu_usage.is_null_or_space ()) {
					var cpuNumberStrings = _cpu_usage.Split (' ').Skip (2);
					if (!cpuNumberStrings.Any (n => !long.TryParse (n, out _))) {
						var cpuNumbers = cpuNumberStrings.Select (long.Parse).ToArray ();
						var idleTime = cpuNumbers [3];
						var totalTime = cpuNumbers.Sum ();
						_cpu_total = (1.0 - (idleTime - m_prevIdleTime) / ((double) (totalTime - m_prevTotalTime))) * 100;
						m_prevIdleTime = idleTime;
						m_prevTotalTime = totalTime;
					}
				}
				return (_current, _cpu_total);
			}
		}

		// 获取当前进程占用内存、内存总使用率及内存总空间
		private long m_mem_total_byte = 0;

		public long MemCount {
			get {
				if (m_mem_total_byte <= 0) {
					var _mem_total = _ReadLineWithStartingAsync ("/proc/meminfo", "MemTotal").Result;
					if (!string.IsNullOrWhiteSpace (_mem_total)) {
						// Format: "MemTotal:       16426476 kB"
						if (long.TryParse (new string (_mem_total.Where (char.IsDigit).ToArray ()), out var _mem_total_kb))
							m_mem_total_byte = _mem_total_kb * 1_000;
					}
				}
				return m_mem_total_byte;
			}
		}
		public (long, long) MemUsage {
			get {
				var _mem_avail = _ReadLineWithStartingAsync ("/proc/meminfo", "MemAvailable").Result;
				long _mem_avail_byte = 0;
				if (!string.IsNullOrWhiteSpace (_mem_avail)) {
					if (long.TryParse (new string (_mem_avail.Where (char.IsDigit).ToArray ()), out var _mem_avail_kb))
						_mem_avail_byte = _mem_avail_kb * 1_000;
				}
				return (m_cur_process.WorkingSet64, m_mem_total_byte - _mem_avail_byte);
			}
		}

		// 获取硬盘总使用量及硬盘总大小
		public (double, double) DiskUsage {
			get {
				var _psi = new ProcessStartInfo ("git", $"pull -v --progress \"origin\"") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
				string _output = "";
				using (var _ps = Process.Start (_psi)) {
					using (var _so = _ps.StandardOutput) {
						_output = _so.ReadToEnd ();
					}
				}
				try {
					var _size_line = (from p in _output.split (true, '\r', '\n') where p.right_is (" /") select p.split (true, ' ')).First ();
					return (_size_line [2].to_double (), _size_line [1].to_double ());
				} catch (Exception) {
					return (0, 0);
				}
			}
		}
	}
}
