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

		const float downScale = 0.33f;
		const float minimumAreaDiagonal = 25.0f;
		// dragging
		bool isDragging = false;
		Vector2 startPoint = Vector2.zero;
		Vector2 endPoint = Vector2.zero;

		// tracker
		Size frameSize = Size.Zero;
		Tracker tracker = null;

		protected Vector2 ConvertToImageSpace(RectTransform imageTransform, Vector2 coord, Size size, bool flipVertically) {

			Vector2 output = new Vector2 ();
			RectTransformUtility.ScreenPointToLocalPointInRectangle (imageTransform, coord, null, out output);

			// pivot is in the center of the rectTransform, we need { 0, 0 } origin
			output.x += size.Width / 2;
			output.y += size.Height / 2;

			// now our image might have various transformations of it's own
			if (!flipVertically)
				output.y = size.Height - output.y;
			/*
			// downscaling
			output.x *= downScale;
			output.y *= downScale;
			*/

			return output;
		}

		public void DrawRect(RectTransform imageTransform, Mat frame, Mat scaledFrame, bool flipVertically) {
			// screen space -> image space
			Vector2 sp = ConvertToImageSpace (imageTransform, startPoint, frame.Size (), flipVertically);
			Vector2 ep = ConvertToImageSpace (imageTransform, endPoint, frame.Size (), flipVertically);
			Point location = new Point (Math.Min (sp.x, ep.x), Math.Min (sp.y, ep.y));
			Size sizeRect = new Size (Math.Abs (ep.x - sp.x), Math.Abs (ep.y - sp.y));
			var areaRect = new OpenCvSharp.Rect (location, sizeRect);
			Rect2d obj = Rect2d.Empty;

			// If not dragged - show the tracking data
			/*
			if (!isDragging) {
				// drop tracker if the frame's size has changed, this one is necessary as tracker doesn't hold it well
				if (frameSize.Height != 0 && frameSize.Width != 0 && scaledFrame.Size () != frameSize)
					DropTracking ();

				// we have to tracker - let's initialize one
				if (null == tracker) {
					// but only if we have big enough "area of interest", this one is added to avoid "tracking" some 1x2 pixels areas
					if ((ep - sp).magnitude >= minimumAreaDiagonal) {
						obj = new Rect2d (areaRect.X, areaRect.Y, areaRect.Width, areaRect.Height);

						// initial tracker with current image and the given rect, one can play with tracker types here
						tracker = Tracker.Create (TrackerTypes.MedianFlow);
						tracker.Init (scaledFrame, obj);

						frameSize = scaledFrame.Size ();
					}
				}
				// if we already have an active tracker - just to to update with the new frame and check whether it still tracks object
				else {
					if (!tracker.Update (scaledFrame, ref obj))
						obj = Rect2d.Empty;
				}

				// save tracked object location
				if (0 != obj.Width && 0 != obj.Height)
					areaRect = new OpenCvSharp.Rect ((int)obj.X, (int)obj.Y, (int)obj.Width, (int)obj.Height);
			}
			*/
			// render rect we've tracker or one is being drawn by the user
			if (isDragging || (null != tracker && obj.Width != 0))
				Cv2.Rectangle ((InputOutputArray)frame, areaRect * (1.0 / downScale), isDragging ? Scalar.Red : Scalar.Blue);
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
			Debug.Log ();
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