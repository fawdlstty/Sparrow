using SparrowServer;
using SparrowServer.Attributes;
using System;
using System.Threading.Tasks;

namespace TestServer {
	// module description / 文档描述
	[WEBModule ("hello world模块")]
	// The Module name must end with Module / 模块名必须以Module结尾
	public class HelloModule {
		// method summary and method description / 方法描述与方法详细说明
		[WEBMethod.GET ("测试同步函数", "这个是函数的完整描述，用于接口详细说明")]
		// synchronization method / 同步方法
		public static string test_hello1 () {
			// the return value likes / 返回值如下：
			// {"result":"failure","content":"What's your problem?"}
			throw new Exception ("What's your problem?");
			//
			//return "hello world";
			// the return value likes / 返回值如下：
			// { "result":"success","content":"hello world"}
		}

		// it's can only summary / 也可以只指定方法描述
		[WEBMethod.POST ("测试任务函数")]
		// synchronization task / 同步任务
		public static Task<string> test_hello2 () {
			// the return value likes / 返回值如下：
			// { "result":"success","content":"hello world"}
			return Task.FromResult ("hello world");
		}

		[WEBMethod.PUT ("测试异步任务函数")]
		// async task / 异步任务
		public static async Task<string> test_hello3 () {
			// the return value likes / 返回值如下：
			// { "result":"success","content":"hello world"}
			return await Task.FromResult ("hello world");
		}

		[WEBMethod.DELETE ("测试Web请求处理函数")]
		// raw http method / 原始http请求方法
		public static void test_hello4 (FawRequest _req, FawResponse _res) {
			// the return value likes / 返回值如下：
			// hello world
			_res.write ("hello world");
		}
	}
}
