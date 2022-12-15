dllpath=~/.nuget/packages/reportgenerator/5.1.10/tools/net6.0/ReportGenerator.dll

dotnet $dllpath -reports:coverage.xml -targetdir:coveragereport -sourcedirs:src -classfilters:+frar.*