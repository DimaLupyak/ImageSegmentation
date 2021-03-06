﻿using ImageSegmentationModel;
using ImageSegmentationModel.Filter;
using ImageSegmentationModel.Segmentation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ImageSegmentation.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private PerfomanceInfo perfomanceInfo;
        public PerfomanceInfo PerfomanceInfo
        {
            get
            {
                return perfomanceInfo;
            }
            set
            {
                perfomanceInfo = value;
                RaisePropertyChanged("PerfomanceInfo");
            }
        }
        private bool randomColor = true;
        public bool RandomColor
        {
            get { return randomColor; }
            set
            {
                if (value != randomColor)
                {
                    randomColor = value;
                    RaisePropertyChanged("RandomColor");
                }
            }
        }
        private bool makeBorders = true;
        public bool MakeBorders
        {
            get { return makeBorders; }
            set
            {
                if (value != makeBorders)
                {
                    makeBorders = value;
                    RaisePropertyChanged("MakeBorders");
                }
            }
        }
        private int k = 400;
        public int K
        {
            get
            {
                return k;
            }
            set
            {
                k = value;
                RaisePropertyChanged("K");
            }
        }
        private int n = 1000;
        public int N
        {
            get
            {
                return n;
            }
            set
            {
                n = value;
                RaisePropertyChanged("N");
            }
        }
        private float _sigma = 0.8f;
        public float Sigma
        {
            get
            {
                return _sigma;
            }
            set
            {
                _sigma = value;
                RaisePropertyChanged("Sigma");
            }
        }
        private int _minSize = 100;
        public int MinSize
        {
            get
            {
                return _minSize;
            }
            set
            {
                _minSize = value;
                RaisePropertyChanged("MinSize");
            }
        }

        private SortModification sortModification = SortModification.NoSorting;
        public SortModification SortModification
        {
            get
            {
                return sortModification;
            }
            set
            {
                sortModification = value;
                RaisePropertyChanged("SortModification");
            }
        }

        private MargeHeuristic margeHeuristic = MargeHeuristic.Credit;
        public MargeHeuristic MargeHeuristic
        {
            get
            {
                return margeHeuristic;
            }
            set
            {
                margeHeuristic = value;
                RaisePropertyChanged("MargeHeuristic");
            }
        }

        private ConnectingMethod connection = ConnectingMethod.Connected_4;
        public ConnectingMethod Connection
        {
            get
            {
                return connection;
            }
            set
            {
                connection = value;
                RaisePropertyChanged("Connection");
            }
        }

        private ColorDifference difType = ColorDifference.RGB_std_deviation;
        public ColorDifference DifType
        {
            get
            {
                return difType;
            }
            set
            {
                difType = value;
                RaisePropertyChanged("DifType");
            }
        }

        public MainViewModel()
        {
        }

        private RelayCommand openImageCommand;
        public RelayCommand OpenImageCommand
        {
            get { return openImageCommand ?? (openImageCommand = new RelayCommand(OpenImage)); }
        }

        private RelayCommand saveImageCommand;
        public RelayCommand SaveImageCommand
        {
            get { return saveImageCommand ?? (saveImageCommand = new RelayCommand(SaveImage)); }
        }

        private RelayCommand gaussianImageCommand;
        public RelayCommand GaussianImageCommand
        {
            get { return gaussianImageCommand ?? (gaussianImageCommand = new RelayCommand(GaussianImage)); }
        }

        private RelayCommand superPixelImageCommand;
        public RelayCommand SuperPixelImageCommand
        {
            get { return superPixelImageCommand ?? (superPixelImageCommand = new RelayCommand(SuperPixelImage)); }
        }

        private RelayCommand segmentImageCommand;
        public RelayCommand SegmentImageCommand
        {
            get { return segmentImageCommand ?? (segmentImageCommand = new RelayCommand(SegmentImage)); }
        }

        public void OpenImage()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Image files (*.gif;*.jpg;*.jpe;*.png;*.bmp;*.dib;*.tif;*.tifF;*.wmf;*.ras;*.eps;*.pcx;*psd;*.tga)|*.gif;*.jpg;*.jpe;*.png;*.bmp;*.dib;*.tif;*.tifF;*.wmf;*.ras;*.eps;*.pcx;*psd;*.tga";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                OpenImage(dlg.FileName);
            }

        }

        public void OpenImage(string fileName)
        {
            try
            {
                SegmentedImage = null;
                OriginImage = new ImageViewModel(new Bitmap(fileName));

                ClearPerfomanceInfo();
                RaisePropertyChanged("PerfomanceInfo");
                RaisePropertyChanged("CanProcessing");
            }
            catch (Exception) { }
        }

        private void SaveImage()
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".png";
            dlg.FileName = "Sigmented Image";
            dlg.Title = "Збереження сегментованого зображення";
            dlg.Filter = "Image files (*.gif;*.jpg;*.jpe;*.png;*.bmp;*.dib;*.tif;*.tifF;*.wmf;*.ras;*.eps;*.pcx;*psd;*.tga)|*.gif;*.jpg;*.jpe;*.png;*.bmp;*.dib;*.tif;*.tifF;*.wmf;*.ras;*.eps;*.pcx;*psd;*.tga| JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif|PNG Image|*.png";
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                SegmentedImage.Bitmap.Save(dlg.FileName);
            }

        }

        public void SegmentImage()
        {

            Task task = Task.Factory.StartNew(() =>
            {
                CanNewExecute = false;
                try
                {
                    IFhSegmentation segmentation = SegmentationFactory.Instance.GetFhSegmentation(SortModification, MargeHeuristic);
                    RGB[,] pixels = ImageHelper.GetPixels(OriginImage.Bitmap);
                    if (pixels != null)
                    {
                        var input = OriginImage.Bitmap;
                        int[] klabels;
                        int numlabels;
                        var watch = Stopwatch.StartNew();                        
                        var superPixel = new SLICO().PerformSLICO_ForGivenK(
                            ref input,
                            out klabels,
                            out numlabels,
                            N,
                            Color.Red);
                        watch.Stop();
                        perfomanceInfo.SuperpixelPerfomance = watch.ElapsedMilliseconds;
                        BitmapData bitmapData = superPixel.LockBits(new Rectangle(0, 0, superPixel.Width, superPixel.Height), ImageLockMode.ReadOnly, superPixel.PixelFormat);
                        var superpixels = ImageHelper.ToMatrix(klabels, bitmapData.Width, bitmapData.Height,
                            bitmapData.Stride);
                        GaussianFilter filter = new GaussianFilter();
                        filter.Filter(OriginImage.Bitmap.Width, OriginImage.Bitmap.Height, pixels, Sigma);
                        int[,] segments = segmentation.BuildSegments(OriginImage.Bitmap.Width, OriginImage.Bitmap.Height, pixels, K, MinSize, Connection, DifType, ref perfomanceInfo, superpixels);
                        if (RandomColor) SegmentedImage = new ImageViewModel(ImageHelper.GetBitmap(segments));
                        else SegmentedImage = new ImageViewModel(ImageHelper.GetBitmap(segments, pixels, MakeBorders));
                        RaisePropertyChanged("PerfomanceInfo");
                    }
                }
                catch { }
                finally
                {
                    CanNewExecute = true;
                }
            });
        }

        public void SuperPixelImage()
        {
            Task.Factory.StartNew(() =>
            {

                ClearPerfomanceInfo();
                RaisePropertyChanged("PerfomanceInfo");
                CanNewExecute = false;
                try
                {
                    var input = OriginImage.Bitmap;
                    int[] klabels;
                    int numlabels;
                    var superPixel = new SLICO().PerformSLICO_ForGivenK(
                        ref input,
                        out klabels,
                        out numlabels,
                        N,
                        Color.Red,
                        20);
                    SegmentedImage = new ImageViewModel(superPixel);
                }
                catch { }
                finally
                {
                    CanNewExecute = true;
                }
            });
        }

        protected void ClearPerfomanceInfo()
        {
            perfomanceInfo.AlgorithmPerfomance = 0;
            perfomanceInfo.BuildingPerfomance = 0;
            perfomanceInfo.SmallSegmentMargingPerfomance = 0;
            perfomanceInfo.SortingPerfomance = 0;
            perfomanceInfo.SuperpixelPerfomance = 0;
        }


        public void GaussianImage()
        {
            Task task = Task.Factory.StartNew(() =>
            {

                ClearPerfomanceInfo();
                RaisePropertyChanged("PerfomanceInfo");
                CanNewExecute = false;
                try
                {
                    GaussianFilter filter = new GaussianFilter();
                    RGB[,] pixels = ImageHelper.GetPixels(OriginImage.Bitmap);
                    if (pixels != null)
                    {
                        filter.Filter(OriginImage.Bitmap.Width, OriginImage.Bitmap.Height, pixels, Sigma);

                        SegmentedImage = new ImageViewModel(ImageHelper.GetFilterBitmap(pixels));
                    }
                }
                catch { }
                finally
                {
                    CanNewExecute = true;
                }
            });
        }


        private List<RelayCommand> commands;
        private IList<RelayCommand> Commands
        {
            get
            {
                if (commands == null)
                {
                    commands = new List<RelayCommand>();
                    commands.Add(OpenImageCommand);
                    commands.Add(SegmentImageCommand);
                    commands.Add(GaussianImageCommand);
                    commands.Add(SuperPixelImageCommand);
                }
                return commands.AsReadOnly();
            }
        }
        public bool CanProcessing
        {
            get { return canNewExecute && OriginImage != null; }
        }
        private bool canNewExecute = true;
        public bool CanNewExecute
        {
            get { return canNewExecute; }
            set
            {
                if (value != canNewExecute)
                {
                    canNewExecute = value;
                    RaisePropertyChanged("CanNewExecute");
                    RaisePropertyChanged("CanProcessing");
                }
            }
        }

        private ImageViewModel _originImage;
        public ImageViewModel OriginImage
        {
            get { return _originImage; }
            set
            {
                if (value != _originImage)
                {
                    _originImage = value;
                    RaisePropertyChanged("OriginImage");
                }
            }
        }

        private ImageViewModel _segmentedImage;
        public ImageViewModel SegmentedImage
        {
            get { return _segmentedImage; }
            set
            {
                if (value != _segmentedImage)
                {
                    _segmentedImage = value;
                    RaisePropertyChanged("SegmentedImage");
                }
            }
        }

        #region Implement INotyfyPropertyChanged members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}