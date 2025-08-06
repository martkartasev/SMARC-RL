using ExternalCommunication;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Network
{
    public class ScreenshotManager : MonoBehaviour
    {
        public Camera renderCamera;
        public int width = 1920;
        public int height = 1080;

        public void DoAwake()
        {
            if (renderCamera == null) GetComponent<Camera>();
        }

        internal byte[] TakeScreenshot(Screenshot screenshot)
        {
            if (renderCamera == null) renderCamera = GameObject.FindGameObjectWithTag("ScreenshotCamera").GetComponent<Camera>();
            TransformCamera(screenshot);

            RenderTexture rt = new RenderTexture(width, height, 24);
            renderCamera.targetTexture = rt;

            Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
            renderCamera.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenShot.Apply();

            renderCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);

            byte[] bytes = screenShot.EncodeToPNG();
            return bytes;
        }

        private void TransformCamera(Screenshot screenshot)
        {
            var transformPosition = screenshot.Camera.Position.ToUnityVector();
            if (transformPosition != Vector3.zero) renderCamera.transform.position = transformPosition;

            var transformOrientation = screenshot.Camera.Euler.ToUnityVector();
            if (transformOrientation != Vector3.zero) renderCamera.transform.rotation = Quaternion.Euler(transformOrientation);
        }
    }
}