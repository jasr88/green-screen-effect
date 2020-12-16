namespace FunnyFace.Simple2D2FAnimation {
	using UnityEngine;
	using OpenCvSharp;

	public class GenericExampleCamera :CaptureCamera {

		public void CicleCamera() {
			int nextCamera = DeviceIndex + 1 >= DevicesCount ? 0 : DeviceIndex + 1;
			SetActiveCamera (nextCamera);
		}

		public void CicleFilterMode() {
			FilterMode fm = (int)(currentDeviceInfo.filterMode + 1) > 2 ? 0 : currentDeviceInfo.filterMode + 1;
			SetFilterMode (fm);
		}

		protected override bool ProcessTexture(WebCamTexture input, ref Texture2D output) {
			Mat img = Unity.TextureToMat (input, TextureParameters);

			// Clean up image using Gaussian Blur
			Mat imgGrayBlur = new Mat ();
			Cv2.GaussianBlur (img, imgGrayBlur, new Size (5, 5), 0);

			output = Unity.MatToTexture (imgGrayBlur, output);
			return true;
		}
	}
}