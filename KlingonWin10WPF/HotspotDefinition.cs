using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KlingonWin10WPF
{
    //16-h,i,D'K TARG KNIFE,V007,IP023,233,239,97,77,114,130,0,98,76,113,126
    //22-h,m,green button,V018_1,V018A,3701,3775,20,65,65,94,2,20,65,65,94,1,4/4/4/3/-2/V019,2/-2/V018A,3/-2/V018A,4/0/-2/V018A,0/-2/V018A
    //h,m,1 raise shields, V016,,4442,4446,47,98,113,123,0,47,98,113,123,1,0/-2/v016A,2/-2/v016A,3/-2/,4/-2/V016D
    //h,d,main viewer,V016,IP087,1220,1274,13,40,293,128,0
    //h,i,gowron,V001,V001A,1300,1337,53,11,180,198,0,95,11,206,198
    //unknown,type,name,ParentVideo,ActionVideo,FrameStart,FrameEnd,<Top Left X,TopLeft Y, bottomRight X, BottomRight Y>,0(dunno, maybe action index..  so like the first action is 0, second action is 1 etc).
    //possible these are tweens over the frame time.   For example, Gowron's box starts big at the beginning of the frame and then thins out
    public enum HotspotType
    {
        Diagnal,
        Interpolate,
        Multi,
        Unknown

    }
    public class HotspotDefinition
    {
        private static float _Scale = 1.0f;
        public string Group { get; set; } = "h";// Don't know what this is.  But it seems to be the first element always.  Maybe represents 'Hit' FailedSleep suggested this might mean hitbox.
        public string HotSpotType { get; set; } = "i"; // Looks like this is a type identifier.   Guessing..   i=interpolate, d=Static.  m=multi (have to hit two or more)
        public string Name { get; set; }
        public string RelativeVideoName { get; set; } = "V001"; // Contains the video that this hotspot is in reference to
        public string ActionVideo { get; set; } = "V001A"; //triggers/affects video.  FOr example V001A is an Alternate/Bad branch.  IP087 is a holodeck info audio.  If this is blank then go-on.
        public int FrameStart { get; set; } = 0;
        public int FrameEnd { get; set; } = 0;
        public List<Box2d> Area { get; set; }
        public int SourceLine { get; set; }
        public List<MultiAction> multiAction { get; set; } = new List<MultiAction>();
        public HotspotType HType
        {
            get
            {
                switch (HotSpotType)
                {
                    case "i":
                        return HotspotType.Interpolate;
                    case "m":
                        return HotspotType.Multi;
                    case "d":
                        return HotspotType.Diagnal;
                }
                return HotspotType.Unknown;

            }
            set
            {
                switch (value)
                {
                    case HotspotType.Diagnal:
                        HotSpotType = "d";
                        break;
                    case HotspotType.Multi:
                        HotSpotType = "m";
                        break;
                    case HotspotType.Interpolate:
                        HotSpotType = "i";
                        break;
                    default:
                        HotSpotType = "u";
                        break;
                }
            }

        }
        public float HotspotScale
        {
            get { return _Scale; }
            set { _Scale = value; }
        }
        public bool HitTest(int X, int Y, long Milliseconds, SceneDefinition scene)
        {
            if (Area == null)
                return false;

            if (Area.Count < 1)
                return false;

            if (scene.Name.ToLowerInvariant() != RelativeVideoName.ToLowerInvariant())
                return false;

            long startMS = Utilities.Frames15fpsToMS(FrameStart) + scene.StartMS;
            long endMS = Utilities.Frames15fpsToMS(FrameEnd) + scene.StartMS;

            bool InFrame = Milliseconds >= startMS && Milliseconds <= endMS;
            float clickscale = 1f;
            switch (HType)
            {
                case HotspotType.Diagnal:
                case HotspotType.Interpolate:
                case HotspotType.Multi:
                    if (Area.Count == 1)
                    {
                        if (X >= (Area[0].TopLeft.X * clickscale) && X <= (Area[0].BottomRight.X * clickscale) && Y >= (Area[0].TopLeft.Y * clickscale) && Y <= (Area[0].BottomRight.Y * clickscale) && InFrame)
                            return true;
                        return false;
                    }
                    if (Area.Count == 2)
                    {
                        float DistanceCompleted = ((float)(Milliseconds - startMS) / (float)(endMS - startMS));

                        long interpolatetopleftX = Area[0].TopLeft.X;
                        long interpolatetopleftY = Area[0].TopLeft.Y;
                        long interpolatebottomrightX = Area[0].BottomRight.X;
                        long interpolatebottomrightY = Area[0].BottomRight.Y;
                        interpolatetopleftX = (long)((float)(interpolatetopleftX + ((Area[1].TopLeft.X - Area[0].TopLeft.X) * DistanceCompleted)));
                        interpolatetopleftY = (long)((float)interpolatetopleftY + ((Area[1].TopLeft.Y - Area[0].TopLeft.Y) * DistanceCompleted));
                        interpolatebottomrightX = (long)((float)interpolatebottomrightX + ((Area[1].BottomRight.X - Area[0].BottomRight.X) * DistanceCompleted));
                        interpolatebottomrightY = (long)((float)interpolatebottomrightY + ((Area[1].BottomRight.Y - Area[0].BottomRight.Y) * DistanceCompleted));

                        if (X >= (interpolatetopleftX * clickscale) && X <= (interpolatebottomrightX * clickscale) && Y >= (interpolatetopleftY * clickscale) && Y <= (interpolatebottomrightY * clickscale) && InFrame)
                            return true;
                        return false;
                    }
                    break;


            }
            return false;
        }
        public void Draw(Grid parentElement, long Milliseconds, SceneDefinition scene, double visualizationWidthMultiplier, double visualizationHeightMultiplier)
        {
            int left = 0;
            int top = 0;
            int right = 0;
            int bot = 0;

            if (Area == null)
                return;

            if (Area.Count < 1)
                return;

            if (scene.Name.ToLowerInvariant() != RelativeVideoName.ToLowerInvariant())
                return;

            long startMS = Utilities.Frames15fpsToMS(FrameStart) + scene.StartMS;
            long endMS = Utilities.Frames15fpsToMS(FrameEnd) + scene.StartMS;

            bool InFrame = Milliseconds >= startMS && Milliseconds <= endMS;

            switch (HType)
            {
                case HotspotType.Diagnal:
                case HotspotType.Interpolate:
                case HotspotType.Multi:
                    if (Area.Count == 1)
                        left = (int)(Area[0].TopLeft.X * (_Scale * visualizationWidthMultiplier));
                    top = (int)(Area[0].TopLeft.Y * (_Scale * visualizationHeightMultiplier));
                    right = (int)(Area[0].BottomRight.X * (_Scale * visualizationWidthMultiplier));
                    bot = (int)(Area[0].BottomRight.Y * (_Scale * visualizationHeightMultiplier));



                    if (Area.Count > 1)
                    {
                        float DistanceCompleted = ((float)(Milliseconds - startMS) / (float)(endMS - startMS));
                        if (DistanceCompleted < 0)
                            DistanceCompleted = 0;

                        if (DistanceCompleted > 1)
                        {
                            DistanceCompleted = 1;
                        }

                        long interpolatetopleftX = Area[0].TopLeft.X;
                        long interpolatetopleftY = Area[0].TopLeft.Y;
                        long interpolatebottomrightX = Area[0].BottomRight.X;
                        long interpolatebottomrightY = Area[0].BottomRight.Y;
                        interpolatetopleftX = (long)((float)(interpolatetopleftX + ((Area[1].TopLeft.X - Area[0].TopLeft.X) * DistanceCompleted)));
                        interpolatetopleftY = (long)((float)interpolatetopleftY + ((Area[1].TopLeft.Y - Area[0].TopLeft.Y) * DistanceCompleted));
                        interpolatebottomrightX = (long)((float)interpolatebottomrightX + ((Area[1].BottomRight.X - Area[0].BottomRight.X) * DistanceCompleted));
                        interpolatebottomrightY = (long)((float)interpolatebottomrightY + ((Area[1].BottomRight.Y - Area[0].BottomRight.Y) * DistanceCompleted));

                        left = (int)(interpolatetopleftX * (_Scale * visualizationWidthMultiplier));
                        top = (int)(interpolatetopleftY * (_Scale * visualizationWidthMultiplier));
                        right = (int)(interpolatebottomrightX * (_Scale * visualizationWidthMultiplier));
                        bot = (int)(interpolatebottomrightY * (_Scale * visualizationWidthMultiplier));

                    }
                    break;
                    //break;


            }
           ;


            if (VisualizationControl == null)
            {
                Rectangle VisiRect = new Rectangle();
                VisiRect.Margin = new Thickness(left, top, 0, 0);// right - left, bot - top);
                VisiRect.HorizontalAlignment = HorizontalAlignment.Left;
                VisiRect.VerticalAlignment = VerticalAlignment.Top;
                VisiRect.Height = bot - top;
                VisiRect.Width = right - left;
                VisiRect.Stroke = new SolidColorBrush(Colors.Pink);
                VisiRect.StrokeThickness = 2;

                VisualizationControl = VisiRect;
                parentElement.Children.Add(VisiRect);

            }
            else
            {
                VisualizationControl.Margin = new Thickness(left, top, 0, 0);// right - left, bot - top);
                VisualizationControl.Height = bot - top;
                VisualizationControl.Width = right - left;
            }
            if (!InFrame)
            {
                VisualizationControl.Stroke = new SolidColorBrush(Colors.LimeGreen);
                VisualizationControl.Visibility = Visibility.Collapsed;
            }
            else
            {
                VisualizationControl.Stroke = new SolidColorBrush(Colors.Red);
                VisualizationControl.Visibility = Visibility.Visible;
            }

        }
        public void ClearVisualization()
        {
            if (VisualizationControl != null)
            {
                var item = VisualizationControl;
                VisualizationControl = null;
                Grid parentGrid = item.Parent as Grid;
                parentGrid.Children.Remove(item);

            }
        }
        private Rectangle VisualizationControl { get; set; }

    }
    public class Box2d
    {
        public System.Drawing.Point TopLeft { get; set; }
        public System.Drawing.Point BottomRight { get; set; }
        public int ActionId { get; set; } = 0;
    }
    public class MultiAction
    {
        public int ClickIndex { get; set; }
        public List<int> NextButtonSequence { get; set; } = new List<int>();
        public string ResultVideo { get; set; } = string.Empty;

        //// THe problem that we have is we're not factoring in the last ID before the first Option/IF    Pretty sure this represents which button to load.
        //public override bool Equals(object obj)
        //{
        //    if (obj is MultiAction)
        //    {
        //        var objitem = obj as MultiAction;
        //        return objitem.ResultVideo == ResultVideo && objitem.NextButtonSequence == NextButtonSequence;
        //    }
        //    return base.Equals(obj);
        //}
        //public static bool operator ==(MultiAction m1, MultiAction m2)
        //{
        //    if (m1 == null && m2 == null)
        //        return true;
        //    if (m1 == null && m2 != null)
        //        return false;
        //    if (m2 == null && m1 != null)
        //        return false;
        //    return m1.Equals(m2);
        //}
        //public static bool operator !=(MultiAction m1, MultiAction m2)
        //{
        //    if (m1 == null && m2 != null)
        //        return true;
        //    if (m1 == null && m2 == null)
        //        return false;

        //    return !m1.Equals(m2);
        //}
        //public override string ToString()
        //{
        //    return base.ToString() + ClickIndex.ToString() + String.Join(",", NextButtonSequence.Select(x => x.ToString()).ToArray()) + ResultVideo.ToString();
        //}

        //public override int GetHashCode() => (ClickIndex, NextButtonSequence, ResultVideo).GetHashCode();
    }
}
