using System;
using Android.Util;

namespace ScanPac.Camera
{
    public static class CameraConstants
    {
        public static readonly string TAG = "FocusCamera";
        public static readonly int REQUEST_CAMERA_PERMISSION = 1;
        public static readonly string FRAGMENT_DIALOG = "dialog";
        // Max preview width that is guaranteed by Camera2 API
        public static readonly int MAX_PREVIEW_WIDTH = 1920;
        // Max preview height that is guaranteed by Camera2 API
        public static readonly int MAX_PREVIEW_HEIGHT = 1080;
        public static Size CustomSize = new Size(960, 1280);

        public enum CameraStates
        {
            // Camera state: Showing camera preview.
            STATE_PREVIEW = 0,
            // Camera state: Waiting for the focus to be locked.
            STATE_WAITING_LOCK = 1,
            // Camera state: Waiting for the exposure to be precapture state.
            STATE_WAITING_PRECAPTURE = 2,
            //Camera state: Waiting for the exposure state to be something other than precapture.
            STATE_WAITING_NON_PRECAPTURE = 3,
            // Camera state: Picture was taken.
            STATE_PICTURE_TAKEN = 4,
        }
    }
}
