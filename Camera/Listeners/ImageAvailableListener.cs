
using System.Threading.Tasks;
using Android.Media;
using Android.Util;
using Java.IO;
using Java.Lang;
using Java.Nio;
using ScanPac.Camera;
using ScanPac.Camera.Helpers;
using ScanPac.scanmodules;

namespace ScanPac.Listeners
{
    public class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
    {
        
        public File File { get; set; }
        public Camera2BasicFragment Owner { get; set; }
        private TesseractScanModule scanModule;

        public ImageAvailableListener(TesseractScanModule module){
            scanModule = module;
        }

        public void OnImageAvailable(ImageReader reader)
        {
            var image = reader.AcquireLatestImage();
            if(Owner.capturingImage){
                if(image != null) {
                    image.Close();
                }

                return;
            }

            Owner.capturingImage = true;

            ByteBuffer buffer = image.GetPlanes()[0].Buffer;
            byte[] bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes);

            image.Close();

            if(Owner.mBackgroundHandler != null)
            {                
                Owner.mBackgroundHandler.Post(new ImageSaver(bytes, File, scanModule) { Owner = Owner });
            }
        }

        // Saves a JPEG {@link Image} into the specified {@link File}.
        private class ImageSaver : Java.Lang.Object, IRunnable
        {
            private byte[] mBytes;
            private File mFile;
            private TesseractScanModule scanModule;
            public Camera2BasicFragment Owner { get; set; }

            public ImageSaver(byte[] bytes, File file, TesseractScanModule module)
            {
                mBytes = bytes;
                mFile = file;
                scanModule = module;
            } 

            public void Run()
            {
                Task.Run(async () => 
                {
                    
                    await ParseImage(mBytes);


                    Owner.capturingImage = false;
                });
            }

            async Task ParseImage(byte[] bytes){
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                var bitmap = BitmapHelper.BytesToBitmap(bytes);
                var middle = CameraConstants.CustomSize.Height / 2;

                var x1 = (middle - (middle / 2));
                var x2 = (middle + (middle / 2));

                bitmap = BitmapHelper.CropBitmap(bitmap, x1, x2);
                bitmap = BitmapHelper.GrayscaleToBin(bitmap);
                var newBytes = BitmapHelper.BitmapToBytes(bitmap);

                sw.Stop();
                Log.Debug("STOPWATCH", sw.ElapsedMilliseconds.ToString());
                SaveImage(newBytes);
                sw.Reset();
                sw.Start();
                await TriggerScanImage(newBytes);
                sw.Stop();
                Log.Debug("STOPWATCH2", sw.ElapsedMilliseconds.ToString());
            }

            void SaveImage(byte[] bytes)
            {
                using (var output = new FileOutputStream(mFile))
                {
                    try
                    {
                        output.Write(bytes);
                    }
                    catch (IOException e)
                    {
                        e.PrintStackTrace();
                    }
                }
            }

            async Task TriggerScanImage(byte[] bytes)
            {
                if(scanModule != null)
                {
                    await scanModule.ScanImage(bytes);
                }
            }
        }
    }
}