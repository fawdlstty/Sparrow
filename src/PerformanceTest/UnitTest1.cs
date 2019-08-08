using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Threading;

namespace PerformanceTest {
	[TestClass]
	public class UnitTest1 {
		[TestMethod]
		public void TestMethod1 () {
			Thread.Sleep (1000);
			for (int i = 0; i < 1000; ++i) {
				using (var c = new FawHttpClient ()) {
					var _data = Encoding.UTF8.GetString (c.post ("http://127.0.0.1:1234/api/Hello/test_struct", hanContentType.FormData, ("val", "{\"name\":\"kangkang\",\"age\":18}")));
					Assert.AreEqual (_data, "{\"result\":\"success\",\"content\":{\"name\":\"kangkang\",\"age\":19}}");
				}
			}
		}
	}
}
