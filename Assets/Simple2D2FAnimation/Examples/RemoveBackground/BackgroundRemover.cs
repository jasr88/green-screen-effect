namespace FunnyFace.Simple2D2FAnimation {
	using UnityEngine;
	using OpenCvSharp;
	using System.Linq;
	using System;

	public class BackgroundRemover :CaptureCamera {
		[Range (0, 100)]
		public int rezisePercentage = 50;
		[Range (1, 50)]
		public int iterations = 5;

		public int x = 1; 
		public int y = 1; 
		public int width = 128; 
		public int height = 72; 

		private OpenCvSharp.Rect rect;
		private Size ns;
			
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
			Vector2 size = Vector2.zero;
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
			//mask = (mask & 1) * 255;// Takes the mask and convert every non 0 value to 255 (White) 



			Mat bg = Unity.PixelsToMat(CreateBgPixels (new Color32 (0, 255, 0, 0), rzImg), rzImg.Width, rzImg.Height,false,false,0);
			rzImg.CopyTo (bg, mask);
			
			// 5. Show result
			output = Unity.MatToTexture (mask, output);
			return true;
		}

		private Color32[] CreateBgPixels(Color32 color, Mat reference) {
			int pixleCount = reference.Width * reference.Height * 3; // Width * Height * Depth (r,g,b)
			Color32 colorBg = color;
			return Enumerable.Repeat (colorBg, pixleCount).ToArray ();
		}


		#region Auxiliary Methods
		private Mat ForegroundToMask(Mat foreground) {
			//Mat mask = new Mat (foreground.Size(),MatType.CV_8U);
			string maskVal = "";
			for (int r = 0; r < foreground.Rows; r++) {
				for (int c = 0; c < foreground.Cols; c++) {
					maskVal = maskVal+" "+ foreground.Get<GrabCutClasses> (r, c);
				/*	var pixel = foreground.Get<GrabCutClasses> (r, c);
					if (pixel == GrabCutClasses.FGD || pixel == GrabCutClasses.PR_FGD) {
						mask.Set (r, c, 1);
					} else {
						mask.Set (r, c, 0);
					}
				*/
				}
				maskVal = maskVal + "\n";
			}

			//mask = mask&1 * 255;
			Debug.Log (maskVal);
			
			return foreground;
		}
		
		private void convertToGrayScaleValues(Mat mask) {
			int width = mask.Rows;
			int height = mask.Cols;
			Int32[] buffer = new Int32[width * height];
			buffer = mask.Get<Int32[]> ();
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					int value = buffer[y * width + x];

					if (value == Imgproc.GC_BGD) {
						buffer[y * width + x] = 0; // for sure background
					} else if (value == Imgproc.GC_PR_BGD) {
						buffer[y * width + x] = 85; // probably background
					} else if (value == Imgproc.GC_PR_FGD) {
						buffer[y * width + x] = (byte)170; // probably foreground
					} else {
						buffer[y * width + x] = (byte)255; // for sure foreground
					}
				}
			}
			mask.Set<byte> (0, 0, buffer);
		}
		
	}

	#endregion
}