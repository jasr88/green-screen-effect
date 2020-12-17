namespace FunnyFace.Simple2D2FAnimation {
	using OpenCvSharp;
	using System.Linq;
	using UnityEngine;

	public class BackgroundRemover :CaptureCamera {
		[Range (0, 100)]
		public int rezisePercentage = 50;
		[Range (1, 50)]
		public int iterations = 5;

		public int x = 1; 
		public int y = 1; 
		public int width = 128; 
		public int height = 72;

		Vector2 size = Vector2.zero;
		private Size ns;

		RectSelector rs;

		private void Awake() {
			rs = GetComponent<RectSelector> ();
		}

		public void CicleCamera() {
			int nextCamera = DeviceIndex + 1 >= DevicesCount ? 0 : DeviceIndex + 1;
			SetActiveCamera (nextCamera);
		}

		public void CicleFilterMode() {
			FilterMode fm = (int)(currentDeviceInfo.filterMode + 1) > 2 ? 0 : currentDeviceInfo.filterMode + 1;
			SetFilterMode (fm);
		}

		protected override bool ProcessTexture(WebCamTexture input, ref Texture2D output) {
			// 1. Obtain Frame
			Mat img = Unity.TextureToMat (input, TextureParameters);
			// calcular rect y dibujar
			rs.DrawRect (imageTransform,img,img, TextureParameters.FlipVertically);
			output = Unity.MatToTexture (img, output);

			/*
			// 2. Rezise frame , for now the best size to analize is 10% of the original size 1280*720
			size.x = img.Width * rezisePercentage / 100;
			size.y = img.Height * rezisePercentage / 100;
			ns = new Size (size.x, size.y);
			Mat rzImg = img.Resize (ns);

			// 3. Apply grabcut Initializating wit a center rect of Size.X-10 * Size.Y-10 and I recommend 5 iterations
			Mat mask = new Mat (ns, MatType.CV_8U);
			Mat bgModel = new Mat ();
			Mat fgModel = new Mat ();
			Cv2.GrabCut (rzImg, mask, new OpenCvSharp.Rect (1, 1, img.Width, img.Height), bgModel, fgModel, iterations, GrabCutModes.InitWithRect);

			// 4. Use foreground or bg as mask
			mask = (mask & 1) * 255; // Takes the mask and convert every non 0 value to 255 (White)
			/* // Use this if you require the information of the mask in GrabCutClasses values (0 - 3);
			convertToGrayScaleValues (mask);
			*/

			/*

			mask = mask.Resize (new Size (img.Width, img.Height),0,0,InterpolationFlags.Lanczos4);

			// 5. Aply the mask while coping the original image to a green background 
			Mat bg = Unity.PixelsToMat(CreateBgPixels (new Color32 (0, 255, 0, 0), rzImg), 2, 2,false,false,0);
			bg = bg.Resize (new Size (img.Width, img.Height));
			img.CopyTo (bg, mask);
			*/

			// 6. Show result
			//output = Unity.MatToTexture (bg, output);
			return true;
		}

		private Color32[] CreateBgPixels(Color32 color, Mat reference) {
			int pixleCount = reference.Width * reference.Height * 3; // Width * Height * Depth (r,g,b)
			Color32 colorBg = color;
			return Enumerable.Repeat (colorBg, pixleCount).ToArray ();
		}

		#region Auxiliary Methods
		private void convertToGrayScaleValues(Mat mask) {
			int width = mask.Rows;
			int height = mask.Cols;
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					int value = mask.Get<byte> (x, y);

					if (value == (int)GrabCutClasses.BGD) {
						mask.Set<byte> (x, y, 0); // for sure background
					} else if (value == (int)GrabCutClasses.PR_BGD) {
						mask.Set<byte> (x, y, 85); // probably background
					} else if (value == (int)GrabCutClasses.BGD) {
						mask.Set<byte> (x, y, 170); // probably foreground
					} else {
						mask.Set<byte> (x, y, 255); // for sure foreground
					}
				}
			}
		}
		#endregion

	}// End of Class BackgroundRemover

} // End of Namespace