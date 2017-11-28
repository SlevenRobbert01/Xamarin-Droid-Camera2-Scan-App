
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using ScanPac.scanmodules;

namespace ScanPac
{
    [Activity(Label = "ScanActivity")]
    public class ScanActivity : Activity
    {
        TesseractScanModule TesseractScanModule { get; set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.TesseractScanModule = new TesseractScanModule(this);
        }

        private bool SafeCameraOpen(int id)
        {            
            try
            {                
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return false;

        }
    }
}
