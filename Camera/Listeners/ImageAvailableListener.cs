
using System.Threading.Tasks;
using Android.Media;
using Java.IO;
using Java.Lang;
using Java.Nio;
using ScanPac.Camera.Helpers;
using ScanPac.scanmodules;

namespace ScanPac.Listeners
{
    public class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
    {
        
        public File File { get; set; }
        public Camera2BasicFragment Owner { get; set; }
        private TesseractScanModule scanModule;
        private ImageReader _reader;

        public ImageAvailableListener(TesseractScanModule module){
            scanModule = module;
        }

        public void OnImageAvailable(ImageReader reader)
        {
            if(Owner.mBackgroundHandler != null || !Owner.capturingImage)
            {
                Owner.capturingImage = true;
                Owner.mBackgroundHandler.Post(new ImageSaver(reader.AcquireNextImage(), File, scanModule) { Owner = Owner });
            }
        }

        // Saves a JPEG {@link Image} into the specified {@link File}.
        private class ImageSaver : Java.Lang.Object, IRunnable
        {
            private Image mImage;
            private File mFile;
            private TesseractScanModule scanModule;
            public Camera2BasicFragment Owner { get; set; }

            public ImageSaver(Image image, File file, TesseractScanModule module)
            {
                mImage = image;
                mFile = file;
                scanModule = module;
            } 

            public async void Run()
            {
                await Task.Run(() =>
                {
                    ParseImage(mImage);
                });
            }

            void ParseImage(Image image){
                ByteBuffer buffer = image.GetPlanes()[0].Buffer;
                byte[] bytes = new byte[buffer.Remaining()];
                buffer.Get(bytes);

                mImage.Close();

                var bitmap = BitmapHelper.BytesToBitmap(bytes);
                bitmap = BitmapHelper.GrayscaleToBin(bitmap);
                var newBytes = BitmapHelper.BitmapToBytes(bitmap);

                SaveImage(newBytes);
                TriggerScanImage(newBytes);
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
                Owner.capturingImage = false;
            }
        }
    }
}