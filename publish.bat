rmdir /s /q "pack"
dotnet clean "src\Emphasis.ScreenCapture.sln"
dotnet pack "src\Emphasis.ScreenCapture.sln" -c Release --include-source --include-symbols -o pack
dotnet nuget push "pack\*.nupkg" -s https://api.nuget.org/v3/index.json --skip-duplicate