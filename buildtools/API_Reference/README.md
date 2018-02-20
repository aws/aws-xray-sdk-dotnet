
# API Reference Doc 

## Prereqs ##
* Windows 7 or higher installed
* Visual Studio 2017, Frameworks : .NET 4.5 and .NET Core 2.0 installed
* Install [SFHB](https://github.com/EWSoftware/SHFB/releases)

## Steps ##
1. Open the `sdk/AWSXRayRecorder.sln` project file in Visual Studio 2017.
2. Right click project, and rebuild all the packages. This step generates xml document inside `bin` folder of each package.
3. Open project `buildtools/API_Reference/AWSXRayDotNet.sln` in Visual Studio 2017. Rebuild projects `AWSXRayDotNetDocumentation` and `AWSXRayDotNetCoreDocumentation`.
4. Documentation is generated inside `Help/` folder of each project.