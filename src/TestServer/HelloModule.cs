using SparrowServer;
using SparrowServer.Attributes;
using System;
using System.Threading.Tasks;

namespace TestServer {
	[WEBModule ("hello world模块")]
	public class HelloModule {
		[WEBMethod ("test method")]
		public static string test_hello () {
			return "hello world";
		}

		[WEBMethod ("test method1")]
		public static string test_hello1 (string name) {
			return $"hello, {name}";
		}

		[WEBMethod ("test test_context")]
		public static void test_context (FawRequest _req, FawResponse _res) {
			_res.write ("hello world");
		}

		[WEBMethod ("test test_context")]
		public static string test_context1 ([WEBParam.IP] string ip, [WEBParam.AgentIP] string agent_ip) {
			return "hello world";
		}

		[WEBMethod.GET ("test test_task1")]
		public static Task test_task1 (FawRequest _req, FawResponse _res) {
			_res.write ("hello world");
			return Task.CompletedTask;
		}

		[WEBMethod.PUT ("test test_task2")]
		public static async Task test_task2 (FawRequest _req, FawResponse _res) {
			_res.write ("hello world");
		}

		[WEBMethod.POST ("test test_task3")]
		public static Task<string> test_task3 () {
			return Task.FromResult ("hello world");
		}

		[WEBMethod.DELETE ("test test_task4")]
		public static async Task<string> test_task4 () {
			return "hello world";
		}
	}
}
