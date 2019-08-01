using SparrowServer;
using System.Reflection;

namespace TestServer {
	class Program {
		static void Main (string [] args) {
			FawHttpServer _sss = new FawHttpServer (1234);
			_sss.set_doc_info (Assembly.GetExecutingAssembly (), new WEBDocInfo {
				DocName = "Test interface documentation / 测试接口文档",
				Version = "0.0.1",
				Description = "This is a large document, and I have omitted 10,000 words here / 这个是很大篇的文档，此处省略10000字",
				Contact = "f@fawdlstty.com",
				Scheme = "http",
				Host = "127.0.0.1:1234"
			});
			_sss.run ();
		}
	}
}
