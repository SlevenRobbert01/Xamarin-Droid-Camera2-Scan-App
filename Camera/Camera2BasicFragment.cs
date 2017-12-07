﻿using System;
using System.Collections.Generic;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Hardware.Camera2;
using Android.Graphics;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.Support.V13.App;
using Android.Support.V4.Content;
using ScanPac.Listeners;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Concurrent;
using Boolean = Java.Lang.Boolean;
using Math = Java.Lang.Math;
using Orientation = Android.Content.Res.Orientation;
using ScanPac.scanmodules;
using ScanPac.Camera.Classes;
using ScanPac.Camera.Helpers;
using ScanPac.Camera;

namespace ScanPac
{
    public class Camera2BasicFragment : Fragment, FragmentCompat.IOnRequestPermissionsResultCallback
    {
        private static readonly SparseIntArray ORIENTATIONS = new SparseIntArray();

        // TextureView.ISurfaceTextureListener handles several lifecycle events on a TextureView
        private Camera2BasicSurfaceTextureListener mSurfaceTextureListener;
        // ID of the current {@link CameraDevice}.
        private string mCameraId;
        // An AutoFitTextureView for camera preview
        private AutoFitTextureView mTextureView;
        // A {@link CameraCaptureSession } for camera preview.
        public CameraCaptureSession mCaptureSession;
        // A reference to the opened CameraDevice
        public CameraDevice mCameraDevice;
        // The size of the camera preview
        private Size mPreviewSize;
        // CameraDevice.StateListener is called when a CameraDevice changes its state
        private CameraStateListener mStateCallback;
        // An additional thread for running tasks that shouldn't block the UI.
        private HandlerThread mBackgroundThread;
        // A {@link Handler} for running tasks in the background.
        public Handler mBackgroundHandler;
        // An {@link ImageReader} that handles still image capture.
        public ImageReader mImageReader;
        // This is the output file for our picture.
        public File mFile;
        // This a callback object for the {@link ImageReader}. "onImageAvailable" will be called when a
        // still image is ready to be saved.
        private ImageAvailableListener mOnImageAvailableListener;
        //{@link CaptureRequest.Builder} for the camera preview
        public CaptureRequest.Builder mPreviewRequestBuilder;
        // {@link CaptureRequest} generated by {@link #mPreviewRequestBuilder}
        public CaptureRequest mPreviewRequest;
        // The current state of camera state for taking pictures.
        public CameraConstants.CameraStates mState = CameraConstants.CameraStates.STATE_PREVIEW;
        // A {@link Semaphore} to prevent the app from exiting before closing the camera.
        public Semaphore mCameraOpenCloseLock = new Semaphore(1);
        // Whether the current camera device supports Flash or not.
        private bool mFlashSupported;
        // Orientation of the camera sensor
        private int mSensorOrientation;
        // A {@link CameraCaptureSession.CaptureCallback} that handles events related to JPEG capture.
        public CameraCaptureListener mCaptureCallback;
        private TesseractScanModule scanModule;
        public bool capturingImage;
        public Activity mActivity;
        private View topOverlay;
        private View bottomOverlay;

        public Orientation mOrientation;

        // Shows a {@link Toast} on the UI thread.
        public void ShowToast(string text)
        {
            if (Activity != null)
            {
                Activity.RunOnUiThread(new ShowToastRunnable(Activity.ApplicationContext, text));
            }
        }

        public Camera2BasicFragment(TesseractScanModule module)
        {
            scanModule = module;
            mActivity = this.Activity;
        }

        public static Camera2BasicFragment NewInstance(TesseractScanModule module)
        {
            return new Camera2BasicFragment(module);
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            mStateCallback = new CameraStateListener() { owner = this };
            mSurfaceTextureListener = new Camera2BasicSurfaceTextureListener(this);


            // fill ORIENTATIONS list
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation0, 90);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation90, 0);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation180, 270);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation270, 180);

            var file = new File(Activity.GetExternalFilesDir(null), "pic.jpg");
            this.mOnImageAvailableListener = new ImageAvailableListener(scanModule) { Owner = this, File = file };
            //this.mCaptureCallback = new CameraCaptureListener() { Owner = this, File = file};
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.fragment_camera2_basic, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            mTextureView = (AutoFitTextureView)view.FindViewById(Resource.Id.texture);
            topOverlay = (View)view.FindViewById(Resource.Id.overlayTop);
            bottomOverlay = (View)view.FindViewById(Resource.Id.overlayBottom);
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            mFile = new File(Activity.GetExternalFilesDir(null), "pic.jpg");
        }

        public override void OnResume()
        {
            base.OnResume();
            StartBackgroundThread();

            // When the screen is turned off and turned back on, the SurfaceTexture is already
            // available, and "onSurfaceTextureAvailable" will not be called. In that case, we can open
            // a camera and start preview from here (otherwise, we wait until the surface is ready in
            // the SurfaceTextureListener).
            if (mTextureView.IsAvailable)
            {
                OpenCamera(mTextureView.Width, mTextureView.Height);
            }
            else
            {
                mTextureView.SurfaceTextureListener = mSurfaceTextureListener;
            }
        }

        public override void OnPause()
        {
            CloseCamera();
            StopBackgroundThread();
            base.OnPause();
        }

        public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
        {
            
            mOrientation = newConfig.Orientation;
            SetTextureAspectRatio(mOrientation);
            ConfigureTransform(mPreviewSize.Width, mPreviewSize.Height);

            base.OnConfigurationChanged(newConfig);
        }

        private void ConfigureOverlayViews(int width)
        {
            float ratio = (float)CameraConstants.CustomSize.Height / (float)CameraConstants.CustomSize.Width;
            var height = width * ratio;
            int overlayHeigth = (int)(height / 3);
            if (topOverlay != null)
            {
                topOverlay.SetBackgroundColor(Color.Black); // black with a variable alpha
                topOverlay.Background.SetAlpha(90);
                FrameLayout.LayoutParams parameters =
                               new FrameLayout.LayoutParams(width, overlayHeigth);
                parameters.Gravity = GravityFlags.Top;
                topOverlay.LayoutParameters = parameters;
                topOverlay.Invalidate();
            }

            if(bottomOverlay != null){
                var screenHeight = Resources.DisplayMetrics.HeightPixels;
                var bottom = (int)(screenHeight - height);

                bottomOverlay.SetBackgroundColor(Color.Black); // black with a variable alpha
                bottomOverlay.Background.SetAlpha(90);
                FrameLayout.LayoutParams parameters =
                               new FrameLayout.LayoutParams(width, overlayHeigth + bottom);
                parameters.Gravity = GravityFlags.Bottom;
                bottomOverlay.LayoutParameters = parameters;
                bottomOverlay.Invalidate();
            }
        }

        private void RequestCameraPermission()
        {
            if (FragmentCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.Camera))
            {
                new ConfirmationDialog().Show(ChildFragmentManager, CameraConstants.FRAGMENT_DIALOG);
            }
            else
            {
                FragmentCompat.RequestPermissions(this, new string[] { Manifest.Permission.Camera }, CameraConstants.REQUEST_CAMERA_PERMISSION);
            }
        }

        public void OnRequestPermissionsResult(int requestCode, string[] permissions, int[] grantResults)
        {
            if (requestCode != CameraConstants.REQUEST_CAMERA_PERMISSION)
                return;

            if (grantResults.Length != 1 || grantResults[0] != (int)Permission.Granted)
            {
                ErrorDialog.NewInstance("Resource.String.request_permission")
                           .Show(ChildFragmentManager, CameraConstants.FRAGMENT_DIALOG);
            }
        }

        // Sets up member variables related to camera.
        private void SetUpCameraOutputs(int width, int height)
        {
            var activity = Activity;
            var manager = (CameraManager)activity.GetSystemService(Context.CameraService);
            try
            {
                for (var i = 0; i < manager.GetCameraIdList().Length; i++)
                {
                    var cameraId = manager.GetCameraIdList()[i];
                    CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraId);

                    // We don't use a front facing camera in this sample.
                    var facing = (Integer)characteristics.Get(CameraCharacteristics.LensFacing);
                    if (facing != null && facing == (Integer.ValueOf((int)LensFacing.Front)))
                    {
                        continue;
                    }

                    var map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                    if (map == null)
                    {
                        continue;
                    }

                    // For still image captures, we use the largest available size.
                    var sizes = Arrays.AsList(map.GetOutputSizes((int)ImageFormatType.Jpeg));
                    Size largest = (Size)Collections.Max(Arrays.AsList(map.GetOutputSizes((int)ImageFormatType.Jpeg)),
                        new CompareSizesByArea());
                    mImageReader = ImageReader.NewInstance(CameraConstants.CustomSize.Width, CameraConstants.CustomSize.Height, ImageFormatType.Jpeg, /*maxImages*/2);
                    mImageReader.SetOnImageAvailableListener(mOnImageAvailableListener, mBackgroundHandler);

                    // Find out if we need to swap dimension to get the preview size relative to sensor
                    // coordinate.
                    var displayRotation = activity.WindowManager.DefaultDisplay.Rotation;
                    //noinspection ConstantConditions
                    mSensorOrientation = (int)characteristics.Get(CameraCharacteristics.SensorOrientation);
                    bool swappedDimensions = false;
                    switch (displayRotation)
                    {
                        case SurfaceOrientation.Rotation0:
                        case SurfaceOrientation.Rotation180:
                            if (mSensorOrientation == 90 || mSensorOrientation == 270)
                            {
                                swappedDimensions = true;
                            }
                            break;
                        case SurfaceOrientation.Rotation90:
                        case SurfaceOrientation.Rotation270:
                            if (mSensorOrientation == 0 || mSensorOrientation == 180)
                            {
                                swappedDimensions = true;
                            }
                            break;
                        default:
                            Log.Error(CameraConstants.TAG, "Display rotation is invalid: " + displayRotation);
                            break;
                    } 

                    Point displaySize = new Point();
                    activity.WindowManager.DefaultDisplay.GetSize(displaySize);
                    var rotatedPreviewWidth = width;
                    var rotatedPreviewHeight = height;
                    var maxPreviewWidth = displaySize.X;
                    var maxPreviewHeight = displaySize.Y;

                    if (swappedDimensions)
                    {
                        rotatedPreviewWidth = height;
                        rotatedPreviewHeight = width;
                        maxPreviewWidth = displaySize.Y;
                        maxPreviewHeight = displaySize.X;
                    }

                    if (maxPreviewWidth > CameraConstants.MAX_PREVIEW_WIDTH)
                    {
                        maxPreviewWidth = CameraConstants.MAX_PREVIEW_WIDTH;
                    }

                    if (maxPreviewHeight > CameraConstants.MAX_PREVIEW_HEIGHT)
                    {
                        maxPreviewHeight = CameraConstants.MAX_PREVIEW_HEIGHT;
                    }

                    // Danger, W.R.! Attempting to use too large a preview size could  exceed the camera
                    // bus' bandwidth limitation, resulting in gorgeous previews but the storage of
                    // garbage capture data.

                    mPreviewSize = CameraHelper.ChooseOptimalSize(map.GetOutputSizes(Class.FromType(typeof(SurfaceTexture))),
                        rotatedPreviewWidth, rotatedPreviewHeight, maxPreviewWidth,
                        maxPreviewHeight, largest);

                    // We fit the aspect ratio of TextureView to the size of preview we picked.
                    mOrientation = Resources.Configuration.Orientation;
                    SetTextureAspectRatio(mOrientation);

                    // Check if the flash is supported.
                    var available = (Boolean)characteristics.Get(CameraCharacteristics.FlashInfoAvailable);
                    mFlashSupported = available != null ? (bool)available : false;

                    mCameraId = cameraId;
                    return;
                }
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
            catch (NullPointerException)
            {
                // Currently an NPE is thrown when the Camera2API is used but not supported on the
                // device this code runs.
                ErrorDialog.NewInstance("Resource.String.camera_error").Show(ChildFragmentManager, CameraConstants.FRAGMENT_DIALOG);
            }
        }

        // Opens the camera specified by {@link Camera2BasicFragment#mCameraId}.
        public void OpenCamera(int width, int height)
        {
            if (ContextCompat.CheckSelfPermission(Activity, Manifest.Permission.Camera) != Permission.Granted)
            {
                RequestCameraPermission();
                return;
            }
            SetUpCameraOutputs(width, height);
            ConfigureTransform(width, height);
            ConfigureOverlayViews(width);
            var activity = Activity;
            var manager = (CameraManager)activity.GetSystemService(Context.CameraService);
            try
            {
                if (!mCameraOpenCloseLock.TryAcquire(2500, TimeUnit.Microseconds))
                {
                    throw new RuntimeException("Time out waiting to lock camera opening.");
                }
                manager.OpenCamera(mCameraId, mStateCallback, mBackgroundHandler);
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
            catch (InterruptedException e)
            {
                throw new RuntimeException("Interrupted while trying to lock camera opening.", e);
            }
        }

        // Closes the current {@link CameraDevice}.
        private void CloseCamera()
        {
            try
            {
                mCameraOpenCloseLock.Acquire();
                if (null != mCaptureSession)
                {
                    mCaptureSession.Close();
                    mCaptureSession = null;
                }
                if (null != mCameraDevice)
                {
                    mCameraDevice.Close();
                    mCameraDevice = null;
                }
                if (null != mImageReader)
                {
                    mImageReader.Close();
                    mImageReader = null;
                }
            }
            catch (InterruptedException e)
            {
                throw new RuntimeException("Interrupted while trying to lock camera closing.", e);
            }
            finally
            {
                mCameraOpenCloseLock.Release();
            }
        }

        // Starts a background thread and its {@link Handler}.
        private void StartBackgroundThread()
        {
            mBackgroundThread = new HandlerThread("CameraBackground");
            mBackgroundThread.Start();
            mBackgroundHandler = new Handler(mBackgroundThread.Looper);
        }

        // Stops the background thread and its {@link Handler}.
        private void StopBackgroundThread()
        {
            mBackgroundThread.QuitSafely();
            try
            {
                mBackgroundThread.Join();
                mBackgroundThread = null;
                mBackgroundHandler = null;
            }
            catch (InterruptedException e)
            {
                e.PrintStackTrace();
            }
        }

        // Creates a new {@link CameraCaptureSession} for camera preview.
        public void CreateCameraPreviewSession()
        {
            try
            {
                SurfaceTexture texture = mTextureView.SurfaceTexture;
                if (texture == null)
                {
                    throw new IllegalStateException("texture is null");
                }

                // We configure the size of default buffer to be the size of camera preview we want.
                texture.SetDefaultBufferSize(mPreviewSize.Width, mPreviewSize.Height);

                // This is the output Surface we need to start preview.
                Surface surface = new Surface(texture);

                // We set up a CaptureRequest.Builder with the output Surface.
                mPreviewRequestBuilder = mCameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
                mPreviewRequestBuilder.AddTarget(surface);

                // Here, we create a CameraCaptureSession for camera preview.
                List<Surface> surfaces = new List<Surface>();
                surfaces.Add(surface);
                surfaces.Add(mImageReader.Surface);
                mCameraDevice.CreateCaptureSession(surfaces, new CameraCaptureSessionCallback(this), null);

            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
        }

        // Configures the necessary {@link android.graphics.Matrix}
        // transformation to `mTextureView`.
        // This method should be called after the camera preview size is determined in
        // setUpCameraOutputs and also the size of `mTextureView` is fixed.

        public void ConfigureTransform(int viewWidth, int viewHeight)
        {
            Activity activity = Activity;
            if (null == mTextureView || null == mPreviewSize || null == activity)
            {
                return;
            }
            var rotation = (int)activity.WindowManager.DefaultDisplay.Rotation;
            Matrix matrix = new Matrix();
            RectF viewRect = new RectF(0, 0, viewWidth, viewHeight);
            RectF bufferRect = new RectF(0, 0, mPreviewSize.Height, mPreviewSize.Width);
            float centerX = viewRect.CenterX();
            float centerY = viewRect.CenterY();
            if ((int)SurfaceOrientation.Rotation90 == rotation || (int)SurfaceOrientation.Rotation270 == rotation)
            {
                bufferRect.Offset(centerX - bufferRect.CenterX(), centerY - bufferRect.CenterY());
                matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);
                float scale = Math.Max((float)viewHeight / mPreviewSize.Height, (float)viewWidth / mPreviewSize.Width);
                matrix.PostScale(scale, scale, centerX, centerY);
                matrix.PostRotate(90 * (rotation - 2), centerX, centerY);
            }
            else if ((int)SurfaceOrientation.Rotation180 == rotation)
            {
                matrix.PostRotate(180, centerX, centerY);
            }
            mTextureView.SetTransform(matrix);
        }

        // Initiate a still image capture.
        private void TakePicture()
        {
            LockFocus();
        }

        // Lock the focus as the first step for a still image capture.
        private void LockFocus()
        {
            try
            {
                mPreviewRequestBuilder.Set(CaptureRequest.ControlAfTrigger, (int)ControlAFTrigger.Start);
                // Tell #mCaptureCallback to wait for the lock.
                mState = CameraConstants.CameraStates.STATE_WAITING_LOCK;
                mCaptureSession.Capture(mPreviewRequestBuilder.Build(), mCaptureCallback,
                        mBackgroundHandler);
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
        }

        // Run the precapture sequence for capturing a still image. This method should be called when
        // we get a response in {@link #mCaptureCallback} from {@link #lockFocus()}.
        public void RunPrecaptureSequence()
        {
            try
            {
                // This is how to tell the camera to trigger.
                mPreviewRequestBuilder.Set(CaptureRequest.ControlAePrecaptureTrigger, (int)ControlAEPrecaptureTrigger.Start);
                // Tell #mCaptureCallback to wait for the precapture sequence to be set.
                mState = CameraConstants.CameraStates.STATE_WAITING_PRECAPTURE;
                mCaptureSession.Capture(mPreviewRequestBuilder.Build(), mCaptureCallback, mBackgroundHandler);
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
        }

        // Capture a still picture. This method should be called when we get a response in
        // {@link #mCaptureCallback} from both {@link #lockFocus()}.
        public void CaptureStillPicture()
        {
            try
            {
                var activity = Activity;
                if (null == activity || null == mCameraDevice || capturingImage)
                {
                    return;
                }
                // This is the CaptureRequest.Builder that we use to take a picture.
                CaptureRequest.Builder captureBuilder = mCameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);
                captureBuilder.AddTarget(mImageReader.Surface);

                captureBuilder.Set(CaptureRequest.ControlMode, (int)ControlMode.Auto);

                // Use the same AE and AF modes as the preview.
                captureBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.ContinuousPicture);
                SetAutoFlash(captureBuilder);

                // Orientation
                int rotation = (int)activity.WindowManager.DefaultDisplay.Rotation;
                captureBuilder.Set(CaptureRequest.JpegOrientation, GetOrientation(rotation));

                mCaptureSession.StopRepeating();
                mCaptureSession.Capture(captureBuilder.Build(), new CameraCaptureStillPictureSessionCallback(this, scanModule), null);
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
        }

        // Retrieves the JPEG orientation from the specified screen rotation.
        public int GetOrientation(int rotation)
        {
            // Sensor orientation is 90 for most devices, or 270 for some devices (eg. Nexus 5X)
            // We have to take that into account and rotate JPEG properly.
            // For devices with orientation of 90, we simply return our mapping from ORIENTATIONS.
            // For devices with orientation of 270, we need to rotate the JPEG 180 degrees.
            return (ORIENTATIONS.Get(rotation) + mSensorOrientation + 270) % 360;
        }

        // Unlock the focus. This method should be called when still image capture sequence is
        // finished.
        public void UnlockFocus()
        {
            try
            {
                if(mCaptureSession != null){
                    // Reset the auto-focus trigger
                    mPreviewRequestBuilder.Set(CaptureRequest.ControlAfTrigger, (int)ControlAFTrigger.Cancel);
                    SetAutoFlash(mPreviewRequestBuilder);
                    mCaptureSession.Capture(mPreviewRequestBuilder.Build(), mCaptureCallback,
                            mBackgroundHandler);
                    // After this, the camera will go back to the normal state of preview.
                    mState = CameraConstants.CameraStates.STATE_PREVIEW;
                    mCaptureSession.SetRepeatingRequest(mPreviewRequest, mCaptureCallback,
                            mBackgroundHandler);
                }
               
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
        }

        public void SetAutoFlash(CaptureRequest.Builder requestBuilder)
        {
            if (mFlashSupported)
            {
                requestBuilder.Set(CaptureRequest.ControlAeMode, (int)ControlAEMode.OnAutoFlash);
            }
        }

        private void SetTextureAspectRatio(Orientation orientation)
        {
            if (orientation == Orientation.Landscape)
            {
                mTextureView.SetAspectRatio(mPreviewSize.Width, mPreviewSize.Height);
            }
            else
            {
                mTextureView.SetAspectRatio(mPreviewSize.Height, mPreviewSize.Width);
            }
        }
    }
}

