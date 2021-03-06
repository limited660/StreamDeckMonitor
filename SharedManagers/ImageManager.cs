﻿using Accord.Video.FFMPEG;
using OpenMacroBoard.SDK;
using StreamDeckSharp;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

namespace SharedManagers
{
    class ImageManager
    {
        //define StreamDeck
        public static IMacroBoard deck = StreamDeck.OpenDevice();

        //define StreamDeck icon dimensions
        public static int dimensWidth = 72;
        public static int dimensHeight = 72;

        //static header text - x Axis
        public static float xAxis = 35;

        //flag for stopping animations before closing for a clean exit 
        public static bool exitflag = false;

        //process header images and display
        public static void ProcessHeaderImages()
        {
            //create working dir
            Directory.CreateDirectory(SharedSettings.generatedDir);

            //start the image header creation
            CreateImage("Time", "header2", SettingsSDMonitor.ImageLocTime, SettingsSDMonitor.headerFontSize2, xAxis, int.Parse(SettingsSDMonitor.headerFont2Position));
            CreateImage("F/sec", "header2", SettingsSDMonitor.ImageLocFps, SettingsSDMonitor.headerFontSize2, xAxis, int.Parse(SettingsSDMonitor.headerFont2Position));
            CreateImage("Cpu:", "header1", SettingsSDMonitor.ImageLocCpu, SettingsSDMonitor.headerFontSize1, xAxis, int.Parse(SettingsSDMonitor.headerFont1Position));
            CreateImage("Gpu:", "header1", SettingsSDMonitor.ImageLocGpu, SettingsSDMonitor.headerFontSize1, xAxis, int.Parse(SettingsSDMonitor.headerFont1Position));
            CreateImage("Temp", "header2", SettingsSDMonitor.ImageLocTemp, SettingsSDMonitor.headerFontSize2, xAxis, int.Parse(SettingsSDMonitor.headerFont2Position));
            CreateImage("Load", "header2", SettingsSDMonitor.ImageLocLoad, SettingsSDMonitor.headerFontSize2, xAxis, int.Parse(SettingsSDMonitor.headerFont2Position));
            CreateImage(":", "colon", SettingsSDMonitor.ImageLocColon, SettingsSDMonitor.colonFontSize, xAxis, SettingsSDMonitor.colonPosition);
            CreateImage("", "header1", SettingsSDMonitor.ImageLocBlank, SettingsSDMonitor.headerFontSize1, xAxis, 35);

            void CreateImage(string text, string type, string filename, int textSize, float x, float y)
            {
                Font font = new Font(SettingsSDMonitor.myFontHeader1, textSize);
                Brush myBrushText = SettingsSDMonitor.HeaderBrush1;
                PointF textLocation = new PointF(x, y);
                Bitmap bitmap = new Bitmap(dimensWidth, dimensHeight);
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    //Some nice defaults for better quality (StreamDeckSharp.Examples.Drawing)
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                    //background fill color
                    Brush myBrushFill = SettingsSDMonitor.BackgroundBrush;
                    graphics.FillRectangle(myBrushFill, 0, 0, dimensHeight, dimensHeight);

                    if (type == "header1")
                    {
                        font = new Font(SettingsSDMonitor.myFontHeader1, textSize);
                        myBrushText = SettingsSDMonitor.HeaderBrush1;
                    }

                    if (type == "header2")
                    {
                        font = new Font(SettingsSDMonitor.myFontHeader2, textSize);
                        myBrushText = SettingsSDMonitor.HeaderBrush2;
                    }

                    if (type == "time")
                    {
                        font = new Font(SettingsSDMonitor.timeFont, textSize);
                        myBrushText = SettingsSDMonitor.TimeBrush;
                    }

                    if (type == "colon")
                    {
                        font = new Font(SettingsSDMonitor.colonFont, textSize);
                        myBrushText = SettingsSDMonitor.ColonBrush;
                    }

                    using (font)
                    {
                        StringFormat format = new StringFormat
                        {
                            LineAlignment = StringAlignment.Center,
                            Alignment = StringAlignment.Center
                        };
                        graphics.DrawString(text, font, myBrushText, textLocation, format);
                        bitmap.Save(filename);
                    }
                }
            }
        }

        //process video frames for animation
        public static void StartAnimation()
        {
            while (true)
            {
                //create instance of video reader and open video file
                VideoFileReader vidReader = new VideoFileReader();
                string vidFile = SharedSettings.animationImgDir + SettingsSDMonitor.animName + ".mp4";
                vidReader.Open(vidFile);

                int frameCount = Convert.ToInt32(vidReader.FrameCount);
                int adjustedCount;

                if (frameCount >= SettingsSDMonitor.framesToProcess)
                {
                    adjustedCount = SettingsSDMonitor.framesToProcess;
                }
                else
                {
                    adjustedCount = frameCount;
                }

                for (int i = 0; i < adjustedCount; i++)
                {
                    using (var vidStream = new MemoryStream())
                    {
                        //resize and save frames to MemoryStream
                        Bitmap videoFrame = new Bitmap(vidReader.ReadVideoFrame(), new Size(dimensHeight, dimensHeight));
                        videoFrame.Save(vidStream, ImageFormat.Png);

                        //dispose the video frame
                        videoFrame.Dispose();

                        //display animation from stream
                        vidStream.Seek(0, SeekOrigin.Begin);
                        var animStream = KeyBitmap.Create.FromStream(vidStream);
                        ShowAnim(animStream);
                        vidStream.Close();
                    }
                }

                vidReader.Close();

                //display animation
                void ShowAnim(KeyBitmap animStream)
                {
                    foreach (var button in SettingsSDMonitor.BgButtonList())
                    {
                        if (exitflag) break;
                        deck.SetKeyBitmap(button, animStream);
                    }

                    //frametime delay
                    int frametime = SettingsSDMonitor.FrametimeValue();
                    System.Threading.Thread.Sleep(frametime);
                }
            }
        }

        //set the static headers
        public static void SetStaticHeaders()
        {
            if (SettingsSDMonitor.CheckForLayout() == "Mini")
            {
                if (SettingsSDMonitor.isFpsCounter == "False")
                {
                    SetStaticImg("gpu", SettingsSDMonitor.KeyLocGpuHeaderMini);
                }

                SetStaticImg("cpu", SettingsSDMonitor.KeyLocCpuHeaderMini);
            }

            else
            {
                SetStaticImg("cpu", SettingsSDMonitor.KeyLocCpuHeader);
                SetStaticImg("gpu", SettingsSDMonitor.KeyLocGpuHeader);
            }
        }

        //process static images and display
        public static void SetStaticImg(string headerType, int headerLocation)
        {
            string bitmapLocation;
            if (headerType == SettingsSDMonitor.imageName)
            {
                bitmapLocation = SharedSettings.staticImgDir + headerType + ".png";
            }

            else
            {
                bitmapLocation = SharedSettings.generatedDir + headerType + ".png";
            }
            var staticBitmap = KeyBitmap.Create.FromFile(bitmapLocation);
            deck.SetKeyBitmap(headerLocation, staticBitmap);
        }

        //process data images and display
        public static void ProcessValueImg(string dataValue, string type, int location)
        {
            Brush myBrush = SettingsSDMonitor.ValuesBrush;
            PointF dataLocation = new PointF(xAxis, 50);
            Font font = new Font(SettingsSDMonitor.myFontValues, SettingsSDMonitor.valueFontSize);

            if (!dataValue.Equals(null))
            {
                if (type.Equals("f"))
                {
                    dataLocation = new PointF(xAxis, int.Parse(SettingsSDMonitor.valuesFontPosition));
                    ProcessImage(SettingsSDMonitor.ImageLocFps);
                }

                if (type.Equals("fmini"))
                {
                    dataLocation = new PointF(xAxis, int.Parse(SettingsSDMonitor.valuesFontPosition));
                    ProcessImage(SettingsSDMonitor.ImageLocFps);
                }

                if (type.Equals("t"))
                {
                    dataLocation = new PointF(xAxis, int.Parse(SettingsSDMonitor.valuesFontPosition));
                    ProcessImage(SettingsSDMonitor.ImageLocTemp);
                }

                if (type.Equals("l"))
                {
                    dataLocation = new PointF(xAxis, int.Parse(SettingsSDMonitor.valuesFontPosition));
                    ProcessImage(SettingsSDMonitor.ImageLocLoad);
                }

                if (type.Equals("ti"))
                {
                    dataLocation = new PointF(xAxis, int.Parse(SettingsSDMonitor.valuesFontPosition));
                    ProcessImage(SettingsSDMonitor.ImageLocTime);
                }

                if (type.Equals("bl"))
                {
                    dataLocation = new PointF(xAxis, SettingsSDMonitor.timePosition);
                    myBrush = SettingsSDMonitor.TimeBrush;
                    font = new Font(SettingsSDMonitor.myFontTime, SettingsSDMonitor.timeFontSize);
                    ProcessImage(SettingsSDMonitor.ImageLocBlank);
                }

                if (type.Equals("bl-sm"))
                {
                    dataLocation = new PointF(xAxis, SettingsSDMonitor.datePosition);
                    myBrush = SettingsSDMonitor.DateBrush;
                    font = new Font(SettingsSDMonitor.myFontDate, SettingsSDMonitor.dateFontSize);
                    ProcessImage(SettingsSDMonitor.ImageLocBlank);
                }

                void ProcessImage(string imagefilepath)
                {
                    string typeImage = imagefilepath;
                    Bitmap bitmap = (Bitmap)Image.FromFile(typeImage);

                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        //Some nice defaults for better quality (StreamDeckSharp.Examples.Drawing)
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                        using (font)
                        {
                            StringFormat format = new StringFormat
                            {
                                LineAlignment = StringAlignment.Center,
                                Alignment = StringAlignment.Center
                            };

                            graphics.DrawString(dataValue, font, myBrush, dataLocation, format);
                        }
                    }

                    using (var valuesStream = new MemoryStream())
                    {
                        bitmap.Save(valuesStream, ImageFormat.Png);
                        bitmap.Dispose();

                        //display values using stream
                        valuesStream.Seek(0, SeekOrigin.Begin);
                        var valStream = KeyBitmap.Create.FromStream(valuesStream);
                        deck.SetKeyBitmap(location, valStream);
                        valuesStream.Close();
                    }
                }
            }
        }

        public static void ClockStateMini(string hours, string minutes)
        {
            string showDate = SharedSettings.ShowDate();
            DateTime today = DateTime.Today;

            string dayString = today.ToString("ddd");
            string dateString = today.ToString("dd");
            string monthString = today.ToString("MMM");

            var locationHours = 0;
            var locationMinutes = 2;
            ProcessValueImg(hours, "bl", locationHours);
            ProcessValueImg(minutes, "bl", locationMinutes);

            if (showDate == "True")
            {
                var locationDayOfWeek = 3;
                var locationDate = 4;
                var locationMonth = 5;

                ProcessValueImg(dayString, "bl-sm", locationDayOfWeek);
                ProcessValueImg(dateString, "bl-sm", locationDate);
                ProcessValueImg(monthString, "bl-sm", locationMonth);
            }
        }

        public static void ClockState(string hours, string minutes)
        {
            string isCompact = SharedSettings.CompactView();
            string showDate = SharedSettings.ShowDate();

            DateTime today = DateTime.Today;

            string dayString = today.ToString("ddd");
            string dateString = today.ToString("dd");
            string monthString = today.ToString("MMM");

            //compact clock view
            if (isCompact == "True")
            {
                var locationHours = 6;
                var locationMinutes = 8;
                ProcessValueImg(hours, "bl", locationHours);
                ProcessValueImg(minutes, "bl", locationMinutes);
            }
            //expanded clock view
            else
            {
                var locationHours1 = 5;
                var locationHours2 = 6;
                var locationMinutes1 = 8;
                var locationMinutes2 = 9;

                string hours1 = hours[0].ToString();
                string hours2 = hours[1].ToString();
                string minutes1 = minutes[0].ToString();
                string minutes2 = minutes[1].ToString();

                ProcessValueImg(hours1, "bl", locationHours1);
                ProcessValueImg(hours2, "bl", locationHours2);
                ProcessValueImg(minutes1, "bl", locationMinutes1);
                ProcessValueImg(minutes2, "bl", locationMinutes2);
            }

            if (showDate == "True")
            {
                var locationDayOfWeek = 11;
                var locationDate = 12;
                var locationMonth = 13;

                ProcessValueImg(dayString, "bl-sm", locationDayOfWeek);
                ProcessValueImg(dateString, "bl-sm", locationDate);
                ProcessValueImg(monthString, "bl-sm", locationMonth);
            }
        }

        public static void StartAnimClock()
        {
            var locationColon = 7;

            if (SettingsSDMonitor.CheckForLayout() == "Mini")
            {
                locationColon = 1;
            }

            //start loop
            while (true)
            {
                if (exitflag) break;

                var loc = KeyBitmap.Create.FromFile(SettingsSDMonitor.ImageLocColon);
                deck.SetKeyBitmap(locationColon, loc);

                //animate clock colon every second
                System.Threading.Thread.Sleep(1000);
                deck.ClearKey(locationColon);
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}
