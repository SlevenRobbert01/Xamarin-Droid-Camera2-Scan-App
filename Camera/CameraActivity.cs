using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Threading.Tasks;
using ScanPac.scanmodules;
using Tesseract.Droid;
using Android.Content.PM;
using Android.Util;

namespace ScanPac
{
    [Activity (Label = "Camera2Basic", MainLauncher = true, Icon = "@drawable/icon", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
	public class CameraActivity : Activity
	{
        TesseractScanModule module { get; set; }
        Camera2BasicFragment cameraFragment { get; set; }

		protected override void OnCreate (Bundle bundle)
		{
            base.OnCreate (bundle);
			ActionBar.Hide ();
			SetContentView (Resource.Layout.activity_camera);

            IntializeScanModule();

            cameraFragment = Camera2BasicFragment.NewInstance(module);
            if (bundle == null)
            {
                FragmentManager.BeginTransaction().Replace(Resource.Id.container, cameraFragment).Commit();
            }
		}

        protected override void OnResume()
        {
            base.OnResume();
        }

        private void IntializeScanModule(){
            module = new TesseractScanModule(this);
            module.OnHandleMrzResult += (result) => {
                RunOnUiThread(() => {
                    var resultString = string.Format("Scan Result: {0} {1}, {2} {3} {4}",
                                                     result.Names.LastName,
                                                     result.Names.FirstNames[0],
                                                     result.DateOfBirth.ToShortDateString(),
                                                     result.Nationality.Value,
                                                     result.NationalNumber
                                                    );
                    Toast.MakeText(this, resultString, ToastLength.Long).Show();
                });
            };
        }
    }
}


