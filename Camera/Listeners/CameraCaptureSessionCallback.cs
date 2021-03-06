
using Android.Hardware.Camera2;

namespace ScanPac.Listeners
{
    public class CameraCaptureSessionCallback : CameraCaptureSession.StateCallback
    {
        public Camera2BasicFragment Owner { get; set; }

        public CameraCaptureSessionCallback(Camera2BasicFragment owner)
        {
            Owner = owner;
        }

        public override void OnConfigureFailed(CameraCaptureSession session)
        {
            Owner.ShowToast("Failed");
        }

        public override void OnConfigured(CameraCaptureSession session)
        {
            // The camera is already closed
            if (null == Owner.mCameraDevice)
            {
                return;
            }

            // When the session is ready, we start displaying the preview.
            Owner.mCaptureSession = session;
            try
            {
                Owner.mPreviewRequestBuilder.AddTarget(Owner.mImageReader.Surface);

                // Orientation
                int rotation = (int)Owner.Activity.WindowManager.DefaultDisplay.Rotation;
                Owner.mPreviewRequestBuilder.Set(CaptureRequest.JpegOrientation, Owner.GetOrientation(rotation));

                // Auto focus should be continuous for camera preview.
                Owner.mPreviewRequestBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.ContinuousVideo);
                // Flash is automatically enabled when necessary.
                //Owner.SetAutoFlash(Owner.mPreviewRequestBuilder);

                Owner.mPreviewRequestBuilder.Set(CaptureRequest.ControlMode, (int)ControlMode.Auto);
                Owner.mPreviewRequestBuilder.Set(CaptureRequest.StatisticsFaceDetectMode, (int)StatisticsFaceDetectMode.Off);

                // Finally, we start displaying the camera preview.
                Owner.mPreviewRequest = Owner.mPreviewRequestBuilder.Build();
                Owner.mCaptureSession.SetRepeatingRequest(Owner.mPreviewRequest,
                        Owner.mCaptureCallback, Owner.mBackgroundHandler);
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
        }
    }
}