namespace FunnyFace.Simple2D2FAnimation {
	using OpenCvSharp;
	using System.Linq;
	using UnityEngine;

	public class BackgroundRemover :CaptureCamera {
		[Range (0, 100)]
		public int rezisePercentage = 50;
		[Range (1, 50)]
		public int iterations = 5;

		RectSelector rs;

		private Scalar green = new Scalar (0,255,0);	

		private void Awake() {
			rs = GetComponent<RectSelector> ();
			rs.SetDownscale(rezisePercentage);
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

			// 2. Rezise frame , for now the best size to analize is 10% of the original size 1280*720
			Mat rzImg = img.Resize (Size.Zero, rs.downScale, rs.downScale);

			// 3. Calculate and draw rect
			OpenCvSharp.Rect fgRect = rs.DrawRect (imageTransform, img, rzImg, TextureParameters, ref output);

			if (rs.isTracking) {
				// 3. Apply grabcut Initializating I recommend 5 iterations
				Mat mask = new Mat (Size.Zero, MatType.CV_8U);
				Mat bgModel = new Mat ();
				Mat fgModel = new Mat ();
				Cv2.GrabCut (rzImg, mask, fgRect, bgModel, fgModel, iterations, GrabCutModes.InitWithRect);

				// 4. Use foreground or bg as mask
				mask = (mask & 1) * 255; // Takes the mask and convert every non 0 value to 255 (White)
				mask = mask.Resize (new Size (img.Width, img.Height), 0, 0, InterpolationFlags.Lanczos4);

				// 5. Aply the mask while coping the original image to a green background 
				Mat bg = new Mat (img.Size(), rzImg.Type(), green);
				img.CopyTo (bg, mask);

				// 6. Show result
				output = Unity.MatToTexture (bg, output);
			}
			return true;
		}

		private Color32[] CreateBgPixels(Color32 color, Mat reference) {
			int pixleCount = reference.Width * reference.Height * 3; // Width * Height * Depth (r,g,b)
			Color32 colorBg = color;
			return Enumerable.Repeat (colorBg, pixleCount).ToArray ();
		}

	}// End of Class BackgroundRemover

} // End of Namespace