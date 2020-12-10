namespace FunnyFace.Simple2D2FAnimation {
	using UnityEngine;
	using OpenCvSharp;
	using System.Linq;
	using OpenCvSharp.Tracking;
	using System;
	using UnityEngine.EventSystems;

	public class BackgroundRemover :CaptureCamera
		, IBeginDragHandler
		, IDragHandler
		, IEndDragHandler {
		[Range (0, 100)]
		public int rezisePercentage = 50;
		[Range (1, 50)]
		public int iterations = 5;

		public int x = 1; 
		public int y = 1; 
		public int width = 128; 
		public int height = 72;

		const float downScale = 0.33f;
		const float minimumAreaDiagonal = 25.0f;

		Vector2 size = Vector2.zero;
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
			size.x = img.Width * rezisePercentage / 100;
			size.y = img.Height * rezisePercentage / 100;
			ns = new Size (size.x, size.y);
			Mat rzImg = img.Resize (ns);

			#region Track a Rect
			// screen space -> image space
			Vector2 sp = ConvertToImageSpace (startPoint, img.Size ());
			Vector2 ep = ConvertToImageSpace (endPoint, img.Size ());
			Point location = new Point (Math.Min (sp.x, ep.x), Math.Min (sp.y, ep.y));
			Size sizeRect = new Size (Math.Abs (ep.x - sp.x), Math.Abs (ep.y - sp.y));
			var areaRect = new OpenCvSharp.Rect (location, sizeRect);
			Rect2d obj = Rect2d.Empty;

			Mat downscaled = img.Resize (Size.Zero, downScale, downScale);

			// If not dragged - show the tracking data
			if (!isDragging) {
				// drop tracker if the frame's size has changed, this one is necessary as tracker doesn't hold it well
				if (frameSize.Height != 0 && frameSize.Width != 0 && downscaled.Size () != frameSize)
					DropTracking ();

				// we have to tracker - let's initialize one
				if (null == tracker) {
					// but only if we have big enough "area of interest", this one is added to avoid "tracking" some 1x2 pixels areas
					if ((ep - sp).magnitude >= minimumAreaDiagonal) {
						obj = new Rect2d (areaRect.X, areaRect.Y, areaRect.Width, areaRect.Height);

						// initial tracker with current image and the given rect, one can play with tracker types here
						tracker = Tracker.Create (TrackerTypes.MedianFlow);
						tracker.Init (downscaled, obj);

						frameSize = downscaled.Size ();
					}
				}
				// if we already have an active tracker - just to to update with the new frame and check whether it still tracks object
				else {
					if (!tracker.Update (rzImg, ref obj))
						obj = Rect2d.Empty;
				}

				// save tracked object location
				if (0 != obj.Width && 0 != obj.Height)
					areaRect = new OpenCvSharp.Rect ((int)obj.X, (int)obj.Y, (int)obj.Width, (int)obj.Height);
			}
			#endregion


			// 3. Apply grabcut Initializating wit a center rect of Size.X-10 * Size.Y-10 and I recommend 5 iterations
			Mat mask = new Mat (ns, MatType.CV_8U);
			Mat bgModel = new Mat ();
			Mat fgModel = new Mat ();
			OpenCvSharp.Rect foregroundRect = obj == Rect2d.Empty ? new OpenCvSharp.Rect (1, 1, img.Width, img.Height) : areaRect;
			Cv2.GrabCut (rzImg, mask, foregroundRect, bgModel, fgModel, iterations, GrabCutModes.InitWithRect);

			// 4. Use foreground or bg as mask
			mask = (mask & 1) * 255; // Takes the mask and convert every non 0 value to 255 (White)
			/* // Use this if you require the information of the mask in GrabCutClasses values (0 - 3);
			convertToGrayScaleValues (mask);
			*/

			mask = mask.Resize (new Size (img.Width, img.Height),0,0,InterpolationFlags.Lanczos4);

			// 5. Aply the mask while coping the original image to a green background 
			Mat bg = Unity.PixelsToMat(CreateBgPixels (new Color32 (0, 255, 0, 0), rzImg), 2, 2,false,false,0);
			bg = bg.Resize (new Size (img.Width, img.Height));
			img.CopyTo (bg, mask);
			// 6. Show result
			// render rect we've tracker or one is being drawn by the user
			if (isDragging || (null != tracker && obj.Width != 0))
				Cv2.Rectangle ((InputOutputArray)bg, areaRect * (1.0 / downScale), isDragging ? Scalar.Red : Scalar.LightGreen);
			output = Unity.MatToTexture (bg, output);
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

		// dragging
		bool isDragging = false;
		Vector2 startPoint = Vector2.zero;
		Vector2 endPoint = Vector2.zero;

		// tracker
		Size frameSize = Size.Zero;
		Tracker tracker = null;

		protected Vector2 ConvertToImageSpace(Vector2 coord, Size size) {

			Vector2 output = new Vector2 ();
			RectTransformUtility.ScreenPointToLocalPointInRectangle (imageTransform, coord, null, out output);

			// pivot is in the center of the rectTransform, we need { 0, 0 } origin
			output.x += size.Width / 2;
			output.y += size.Height / 2;

			// now our image might have various transformations of it's own
			if (!TextureParameters.FlipVertically)
				output.y = size.Height - output.y;

			// downscaling
			output.x *= downScale;
			output.y *= downScale;

			return output;
		}

		protected void DropTracking() {
			if (null != tracker) {
				tracker.Dispose ();
				tracker = null;

				startPoint = endPoint = Vector2.zero;
			}
		}

		public void OnBeginDrag(PointerEventData eventData) {
			DropTracking ();

			isDragging = true;
			startPoint = eventData.position;
		}

		public void OnDrag(PointerEventData eventData) {
			endPoint = eventData.position;
		}

		public void OnEndDrag(PointerEventData eventData) {
			endPoint = eventData.position;
			isDragging = false;
		}

	}

	#endregion
}