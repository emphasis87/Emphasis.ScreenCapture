# Emphasis.ScreenCapture

An extensible screen capture library written in C# using native APIs.

## Getting started

The system is split in multiple packages. To install the main package you can use:
```shell
PM> Install Emphasis.ScreenCapture
```
For exporting the screen capture as `System.Drawing.Bitmap` you can install:
```shell
PM> Install Emphasis.ScreenCapture.Runtime.Windows.DXGI.Bitmap
```
And here is a code example how to get a screen capture:
```csharp
var manager = new ScreenCaptureManager();
var screen = manager.GetScreens().FirstOrDefault();

using var capture = await manager.Capture(screen);
using var bitmap = await capture.ToBitmap();

bitmap.Save("capture.png");
var info = new ProcessStartInfo("capture.png") { UseShellExecute = true };
Process.Start(info);
```
The DXGI API requires that each frame is released before acquiring the next. Therefore make sure to dispose each ```IScreenCapture``` instance before capturing the next one.

## Tests

To test screen capture run in the git folder:
```csharp
dotnet test "src/Emphasis.ScreenCapture.sln" --filter Name~Capture_Bitmap
```