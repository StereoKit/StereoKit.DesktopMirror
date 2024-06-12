# StereoKit.DesktopMirror

A small library for duplicating your desktop window within a [StereoKit](https://stereokit.net) application on Windows.

To use this library, you do _not_ need to clone this repository, simply add the StereoKit.DesktopMirror NuGet package to your project! You can do this via Visual Studio's NuGet package manager UI, or via CLI like so:

```bat
dotnet add package StereoKit.DesktopMirror
```

## Usage

To use this functionality in your project, the simplest thing to do is add the `DesktopMirror` `IStepper` once, anytime in your StereoKit project.

```csharp
SK.AddStepper<DesktopMirror>();
```

For more advanced usage, for example if you would like to apply the desktop texture to some other surface in your project, you can use the `DesktopMaterial` directly:

```csharp
DesktopMaterial desktop = new DesktopMaterial();
desktop.Start();

SK.Run(() => {
	desktop.Step();
	Mesh.Cube.Draw(desktop.Material, Matrix.TS(0,0,-0.5f, 0.2f));
});
```

## Contributing

If you wish to contribute changes or improvements to DesktopMirror, you can clone and modify the package source at https://github.com/StereoKit/StereoKit.DesktopMirror.