# Sparrow

Sparrow is a C# HTTP/REST/RPC Network Library, the name is from "麻雀虽小，五脏俱全".

Sparrow（麻雀）是一个C#的 HTTP/REST/RPC 网络库，名称源于“麻雀虽小，五脏俱全”。

Usage / 用法：

```csharp
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
```

Sparrow searches for all possible Web services in a module and passes the specified module assembly when the document is generated.Co-provision of Web services in multiple assemblies is not currently supported (considering that large projects can be time-consuming).The main service function is as follows:

Sparrow会在模块中搜索所有可能的Web服务，生成文档时需要传递指定的模块assembly。暂不支持多个assembly中共同提供Web服务（考虑到大型项目可能比较耗时）。服务主函数如下：

```csharp
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
```

Run the project at this point and the document will be generated automatically. The document address is [http://127.0.0.1:1234/swagger/index.html](http://127.0.0.1:1234/swagger/index.html). Enjoy it!

此时运行项目，文档将会自动生成，文档地址位于[http://127.0.0.1:1234/swagger/index.html](http://127.0.0.1:1234/swagger/index.html)，享受它吧！
