using StereoKit;
using StereoKit.DesktopMirror;

SK.Initialize();

DesktopMaterial desktop = new DesktopMaterial();
desktop.Start();

SK.Run(() => {
	desktop.Step();
	Mesh.Cube.Draw(desktop.Material, Matrix.TS(0,0,-0.5f, 0.2f));
});