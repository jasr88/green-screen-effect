namespace FunnyFace.Simple2D2FAnimation {
	using TMPro;
	using UnityEngine;

	public class CameraSettings :MonoBehaviour {
		[SerializeField]
		private CaptureCamera captureCamera;
		[SerializeField]
		private Canvas settingsCanvas;

		public TextMeshProUGUI osLabel;
		public TextMeshProUGUI fpsLabel;
		public TextMeshProUGUI cameraLabel;
		public TextMeshProUGUI resolutionLabel;
		public TextMeshProUGUI filterLabel;

		private void Awake() {
			captureCamera.OnDeviceReady += UpdateDeviceInfo;
		}

		public void UpdateDeviceInfo() {
			osLabel.text = "OS: " + SystemInfo.operatingSystem;
			cameraLabel.text = "Camera: " + captureCamera.currentDeviceInfo.deviceName;
			resolutionLabel.text = "Resolution: " + captureCamera.currentDeviceInfo.resolution;
			filterLabel.text = captureCamera.currentDeviceInfo.filterMode.ToString();
		}
		public void ToogleSettingsCanvas() {
			settingsCanvas.enabled = !settingsCanvas.enabled;
		}

		void Update() {
			fpsLabel.text = "FPS: " + Mathf.RoundToInt (1.0f / Time.deltaTime);
		}
		
	}
}
