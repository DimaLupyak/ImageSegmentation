﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageSegmentation.ViewModel
{
    public class ImageViewModel
    {
        public ImageViewModel(Bitmap bitmap)
        {
            Bitmap = bitmap;
        }

        public Bitmap Bitmap { get; private set; }

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr hObject);

        public static BitmapSource ToBitmapSource(Bitmap source)
        {
            BitmapSource bitSrc = null;
            var hBitmap = source.GetHbitmap();
            try
            {
                bitSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Win32Exception)
            {
                bitSrc = null;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
            return bitSrc;
        }

        private BitmapSource image;
        public BitmapSource Image
        {
            get { return image ?? (image = ToBitmapSource(Bitmap)); }
        }
    }
}
