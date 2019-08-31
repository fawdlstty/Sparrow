using System;
using System.Collections.Generic;
using System.Text;

namespace Sparrow.Monitor.ComputerState {
	internal interface ISystem {
		// 获取当前进程占用cpu比例及cpu总使用率
		(double, double) CpuUsage { get; }
		// 获取当前进程占用内存、内存总使用率及内存总空间
		long MemCount { get; }
		(long, long) MemUsage { get; }
		// 获取硬盘总使用量及硬盘总大小
		(double, double) DiskUsage { get; }
	}
}
