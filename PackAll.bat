dotnet pack .\Rampastring.XNAUI.csproj --include-symbols -c WindowsDXRelease
dotnet pack .\Rampastring.XNAUI.csproj --include-symbols -c UniversalGLRelease
dotnet pack .\Rampastring.XNAUI.csproj --include-symbols -c WindowsGLRelease
dotnet pack .\Rampastring.XNAUI.csproj --include-symbols -c WindowsXNARelease

dotnet pack .\Rampastring.XNAUI.csproj --include-symbols -c WindowsDXDebug
dotnet pack .\Rampastring.XNAUI.csproj --include-symbols -c UniversalGLDebug
dotnet pack .\Rampastring.XNAUI.csproj --include-symbols -c WindowsGLDebug
dotnet pack .\Rampastring.XNAUI.csproj --include-symbols -c WindowsXNADebug