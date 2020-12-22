namespace FunnyFace.Simple2D2FAnimation {
	using OpenCvSharp;
	using OpenCvSharp.Tracking;
	using System;
	using UnityEngine;
    using UnityEngine.EventSystems;

    public class RectSelector :MonoBehaviour
            , IBeginDragHandler
            , IDragHandler
            , IEndDragHandler {
		[NonSerialized]
		public float downScale;
		const float minimumAreaDiagonal = 15.0f;
		// dragging
		bool isDragging = false;
		Vector2 startPoint = Vector2.zero;
		Vector2 endPoint = Vector2.zero;

		// tracker
		Size frameSize = Size.Zero;
		Tracker tracker = null;

		public bool isTracking { 
			get { return tracker != null; }
		}

		public void SetDownscale(int percentage){
			downScale = percentage/100.0f;
		}
		protected Vector2 ConvertToImageSpace(RectTransform imageTransform, Vector2 coord, Size size, Unity.TextureConversionParams TextureParameters) {

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

		public OpenCvSharp.Rect DrawRect(RectTransform imageTransform, Mat image, Mat downscaled, Unity.TextureConversionParams TextureParameters,  ref Texture2D output) {
			// screen space -> image space
			Vector2 sp = ConvertToImageSpace (imageTransform, startPoint, image.Size (), TextureParameters);
			Vector2 ep = ConvertToImageSpace (imageTransform, endPoint, image.Size (), TextureParameters);
			Point location = new Point (Math.Min (sp.x, ep.x), Math.Min (sp.y, ep.y));
			Size size = new Size (Math.Abs (ep.x - sp.x), Math.Abs (ep.y - sp.y));
			var areaRect = new OpenCvSharp.Rect (location, size);
			Rect2d obj = Rect2d.Empty;

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
						tracker = Tracker.Create (TrackerTypes.MIL);
						tracker.Init (downscaled, obj);

						frameSize = downscaled.Size ();
					}
				}
				// if we already have an active tracker - just to to update with the new frame and check whether it still tracks object
				else {
					if (!tracker.Update (downscaled, ref obj))
						obj = Rect2d.Empty;
				}

				// save tracked object location
				if (0 != obj.Width && 0 != obj.Height)
					areaRect = new OpenCvSharp.Rect ((int)obj.X, (int)obj.Y, (int)obj.Width, (int)obj.Height);
			}

			// render rect we've tracker or one is being drawn by the user
			if (isDragging || (null != tracker && obj.Width != 0))
				Cv2.Rectangle ((InputOutputArray)image, areaRect * (1.0 / downScale), isDragging ? Scalar.Red : Scalar.Blue, 4);

			// result, passing output texture as parameter allows to re-use it's buffer
			// should output texture be null a new texture will be created
			if(!isTracking)
				output = Unity.MatToTexture (image, output);
			return areaRect;
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
}