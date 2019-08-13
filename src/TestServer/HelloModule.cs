using SparrowServer.Attributes;
using SparrowServer.HttpProtocol;
using System.Threading.Tasks;

namespace TestServer {
	[HTTPModule ("hello world模块")]
	public class HelloModule {
		[HTTP ("test method")]
		public static string test_hello () {
			return "hello world";
		}

		[HTTP ("test method1")]
		public static string test_hello1 ([Param ("test name")] string name) {
			return $"hello, {name}";
		}

		[HTTP ("test test_context")]
		public static void test_context (FawRequest _req, FawResponse _res) {
			_res.write ("hello world");
		}

		[HTTP ("test test_context")]
		public static string test_context1 ([ReqParam.IP] string ip, [ReqParam.AgentIP] string agent_ip) {
			return "hello world";
		}

		[HTTP.GET ("test test_task1")]
		public static Task test_task1 (FawRequest _req, FawResponse _res) {
			_res.write ("hello world");
			return Task.CompletedTask;
		}

		[HTTP.PUT ("test test_task2")]
		public static async Task test_task2 (FawRequest _req, FawResponse _res) {
			_res.write ("hello world");
			await Task.CompletedTask;
		}

		[HTTP.POST ("test test_task3")]
		public static Task<string> test_task3 () {
			return Task.FromResult ("hello world");
		}

		[HTTP.DELETE ("test test_task4")]
		public static async Task<string> test_task4 () {
			return await Task.FromResult ("hello world");
		}

		[HTTP ("test struct")]
		public static test_struct test_struct (test_struct val) {
			return new test_struct () { name = val.name, age = val.age + 1 };
		}
	}

	public struct test_struct {
		public string name;
		public int age;
	}
}
