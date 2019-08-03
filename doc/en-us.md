# Sparrow English document

I'll walk you through the project with this sample code.

```csharp
[WEBModule ("hello world模块")]
public class HelloModule {
    [WEBMethod ("Test Hello World")]
    public static string test_hello () {
        return "hello world";
    }
}
```

## Sparrow Module

There are three conditions for a module to be a Sparrow module:

* Add the [WEBModule] Attribute to the module
* The class name of a Module needs to end with 'Module', such as' UserServiceModule ', 'TestModule', etc
* A module can only be in one project, and then needs to pass the project Assembly to HttpServer before it finally takes effect

## Sparrow Method

The service method must be within the service module class, which means that if the class does not have the sparrow service module Attribute (webmodule), the method is not identifiable. The other method must be declared static

```csharp
[WEBMethod ("test method")]
public static string test_hello () {
    return "hello world";
}
```

Then, after the HTTP protocol is invoked, the return content is obtained.The actual return contents are as follows:

```json
{"result":"success","content":"hello world"}
```

The Web method also supports handling exceptions if the test_hello method body content is changed to:

```csharp
throw new Exception ("What's your problem?");
```

The return will be:

```json
{"result":"failure","content":"What's your problem?"}
```

### Document URL

Sparrow provides swagger document, after start the project, by visiting `http://127.0.0.1:port/swagger/index.html` can see interface document.

### URL of the interface

Method the default external interface address is: `http://127.0.0.1:port/api/Module name removed the Module/method name`. For example, the interface address here is:  
<http://127.0.0.1:1234/api/Hello/test_hello>

The HTTPS approach is not supported here, and considering that it is not recommended to expose services directly to the outside world, it is recommended to use gateways (kong, ocelot) or reverse proxies (nginx, apache) for real deployment.

### Attribute

The method must have Web method Attribute [WEBMethod], with two parameters, method description and method description in detail;The latter can be omitted and the differences are as follows:

* [WEBMethod]：Unlimited HTTP request type then specified
* [WEBMethod.GET]：Only GET requests can be invoke
* [WEBMethod.PUT]：Only PUT requests can be invoke
* [WEBMethod.POST]：Only POST requests can be invoke
* [WEBMethod.DELETE]：Only DELETE requests can be invoke

### Parameter

Parameters can be specified in two types. The first type is the request parameter, which is what the caller needs to pass when the method is called.General parameters can be specified directly as types.

You can then look at the method's return values and parameters.It is designed to be very flexible, both to implement 'Rest RPC' in the simplest way and to provide HTTP services as a normal HTTP server.Here's an example of a method:

```csharp
[WEBMethod ("test method1")]
public static string test_hello1 (string name) {
    return $"hello, {name}";
}
```

This method is invoke by <http://127.0.0.1:1234/api/Hello/test_hello1?name=michael>

The name parameter can be a GET variable or a POST variable.The return contents are: `{"result":"success","content":"hello, michael"}`

There is another type of parameter, called the request variable type, which does not need to be passed by the caller but takes its value directly from the request session.Four types are currently supported, for example:

```csharp
[WEBMethod ("test test_context")]
public static void test_context (FawRequest _req, FawResponse _res) {
    _res.write ("hello world");
}

[WEBMethod ("test test_context")]
public static string test_context1 ([WEBParam.IP] string ip, [WEBParam.AgentIP] string agent_ip) {
    return "hello world";
}
```

The 'test_context' method is the primitive type request, this request does not need to return a value, if you need to return content directly through the _res variable method directly processed.For example, use the write method to return content here.At this point, the content written will not go through json wrapping, and the return data obtained by the caller is 'hello world'.

The two parameters of the 'test_context1' method have WEBParam attributes, respectively. The marked method does not need to be passed by the caller and is taken directly from the request content.Where 'webparam.IP' means to obtain the IP of the caller (note: the caller may forge this parameter), and 'webparam.AgentIP', which is the IP of the forward proxy used by the caller (note: the caller may forge this parameter).

### Type of return value

The return value is valid without 'FawResponse' in the parameter.When 'byte' or 'byte []' type is returned, the returned content will not be processed and will be returned directly to the caller.If you return other types, such as' int ', 'string', etc., it will go through json wrapping;Regardless of the return value type, throwing an exception directly returns a json-wrapped error prompt.Since exception handling is very inefficient in C#, the actual testing will result in very low QPS, so it is recommended to add 'try...Catch' to all function handling to avoid exception passing.

### Task returns

Task returns refers to the web method that returns 'Task' or 'Task\<T>', regardless of whether 'async' keyword is added.Any task method, like any normal web method, returns to callers synchronously.Examples are as follows:

```csharp
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
```
