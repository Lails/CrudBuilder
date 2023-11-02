dotnet pack -c Release
dotnet nuget push bin/Release/Lails.CrudBuilder.1.0.0.nupkg -k 121212 -s https://api.nuget.org/v3/index.json

pause