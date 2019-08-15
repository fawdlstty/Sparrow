using Newtonsoft.Json.Linq;
using SparrowServer;
using SparrowServer.Monitor;
using System;
using System.Linq;
using System.Reflection;

namespace TestServer {
	class Program {
		static void Main (string [] args) {
			FawHttpServer _sss = new FawHttpServer (Assembly.GetExecutingAssembly (), Guid.NewGuid ().ToString ("N"));
			// Without calling this interface, the swagger document is not generated
			// 如果没有调用这个接口，将不会生成swagger文档
			_sss.set_doc_info (new WEBDocInfo {
				DocName = "Test interface documentation",
				Version = "0.0.1",
				Description = "This is a large document, and I have omitted 10,000 words here",
				Contact = "f@fawdlstty.com",
				Host = "127.0.0.1:1234"
			});
			// enable monitor (current not implement)
			// 启用状态监控（暂时无用）
			_sss.enable_monitor ();
			// If this function is called, the HTTPS protocol is provided externally, otherwise the HTTP protocol is provided
			// 如果调用了这个函数，那么将对外提供https协议，否则提供http协议
			_sss.set_ssl_file ("F:/test.pfx", "12345678");
			_sss.run (1234);
		}
	}
}
