dotnet clean "src\Emphasis.ScreenCapture.sln"
dotnet build -c Release "src\Emphasis.ScreenCapture.sln"
cd "src\Emphasis.ScreenCapture.Benchmarks\"
dotnet run -c Release -- --filter * --anyCategories=dedicated-gpu
cd ..\..
