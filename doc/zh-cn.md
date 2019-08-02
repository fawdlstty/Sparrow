# Sparrow 中文文档

## Sparrow 服务模块

想要使一个模块成为 Sparrow 服务模块，有三个条件：

* 将模块添加 [WEBModule] 标记
* 模块的类名称需要以 `Module` 结尾，比如 `UserServiceModule`、`TestModule` 等
* 服务模块只能在一个项目里面，然后需要将项目Assembly传递给HttpServer，才会最终生效

然后是模块提供的方法了。模块方法有三个要求：

* 必须在服务模块类的内部，也就是说如果类没有 Sparrow 服务模块标记（WEBModule），那么方法无法识别
* 方法必须具有Web方法标记 [WEBMethod]，需附带两个参数，方法描述与方法详细说明，其中后者可省略，区别如下：
    + [WEBMethod]：不限HTTP请求类型那么指定
    + [WEBMethod.GET]：只能使用 GET 方式请求
    + [WEBMethod.PUT]：只能使用 PUT 方式请求
    + [WEBMethod.POST]：只能使用 POST 方式请求
    + [WEBMethod.DELETE]：只能使用 DELETE 方式请求
* 方法必须声明为静态方法

然后可以来看看方法的返回值与参数。这儿设计的非常灵活，既可以以最简单的方式实现`Rest RPC`，也能作为一个正常的HTTP服务器来对外提供HTTP服务。下面我说说对于方法的示例：

```csharp
[WEBMethod ("test method")]
public static string test_hello () {
    return "hello world";
}
```

这个示例非常简单了，没有任何参数，返回hello world的字符串。这个HTTP方法的请求地址为

<http://127.0.0.1:1234/api/Hello/test_hello>

接口的URL地址规则为，`http://127.0.0.1:服务端口/api/模块名去掉Module/方法名`

此处不支持HTTPS方法，考虑到不建议直接将服务暴露给外部，真正部署时建议使用网关（kong、ocelot）或反向代理（nginx、apache）。

最终通过HTTP协议调用后，获得返回内容。实际返回内容如下：

```json
{"result":"success","content":"hello world"}
```

Web方法同时支持异常的处理，假如test_hello方法体内容改为

```csharp
throw new Exception ("What's your problem?");
```

这时候返回内容将会是：

```json
{"result":"failure","content":"What's your problem?"}
```

对于一般的HTTP请求可能需要携带参数的情况，只需要修改
