using StereoKit;
using StereoKit.DesktopMirror;

SK.Initialize(new SKSettings{assetsFolder="Assets"});
SK.AddStepper<DesktopMirror>();
SK.Run();