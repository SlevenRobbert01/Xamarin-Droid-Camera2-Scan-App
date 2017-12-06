using System;
using Android.App;
using Android.Content;
using Android.Util;
using Android.Views;

namespace ScanPac
{
	public class AutoFitTextureView : TextureView
	{
		private int mRatioWidth = 0;
		private int mRatioHeight = 0;
        private Context _context;

		public AutoFitTextureView(Context context)
			: this (context, null)
		{
            _context = context;
		}
		public AutoFitTextureView (Context context, IAttributeSet attrs)
			: this (context, attrs, 0)
		{
            _context = context;
		}
		public AutoFitTextureView (Context context, IAttributeSet attrs, int defStyle)
			: base (context, attrs, defStyle)
		{
            _context = context;
		}

		public void SetAspectRatio(int width, int height)
		{
			if (width == 0 || height == 0)
				throw new ArgumentException ("Size cannot be negative.");
			mRatioWidth = width;
			mRatioHeight = height;
			RequestLayout ();
		}

		protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure (widthMeasureSpec, heightMeasureSpec);
			int width = MeasureSpec.GetSize (widthMeasureSpec);
			int height = MeasureSpec.GetSize (heightMeasureSpec);

			if (0 == mRatioWidth || 0 == mRatioHeight) {
				SetMeasuredDimension (width, height);
			} else {
				if (width < (float)height * mRatioWidth / (float)mRatioHeight) {
					SetMeasuredDimension (width, width * mRatioHeight / mRatioWidth);
				} else {
					SetMeasuredDimension (height * mRatioWidth / mRatioHeight, height);
				}
			}
		}
	}
}

