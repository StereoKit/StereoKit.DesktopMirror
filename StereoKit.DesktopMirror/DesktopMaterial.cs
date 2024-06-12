using Vortice.DXGI;
using Vortice.Direct3D11;
using SharpGen.Runtime;
using System;
using System.Runtime.InteropServices;

namespace StereoKit.DesktopMirror
{
	public class DesktopMaterial {
		IDXGIOutputDuplication duplication    = null;
		Tex                    duplicationTex = null;
		Tex                    pointerTex     = null;
		Vec2                   pointerPos;

		MaterialDesktopBlit blitMat = null;
		Tex      desktopTex      = null;
		Material desktopMaterial = null;

		public Material Material => desktopMaterial;
		public int Width => desktopTex.Width;
		public int Height => desktopTex.Height;
		public float Aspect => desktopTex.Width / (float)desktopTex.Height;

		public DesktopMaterial() {
			desktopTex     = new Tex(TexType.Rendertarget);
			duplicationTex = new Tex();
			desktopTex    .AddressMode = TexAddress.Clamp;
			duplicationTex.AddressMode = TexAddress.Clamp;

			desktopMaterial = Material.Unlit.Copy();
			desktopMaterial.FaceCull = Cull.None;
			desktopMaterial[MatParamName.DiffuseTex] = desktopTex;

			blitMat = new MaterialDesktopBlit();
			blitMat.source = duplicationTex;
		}

		public void Start() {
			ID3D11Device d3dDevice = new ID3D11Device(Backend.D3D11.D3DDevice);
			IDXGIDevice  device    = d3dDevice.QueryInterface<IDXGIDevice>();
			IDXGIAdapter adapter   = device.GetParent<IDXGIAdapter>();

			Result hr = adapter.EnumOutputs(0, out IDXGIOutput output);
			IDXGIOutput1 output1 = output.QueryInterface<IDXGIOutput1>();
			duplication = output1.DuplicateOutput(d3dDevice);
		}

		public void Stop() {
			duplication?.ReleaseFrame();
			duplication = null;
		}

		public void Step() {
			if (duplication == null) return;

			// Update our desktop texture
			if (DuplicationNextFrame(duplication, duplicationTex, ref pointerPos, ref pointerTex)) {
				if (desktopTex.Width  != duplicationTex.Width ||
					desktopTex.Height != duplicationTex.Height) {
					desktopTex.SetSize(duplicationTex.Width, duplicationTex.Height);
				}

				blitMat.cursor_pos = pointerPos;
				if (pointerTex != null)
				{
					blitMat.cursor_size = new Vec2(pointerTex.Width / (float)desktopTex.Width, pointerTex.Height / (float)desktopTex.Height);
					blitMat.cursor      = pointerTex;
				}
				Renderer.Blit(desktopTex, blitMat);
			}
		}

		IntPtr pointerMem;
		int    pointerMemSize = 0;
		byte[] pointerBytes;
		bool DuplicationNextFrame(IDXGIOutputDuplication duplication, Tex frameTex, ref Vec2 pointerAt, ref Tex pointerTex)
		{
			if (duplication == null) return false;

			if (frameTex.AssetState == AssetState.Loaded)
			{
				duplication.ReleaseFrame();
				frameTex.SetNativeSurface(IntPtr.Zero);
			}

			Result hr = duplication.AcquireNextFrame(0, out var info, out IDXGIResource resource);
			if (hr == Result.WaitTimeout) return false;
			if (hr.Failure) return false;

			ID3D11Texture2D desktopImage = resource.QueryInterface<ID3D11Texture2D>();
			frameTex.SetNativeSurface(desktopImage.NativePointer, TexType.ImageNomips);

			if (info.PointerPosition.Visible)
			{
				if (pointerMemSize < info.PointerShapeBufferSize)
				{
					if (pointerMemSize > 0) Marshal.FreeHGlobal(pointerMem);
					pointerMemSize = info.PointerShapeBufferSize;
					pointerMem     = Marshal.AllocHGlobal(pointerMemSize);
				}
				hr = duplication.GetFramePointerShape(info.PointerShapeBufferSize, pointerMem, out int req, out var shapeInfo);
				if (hr.Success && info.PointerShapeBufferSize > 0)
				{
					int width  = shapeInfo.Width;
					int height = shapeInfo.Type == 0x1 ? shapeInfo.Height/2 : shapeInfo.Height;
					
					if (pointerBytes == null || pointerBytes.Length < width * height * 4)
						pointerBytes = new byte[width * height * 4];
					if (shapeInfo.Type == 0x1) // monochrome 1bpp
					{
						byte[] srcData = new byte[shapeInfo.Pitch * shapeInfo.Height];
						Marshal.Copy(pointerMem, srcData, 0, srcData.Length);
						
						int xorOff = height * shapeInfo.Pitch;
						for (int y = 0; y < height; y++)
						{
							int yOff = y * shapeInfo.Pitch;
							for (int x = 0; x < width; x++)
							{
								byte mask = (byte)(1 << (7 - (x % 8)));
								bool and_bit = (srcData[x / 8 + yOff         ] & mask) > 0;
								bool xor_bit = (srcData[x / 8 + yOff + xorOff] & mask) > 0;
								int  i       = (x + y * width) * 4;
								byte col, alpha;
								if (and_bit) {
									if (xor_bit) { col = 255; alpha = 255; }
									else         { col = 0;   alpha = 0;   }
								} else {
									if (xor_bit) { col = 255; alpha = 255; }
									else         { col = 0;   alpha = 255; }
								}
								pointerBytes[i    ] = col;
								pointerBytes[i + 1] = col;
								pointerBytes[i + 2] = col;
								pointerBytes[i + 3] = alpha;
							}
						}
					}
					else if (shapeInfo.Type == 0x2)
					{
						Marshal.Copy(pointerMem, pointerBytes, 0, shapeInfo.Width * shapeInfo.Height * 4);
					}
					else
					{
						Log.Warn($"Unknown shape info: {shapeInfo.Type}");
					}

					if (pointerTex == null)
						pointerTex = new Tex(TexType.ImageNomips|TexType.Dynamic, TexFormat.Rgba32);
					pointerTex.SetColors(width, height, pointerBytes);
				}

				pointerAt = new Vec2(
					(info.PointerPosition.Position.X) / (float)frameTex.Width,
					(info.PointerPosition.Position.Y) / (float)frameTex.Height);
			}

			return true;
		}
	}
}