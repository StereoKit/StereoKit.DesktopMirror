using StereoKit.Framework;

namespace StereoKit.DesktopMirror
{
	public class DesktopMirror : IStepper
	{
		bool enabled = true;
		public bool Enabled => enabled;

		DesktopMaterial desktop     = null;
		Mesh            desktopMesh = null;

		Pose pose;
		Pose poseSmooth;
		float prevAspect = 0;
		bool wasInteracting = false;
		float curveRadius = 1;

		public float Size = 0.72f;
		public float CurveRadius {get => curveRadius; set { if (curveRadius == value) return; curveRadius = value; CreateScreenMesh(desktop.Aspect, curveRadius, ref desktopMesh); } }
		public Pose Pose { get => pose; set => poseSmooth = pose = value; }

		public bool Initialize()
		{
			pose        = new Pose(Input.Head.position + Input.Head.Forward * 0.5f, Quat.LookDir(-Input.Head.Forward.X0Z));
			poseSmooth  = pose;
			desktopMesh = Mesh.GeneratePlane(Vec2.One, V.XYZ( 0,0,1 ), V.XYZ(0,1,0));
			desktop     = new DesktopMaterial();

			LoadSettings();

			if (enabled)
				Start();

			return true;
		}

		public void Step()
		{
			desktop.Step();

			// If the aspect ratio changes, we need to make a new mesh for it.
			float aspect = desktop.Aspect;
			if (prevAspect != aspect ) {
				prevAspect =  aspect;
				CreateScreenMesh(desktop.Aspect, curveRadius, ref desktopMesh);
			}

			// prepare a little grab handle underneath the desktop texture
			Bounds bounds = desktopMesh.Bounds;
			bounds.center     = (bounds.center + V.XYZ( 0, -bounds.dimensions.y / 2, bounds.dimensions.z / 2 )) * Size + V.XYZ(0,-0.02f,-0.02f);
			bounds.dimensions = V.XYZ(0.2f, 0.02f, 0.02f);

			// UI for the grab handle
			bool interacting = UI.Handle("Desktop", ref pose, bounds, true);
			
			// Smooth out the motion extra for nicer placement
			poseSmooth = Pose.Lerp(poseSmooth, pose, 4 * Time.Stepf);
			pose       = poseSmooth;
			desktopMesh.Draw(desktop.Material, pose.ToMatrix(Size));

			// Save the pose the file if we just stopped interacting with it!
			if (interacting == false && wasInteracting == true)
			{
				SaveSettings();
			}
			wasInteracting = interacting;
		}

		void SaveSettings()
		{
			Pose p = World.HasBounds
				? World.BoundsPose.ToMatrix().Inverse.Transform(Pose)
				: Pose;
			Platform.WriteFile("DesktopMirror.ini", $"{enabled} {p.position.x} {p.position.y} {p.position.z} {p.orientation.x} {p.orientation.y} {p.orientation.z} {p.orientation.w}");
		}

		void LoadSettings()
		{
			string[] at = Platform.ReadFileText("DesktopMirror.ini")?.Split(' ');
			if (at != null && at.Length == 8)
			{
				enabled = bool.Parse(at[0]);
				Pose    = new Pose(
					new Vec3(float.Parse(at[1]), float.Parse(at[2]), float.Parse(at[3])),
					new Quat(float.Parse(at[4]), float.Parse(at[5]), float.Parse(at[6]), float.Parse(at[7])));

				if (World.HasBounds)
					Pose = World.BoundsPose.ToMatrix().Transform(Pose);

					
				Log.Info(Pose.ToString());
			}
		}

		public void Shutdown()
		{
			Stop();
		}

		public void Start()
		{
			if (enabled == false)
			{
				enabled = true;
				SaveSettings();
			}
			desktop.Start();
		}
		public void Stop()
		{
			if (enabled == true)
			{
				SaveSettings();
				enabled = false;
			}
			desktop.Stop();
		}

		static void CreateScreenMesh(float aspect, float curveRadius, ref Mesh mesh) {
			int   cols = 32;
			int   rows = 2;
			float angle = aspect / curveRadius;

			Vertex[] verts = new Vertex[cols * rows];
			uint  [] inds  = new uint[ (cols-1) * (rows-1) * 6];
			for (int y = 0; y < rows; y++)
			{
				float yp = y / (float)(rows - 1);
				for (int x = 0; x < cols; x++)
				{
					float xp   = x / (float)(cols - 1);
					float curr = (xp - 0.5f) * angle;

					verts[x + y * cols] = new Vertex( V.XYZ(SKMath.Sin(curr) * curveRadius, yp - 0.5f, SKMath.Cos(curr)*curveRadius -curveRadius) * 0.7f, -Vec3.Forward, V.XY(1-xp,1-yp));

					if (x < cols-1 && y < rows-1) {
						int ind = (x+y*cols)*6;
						inds[ind  ] = (uint)((x  ) + (y  ) * cols);
						inds[ind+1] = (uint)((x+1) + (y+1) * cols);
						inds[ind+2] = (uint)((x+1) + (y  ) * cols);

						inds[ind+3] = (uint)((x  ) + (y  ) * cols);
						inds[ind+4] = (uint)((x  ) + (y+1) * cols);
						inds[ind+5] = (uint)((x+1) + (y+1) * cols);
					}
				}
			}

			mesh.SetData(verts, inds);
		}
	}
}
