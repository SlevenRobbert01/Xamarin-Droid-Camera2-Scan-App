
using System;
using Android.Hardware.Camera2;
using Android.Util;
using ScanPac.scanmodules;

namespace ScanPac.Listeners
{
    public class CameraCaptureStillPictureSessionCallback : CameraCaptureSession.CaptureCallback
    {
        private static readonly string TAG = "CameraCaptureStillPictureSessionCallback";

        public Camera2BasicFragment Owner { get; set; }
        TesseractScanModule scanModule;

        public CameraCaptureStillPictureSessionCallback(Camera2BasicFragment owner)
        {
            Owner = owner;
        }

        public CameraCaptureStillPictureSessionCallback(Camera2BasicFragment owner, TesseractScanModule module)
        {
            Owner = owner;
            scanModule = module;
        }

        public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
        {
            base.OnCaptureCompleted(session,request,result);
            //Owner.ShowToast("Saved: " + Owner.mFile);

            //Log.Debug(TAG, Owner.mFile.ToString());

            Owner.UnlockFocus();

        }

        void TriggerScanImageAsync(string path)
        {
            if (scanModule != null)
            {
                scanModule.ScanImage(path);
            }
        }
    }
}