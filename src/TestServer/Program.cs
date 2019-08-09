using SparrowServer;
using SparrowServer.Monitor;
using System;
using System.Linq;
using System.Reflection;

namespace TestServer {
	class Program {
		static void Main (string [] args) {
			//			string _ss = @"Filesystem      Size  Used Avail Use% Mounted on
			///dev/vda1        50G  1.6G   46G   4% /
			//devtmpfs        487M     0  487M   0% /dev
			//tmpfs           497M   24K  497M   1% /dev/shm
			//tmpfs           497M  280K  497M   1% /run
			//tmpfs           497M     0  497M   0% /sys/fs/cgroup
			//tmpfs           100M     0  100M   0% /run/user/0";
			//			var _size_line = (from p in _ss.split (true, '\r', '\n') where p.right_is (" /") select p.split (true, ' ')).First ();
			//			var _total_use = _size_line [2].to_long ();
			//			var _total = _size_line [1].to_long ();

			FawHttpServer _sss = new FawHttpServer (Assembly.GetExecutingAssembly (), 1234);
			_sss.set_doc_info (new WEBDocInfo {
				DocName = "Test interface documentation / 测试接口文档",
				Version = "0.0.1",
				Description = "This is a large document, and I have omitted 10,000 words here / 这个是很大篇的文档，此处省略10000字",
				Contact = "f@fawdlstty.com",
				Scheme = "http",
				Host = "127.0.0.1:1234"
			});
			_sss.enable_monitor ();
			_sss.run ();
		}
	}
}
