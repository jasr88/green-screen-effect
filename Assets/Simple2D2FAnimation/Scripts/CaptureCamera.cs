namespace FunnyFace.Simple2D2FAnimation {
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using OpenCvSharp;
	using UnityEngine.UI;

	public abstract class CaptureCamera :MonoBehaviour {

		public delegate void DeviceReady();
		public DeviceReady OnDeviceReady;

		[SerializeField]
		private RawImage resultImage;
		private RectTransform parent;
		protected RectTransform imageTransform;
		private Texture2D renderedTexture = null;

		protected Unity.TextureConversionParams TextureParameters { get; private set; }
		private Dictionary<int, DeviceAndTexture> availableDevices = new Dictionary<int, DeviceAndTexture> ();
		private DeviceAndTexture currentDeviceAndTexture;
		private bool canContinue = false;
		public int DeviceIndex {
			get {
				return activeDeviceIndex;
			}
		}

		public int DevicesCount {
			get {
				return availableDevices.Count;
			}
		} 

		private int activeDeviceIndex = -1;

		public InputDeviceData currentDeviceInfo {
			get {
				InputDeviceData idd = new InputDeviceData ();
				idd.deviceName = currentDeviceAndTexture.device.name;
				idd.resolution = "W: " + currentDeviceAndTexture.texture.width + ", H: " + currentDeviceAndTexture.texture.height;
				idd.width = currentDeviceAndTexture.texture.width;
				idd.height = currentDeviceAndTexture.texture.height;
				idd.filterMode = currentDeviceAndTexture.texture.filterMode;
				idd.isFrontCamera = currentDeviceAndTexture.device.isFrontFacing;
				idd.isVMirror = currentDeviceAndTexture.texture.videoVerticallyMirrored;
				idd.rotationAngle = currentDeviceAndTexture.texture.videoRotationAngle;
				return idd;
			}
		}

		private bool InitializeCameras() {
			if (WebCamTexture.devices.Length <= 0) {
				Debug.LogError ("No devices found!");
				return false;
			}

			int index = 0;
			foreach (WebCamDevice wcd in WebCamTexture.devices) {
				WebCamTexture wct = new WebCamTexture (wcd.name);
				availableDevices.Add (
					index,
					new DeviceAndTexture (wcd, wct)
				);
				index++;
			}

			SetActiveCamera (0);
			return true;
		}

		public void SetFilterMode(FilterMode fm) {
			currentDeviceAndTexture.texture.filterMode = fm;
		}

		public void SetActiveCamera(int index) {
			if (activeDeviceIndex != -1) {
				currentDeviceAndTexture.texture.Stop ();
			}

			if (availableDevices.TryGetValue (index, out currentDeviceAndTexture)) {
				activeDeviceIndex = index;
				currentDeviceAndTexture.texture.Play ();
			} else {
				Debug.LogErrorFormat ("Someting went wrong. Device {0} not found!. Setting Device Index 0", index);
			}
		}

		private void RenderFrame() {
			if (resultImage != null) {
				// apply
				resultImage.texture = renderedTexture;

				// Adjust image ration according to the texture sizes 
				//resultImage.GetComponent<RectTransform> ().sizeDelta = new Vector2 (renderedTexture.width, renderedTexture.height);
				SizeToParent (resultImage);
			}
		}

		private void ReadTextureConversionParameters() {
			Unity.TextureConversionParams parameters = new Unity.TextureConversionParams ();
			parameters.FlipHorizontally = currentDeviceAndTexture.device.isFrontFacing;

			if (0 != currentDeviceAndTexture.texture.videoRotationAngle)
				parameters.RotationAngle = currentDeviceAndTexture.texture.videoRotationAngle; // cw -> ccw

			TextureParameters = parameters;
		}

		protected abstract bool ProcessTexture(WebCamTexture input, ref Texture2D output);

		private IEnumerator Start() {
			parent = resultImage.GetComponentInParent <RectTransform>();
			imageTransform = resultImage.GetComponent<RectTransform> ();

			yield return Application.RequestUserAuthorization (UserAuthorization.WebCam);
			if (Application.HasUserAuthorization (UserAuthorization.WebCam)) {
				if (!InitializeCameras ()) {
					Debug.LogError ("The app can't continue");
				}
				canContinue = true;
				OnDeviceReady?.Invoke ();
			} else {
				Debug.Log ("webcam not found");
			}
		}

		private void Update() {
			if (!canContinue) return;
			if (currentDeviceAndTexture.texture != null && currentDeviceAndTexture.texture.didUpdateThisFrame) {
				// this must be called continuously
				ReadTextureConversionParameters ();

				// process texture with whatever method sub-class might have in mind
				if (ProcessTexture (currentDeviceAndTexture.texture, ref renderedTexture)) {
					RenderFrame ();
				}
			}
		}

		public Vector2 SizeToParent(RawImage image, float padding = 0) {
			float w = 0, h = 0;

			padding = 1 - padding;
			float ratio = image.texture.width / (float)image.texture.height;
			var bounds = new UnityEngine.Rect (0, 0, parent.rect.width, parent.rect.height);
			if (Mathf.RoundToInt (imageTransform.eulerAngles.z) % 180 == 90) {
				//Invert the bounds if the image is rotated
				bounds.size = new Vector2 (bounds.height, bounds.width);
			}
			//Size by height first
			h = bounds.height * padding;
			w = h * ratio;
			if (w > bounds.width * padding) { //If it doesn't fit, fallback to width;
				w = bounds.width * padding;
				h = w / ratio;
			}

			imageTransform.SetSizeWithCurrentAnchors (RectTransform.Axis.Horizontal, w);
			imageTransform.SetSizeWithCurrentAnchors (RectTransform.Axis.Vertical, h);
			return imageTransform.sizeDelta;
		}


		void OnDestroy() {
			if (currentDeviceAndTexture != null) {
				if (currentDeviceAndTexture.texture.isPlaying) {
					currentDeviceAndTexture.texture.Stop ();
					currentDeviceAndTexture.texture = null;
				}
			}
		}
	} // End of class:  Capture Camera

	#region Helper Clases ==============================================================================================================================================
	public struct InputDeviceData {
		public string deviceName;
		public string resolution;
		public int width;
		public int height;
		public FilterMode filterMode;
		public bool isFrontCamera;
		public bool isVMirror;
		public int rotationAngle;
	}
	public class DeviceAndTexture {
		public WebCamDevice device;
		public WebCamTexture texture;

		public DeviceAndTexture(WebCamDevice device, WebCamTexture texture) {
			this.device = device;
			this.texture = texture;
		}
	}
	#endregion  =========================================================================================================================================================
}// End of namespace:  Simple2D2FAnimation
