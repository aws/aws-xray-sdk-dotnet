![Build Status](https://github.com/aws/aws-xray-sdk-dotnet/actions/workflows/ci.yml/badge.svg)

### :mega: OpenTelemetry .NET with AWS X-Ray

AWS X-Ray recommends using AWS Distro for OpenTelemetry (ADOT) to instrument your application **instead of this X-Ray SDK** due to its wider range of features and instrumentations. See the [AWS X-Ray docs on Working with .NET](https://docs.aws.amazon.com/xray/latest/devguide/xray-dotnet.html) for more help with choosing between ADOT and X-Ray SDK.

If you want additional features when tracing your .NET applications, please [open an issue on the OpenTelemetry .NET Instrumentation repository](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/issues/new?labels=enhancement&template=miscellaneous.md&title=X-Ray%20Compatible%20Feature%20Request).

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
* AWS X-Ray SDK Documentation for [.NET SDK](https://docs.aws.amazon.com/xray/latest/devguide/xray-sdk-dotnet.html)
* [Sample Apps](https://github.com/aws-samples/aws-xray-dotnet-webapp)

## Quick Start

1. [Configuration](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#configuration)
2. [ASP.NET Core Framework](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#aspnet-core-framework-net-core--nuget)
3. [ASP.NET Framework](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#aspnet-framework-net--nuget)
4. [Trace AWS SDK request](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#trace-aws-sdk-request-net-and-net-core--nuget) 
5. [Trace out-going HTTP requests](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#trace-out-going-http-requests-net-and-net-core--nuget)
6. [Trace Query to SQL Server](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#trace-query-to-sql-server-net-and-net-core--nuget)
7. [Trace SQL Query through Entity Framework](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#trace-sql-query-through-entity-framework-net-and-net-core--nuget)
8. [Multithreaded Execution](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#multithreaded-execution-net-and-net-core--nuget)
9. [Trace custom methods ](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#trace-custom-methods-net-and-net-core)
10. [Creating custom Segment/Subsegment](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#creating-custom-segmentsubsegment-net-and-net-core)
11. [Adding metadata/annotations](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#adding-metadataannotations-net-and-net-core)
12. [AWS Lambda support (.NET Core)](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#aws-lambda-support-net-core)
13. [ASP.NET Core on AWS Lambda](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#aspnet-core-on-aws-lambda-net-core)
14. [Logging](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#logging-net)
15. [Enabling X-Ray on Elastic Beanstalk](https://docs.aws.amazon.com/xray/latest/devguide/xray-services-beanstalk.html)
16. [Enabling X-Ray on AWS Lambda](https://docs.aws.amazon.com/xray/latest/devguide/xray-services-lambda.html)

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
    <add key="UseRuntimeErrors" value="false"/>
    <add key="CollectSqlQueries" value="false"/>
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
    "UseRuntimeErrors":"false",
    "CollectSqlQueries":"false"
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

### Programmatic Configuration (.NET and .NET Core)

Alternatively, you can also set up the `AWSXRayRecorder` instance programmatically by using the `AWSXRayRecorderBuilder` class instead of a configuration file. 
For initializing an AWSXRayRecorder instance with default configurations, simply do the following.
```csharp
using Amazon.XRay.Recorder.Core;

AWSXRayRecorder recorder = new AWSXRayRecorderBuilder().Build();
AWSXRayRecorder.InitializeInstance(recorder: recorder);
```

The following code initializes an `AWSXRayRecorder` instance with a custom `IStreamingStrategy` and a custom `ISamplingStrategy`. 
```csharp
using Amazon.XRay.Recorder.Core;

AWSXRayRecorder recorder = new AWSXRayRecorderBuilder().WithStreamingStrategy(new CustomStreamingStrategy()).WithSamplingStrategy(CustomSamplingStrategy()).Build();
AWSXRayRecorder.InitializeInstance(recorder: recorder);
```

*Note*:
1. `CustomStreamingStrategy` and `CustomSamplingStrategy` must implement `IStreamingStrategy` and `ISamplingStrategy` before being used to build the `recorder`.
2. `recorder` must be instantiated using `AWSXRayRecorder.InitializeInstance(recorder: recorder)` before being used in the program. 




## How to Use

### Incoming Requests

### ASP.NET Core Framework (.NET Core) : [Nuget](https://www.nuget.org/packages/AWSXRayRecorder.Handlers.AspNetCore/)

You can instrument X-Ray for your `ASP.NET Core` App in the `Configure()` method of `Startup.cs` file of your project.  
*Note* :  
1. For .Net Core 2.1 and above, use `app.UseXRay()` middleware **before** any other middleware to trace incoming requests. For .Net Core 2.0 place the `app.UseXRay()` middleware **after** the `app.UseExceptionHandler("/Error")` in order to catch exceptions. You would be able to see any runtime exception with its stack trace, however, the status code might show 200 due to a known limitation of the ExceptionHandler middleware in .Net Core 2.0. 
2. You need to install `AWSXRayRecorder.Handlers.AspNetCore` nuget package. This package adds extension methods to the `IApplicationBuilder` to make it easy to register AWS X-Ray to the ASP.NET Core HTTP pipeline.

A) With default configuration:

* For .Net Core 2.1 and above: 

```csharp
using Microsoft.AspNetCore.Builder;

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    app.UseXRay("SampleApp"); // name of the app
    app.UseExceptionHandler("/Error");
    app.UseStaticFiles(); // rest of the middlewares
    app.UseMVC();
}
```

* For .Net Core 2.0: 

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

**HTTP Message handler for ASP.NET framework**  
Register your application with X-Ray in the `Init()` method of ***Global.asax*** file

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

### Trace AWS SDK request (.NET and .NET Core) : [Nuget](https://www.nuget.org/packages/AWSXRayRecorder.Handlers.AwsSdk/)

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

### Trace out-going HTTP requests (.NET and .NET Core) : [Nuget](https://www.nuget.org/packages/AWSXRayRecorder.Handlers.System.Net/)

#### Using `System.Net.HttpWebRequest`

#### Synchronous request

An extension method `GetResponseTraced()` is provided to trace `GetResponse()` in `System.Net.HttpWebRequest` class. If you want to trace the out-going HTTP request, call the `GetResponseTraced()` instead of `GetResponse()`. The extension method will generate a trace subsegment, inject the trace header to the out-going HTTP request header and collect trace information. 

```csharp
using Amazon.XRay.Recorder.Handlers.System.Net;

HttpWebRequest request = (HttpWebRequest) WebRequest.Create(URL); // enter desired url

// Any other configuration to the request

request.GetResponseTraced();
```

for query parameter stripped http requests in trace 

```csharp
using Amazon.XRay.Recorder.Handlers.System.Net;

HttpWebRequest request = (HttpWebRequest) WebRequest.Create(URL); // enter desired url

// Any other configuration to the request

request.GetResponseTraced(true);
```

#### Asynchronous request

An extension method `GetAsyncResponseTraced()` is provided to trace `GetResponseAsync()` in `System.Net.HttpWebRequest` class. If you want to trace the out-going HTTP request, call the `GetAsyncResponseTraced()` instead of `GetResponseAsync()`. The extension method will generate a trace subsegment, inject the trace header to the out-going HTTP request header and collect trace information. 

```csharp
using Amazon.XRay.Recorder.Handlers.System.Net;

HttpWebRequest request = (HttpWebRequest) WebRequest.Create(URL); // enter desired url

// Any other configuration to the request

request.GetAsyncResponseTraced();
```

for query parameter stripped http requests in trace 

```csharp
using Amazon.XRay.Recorder.Handlers.System.Net;

HttpWebRequest request = (HttpWebRequest) WebRequest.Create(URL); // enter desired url

// Any other configuration to the request

request.GetAsyncResponseTraced(true);
```

#### Using `System.Net.HttpClient`

A handler derived from `DelegatingHandler` is provided to trace the `HttpMessageHandler.SendAsync` method

```csharp
using Amazon.XRay.Recorder.Handlers.System.Net;

var httpClient = new HttpClient(new HttpClientXRayTracingHandler(new HttpClientHandler()));

// Any other configuration to the client

httpClient.GetAsync(URL);
```

If you want to santize the Http request tracing then define the Tracing Handler as - 

```CSharp

using Amazon.XRay.Recorder.Handlers.System.Net;

var httpClient = new HttpClient(new HttpClientXRaySanitizedTracingHandler(new HttpClientHandler()));

// Any other configuration to the client

httpClient.GetAsync(URL);

```

#### Using `System.Net.Http.HttpClientFactory` (.Net Core 2.1 and above)

The `Amazon.XRay.Recorder.Handlers.System.Net` package includes a delegate that can be used to trace outbound requests without the need to specifically wrap outbound requests from that class.

Register the `HttpClientXRayTracingHandler` as a middleware for your http client.

```csharp
services.AddHttpClient<IFooClient, FooClient>()
        .AddHttpMessageHandler<HttpClientXRayTracingHandler>();
```
or

```csharp
services.AddHttpClient("foo")
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new HttpClientXRayTracingHandler(new HttpClientHandler());
        });
```

And to get sanitized http requests in tracing 

```csharp
services.AddHttpClient<IFooClient, FooClient>()
        .AddHttpMessageHandler<HttpClientXRaySanitizedTracingHandler>();
```
or

```csharp
services.AddHttpClient("foo")
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new HttpClientXRaySanitizedTracingHandler(new HttpClientHandler());
        });
```

Use the above client factory to create clients with outgoing requests traced.

```csharp
var client = _clientFactory.CreateClient("foo");
var request = new HttpRequestMessage(HttpMethod.Get, "https://www.foobar.com");
var response = await client.SendAsync(request);
```

### Trace Query to SQL Server (.NET and .NET Core) : [Nuget](https://www.nuget.org/packages/AWSXRayRecorder.Handlers.SqlServer/)

The SDK provides a wrapper class for `System.Data.SqlClient.SqlCommand`. The wrapper class can be used interchangable with `SqlCommand` class. By replacing instance of `SqlCommand` to `TraceableSqlCommand`, synchronized/asynchronized method will automatically generate subsegment for the SQL query.

Following examples illustrate the use of `TraceableSqlCommand` to automatically trace SQL Server queries using Synchronous/Asynchronous methods:

#### Synchronous query

```csharp
using Amazon.XRay.Recorder.Handlers.SqlServer;

using (var connection = new SqlConnection("fake connection string"))
using (var command = new TraceableSqlCommand("SELECT * FROM products", connection))
{
    command.ExecuteNonQuery();
}
```

#### Asynchronous query

```csharp
using Amazon.XRay.Recorder.Handlers.SqlServer;

using (var connection = new SqlConnection(ConnectionString))
{
    var command = new TraceableSqlCommand("SELECT * FROM Products FOR XML AUTO, ELEMENTS", connection);
    command.Connection.Open();
    await command.ExecuteXmlReaderAsync();
}
```

#### Capture SQL Query text in the traced SQL calls to SQL Server

 You can also opt in to capture the `SqlCommand.CommandText` as part of the subsegment created for your SQL query. The collected `SqlCommand.CommandText` will appear as `sanitized_query` in the subsegment JSON. By default, this feature is disabled due to security reasons. If you want to enable this feature, it can be done in two ways. First, by setting the `CollectSqlQueries` to `true` in the global configuration for your application as follows:

##### For .Net (In `appsettings` of your `App.config` or `Web.config` file)

```xml
<configuration>
  <appSettings>
    <add key="CollectSqlQueries" value="true">
  </appSettings>
</configuration>
```

##### For .Net Core (In `appsettings.json` file, configure items under XRay key)

```json
{
  "XRay": {
    "CollectSqlQueries":"true"
  }
}
```

This will enable X-Ray to collect all the sql queries made to SQL Server by your application.

Secondly, you can set the `collectSqlQueries` parameter in the `TraceableSqlCommand` instance as `true` to collect the SQL query text for SQL Server query calls made using this instance. If you set this parameter as `false`, it will disable the CollectSqlQuery feature for this `TraceableSqlCommand` instance.

```csharp
using Amazon.XRay.Recorder.Handlers.SqlServer;

using (var connection = new SqlConnection("fake connection string"))
using (var command = new TraceableSqlCommand("SELECT * FROM products", connection, collectSqlQueries: true))
{
    command.ExecuteNonQuery();
}
```

*NOTE:* 
1. You should **not** enable either of these properties if you are including sensitive information as clear text in your SQL queries.
2. Parameterized values will appear in their tokenized form and will not be expanded.
3. The value of `collectSqlQueries` in the `TraceableSqlCommand` instance overrides the value set in the global configuration using the `CollectSqlQueries` property.

### Trace SQL Query through Entity Framework (.NET and .NET Core) : [Nuget](https://www.nuget.org/packages/AWSXRayRecorder.Handlers.EntityFramework/)

#### Setup

##### .NET Core

AWS XRay SDK for .NET Core provides interceptor for tracing SQL query through Entity Framework Core (>=3.0).

For how to start with Entity Framework Core in an ASP.NET Core web app, please take reference to [Link](https://docs.microsoft.com/en-us/aspnet/core/data/ef-mvc/?view=aspnetcore-3.1)

*NOTE:*

* You need to install `AWSXRayRecorder.Handlers.EntityFramework` nuget package. This package adds extension methods to the `DbContextOptionsBuilder` to make it easy to register AWS X-Ray interceptor.
* Not all database provider support Entity Framework Core 3.0 and above, please make sure that you are using the [Nuget package](https://docs.microsoft.com/en-us/ef/core/providers/?tabs=dotnet-core-cli) with a compatible version (EF Core >= 3.0).

*Known Limitation (as of 12-03-2020):* If you're using another `DbCommandInterceptor` implementation along with the `AddXRayInterceptor` in the `DbContext`, it may not work as expected and you may see a "EntityNotAvailableException" from the XRay EFCore interceptor. This is due to [`AsyncLocal`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.asynclocal-1?view=netcore-2.0) not being able to maintain context across the `ReaderExecutingAsync` and `ReaderExecutedAsync` methods. Ref [here](https://github.com/dotnet/efcore/issues/22766) for more details on the issue.

In order to trace SQL query, you can register your `DbContext` with `AddXRayInterceptor()` accordingly in the `ConfigureServices` method in `startup.cs` file. 

For instance, when dealing with MySql server using Nuget: [Pomelo.EntityFrameworkCore.MySql](https://www.nuget.org/packages/Pomelo.EntityFrameworkCore.MySql) (V 3.1.1). 

```csharp
using Microsoft.EntityFrameworkCore;

public void ConfigureServices(IServiceCollection services)
{ 
    services.AddDbContext<your_DbContext>(options => options.UseMySql(your_connectionString).AddXRayInterceptor());
}
```

Alternatively, you can register `AddXRayInterceptor()` in the `Onconfiguring` method in your `DbContext` class. Below we are using Nuget: [Microsoft.EntityFrameworkCore.Sqlite](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Sqlite) (V 3.1.2)

```csharp
using Microsoft.EntityFrameworkCore;

public class your_DbContext : DbContext 
{
	protected override void OnConfiguring(DbContextOptionsBuilder options)
    => options.UseSqlite(your_connectionString).AddXRayInterceptor();
}
```

The connection string can be either hard coded or configured from `appsettings.json` file.

##### .NET 

AWS XRay SDK for .NET provides interceptor for tracing SQL query through Entity Framework 6 (>= 6.2.0). 

For how to start with Entity Framework 6 in an ASP.NET web app, please take reference to [link](https://docs.microsoft.com/en-us/aspnet/mvc/overview/getting-started/getting-started-with-ef-using-mvc/creating-an-entity-framework-data-model-for-an-asp-net-mvc-application).

For instrumentation, you will need to install `AWSXRayRecorder.Handlers.EntityFramework` nuget package and call `AWSXRayEntityFramework6.AddXRayInterceptor()` in your code. Make sure to call it **only once** to avoid duplicate tracing.

For instance, you can call `AddXRayInterceptor()` in the `Application_Start` method of **Global.asax** file.

```
using Amazon.XRay.Recorder.Handlers.EntityFramework;

protected void Application_Start()
{
    AWSXRayEntityFramework6.AddXRayInterceptor();
}
```

Or you can call it in the `DbConfiguration` class if there is one in your application to configure execution policy.

```
using Amazon.XRay.Recorder.Handlers.EntityFramework;

public class YourDbConfiguration : DbConfiguration
{
    public YourDbConfiguration()
    {
        AWSXRayEntityFramework6.AddXRayInterceptor();
    }
}
```

#### Capture SQL Query text in the traced SQL calls to SQL Server

You can also opt in to capture the `DbCommand.CommandText` as part of the subsegment created for your SQL query. The collected `DbCommand.CommandText` will appear as `sanitized_query` in the subsegment JSON. By default, this feature is disabled due to security reasons. 

##### .NET Core

If you want to enable this feature, it can be done in two ways. First, by setting the `CollectSqlQueries` to **true** in the `appsettings.json` file as follows:

```json
{
  "XRay": {
    "CollectSqlQueries":"true"
  }
}
```

Secondly, you can set the `collectSqlQueries` parameter in the `AddXRayInterceptor()` as **true** to collect the SQL query text. If you set this parameter as **false**, it will disable the `collectSqlQueries` feature for this `AddXRayInterceptor()`. Opting in `AddXRayInterceptor()` has the highest execution priority, which will override the configuration item in `appsettings.json` mentioned above.

```csharp
using Microsoft.EntityFrameworkCore;

public void ConfigureServices(IServiceCollection services)
{
    services.AddDbContext<your_DbContext>(options => options.UseMySql(your_connectionString).AddXRayInterceptor(true));
}
```

Or

```csharp
using Microsoft.EntityFrameworkCore;

public class your_DbContext : DbContext 
{
	protected override void OnConfiguring(DbContextOptionsBuilder options)
    => options.UseSqlite(your_connectionString).AddXRayInterceptor(true);
}
```

##### .NET 

You can enable tracing SQL query text for EF 6 interceptor in the `Web.config` file.

```xml
<configuration>
  <appSettings>
    <add key="CollectSqlQueries" value="true"/>
  </appSettings>
</configuration>
```

You can also pass **true** to `AddXRayInterceptor()` to collect SQL query text, otherwise pass **false** to disable. Opting in `AddXRayInterceptor()` has the highest execution priority, which will override the configuration item in `Web.config` mentioned above.

```
using Amazon.XRay.Recorder.Handlers.EntityFramework;

AWSXRayEntityFramework6.AddXRayInterceptor(true);
```

### Multithreaded Execution (.NET and .NET Core) : [Nuget](https://www.nuget.org/packages/AWSXRayRecorder.Core/)

In multithreaded execution, X-Ray context from current to its child thread is automatically set.  

```csharp
using Amazon.XRay.Recorder.Core;

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
using Amazon.XRay.Recorder.Core;

AWSXRayRecorder.Instance.TraceMethod("custom method", () => DoSomething(arg1, arg2, arg3));
```

#### Asynchronous method

```csharp
using Amazon.XRay.Recorder.Core;

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
using Amazon.XRay.Recorder.Core;

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
using Amazon.XRay.Recorder.Core;

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
using Amazon.XRay.Recorder.Core;

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
The AWS Lambda execution environment by default creates a `Segment` before each Lambda function invocation and sends it to the X-Ray service. AWS X-Ray .NET/Core SDK will make sure there will be a `FacadeSegment` inside the lambda context so that you can instrument your application successfully through subsegments only. `Subsegments` generated inside a Lambda function are attached to this `FacadeSegment` and only subsegments are streamed by the SDK. In addition to the custom subsegments, the middlewares would generate subsegments for outgoing HTTP calls, SQL queries, and AWS SDK calls within the lambda function the same way they do in a normal application.

*Note*: You can only create and close `Subsegment` inside a lambda function. `Segment` cannot be created inside the lambda function. All operations on `Segment` will throw an `UnsupportedOperationException` exception.

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

### Oversampling Mitigation
Oversampling mitigation allows you to ignore a parent segment/subsegment's sampled flag and instead sets the subsegment's sampled flag to false.
This ensures that downstream calls are not sampled and this subsegment is not emitted.

```csharp
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.XRay.Recorder.Core;
using Amazon.SQS;
using Amazon.SQS.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace MyFunction;

public class Function
{
    public string HandleSQSEvent(SQSEvent sqsEvent, ILambdaContext context)
    {
        AWSXRayRecorder.Instance.BeginSubsegmentWithoutSampling("Processing Event");

        var client = new AmazonSQSClient();

        var request = new ListQueuesRequest();

        var response = client.ListQueuesAsync(request);

        foreach (var url in response.Result.QueueUrls)
        {
            Console.WriteLine(url);
        }

        AWSXRayRecorder.Instance.EndSubsegment();

        return "Success";
    }
}
```

The code below demonstrates overriding the sampled flag based on the SQS messages sent to Lambda.

```csharp
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Lambda;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace MyFunction;

public class Function
{
    public string HandleSQSEvent(SQSEvent sqsEvent, ILambdaContext context)
    {

        foreach (SQSEvent.SQSMessage sqsMessage in sqsEvent.Records)
        {
            if (SQSMessageHelper.IsSampled(sqsMessage))
            {
                AWSXRayRecorder.Instance.BeginSubsegment("Processing Message");
            }
            else
            {
                AWSXRayRecorder.Instance.BeginSubsegmentWithoutSampling("Processing Message");
            }


            // Do my processing work here
            Console.WriteLine("Doing processing work");

            // End my subsegment
            AWSXRayRecorder.Instance.EndSubsegment();
        }

        return "Success";
    }
}
```

### ASP.NET Core on AWS Lambda (.NET Core)

We support instrumenting ASP.NET Core web app on Lambda. Please follow the steps of [ASP.NET Core](https://github.com/aws/aws-xray-sdk-dotnet/tree/master#aspnet-core-framework-net-core--nuget) instrumentation.

### Logging (.NET)

The AWS X-Ray .NET SDK share the same logging mechanism as [AWS .NET SDK](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-config-other.html#config-setting-awslogging). If the application had already configured logging for AWS .NET SDK, it should just work for AWS X-Ray .NET SDK.
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

The AWS X-Ray .NET SDK share the same logging mechanism as [AWS .NET SDK](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-config-other.html#config-setting-awslogging). To configure logging for .NET Core application, pass one of these options to the `AWSXRayRecorder.RegisterLogger` method.
	
Following is the way to configure `log4net` with X-Ray SDK:

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
