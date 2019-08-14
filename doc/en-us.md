# Sparrow English document

I'll walk you through the project with this sample code.

```csharp
[HTTPModule ("hello world模块")]
public class HelloModule {
    [HTTP ("Test Hello World")]
    public static string test_hello () {
        return "hello world";
    }
}
```

## Sparrow Module

There are three conditions for a module to be a Sparrow module:

* Add the [HTTPModule] Attribute to the module
* The class name of a Module needs to end with 'Module', such as' UserServiceModule ', 'TestModule', etc
* A module can only be in one project, and then needs to pass the project Assembly to HttpServer before it finally takes effect

## Sparrow Static Method

The service method must be within the service module class, which means that if the class does not have the sparrow service module Attribute (HTTPModule), the method is not identifiable. The other method must be declared static

```csharp
[HTTP ("test method")]
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

Sparrow provides swagger document, after start the project, by visiting `http(s)://127.0.0.1:port/swagger/index.html` can see interface document, whether HTTPS is enabled depends on whether the set_ssl_file function is eventually called (the protocol is assumed to be HTTP).

### URL of the interface

Method the default external interface address is: `http(s)://127.0.0.1:port/api/Module name removed the Module/method name`. For example, the interface address here is:  
<http://127.0.0.1:1234/api/Hello/test_hello>

### Attribute

The method must have Web method Attribute [HTTP], with two parameters, method description and method description in detail;The latter can be omitted and the differences are as follows:

* [HTTP]：Unlimited HTTP request type then specified
* [HTTP.GET]：Only GET requests can be invoke
* [HTTP.PUT]：Only PUT requests can be invoke
* [HTTP.POST]：Only POST requests can be invoke
* [HTTP.DELETE]：Only DELETE requests can be invoke

### Parameter

Parameters can be specified in two types. The first type is the request parameter, which is what the caller needs to pass when the method is called.General parameters can be specified directly as types.

You can then look at the method's return values and parameters.It is designed to be very flexible, both to implement 'Rest RPC' in the simplest way and to provide HTTP services as a normal HTTP server.Here's an example of a method:

```csharp
[HTTP ("test method1")]
public static string test_hello1 (string name) {
    return $"hello, {name}";
}
```

This method is invoke by <http://127.0.0.1:1234/api/Hello/test_hello1?name=michael>

The name parameter can be a GET variable or a POST variable.The return contents are: `{"result":"success","content":"hello, michael"}`

Parameters can be annotated as required.When added, the description of the parameters can be automatically generated when the document is generated. Example:

```csharp
[HTTP ("test method1")]
public static string test_hello1 ([Param ("test name")] string name) {
    return $"hello, {name}";
}
```

There is another type of parameter, called the request variable type, which does not need to be passed by the caller but takes its value directly from the request session.Four types are currently supported, for example:

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

The 'test_context' method is the primitive type request, this request does not need to return a value, if you need to return content directly through the _res variable method directly processed.For example, use the write method to return content here.At this point, the content written will not go through json wrapping, and the return data obtained by the caller is 'hello world'.

The two parameters of the 'test_context1' method have ReqParam attributes, respectively. The marked method does not need to be passed by the caller and is taken directly from the request content.Where 'ReqParam.IP' means to obtain the IP of the caller (note: the caller may forge this parameter), and 'ReqParam.AgentIP', which is the IP of the forward proxy used by the caller (note: the caller may forge this parameter).

### Type of return value

The return value is valid without 'FawResponse' in the parameter.When 'byte' or 'byte []' type is returned, the returned content will not be processed and will be returned directly to the caller.If you return other types, such as' int ', 'string', etc., it will go through json wrapping;Regardless of the return value type, throwing an exception directly returns a json-wrapped error prompt.

### Task returns

Task returns refers to the web method that returns 'Task' or 'Task\<T>', regardless of whether 'async' keyword is added.Any task method, like any normal web method, returns to callers synchronously.Examples are as follows:

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

### Custom structure

Custom structures currently only support json serialization. Examples are as follows:

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

Invoke method (of course, parameters can be passed by post, here through GET to facilitate the browser to directly test the call):

<http://127.0.0.1:1234/api/Hello/test_struct?val={%22name%22:%22kangkang%22,%22age%22:18}>

Return to the result:

```json
{"result":"success","content":{"name":"kangkang","age":19}}
```

## Sparrow Authorization

### Description

Currently, Sparrow supports JWT authentication.The idea of JWT authentication is: first, the server saves a secret key, and then gives it to the front end by issuing JWT, which contains a small amount of custom data and expiration time.The front end adds:

```http header
X-API-Key: token_data...
```

You can call the authentication method.One of the advantages of this approach is that the server does not need to store Session information, but there is one caveat: the issuing secret key on the server side must not be disclosed, otherwise it needs to be modified.Recommended single application:

```csharp
Guid.NewGuid ().ToString ("N");
```

This way to obtain, can largely ensure the security of the secret key, if accidentally leaked, restart the server on the line.

### Usage

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

First generate static method has two attributes, `[HTTP.GET]` and `[JWTGen]`, which mean, the Web method is used for issuing the API - the Key, with this annotation, the return value types must be `(JObject, DateTime)` JObject, DateTime, the former for the user to store the data content (name can't for exp), which represents the term of validity of the Key issued such as the Key here is valid for two minutes.At this point, the front end calls this static method and gets the api-key.

The second method, _check_auth, has the '[JWTRequest]' attribute, which can only have one per HTTP service class, to construct objects from when a user wants to request a non-static method.You can call this function to indicate that it has been authenticated, and you need to manually validate your stored JWT data to generate the object.There are two ways to do this, new one or get it out of the dictionary cache, depending on your business.

And then the get_value method.If the user wants to request this non-static method, the HTTP header needs to be set first: `x-api-key`. The content is the Key signed by the `[JWTGen]` method. Sparrow will then verify that the Key is valid, which will generate the object by calling the annotated `[JWTRequest]` method and then calling get_value. Here the test call shows that the value of the variable `n` is 100.
