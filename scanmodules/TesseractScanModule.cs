using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Util;
using Tesseract.Droid;
using NativeScanLib;
using NativeScanLib.Models;
using Android.Widget;
using System.Text.RegularExpressions;

namespace ScanPac.scanmodules
{
    public delegate void HandleMrzResult(PassportModel result);

    public class TesseractScanModule
    {
        Context Context { get; set; }
        const string TAG = "TESSERACT OCR";

        public event HandleMrzResult OnHandleMrzResult;

        public TesseractScanModule(Context context)
        {
            this.Context = context;
        }

        public async Task<string> ScanImage(Stream data){
            var api = await InitaliazeApi();
            await api.SetImage(data);

            Log.Debug("TESSERACT OCR", api.Text);
            return api.Text;
        }

        public async Task<string> ScanImage(byte[] data)
        {
            var api =  await InitaliazeApi();
            await api.SetImage(data);
            Log.Debug(TAG, api.Text);

            var result = GetResultFromTextLines(api.Text);
            TriggerHandleMrzResult(result);

            return api.Text;
        }

        public async Task<string> ScanImage(string filePath)
        {
            var api = await InitaliazeApi();
            await api.SetImage(filePath);

            Log.Debug("TESSERACT OCR", api.Text);
            api.SetRectangle(new Tesseract.Rectangle());
            return api.Text;
        }

        async Task<TesseractApi>InitaliazeApi(){
            var api = new TesseractApi(this.Context, AssetsDeployment.OncePerVersion);
            await api.Init("ocrb");

            api.SetWhitelist("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789<");
            api.Progress += ScanProgress;

            return api;
        }

        private void ScanProgress(object sender, Tesseract.ProgressEventArgs e){
            //Log.Debug("TesseractScanModule", e.Progress.ToString());
        }

        private PassportModel GetResultFromTextLines(string text){
            var mrz = PassportParser.ParsePassport(text);
            if (mrz != null && mrz.Valid)
            {
                return mrz;
            }

            return null;
        }

        private void TriggerHandleMrzResult(PassportModel result)
        {
            if(OnHandleMrzResult != null && result != null){
                OnHandleMrzResult(result);
            }
        }
    }
}
