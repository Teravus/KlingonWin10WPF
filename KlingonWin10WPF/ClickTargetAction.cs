using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xaml;


namespace KlingonWin10WPF
{
    public class VideoAction
    {
        public long FrameStart { get; set; }
        public long FrameEnd { get; set; }
    }
    public class ClickTargetAction : VideoAction
    {


        public int Left { get; set; }
        public int Width { get; set; }
        public int Top { get; set; }
        public int Height { get; set; }


    }
    public enum ClickActionType
    {
        Info,
        Jump,

    }
    public class ClickAction : VideoAction
    {
        public ClickActionType ClickResult { get; set; }

    }
}
