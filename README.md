# AWS X-Ray SDK for .NET and .NET Core

![Screenshot of the AWS X-Ray console](images/example_servicemap.png?raw=true)

## Installing

The AWS X-Ray SDK for .NET and .NET Core (.netstandard 2.0 and above) is in the form of Nuget packages. You can install the packages from [Nuget](https://www.nuget.org/packages?q=AWSXRayRecorder) gallery or from Visual Studio editor. Search `AWSXRayRecorder*` to see various middlewares available.  

## Getting Help

Use the following community resources for getting help with the SDK. We use the GitHub issues for tracking bugs and feature requests.

* Ask a question in the [AWS X-Ray Forum](https://forums.aws.amazon.com/forum.jspa?forumID=241&start=0).
* Open a support ticket with [AWS Support](http://docs.aws.amazon.com/awssupport/latest/user/getting-started.html).
* If you think you may have found a bug, open an [issue](https://github.com/aws/aws-xray-sdk-dotnet/issues/new).

## Opening Issues

If you encounter a bug with the AWS X-Ray SDK for .NET/.NET Core, we want to hear about
it. Before opening a new issue, search the [existing issues](https://github.com/aws/aws-xray-sdk-dotnet/issues) to see if others are also experiencing the issue. Include platform (.NET/ .NET Core). 
In addition, include the repro case when appropriate.

The GitHub issues are intended for bug reports and feature requests. For help and questions about using the AWS X-Ray SDK for .NET and .NET Core, use the resources listed
in the [Getting Help](https://github.com/aws/aws-xray-sdk-dotnet#getting-help) section. Keeping the list of open issues lean helps us respond in a timely manner.

## Documentation

The [developer guide](https://docs.aws.amazon.com/xray/latest/devguide) provides in-depth guidance about using the AWS X-Ray service.
Following API reference documentation provides guidance for using the SDK and module-level documentation.
* The [API Reference for .NET](http://docs.aws.amazon.com/xray-sdk-for-dotnet/latest/reference/index.html)
* The [API Reference for .NET Core](http://docs.aws.amazon.com/xray-sdk-for-dotnetcore/latest/reference/index.html)
* [Sample Apps](https://github.com/aws-samples/aws-xray-dotnet-webapp)

## Quick Start

1. [Configuration](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#configuration)
2. [ASP.NET Core Framework](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#aspnet-core-framework-net-core--nuget)
3. [ASP.NET Framework](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#aspnet-framework-net--nuget)
4. [Trace AWS SDK request](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#trace-aws-sdk-request-net-and-net-core--nuget) 
5. [Trace out-going HTTP requests](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#trace-out-going-http-requests-net-and-net-core--nuget)
6. [Trace Query to SQL Server](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#trace-query-to-sql-server-net-and-net-core--nuget)
7. [Multithreaded Execution](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#multithreaded-execution-net-and-net-core--nuget)
8. [Trace custom methods ](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#trace-custom-methods-net-and-net-core)
9. [Creating custom Segment/Subsegment](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#creating-custom-segmentsubsegment-net-and-net-core)
10. [Adding metadata/annotations](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#adding-metadataannotations-net-and-net-core)
11. [AWS Lambda support (.NET Core)](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#aws-lambda-support-net-core)
12. [ASP.NET Core on AWS Lambda](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#aspnet-core-on-aws-lambda-net-core)
13. [Logging](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#logging-net)
14. [Enabling X-Ray on Elastic Beanstalk](https://docs.aws.amazon.com/xray/latest/devguide/xray-services-beanstalk.html)
15. [Enabling X-Ray on AWS Lambda](https://docs.aws.amazon.com/xray/latest/devguide/xray-services-lambda.html)

## Configuration

### .NET

You can configure X-Ray in the `appsettings` of your `App.config` or `Web.config` file.

```xml
<configuration>
  <appSettings>
    <add key="DisableXRayTracing" value="false"/>
    <add key="AWSXRayPlugins" value="EC2Plugin, ECSPlugin, ElasticBeanstalkPlugin"/>
    <add key="SamplingRuleManifest" value="JSONs\DefaultSamplingRules.json"/>
    <add key="AwsServiceHandlerManifest" value="JSONs\AWSRequestInfo.json"/>
    <add key="UseRuntimeErrors" value="true"/>
  </appSettings>
</configuration>
```

### .NET Core

Following are the steps to configure your .NET Core project with X-Ray.

a) In `appsettings.json` file, configure items under `XRay` key

```
{
  "XRay": {
    "DisableXRayTracing": "false",
    "SamplingRuleManifest": "SamplingRules.json",
    "AWSXRayPlugins": "EC2Plugin, ECSPlugin, ElasticBeanstalkPlugin",
    "AwsServiceHandlerManifest": "JSONs\AWSRequestInfo.json",
    "UseRuntimeErrors":"true"
  }
}
```

b) Register `IConfiguration` instance with X-Ray:

```csharp
using Amazon.XRay.Recorder.Core;
AWSXRayRecorder.InitializeInstance(configuration); // pass IConfiguration object that reads appsettings.json file
```

*Note*:  
1. You should configure this before initialization of `AWSXRayRecorder` instance and using any AWS X-Ray methods.  
2. If you manually need to configure `IConfiguration` object refer: [Link](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?tabs=basicconfiguration)  
3. For more information on configuration, please refer : [Link](https://docs.aws.amazon.com/xray/latest/devguide/xray-sdk-dotnet-configuration.html)

## How to Use

### Incoming Requests

### ASP.NET Core Framework (.NET Core) : [Nuget](https://www.nuget.org/packages/AWSXRayRecorder.Handlers.AspNetCore/)

You can instrument X-Ray for your `ASP.NET Core` App in the `Configure()` method of `Startup.cs` file of your project.  
*Note* :  
1. Use `app.UseXRay()` middleware after `app.UseExceptionHandler("/Error")` in order to catch exceptions.  
2. You need to install `Amazon.XRay.Recorder.Handlers.AspNetCore` nuget package. This package adds extension methods to the `IApplicationBuilder` to make it easy to register AWS X-Ray to the ASP.NET Core HTTP pipeline.

A) With default configuration:

```csharp
using Microsoft.AspNetCore.Builder;

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    app.UseExceptionHandler("/Error");
    app.UseXRay("SampleApp"); // name of the app
    app.UseStaticFiles(); // rest of the middlewares
    app.UseMVC();
}
```

B) With custom X-Ray configuration  

```csharp
using Microsoft.AspNetCore.Builder;

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    app.UseExceptionHandler("/Error");
    app.UseXRay("SampleApp",configuration); // IConfiguration object is not required if you have used "AWSXRayRecorder.InitializeInstance(configuration)" method
    app.UseStaticFiles(); // rest of the middlewares
    app.UseMVC();	
}
```

Instead of name you can also pass `SegmentNamingStrategy` in the above two ways. Please refer: [Link](https://docs.aws.amazon.com/xray/latest/devguide/xray-sdk-dotnet-messagehandler.html#xray-sdk-dotnet-messagehandler-naming)  

### ASP.NET Framework (.NET) : [Nuget](https://www.nuget.org/packages/AWSXRayRecorder.Handlers.AspNet/)

*Note*: We are deprecating `TracingMessageHandler` class (approach 2) that supports just WEB API. Approach 1 includes WEB API support. 

**1. HTTP Message handler for ASP.NET framework (*Recommended*)**  
Register your application with X-Ray in the `Init()` method of ***global.asax*** file

```csharp
using Amazon.XRay.Recorder.Handlers.AspNet;

public class MvcApplication : System.Web.HttpApplication
{
     public override void Init()
     {
        base.Init();
        AWSXRayASPNET.RegisterXRay(this, "ASPNETTest"); // default name of the web app
     }
}
```

At the start of each Http request, a `segment` is created and stored in the `context` (Key : AWSXRayASPNET.XRayEntity) of `HttpApplication` instance. If users write their custom error handler for ASP.NET framework, they can access `segment` for the current request by following way : 

```csharp
<%@ Import Namespace="Amazon.XRay.Recorder.Handlers.AspNet" %>
<%@ Import Namespace="Amazon.XRay.Recorder.Core.Internal.Entities" %>
<script runat="server">
  protected void Page_Load(object sender, EventArgs e)
  {
     var context = System.Web.HttpContext.Current.ApplicationInstance.Context;
     var segment = (Segment) context.Items[AWSXRayASPNET.XRayEntity]; // get segment from the context
     segment.AddMetadata("Error","404");
  }
</script>
```

**2. HTTP Message handler for ASP.NET WEB API (*Deprecated*)**  

On the server side, the Web API pipeline invokes message handler before the request reaches controller, and after the response leaves controller. We recommend you inserting the `TracingMessageHandler` as the first message handler to have maximum tracing coverage.
To add a message handler on the server side, add the handler to the `HttpConfiguration.MessageHandlers` collection.

```csharp
using AWSXRayRecorder.Handlers.AspNet.WebApi;

public static class WebApiConfig
{
    public static void Register(HttpConfiguration config)
    {
        // Add the message handler to HttpCofiguration
    	config.MessageHandlers.Add(new TracingMessageHandler(new FixedSegmentNamingStrategy("defaultName")));

        // Other code not shown...
    }
}
```

Or add the message handler to ***global.asax*** file

```csharp
GlobalConfiguration.Configuration.MessageHandlers.Add(new TracingMessageHandler(new FixedSegmentNamingStrategy("defaultName")));
```

### Trace AWS SDK request (.NET and .NET Core) : [Nuget](https://www.nuget.org/packages/AWSXRayRecorder.Handlers.AwsSdk/)

*Note*: We recommend using *approach 1* since its easy to configure, works well with ASP.NET Core dependency injection and handles async operations in better way. Avoid using both approaches in same project to maintain consistency in results.

1) Following way is *recommended* (.NET and .NET Core):

```csharp
using Amazon.XRay.Recorder.Handlers.AwsSdk;

AWSSDKHandler.RegisterXRayForAllServices(); //place this before any instantiation of AmazonServiceClient
AmazonDynamoDBClient client = new AmazonDynamoDBClient(RegionEndpoint.USWest2); // AmazonDynamoDBClient is automatically registered with X-Ray
```

Methods of `AWSSDKHandler` class:

```csharp
AWSSDKHandler.RegisterXRayForAllServices(); // all instances of AmazonServiceClient created after this line are registered

AWSSDKHandler.RegisterXRay<IAmazonDynamoDB>(); // Registers specific type of AmazonServiceClient : All instances of IAmazonDynamoDB created after this line are registered

AWSSDKHandler.RegisterXRayManifest(String path); // To configure custom AWS Service Manifest file. This is optional, if you have followed "Configuration" section
```

2) Following way is *deprecated* (.NET):

```csharp
using using Amazon.XRay.Recorder.Handlers.AwsSdk;

var ddbClient = new AmazonDynamoDBClient(RegionEndpoint.USEast1);
new AwsSdkTracingHandler(AWSXRayRecorder.Instance).AddEventHandler(client);
```

The `AddEventHandler()` method will subscribe to `BeforeRequestEvent`, `AfterResponseEvent`, `ExceptionEvent` in the service client.

### Trace out-going HTTP requests (.NET and .NET Core) : [Nuget](https://www.nuget.org/packages/AWSXRayRecorder.Handlers.System.Net/)

#### Using `System.Net.HttpWebRequest`

#### Synchronous request

An extension method `GetResponseTraced()` is provided to trace `GetResponse()` in `System.Net.HttpWebRequest` class. If you want to trace the out-going HTTP request, call the `GetResponseTraced()` instead of `GetResponse()`. The extension method will generate a trace subsegment, inject the trace header to the out-going HTTP request header and collect trace information. 

```csharp
using AWSXRayRecorder.Handlers.System.Net;

HttpWebRequest request = (HttpWebRequest) WebRequest.Create(URL); // enter desired url

// Any other configuration to the request

request.GetResponseTraced();
```

#### Asynchronous request

An extension method `GetAsyncResponseTraced()` is provided to trace `GetResponseAsync()` in `System.Net.HttpWebRequest` class. If you want to trace the out-going HTTP request, call the `GetAsyncResponseTraced()` instead of `GetResponseAsync()`. The extension method will generate a trace subsegment, inject the trace header to the out-going HTTP request header and collect trace information. 

```csharp
using AWSXRayRecorder.Handlers.System.Net;

HttpWebRequest request = (HttpWebRequest) WebRequest.Create(URL); // enter desired url

// Any other configuration to the request

request.GetAsyncResponseTraced();
```

#### Using `System.Net.HttpClient`

A handler derived from `DelegatingHandler` is provided to trace the `HttpMessageHandler.SendAsync` method

```csharp
using AWSXRayRecorder.Handlers.System.Net;

var httpClient = new HttpClient(new HttpClientXRayTracingHandler(new HttpClientHandler()));

// Any other configuration to the client

httpClient.GetAsync(URL);
```

### Trace Query to SQL Server (.NET and .NET Core) : [Nuget](https://www.nuget.org/packages/AWSXRayRecorder.Handlers.SqlServer/)

The SDK provides a wrapper class for `System.Data.SqlClient.SqlCommand`. The wrapper class can be used interchangable with `SqlCommand` class. By replacing instance of `SqlCommand` to `TraceableSqlCommand`, synchronized/asynchronized method will automatically generate subsegment for the SQL query.

#### Synchronous query

```csharp
using AWSXRayRecorder.Handlers.SqlServer;

using (var connection = new SqlConnection("fake connection string"))
using (var command = new TraceableSqlCommand("SELECT * FROM products", connection))
{
    command.ExecuteNonQuery();
}
```

#### Asynchronous query

```csharp
using AWSXRayRecorder.Handlers.SqlServer;

using (var connection = new SqlConnection(ConnectionString))
{
    var command = new TraceableSqlCommand("SELECT * FROM Products FOR XML AUTO, ELEMENTS", connection);
    command.Connection.Open();
    await command.ExecuteXmlReaderAsync();
}
```

### Multithreaded Execution (.NET and .NET Core) : [Nuget](https://www.nuget.org/packages/AWSXRayRecorder.Core/)

In multithreaded execution, X-Ray context from current to its child thread is automatically set.  

```csharp
using AWSXRayRecorder.Core;

private static void TestMultiThreaded()
{
    int numThreads = 3;
    AWSXRayRecorder.Instance.BeginSegment("MainThread");
    Thread[] t= new Thread[numThreads];
 
    for(int i = 0; i < numThreads; i++)
    {
    	t[i] = new Thread(()=>MakeHttpRequest(i)); 
        t[i].Start();
    }
    for (int i = 0; i < numThreads; i++)
    {
        t[i].Join();
    }

    AWSXRayRecorder.Instance.EndSegment();
}

private static void MakeHttpRequest(int i)
{
    AWSXRayRecorder.Instance.TraceMethodAsync("Thread "+i, CreateRequestAsync<HttpResponseMessage>).Wait();
}

private static async Task<HttpResponseMessage> CreateRequestAsync <TResult>()
{
    var request = new HttpClient();
    var result = await request.GetAsync(URL); // Enter desired url
    return result;
}
```

*Note*:
1. Context used to save traces in .NET : [CallContext](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.remoting.messaging.callcontext?view=netframework-4.5)
2. Context used to save traces in .NET Core : [AsyncLocal](https://docs.microsoft.com/en-us/dotnet/api/system.threading.asynclocal-1?view=netcore-2.0)

### Trace custom methods (.NET and .NET Core)

It may be useful to further decorate portions of an application for which performance is critical. Generating subsegments around these hot spots will help in understanding their impact on application performance.

#### Synchronous method

```csharp
using AWSXRayRecorder.Core;

AWSXRayRecorder.Instance.TraceMethod("custom method", () => DoSomething(arg1, arg2, arg3));
```

#### Asynchronous method

```csharp
using AWSXRayRecorder.Core;

var response = await AWSXRayRecorder.Instance.TraceMethodAsync("AddProduct", () => AddProduct<Document>(product));

private async Task<Document> AddProduct <TResult>(Product product)
{
    var document = new Document();
    document["Id"] = product.Id;
    document["Name"] = product.Name;
    document["Price"] = product.Price;
    return await LazyTable.Value.PutItemAsync(document);
}
```

### Creating custom Segment/Subsegment (.NET and .NET Core)

#### Segment
```csharp
using AWSXRayRecorder.Core;

AWSXRayRecorder.Instance.BeginSegment("segment name"); // generates `TraceId` for you
try
{
    DoSometing();
    // can create custom subsegments
}
catch (Exception e)
{
    AWSXRayRecorder.Instance.AddException(e);
}
finally
{
    AWSXRayRecorder.Instance.EndSegment();
}
```

If you want to pass custom `TraceId`:

```csharp
using AWSXRayRecorder.Core;

String traceId = TraceId.NewId(); // This function is present in : Amazon.XRay.Recorder.Core.Internal.Entities
AWSXRayRecorder.Instance.BeginSegment("segment name",traceId); // custom traceId used while creating segment
try
{
    DoSometing();
    // can create custom subsegments
}
catch (Exception e)
{
    AWSXRayRecorder.Instance.AddException(e);
}
finally
{
    AWSXRayRecorder.Instance.EndSegment();
}
```

#### Subsegment

*Note*: This should only be used after `BeginSegment()` method.  
```csharp
using AWSXRayRecorder.Core;

AWSXRayRecorder.Instance.BeginSubsegment("subsegment name");
try
{
    DoSometing();
}
catch (Exception e)
{
    AWSXRayRecorder.Instance.AddException(e);
}
finally
{
    AWSXRayRecorder.Instance.EndSubsegment();
}
```



### Adding metadata/annotations (.NET and .NET Core)

```csharp
using Amazon.XRay.Recorder.Core;
AWSXRayRecorder.Instance.AddAnnotation("mykey", "my value");
AWSXRayRecorder.Instance.AddMetadata("my key", "my value");
```

### AWS Lambda support (.NET Core)

You can create `Subsegment` inside lambda function.     
*Note*: The AWS Lambda execution environment creates a `Segment` before the Lambda function is invoked, so a `Segment` cannot be created inside the Lambda function.

```csharp
public string FunctionHandler(string input, ILambdaContext context)
{
    AWSXRayRecorder recorder = new AWSXRayRecorder();
    recorder.BeginSubsegment("UpperCase");
    recorder.BeginSubsegment("Inner 1");
    String result = input?.ToUpper();
    recorder.EndSubsegment();
    recorder.BeginSubsegment("Inner 2");
    recorder.EndSubsegment();
    recorder.EndSubsegment();
    return result;
}
```

### ASP.NET Core on AWS Lambda (.NET Core)

We support instrumenting ASP.NET Core web app on Lambda. Please follow the steps of [ASP.NET Core](https://github.com/aws/aws-xray-sdk-dotnet/tree/release#aspnet-core-framework-net-core) instrumentation.

### Logging (.NET)

The AWS X-Ray .NET SDK share the same logging mechanism as AWS .NET SDK. If the application had already configured logging for AWS .NET SDK, it should just work for AWS X-Ray .NET SDK.
The recommended way to configure an application is to use the <aws> element in the project’s `App.config` or `Web.config` file.

```xml
<configuration>
  <configSections>
    <section name="aws" type="Amazon.AWSSection, AWSSDK.Core"/>
  </configSections>
  <aws>
    <logging logTo="Log4Net"/>
  </aws>
</configuration>
```

Other ways to configure logging is to edit the <appsetting> element in the `App.config` or `Web.config` file, and set property values in the `AWSConfig` class. Refer to the following page for more details and example : [Link](http://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-config.html)


### Logging (.NET Core)

Currently we support `log4net` logging and options provided in [LoggingOptions](https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/Amazon/TLoggingOptions.html). You should configure logging before initialization of `AWSXRayRecorder` instance or using any X-Ray methods.  
Following is the way to configure logging with X-Ray SDK:

```csharp
using Amazon;
using Amazon.XRay.Recorder.Core;

class Program
{
    static Program()
    {
         AWSXRayRecorder.RegisterLogger(LoggingOptions.Log4Net); // Log4Net instance should already be configured before this point
    }
}
``` 

log4net.config example:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<log4net>
  <appender name="FileAppender" type="log4net.Appender.FileAppender,log4net">
    <file value="c:\logs\sdk-log.txt" />
    <layout type="log4net.Layout.PatternLayout">
      <conversionPattern value="%date [%thread] %level %logger - %message%newline" />
    </layout>
  </appender>
  <logger name="Amazon">
    <level value="DEBUG" />
    <appender-ref ref="FileAppender" />
  </logger>
</log4net>
```

*Note*: For `log4net` configuration, refer : [Link](https://logging.apache.org/log4net/release/manual/configuration.html)

## License

The AWS X-Ray SDK for .NET and .NET Core is licensed under the Apache 2.0 License. See LICENSE and NOTICE.txt for more information.
