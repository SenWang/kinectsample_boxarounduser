using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace WpfApplication1
{

    public partial class ColorWindow : Window
    {
        KinectSensor kinect;
        public ColorWindow(KinectSensor sensor) : this()
        {
            kinect = sensor;
        }

        public ColorWindow()
        {
            InitializeComponent();
            Loaded += ColorWindow_Loaded;
            Unloaded += ColorWindow_Unloaded;
        }
        void ColorWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (kinect != null)
            {
                kinect.ColorStream.Disable();
                kinect.DepthStream.Disable();
                kinect.SkeletonStream.Disable();
                kinect.AllFramesReady -= mykinect_AllFramesReady;
                kinect.Stop();
            }
        }
        private WriteableBitmap _ColorImageBitmap;
        private Int32Rect _ColorImageBitmapRect;
        private int _ColorImageStride;
        private WriteableBitmap _DepthImageBitmap;
        private Int32Rect _DepthImageBitmapRect;
        private int _DepthImageStride;
        void ColorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (kinect != null)
            {
                #region 彩色影像與深度影像初始化
                ColorImageStream colorStream = kinect.ColorStream;
                kinect.ColorStream.Enable();
                _ColorImageBitmap = new WriteableBitmap(colorStream.FrameWidth,colorStream.FrameHeight, 96, 96,PixelFormats.Bgr32, null);
                _ColorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth,colorStream.FrameHeight);
                _ColorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
                ColorData.Source = _ColorImageBitmap;

                DepthImageStream depthStream = kinect.DepthStream;

                //預設解析度
                kinect.DepthStream.Enable(); 

                //降低解析度至 80x60
                //kinect.DepthStream.Enable(DepthImageFormat.Resolution80x60Fps30);  
 
                _DepthImageBitmap = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight, 96, 96, PixelFormats.Gray16, null);
                _DepthImageBitmapRect = new Int32Rect(0, 0, depthStream.FrameWidth, depthStream.FrameHeight);
                _DepthImageStride = depthStream.FrameWidth * depthStream.FrameBytesPerPixel;
                DepthData.Source = _DepthImageBitmap;
                #endregion

                kinect.SkeletonStream.Enable();

                kinect.AllFramesReady += mykinect_AllFramesReady;

                kinect.Start();
            }
        }

        DepthImageFrame depthframe;
        short[] depthpixelData;
        DepthImagePixel[] depthPixel;
        ColorImageFrame colorframe;
        byte[] colorpixelData;
        void mykinect_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            depthframe = e.OpenDepthImageFrame();          
            colorframe = e.OpenColorImageFrame();

            if (depthframe == null || colorframe == null)
                return;

            depthpixelData = new short[depthframe.PixelDataLength];                
            depthframe.CopyPixelDataTo(depthpixelData);
            _DepthImageBitmap.WritePixels(_DepthImageBitmapRect, depthpixelData, _DepthImageStride, 0);
            depthPixel = new DepthImagePixel[depthframe.PixelDataLength];
            depthframe.CopyDepthImagePixelDataTo(depthPixel);
            
            colorpixelData = new byte[colorframe.PixelDataLength];
            colorframe.CopyPixelDataTo(colorpixelData);
            
            if (depthpixelData != null)
                UserBorderCaculation();
            
            _ColorImageBitmap.WritePixels(_ColorImageBitmapRect, colorpixelData, _ColorImageStride, 0);
            
            depthframe.Dispose();
            colorframe.Dispose();
        }
        
        int min_x = 0;
        int min_y = 0;
        int max_x = 0;
        int max_y = 0;
        void BorderReset()
        {
            min_x = 640;
            min_y = 480;
            max_x = 0;
            max_y = 0;
        }

        void DisplayBorder()
        {
            if (max_x < min_x || max_y < min_y)
                return;
            //Title = "min_x:" + min_x + "; min_y:" + min_y + "; max_x:" + max_x + "; max_y:" + max_y; 
            UserBorder.Width = max_x - min_x;
            UserBorder.Height = max_y - min_y;

            Canvas.SetLeft(UserBorder, min_x);
            Canvas.SetTop(UserBorder, min_y);
        }

        ColorImagePoint[] colorpoints;
        void UserBorderCaculation()
        {
            colorpoints = new ColorImagePoint[depthpixelData.Length] ;
            kinect.CoordinateMapper.MapDepthFrameToColorFrame(
                depthframe.Format, 
                depthPixel, 
                colorframe.Format, 
                colorpoints);

            BorderReset();
            for (int i = 0; i < depthpixelData.Length; i++)
            {
                PixelInRange(i);
            }
            DisplayBorder();
        }

        void PixelInRange(int i)
        {
            //取出Player Index 傳統方法
            //int playerIndex = depthpixelData[i] & DepthImageFrame.PlayerIndexBitmask; 

            //取出Player Index 新方法
            int playerIndex = depthPixel[i].PlayerIndex;

            if (playerIndex > 0)
            {
                ColorImagePoint p = colorpoints[i];
                if (p.X < min_x)
                    min_x = p.X;
                else if (p.X > max_x)
                    max_x = p.X;

                if (p.Y < min_y)
                    min_y = p.Y;
                else if (p.Y > max_y)
                    max_y = p.Y;
            }
        }

    }
}
