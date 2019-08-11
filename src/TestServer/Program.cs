using Newtonsoft.Json.Linq;
using SparrowServer;
using SparrowServer.Monitor;
using System;
using System.Linq;
using System.Reflection;

namespace TestServer {
	class Program {
		static void Main (string [] args) {
			FawHttpServer _sss = new FawHttpServer (Assembly.GetExecutingAssembly (), 1234);
			// Without calling this interface, the swagger document is not generated
			// 如果没有调用这个接口，将不会生成swagger文档
			_sss.set_doc_info (new WEBDocInfo {
				DocName = "Test interface documentation",
				Version = "0.0.1",
				Description = "This is a large document, and I have omitted 10,000 words here",
				Contact = "f@fawdlstty.com",
				Scheme = "http",
				Host = "127.0.0.1:1234"
			});
			_sss.enable_monitor ();
			_sss.run ();
		}
	}
}
