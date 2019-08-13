# Sparrow

## Description / 描述

Sparrow is a C# HTTP/REST/RPC Network Library, it can be very easy to act as an HTTP server or heterogeneous RPC service provider, Suitable for single server development (consider adding a master-slave later to ensure 100% availability of the service), the name is from "麻雀虽小，五脏俱全".

Sparrow（麻雀）是一个C#的 HTTP/REST/RPC 网络库，可以非常容易的作为HTTP服务器或异构RPC服务提供者，适合用于开发单机型服务器（后期考虑加入主从，用来保证服务的100%可用性），名称源于“麻雀虽小，五脏俱全”。

Usage / 用法：

```csharp
using SparrowServer;
using SparrowServer.Attributes;
using System;
using System.Threading.Tasks;

namespace TestServer {
    [HTTPModule ("hello world模块")]
    public class HelloModule {
        [HTTP ("Test Hello World")]
        public static string test_hello () {
            return "hello world";
        }
    }
}
```

Sparrow searches for all possible Web services in a module and passes the specified module assembly when the document is generated.Co-provision of Web services in multiple assemblies is not currently supported (considering that large projects can be time-consuming).The main service function is as follows:

Sparrow会在模块中搜索所有可能的Web服务，生成文档时需要传递指定的模块assembly。暂不支持多个assembly中共同提供Web服务（考虑到大型项目可能比较耗时）。服务主函数如下：

```csharp
static void Main (string [] args) {
    FawHttpServer _sss = new FawHttpServer (1234, Assembly.GetExecutingAssembly (), Guid.NewGuid ().ToString ("N"));
    // Without calling this interface, the swagger document is not generated
    // 如果没有调用这个接口，将不会生成swagger文档
    _sss.set_doc_info (new WEBDocInfo {
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

At this time will be automatically generated rest RPC interface, request <http://127.0.0.1:1234/api/Hello/test_hello> will invoke this method, return to the content of: `{ "result":"success","content":"hello world"}`

此时将会自动生成rest rpc接口，请求<http://127.0.0.1:1234/api/Hello/test_hello>将调用此方法，返回内容为：`{ "result":"success","content":"hello world"}`

Run the project at this point and the document will be generated automatically. The document address is <http://127.0.0.1:1234/swagger/index.html>.

此时运行项目，文档将会自动生成，文档地址位于<http://127.0.0.1:1234/swagger/index.html>。

## Document / 文档

[English document](./doc/en-us.md)

[中文文档](./doc/zh-cn.md)

## Reference / 引用

<https://github.com/swagger-api/swagger-ui>

<https://github.com/JamesNK/Newtonsoft.Json>

<https://github.com/jwt-dotnet/jwt>

## TODO / 待完善

HTTP with SSL

数据检查

监控流量

CSRF防御

主机及程序运行监视页面（暂时考虑grafana、skywalking或自建）

双机/三机主从（分布式，保证服务100%可用性）
