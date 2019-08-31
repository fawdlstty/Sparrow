using Newtonsoft.Json.Linq;
using Sparrow;
using Sparrow.Monitor;
using System;
using System.Linq;
using System.Reflection;

namespace TestServer {
	class Program {
		static void Main (string [] args) {
			SparrowServer _server = new SparrowServer (Assembly.GetExecutingAssembly (), Guid.NewGuid ().ToString ("N"));

			// api path: http://127.0.0.1:1234/api/module_method...
			_server.set_api_path ("/api");

			// doc path: http://127.0.0.1:1234/doc/
			_server.set_doc_info ("/doc", new WEBDocInfo {
				DocName = "Test interface documentation",
				Version = "0.0.1",
				Description = "This is a large document, and I have omitted <b>10,000</b> words here",
			});

			// if do not call this, log will print to console
			_server.set_log_path ("D:/www_log/");

			// If this function is called, the HTTPS protocol is provided externally, otherwise the HTTP protocol is provided
			//_server.set_ssl_file ("F:/test.pfx", "testpfx123456");

			// set static file path or namespace path
			_server.set_res_from_path ("F:/wwwroot");
			//_server.set_res_from_namespace ("TestServer.res");

			// run service
			_server.run (1234);
		}
	}
}
