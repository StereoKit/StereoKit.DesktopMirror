using StereoKit;
using StereoKit.DesktopMirror;

SK.Initialize(new SKSettings{assetsFolder="Assets"});

DesktopMaterial desktop = new DesktopMaterial();
desktop.Start();

SK.Run(() => {
    desktop.Step();
    Mesh.Cube.Draw(desktop.Material, Matrix.TS(0,0,-0.5f, 0.2f));
});