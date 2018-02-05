# Change Log
All notable changes to this project will be documented in this file.
## 2.0.0-beta (2018-02-05)
### AWSXRayRecorder.Core (2.0.0-beta)
#### Added
- .NET Core 2.0 support
- Added AWS Lambda support for .NET Core 2.0 and above
- Added Elastic Beanstalk and ECS plugins

### AWSXRayRecorder.Handlers.System.Net (2.0.0-beta)
#### Added
- .NET Core 2.0 support
- Added support for asynchronous method calls

### AWSXRayRecorder.Handlers.SqlServer (2.0.0-beta)
#### Added
- .NET Core 2.0 support
- Added support for asynchronous method calls

### AWSXRayRecorder.Handlers.AwsSdk (2.0.0-beta)
#### Added
- .NET Core 2.0 support

#### changed
- AWS SDK Handler is changed. Deprecated - AWSSdkTracingHandler class

### AWSXRayRecorder.Handlers.AspNet (2.0.0-beta)
- Package for ASP.NET Framework

### AWSXRayRecorder.Handlers.AspNetCore (2.0.0-beta)
- Package for ASP.NET Core 2.0 Framework

### Deprecated
### AWSXRayRecorder.Handlers.AspNet.WebApi (1.1.2)
- This functionality is included in AWSXRayRecorder.Handlers.AspNet package

## 1.1.2 (2017-10-17)
### AWSXRayRecorder.Core (1.1.2)
#### Changed
- Reorganize 'aws.xray' filed to move runtime related informantion into 'service' field.

### AWSXRayRecorder.Handlers.System.Net (1.1.2)
- Just bump up version for GA

### AWSXRayRecorder.Handlers.AspNet.WebApi (1.1.2)
- Just bump up version for GA

### AWSXRayRecorder.Handlers.SqlServer (1.1.2)
- Just bump up version for GA

### AWSXRayRecorder.Handlers.AwsSdk (1.1.2)
- Just bump up version for GA

#### Fixed
- Exceptions that Entity and Subsegment are not decorated as Serializable.

## 1.1.1
### AWSXRayRecorder.Core (1.1.1)
#### Changed
- Reorganize 'aws.xray' filed to move runtime related informantion into 'service' field.

#### Fixed
- Exceptions that Entity and Subsegment are not decorated as Serializable.

## 1.1.0 (2017-04-18)
### AWSXRayRecorder.Core (1.1.0)
- Just bump up version for GA

### AWSXRayRecorder.Handlers.System.Net (1.1.0)
- Just bump up version for GA

### AWSXRayRecorder.Handlers.AspNet.WebApi (1.1.0)
- Just bump up version for GA

### AWSXRayRecorder.Handlers.SqlServer (1.1.0)
- Just bump up version for GA

### AWSXRayRecorder.Handlers.AwsSdk (1.1.0)
- Just bump up version for GA

#### Fixed
- Subsegment name for S3 client is not captured correctly.

## 1.0.6-beta (2017-04-12)
### AWSXRayRecorder.Handlers.AwsSdk (1.0.2-beta)
#### Added
- Dynamo ListTables response descriptor to get number of tables returned. 

#### Fixed
- Update usage of interface in sync with Core Runtime 1.0.2-beta

### AWSXRayRecorder.Handlers.System.Net (1.0.2-beta)
#### Fixed
- Update usage of interface in sync with Core Runtime 1.0.2-beta

## 1.0.5-beta (2017-03-30)
### AWSXRayRecorder.Core (1.0.2-beta)
#### Added
- Runtime information to the `aws.xray` namespace on segments.
- Added a `ContextMissingStrategy` property to the `IAWSXRayRecorder` interface. This allows configuration of the exception behavior exhibited when trace context is not properly propagated. The behavior can be configured in code. Alternatively, the environment variable `AWS_XRAY_CONTEXT_MISSING` can be used (overrides any modes set in code). Valid values for this environment variable are currently (case insensitive) `RUNTIME_ERROR` and `LOG_ERROR`. By default, an exception will be thrown on missing context.

#### Changed
- Renamed `SegmentNotAvailableException` to `EntityNotAvailableException`

### AWSXRayRecorder.Handlers.AspNet.WebApi (1.0.1-beta)
#### Added
- Capturing 'x_forwarded_for' header from incoming HTTP request

## 1.0.4-beta (2017-03-06)
### AWSXRayRecorder.Core (1.0.1-beta)
#### Added
- Method `AddMetadata` to `IAWSXRayRecorder` interface.
- Method `SetDaemonAddress` to `IAWSXRayRecorder` interface.
- Propert `RuntimeContext` to `IAWSXRayRecorder` interface.
- `DefaultSamplingRule.json` as embedded resource.
- Requirement to provide default sampling rule, otherwise `InvalidSamplingConfigurationException` will be thrown.
- Constructor to `LocalizedSamplingStrategy` without path which loads default sampling rules.
- Method `Sample(string serviceName, string path, string method)` to interface `ISamplingStrategy`.
- New Exception type `InvalidSamplingConfigurationException`.

#### Changed
- Attribute *aws.xray.sdk* get renamed to *aws.xray.sdk_version*.
- Resolution of start/end time to microsecond.
- Parameter type of method `TryGetRuntimeContext` in intercae `IPlug` from `Dictionary` to `IDictionary`.
- The json format of sampling configuration.

### AWSXRayRecorder.Handlers.AwsSdk (1.0.1-beta)
#### Added
- `DefaultAWSWhitelist.json` as embedded resource.
- Constructor to `AWSSdkTracingHandler` without path which loads default AWS whitelist configuration.

#### Changed
- Rename `TracingEventHandler` to `AWSSdkTracingHandler`.
- Reorganized classed to `Entities` namespace.

### AWSXRayRecorder.Handlers.System.Net (1.0.1-beta)
#### Added
- Feature of setting error/fault/throttle to subsegment based on the response code received from downstream HTTP service.

## 1.0.2-beta (2017-01-25)
### Added
- Package **AWSXRayRecorder.Core**. It provides the core functions to control tracing segment and communication to daemon.
- Package **AWSXRayRecorder.Handlers.AspNet.WebApi**` package. It provides functionality to trace ASP.NET Web API requests.
- Package **AWSXRayRecorder.Handlers.AwsSdk** package. It provides functionality to trace AWSSDK request.
- Package **AWSXRayRecorder.Handlers.SqlServer** package. It provides functionality to trace queries to SQl Server.
- Package **AWSXRayRecorder.Handlers.System.Net**` package. It provides functionality to trace WebRequest from System.Net namespace.
- Environment variable **AWS_XRAY_DAEMON_ADDRESS** to configure target daemon address and port.
- Environment variable **AWS_XRAY_TRACING_NAME** to configure segment name.
- Support of chaining exceptions, which increase efficiency to serialize Exceptions.
- `FixedSegmentNamingStrategy` and `DynamicSegmentNamingStrategy`.
- Attribute *aws.xray.sdk* to capture version of the SDK.

### Deprecated
- **AWSXRayRecorder** package. It has been split into **AWSXRayRecorder.Core** and **AWSXRayRecorder.Handlers** packages. **AWSXRayRecorder** package become a meta-package, which do not contain any code, but only defines dependencies to all subpackages.

### Changed
- Modified `TracingMessageHandler` to require an instance of `SegmentNamingStrategy` when instantiating. A shorthand constructor which accepts a single `string` is also provided to simplify use of the `FixedSegmentNamingStrategy`.

### Fixed
- A bug in the wildcard matching logic used in sampling rules and `TracingMessageHandler`.

### Removed
- Method `AddEventHandler` from interface `IAWSXRayRecorder`

## 1.0.1-beta (2016-12-01)
### Fixed
- Fixed a bug where file handler is not properly closed.
