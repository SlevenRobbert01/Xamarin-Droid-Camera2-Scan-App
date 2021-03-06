﻿using System;
using Android.Content;
using Android.Widget;
using Java.Lang;

namespace ScanPac.Camera.Classes
{
    public class ShowToastRunnable : Java.Lang.Object, IRunnable
    {
        private string text;
        private Context context;

        public ShowToastRunnable(Context context, string text)
        {
            this.context = context;
            this.text = text;
        }

        public void Run()
        {
            Toast.MakeText(context, text, ToastLength.Short).Show();
        }
    }
}
