# Sparrow 中文文档

下面我将以此示例代码对项目进行讲解。

```csharp
[HTTPModule ("hello world模块")]
public class HelloModule {
    [HTTP ("Test Hello World")]
    public static string test_hello () {
        return "hello world";
    }
}
```

## Sparrow 模块

想要使一个模块成为 Sparrow 模块，有三个条件：

* 将模块添加 [HTTPModule] 标记
* 模块的类名称需要以 `Module` 结尾，比如 `UserServiceModule`、`TestModule` 等
* 模块只能在一个项目里面，然后需要将项目Assembly传递给HttpServer，才会最终生效

## Sparrow 静态方法

服务方法必须在服务模块类的内部，也就是说如果类没有 Sparrow 服务模块标记（HTTPModule），那么方法无法识别。另外方法必须声明为静态方法

```csharp
[HTTP ("test method")]
public static string test_hello () {
    return "hello world";
}
```

然后通过HTTP协议调用后，获得返回内容。实际返回内容如下：

```json
{"result":"success","content":"hello world"}
```

Web方法同时支持异常的处理，假如test_hello方法体内容改为：

```csharp
throw new Exception ("What's your problem?");
```

这时候返回内容将会是：

```json
{"result":"failure","content":"What's your problem?"}
```

### 文档地址

Sparrow 对外提供swagger文档，启动项目后，通过访问 `http(s)://127.0.0.1:port/swagger/index.html` 即可看到接口文档，是否启用https根据最终是否调用set_ssl_file函数来确定（后面都假定协议为http）。

### 接口地址

方法默认对外的接口地址为：`http(s)://127.0.0.1:port/api/模块名去掉Module/方法名`。比如此处的接口地址为：  
<http://127.0.0.1:1234/api/Hello/test_hello>

### 标记

方法必须具有Web方法标记 [HTTP]，需附带两个参数，方法描述与方法详细说明；其中后者可省略，区别如下：

* [HTTP]：不限HTTP请求类型那么指定
* [HTTP.GET]：只能使用 GET 方式请求
* [HTTP.PUT]：只能使用 PUT 方式请求
* [HTTP.POST]：只能使用 POST 方式请求
* [HTTP.DELETE]：只能使用 DELETE 方式请求

### 参数

参数可以指定两类，第一类是请求参数，也就是调用此方法时，调用者需要传递的参数。一般的参数可以直接指定类型。

然后可以来看看方法的返回值与参数。这儿设计的非常灵活，既可以以最简单的方式实现`Rest RPC`，也能作为一个正常的HTTP服务器来对外提供HTTP服务。下面我说说对于方法的示例：

```csharp
[HTTP ("test method1")]
public static string test_hello1 (string name) {
    return $"hello, {name}";
}
```

此方法的调用方式是<http://127.0.0.1:1234/api/Hello/test_hello1?name=michael>

其中name参数可以是GET变量，也可以是POST变量。返回内容为：`{"result":"success","content":"hello, michael"}`

参数根据需求可以加入注释。加上后，可以在生成文档时自动生成参数的描述。例如：

```csharp
[HTTP ("test method1")]
public static string test_hello1 ([Param ("test name")] string name) {
    return $"hello, {name}";
}
```

参数还有一种类型，称之为请求变量类型，这种类型不需要调用者传递，而是直接从请求会话中取值。目前支持四种类型，示例：

```csharp
[HTTP ("test test_context")]
public static void test_context (FawRequest _req, FawResponse _res) {
    _res.write ("hello world");
}

[HTTP ("test test_context")]
public static string test_context1 ([ReqParam.IP] string ip, [ReqParam.AgentIP] string agent_ip) {
    return "hello world";
}
```

`test_context` 方法是原始类型请求，这种请求不需要返回值，如果需要返回内容直接通过_res变量的方法直接处理即可。比如此处使用write方法返回内容。这时候写入的内容将不经过json包装，调用者获得的返回数据是 `hello world`。

`test_context1` 方法的两个参数分别有ReqParam标记，被标记的方法不需要调用者传递，直接从请求内容中取的。其中 `ReqParam.IP` 含义为取得调用者的IP（注意：此参数调用者可能伪造），还有就是 `ReqParam.AgentIP`，此参数为调用者使用的正向代理的IP（注意：此参数调用者可能伪造）。

### 返回值的类型

返回值在参数中没有 `FawResponse` 的情况下有效。当返回 `byte` 类型或 `byte []` 类型时，返回内容将不做处理，直接返回给调用者；如果返回其他类型比如 `int`、`string` 等类型时，将会经过json包装；另外不论返回值类型，直接抛出异常后，也会返回经过json包装的错误提示内容。

### 任务模式

任务模式指web方法返回 `Task` 或 `Task<T>`，不限是否添加 `async` 关键字的情况。任何任务方法和其他普通web方法一样，均采用同步的方式对调用者进行返回。示例如下：

```csharp
[HTTP.GET ("test test_task1")]
public static Task test_task1 (FawRequest _req, FawResponse _res) {
    _res.write ("hello world");
    return Task.CompletedTask;
}

[HTTP.PUT ("test test_task2")]
public static async Task test_task2 (FawRequest _req, FawResponse _res) {
    _res.write ("hello world");
}

[HTTP.POST ("test test_task3")]
public static Task<string> test_task3 () {
    return Task.FromResult ("hello world");
}

[HTTP.DELETE ("test test_task4")]
public static async Task<string> test_task4 () {
    return "hello world";
}
```

### 自定义结构

自定义结构目前仅支持json序列化。示例如下：

```csharp
public struct test_struct {
    public string name;
    public int age;
}

// ...

[HTTP ("test struct")]
public static test_struct test_struct (test_struct val) {
    return new test_struct () { name = val.name, age = val.age + 1 };
}
```

调用方式（当然参数可以通过post传递，此处通过GET方便浏览器中直接测试调用）：

<http://127.0.0.1:1234/api/Hello/test_struct?val={%22name%22:%22kangkang%22,%22age%22:18}>

返回结果：

```json
{"result":"success","content":{"name":"kangkang","age":19}}
```

## Sparrow 鉴权

### 描述

目前 Sparrow 支持的鉴权方式是 JWT 鉴权。JWT 鉴权的思路为：首先服务器保存一条秘钥，然后通过签发JWT的方式给予前端，这条签发数据包含少量自定义数据及过期时间。前端在过期时间内通过在 HTTP 请求头加入：

```http header
X-API-Key: token_data...
```

即可调用鉴权方法。这种方式有个好处就是，服务器不用保存 Session 信息，但有一点需要特别注意：服务器端的签发秘钥一定不能泄露，否则需要修改。建议单体应用使用：

```csharp
Guid.NewGuid ().ToString ("N");
```

这样的方式获取，可以在很大程度上保证秘钥安全，如果不小心泄露，重启一下服务器就行了。

### 用法

```csharp
[HTTPModule ("Test for JWT")]
public class JWTTestModule {
    [HTTP.GET ("generate jwt token"), JWTGen]
    public static (JObject, DateTime) generate () {
        return (new JObject { ["test"] = "hello" }, DateTime.Now.AddMinutes (2));
    }

    [JWTRequest]
    public static JWTTestModule _check_auth (JObject _jwt) {
        return new JWTTestModule { n = 100 };
    }

    [HTTP.GET ("get the 'n' value")]
    public int get_value () {
        return n;
    }

    private int n = 0;
}
```

首先 generate 静态方法具有两个属性，`[HTTP.GET]` 及 `[JWTGen]`，后者的意思是，这个Web方法用于签发API-Key，有了这个标注后，返回值类型必须为 `(JObject, DateTime)`，前者为用户自己储存的数据内容（名称不能为exp），后者代表签发的 Key 的有效期限，比如此处 Key 的有效期为两分钟。这时候，前端调用这个静态方法，就能获取到 API-Key 了。

第二个方法 _check_auth 具有 `[JWTRequest]` 属性，每个 HTTP 服务类都只能有一个，作用是，用户想要请求非静态方法时，通过这个方法来构造对象。能调用到这个函数代表已经鉴权通过了，接下来需要手工验证自己存储的 JWT 数据来生成对象。一般有两种方式，new 一个或者从字典缓存中取出来，具体做法需要看自己的业务情况。

然后是 get_value 方法。用户如果想要请求到这个非静态方法，首先需要设置 HTTP 头：`X-API-Key`，内容是通过 `[JWTGen]` 方法签发的 Key，Sparrow 此时会验证 Key 是否有效，有效将调用 `[JWTRequest]` 标注的方法来生成对象，然后再调用到 get_value。此处测试调用显示变量 `n` 的值为 100。
