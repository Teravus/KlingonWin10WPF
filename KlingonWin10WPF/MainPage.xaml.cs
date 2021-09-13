using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LibVLCSharp;
using LibVLCSharp.WPF;
using LibVLCSharp.Shared;
using System.Windows.Threading;
using XamlAnimatedGif;

namespace KlingonWin10WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainPage : Window
    {

        private LibVLC _libVLCMain;
        private LibVLC _libVLCInfo;

        private LibVLCSharp.Shared.MediaPlayer _mediaPlayerMain;

        private Point _lastClickPoint = new Point(0d, 0d);


        private static double _OriginalMainVideoHeight = 200d;
        private static double _OriginalMainVideoWidth = 320d;

        private double _OriginalAspectRatio = _OriginalMainVideoWidth / _OriginalMainVideoHeight;

        private List<SceneDefinition> _scenes = new List<SceneDefinition>();
        private List<SceneDefinition> _infoScenes = new List<SceneDefinition>();
        private List<SceneDefinition> _computerScenes = new List<SceneDefinition>();
        private List<SceneDefinition> _holodeckScenes = new List<SceneDefinition>();

        private List<HotspotDefinition> _hotspots = new List<HotspotDefinition>();
        private Rectangle _clickRectangle = null;
        private long _maxVideoMS = 0;
        private int _titlebarsize = 40;
        private int _titlebarShowHeight = 20;
        private int _titlebarcurrentsize = 0;
        private int _titlebarinvisibleheight = 15;
        private readonly DispatcherTimer _clickTimer = new DispatcherTimer();


        private bool _MainVideoLoaded = false;
        private bool _mcurVisible = false;
        private ScenePlayer _mainScenePlayer = null;
        private SupportingPlayer _supportingPlayer = null;
        private bool _actionTime = false;
        private const Key AccentTilde = (Key)223;
        private SelectionChangedEventHandler lstSceneChanged = null;

        BitmapImage HolodeckCursor = null;  //new BitmapImage(new Uri(Path.Combine("Assets", "KlingonHolodeckCur.gif"), UriKind.Relative));
        BitmapImage dktahgCursor = null; // new BitmapImage(new Uri(Path.Combine("Assets", "dktahg.gif"), UriKind.Relative));
        //Uri HolodeckCursor = new Uri(System.IO.Path.Combine("Assets", "KlingonHolodeckCur.gif"), UriKind.Relative);
        //Uri dktahgCursor = new Uri(System.IO.Path.Combine("Assets", "dktahg.gif"), UriKind.Relative);
        bool _coreVLCInitialized = false;


        public MainPage()
        {
            InitializeComponent();
            this.KeyDown += (ob, ea) =>
            {
                Keyup(ob, ea);

            };
           
            _clickTimer.Interval = TimeSpan.FromSeconds(0.2); //wait for the other click for 200ms
            _clickTimer.Tick += (o1, em) =>
            {
                lock (_clickTimer)
                    _clickTimer.Stop();
                // Fire Single Click.

                var aspectquery = Utilities.GetMax(ClickSurface.ActualWidth, ClickSurface.ActualHeight, _OriginalAspectRatio);
                var clickareawidth = ClickSurface.ActualWidth;
                var clickareaheight = ClickSurface.ActualHeight;
                var clickOffsetL = 0d;
                var clickOffsetT = 12d;

                switch (aspectquery.Direction)
                {
                    case "W":
                        clickareawidth = aspectquery.Length;
                        clickOffsetL = (ClickSurface.ActualWidth - clickareawidth) * 0.106d; // Why are we chopping it into almost thirds instead of half 0.376d?  This should be the black bar width for top and bottom.
                        break;
                    case "H":
                        clickareaheight = aspectquery.Length;
                        clickOffsetT = (ClickSurface.ActualHeight - clickareaheight) * 0.376d;
                        break;
                }
                

                //var relclickX = (int)_lastClickPoint.X; //(int)(((_lastClickPoint.X / clickareawidth) * _OriginalMainVideoWidth) - clickOffsetL);
                //var relclickY = (int)_lastClickPoint.Y; //(int)(((_lastClickPoint.Y / clickareaheight) * _OriginalMainVideoHeight) - clickOffsetT);
                var relclickX = (int)(((_lastClickPoint.X / clickareawidth) * _OriginalMainVideoWidth) - clickOffsetL);
                var relclickY = (int)(((_lastClickPoint.Y / clickareaheight) * _OriginalMainVideoHeight) - clickOffsetT);
                long time = 0;
                TimeSpan ts = TimeSpan.Zero;
                if (_MainVideoLoaded)
                {
                    time = VideoView.MediaPlayer.Time;
                    ts = TimeSpan.FromMilliseconds(time);
                }
                System.Diagnostics.Debug.WriteLine("{0},{1}({2},{3})[{4}] - t{5} - f{6}", _lastClickPoint.X, _lastClickPoint.Y, relclickX, relclickY, ts.ToString(@"hh\:mm\:ss"), (long)((float)time), Utilities.MsTo15fpsFrames(time));
                if (_MainVideoLoaded && _mainScenePlayer != null && !string.IsNullOrEmpty(_mainScenePlayer.ScenePlaying))
                {
                    _mainScenePlayer.MouseClick(relclickX, relclickY, VideoView.MediaPlayer.IsPlaying);
                }
                if (_clickRectangle == null)
                {
                    _clickRectangle = new Rectangle();
                    _clickRectangle.Margin = new Thickness(relclickX, relclickY, 0, 0);// right - left, bot - top);
                    _clickRectangle.HorizontalAlignment = HorizontalAlignment.Left;
                    _clickRectangle.VerticalAlignment = VerticalAlignment.Top;
                    _clickRectangle.Height = 2;
                    _clickRectangle.Width = 2;
                    _clickRectangle.Stroke = new SolidColorBrush(Colors.Pink);
                    _clickRectangle.StrokeThickness = 2;
                    VVGrid.Children.Add(_clickRectangle);
                }
                else
                {
                    _clickRectangle.Margin = new Thickness(relclickX, relclickY, 0, 0);// right - left, bot - top);
                }
                //VideoView.MediaPlayer.Pause();
                //VideoView.MediaPlayer.Time = 269200;



            };


            Loaded += (s, e) =>
            {
                //ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
                //formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
                //CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
                //coreTitleBar.ExtendViewIntoTitleBar = true;

                VideoView.Loaded += (s1, e1) =>
                {
                    if (!_coreVLCInitialized)
                    {
                        _coreVLCInitialized = true;
                        Core.Initialize();
                    }
                    List<string> options = new List<string>();
                    //options.AddRange(VideoView.SwapChainOptions.ToList());
                    //options.Add("--verbose=2");
                    //options.Add("--sout-delay-id=1");
                    //options.Add("--sout-delay-delay=2000");
                    //options.Add("--sout-display-delay=3000");
                    //options.Add("--audio-desync=-5000");
                    //options.Add("--avi-index=1");
                    var optionsarray = options.ToArray();
                    _libVLCMain = new LibVLC(optionsarray);
                    _mediaPlayerMain = new LibVLCSharp.Shared.MediaPlayer(_libVLCMain);
                    VideoView.MediaPlayer = _mediaPlayerMain;
                    //_mediaPlayerMain.EnableMouseInput = false;
                    //_mediaPlayerMain.EnableKeyInput = false;

                    _libVLCMain.Log += Log_Fired;




                    VideoView.PreviewMouseDown += (s2,e2) =>
                    {
                        if (e2.ClickCount != 2)
                            return;
                        if (!_actionTime) // No pausing during active time.  It is too difficult to separate single and double clicks during some scenes that you need to rapid click.
                        {

                            SwitchGameModeActiveInfo();

                            e2.Handled = true;

                            lock (_clickTimer)
                                _clickTimer.Stop();
                        }
                    };
                    VideoView.MouseDoubleClick += (s2, e2) =>
                    {
                        if (!_actionTime) // No pausing during active time.  It is too difficult to separate single and double clicks during some scenes that you need to rapid click.
                        {

                            SwitchGameModeActiveInfo();

                            e2.Handled = true;

                            lock (_clickTimer)
                                _clickTimer.Stop();
                        }
                    };
                    VideoViewGrid.MouseDown += (s2, e2) =>
                    {
                        if (e2.ClickCount != 2)
                            return;
                        if (!_actionTime) // No pausing during active time.  It is too difficult to separate single and double clicks during some scenes that you need to rapid click.
                        {

                            SwitchGameModeActiveInfo();

                            e2.Handled = true;

                            lock (_clickTimer)
                                _clickTimer.Stop();
                        }
                    };

                    VideoView.MouseDown += (s3, s4) =>
                    {
                        var tappedspot = s4.GetPosition(s3 as UIElement);


                        _lastClickPoint = tappedspot;

                        lock (_clickTimer)
                        {
                            _clickTimer.Stop();
                            _clickTimer.Start();
                        }
                    };
                    
                    VideoViewGrid.KeyUp += (o4, s5) =>
                    {
                        Keyup(o4, s5);
                    };
                    this.KeyUp += (o4, s5) =>
                    {
                        Keyup(o4, s5);
                    };
                    VideoView.KeyUp += (o4, s5) =>
                    {
                        Keyup(o4, s5);
                    };
                    
                    
                };
                VideoInfo.Loaded += (s2, e2) =>
                {
                    if (!_coreVLCInitialized)
                    {
                        _coreVLCInitialized = true;
                        Core.Initialize();
                    }
                    List<string> options2 = new List<string>();
                    //options.AddRange(VideoInfo.SwapChainOptions.ToList());
                    //options.Add("--verbose=2");

                    _libVLCInfo = new LibVLC();
                    var _mediaPlayerInfo = new LibVLCSharp.Shared.MediaPlayer(_libVLCInfo);
                    VideoInfo.MediaPlayer = _mediaPlayerInfo;
                    _mediaPlayerInfo.EnableMouseInput = false;
                    _mediaPlayerInfo.EnableKeyInput = false;

                    _libVLCInfo.Log += Log_Fired;

                    var InfoScenes = _scenes.Where(xy => xy.SceneType == SceneType.Info).ToList();
                    _infoScenes = InfoScenes;

                    var ComputerScenes = SceneLoader.LoadSupportingScenesFromAsset("computerscenes.txt");
                    _computerScenes = ComputerScenes;
                    var HolodeckScenes = SceneLoader.LoadSupportingScenesFromAsset("holodeckscenes.txt");
                    _holodeckScenes = HolodeckScenes;
                    _supportingPlayer = new SupportingPlayer(VideoInfo, InfoScenes, ComputerScenes, HolodeckScenes, _libVLCInfo);
                    Load_Computer_list(_computerScenes);

                };
                // Save this as a delgate because we need to hook/unhook it
                lstSceneChanged =  (o, arg) =>
                {
                    
                    var ClickedItems = arg.AddedItems;
                    foreach (var item in ClickedItems)
                    {
                        ComboBoxItem citem = item as ComboBoxItem;
                        string scenename = citem.Content.ToString();
                        SceneDefinition founddef = null;
                        foreach (var scenedef in _scenes)
                        {
                            if (scenedef.Name == scenename)
                            {
                                founddef = scenedef;
                                break;
                            }
                        }
                        if (founddef != null && _MainVideoLoaded)
                        {
                            _mainScenePlayer.PlayScene(founddef);
                        }
                    }
                };
                lstScene.SelectionChanged += lstSceneChanged;

            };

            Unloaded += (s, e) =>
            {
                VideoView.MediaPlayer = null;
                _supportingPlayer.Dispose();
                VideoInfo.MediaPlayer = null;

                _mediaPlayerMain.Dispose();


                this._libVLCMain.Dispose();
                this._libVLCInfo.Dispose();
            };
            SizeChanged += (s, e) => WindowResized( s,  e);

            this.StateChanged += (s, e) =>
             {
                 if (this.WindowState == WindowState.Maximized)
                 {
                     WindowResized(s, null);
                 }
             };

            btnNewGame.Click += (s, e) =>
            {
                PrepPlayer();
                _mainScenePlayer.TheSupportingPlayer = _supportingPlayer;
                WindowResized(this, null);
                _mainScenePlayer.PlayScene(_scenes[0]);

                //CurEmulator.Visibility = Visibility.Visible;


            };

            

            lstComputer.SelectionChanged += (s, e) =>
            {

                var ClickedItems = e.AddedItems;
                foreach (var item in ClickedItems)
                {
                    ComboBoxItem citem = item as ComboBoxItem;
                    string scenename = citem.Content.ToString();
                    SceneDefinition founddef = null;
                    foreach (var scenedef in _computerScenes)
                    {
                        if (scenedef.Name == scenename)
                        {
                            founddef = scenedef;
                            break;
                        }
                    }
                    if (founddef != null && _MainVideoLoaded)
                    {
                        _supportingPlayer.DebugSetEvents(false);
                        _supportingPlayer.QueueScene(founddef, "computer"); // I'm treating these like info regardless of the actual type so it doesn't affect the main video when testing.
                    }
                }
            };

            this.MouseMove += (s, e) =>
            {
                Mouse_Moved();

            };
            btnLoadGame.Click += async (s, e) =>
            {

                var saves = await SaveLoader.LoadSavesFromAsset("RIVER.TXT");
                lstRiver.Items.Clear();
                foreach (var save in saves)
                    lstRiver.Items.Add(new ComboBoxItem() { Content = save.SaveName });

                LoadDialog.Visibility = Visibility.Visible;
                //
            };

            lstRiver.SelectionChanged += (s, e) =>
            {
                if (lstRiver.SelectedIndex >= 0)
                {
                    btnLoad.IsEnabled = true;
                }
                else
                {
                    btnLoad.IsEnabled = false;
                }
            };

            btnLoad.Click += async (s, e) =>
            {
                if (lstRiver.SelectedIndex >= 0)
                {
                    SaveDefinition savedef = null;
                    var saves = await SaveLoader.LoadSavesFromAsset("RIVER.TXT");
                    foreach (var save in saves)
                    {
                        ComboBoxItem citem = lstRiver.SelectedValue as ComboBoxItem;
                        if (save.SaveName == citem.Content.ToString())
                        {
                            savedef = save;
                        }
                    }
                    if (savedef != null)
                    {
                        LoadDialog.Visibility = Visibility.Collapsed;
                        PrepPlayer();
                        _mainScenePlayer.TheSupportingPlayer = _supportingPlayer;
                        WindowResized(this, null);
                        _mainScenePlayer.LoadSave(savedef);

                    }
                }

            };
            btnLoadCancel.Click += (s, e) =>
            {
                LoadDialog.Visibility = Visibility.Collapsed;
            };
            btnQuitGame.Click += (s, e) =>
            {
                VideoView.Width = VideoViewGrid.ActualWidth;
                VideoView.Height = VideoViewGrid.ActualHeight;
                Quit();
            };
            HolodeckCursor = new BitmapImage();
            HolodeckCursor.BeginInit();
            HolodeckCursor.UriSource = new Uri(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "KlingonHolodeckCur.gif"));
            HolodeckCursor.EndInit();
            
            new BitmapImage(new Uri(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "KlingonHolodeckCur.gif")));


            dktahgCursor = new BitmapImage();
            dktahgCursor.BeginInit();
            dktahgCursor.UriSource = new Uri(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "dktahg.gif"));// new BitmapImage(new Uri(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "dktahg.gif")));
            dktahgCursor.EndInit();

            AnimationBehavior.SetSourceUri(CurEmulator, dktahgCursor.UriSource);

            btnSaveCancel.Click += (s, e) =>
            {
                if (!VideoView.MediaPlayer.IsPlaying)
                    VideoView.MediaPlayer.Play();
                SaveDialog.Visibility = Visibility.Collapsed;

                _mcurVisible = false;
                CurEmulator.Source = dktahgCursor;
                AnimationBehavior.SetSourceUri(CurEmulator, dktahgCursor.UriSource);
                CurEmulator.Visibility = Visibility.Collapsed;
            };
            btnSave.Click += async (s, e) =>
            {
                string SaveName = txtSaveName.Text;

                if (string.IsNullOrEmpty(SaveName))
                {
                    txtSaveErrorText.Text = "Please type a Name in the box.";
                    txtSaveErrorText.Visibility = Visibility.Visible;
                    return;
                }

                if (Utilities.ValidateSaveGameName(SaveName))
                {
                    SaveDefinition info = _mainScenePlayer.GetSaveInfo();

                    info.SaveName = SaveName;

                    List<SaveDefinition> saves = await SaveLoader.LoadSavesFromAsset("RIVER.TXT");
                    saves.Add(info);
                    await SaveLoader.SaveSavesToAsset(saves, "RIVER.TXT");

                    if (!VideoView.MediaPlayer.IsPlaying)
                        VideoView.MediaPlayer.Play();
                    SaveDialog.Visibility = Visibility.Collapsed;

                    _mcurVisible = false;

                    CurEmulator.Source = dktahgCursor;
                    AnimationBehavior.SetSourceUri(CurEmulator, dktahgCursor.UriSource);

                    CurEmulator.Visibility = Visibility.Collapsed;
                    txtSaveErrorText.Visibility = Visibility.Collapsed;
                    txtSaveErrorText.Text = "";
                    return;

                }
                txtSaveErrorText.Text = "Please remove this dishonorable text you Ferengi Ha'DIbaH!";
                txtSaveErrorText.Visibility = Visibility.Visible;


            };
            ClickSurface.Click += (o, cEventArgs) =>
            {
                var tappedspot = Mouse.GetPosition(ClickSurface);
                tappedspot = new Point(tappedspot.X, tappedspot.Y - _titlebarcurrentsize);// Counter for titlebar.
                _lastClickPoint = tappedspot;

                lock (_clickTimer)
                {
                    _clickTimer.Stop();
                    _clickTimer.Start();
                }

            };
            ClickSurface.MouseDoubleClick += (o, cEventArgs) =>
            {
                if (!_actionTime) // No pausing during active time.  It is too difficult to separate single and double clicks during some scenes that you need to rapid click.
                {

                    SwitchGameModeActiveInfo();

                    cEventArgs.Handled = true;

                    lock (_clickTimer)
                        _clickTimer.Stop();
                }

            };
            ClickSurface.MouseMove += (o, cEventArgs) =>
            {
                Mouse_Moved();
            };
            CurEmulator.MouseMove += (o, cEventArgs) =>
            {
                Mouse_Moved();
            };
            ClickSurface.KeyUp += (o4, s5) =>
            {
                Keyup(o4, s5);
            };
            txtSaveText.MouseMove += (o, cEventArgs) =>
            {
                Mouse_Moved();
            };
            txtSaveName.MouseMove += (o, cEventArgs) =>
            {
                Mouse_Moved();
            };
            txtSaveErrorText.MouseMove += (o, cEventArgs) =>
            {
                Mouse_Moved();
            };
            txtOffsetMs.MouseMove += (o, cEventArgs) =>
            {
                Mouse_Moved();
            };
            txtMS.MouseMove += (o, cEventArgs) =>
            {
                Mouse_Moved();
            };
            txtLoadText.MouseMove += (o, cEventArgs) =>
            {
                Mouse_Moved();
            };
            lstScene.MouseMove += (o, cEventArgs) =>
            {
                Mouse_Moved();
            };
            SaveDialog.MouseMove += (o, cEventArgs) =>
            {
                Mouse_Moved();
            };
            LoadDialog.MouseMove += (o, cEventArgs) =>
            {
                Mouse_Moved();
            };

        }
        private void Mouse_Moved()
        {
            
            var point = Mouse.GetPosition(ClickSurface);
            
            if (point.Y < _titlebarShowHeight && WindowStyle == WindowStyle.None)
            {
                _titlebarcurrentsize = _titlebarsize;
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowResized(this, null);
            }
            if (point.Y > _titlebarShowHeight && WindowStyle == WindowStyle.SingleBorderWindow)
            {
                WindowStyle = WindowStyle.None;
                _titlebarcurrentsize = _titlebarinvisibleheight;
                WindowResized(this, null);
            }
            if (!_mcurVisible)
                return;
            CurEmulator.Margin = new Thickness(point.X, point.Y + 1, 0, 0);
        }
        private void WindowResized(object o, SizeChangedEventArgs e)
        {

            var width = this.ActualWidth;
            var height = this.ActualHeight;
            if (e != null)
            {
                width = e.NewSize.Width;
                height = e.NewSize.Height;
            }
            width = width - 15;
            height = height - _titlebarcurrentsize;
            VideoViewGrid.Width = width;
            VideoViewGrid.Height = height;
            if (_MainVideoLoaded)
            {
                ClickSurface.Width = width;
                ClickSurface.Height = height;
                if (_mainScenePlayer != null)
                {
                    var aspectquery = Utilities.GetMax(width, height, _OriginalAspectRatio);
                    switch (aspectquery.Direction)
                    {
                        case "W":
                            //_mainScenePlayer.HotspotScale = (float)(width / _OriginalMainVideoWidth);
                            break;
                        case "H":
                            //_mainScenePlayer.HotspotScale = (float)(height / _OriginalMainVideoHeight);
                            break;
                    }
                }
                VideoView.Width = width;
                VideoView.Height = height;
            }
            ImgStartMain.Height = height;
            ImgStartMain.Width = width;
            grdStartControls.Height = height;
            grdStartControls.Width = width;
            //VideoView.HorizontalAlignment = HorizontalAlignment.Left;
            //VideoView.VerticalAlignment = VerticalAlignment.Top;
        }
        private void Keyup(object o, KeyEventArgs ea)
        {
            if (_MainVideoLoaded)
            {
                // OemPlus
                // OemMinus
                // Add
                // Subtract
                switch (ea.Key)
                {
                    case Key.Q:
                        Quit();
                        return;
                    case Key.S:
                        if (SaveDialog.Visibility == Visibility.Collapsed)
                        {

                            if (VideoView.MediaPlayer.IsPlaying)
                                VideoView.MediaPlayer.Pause();
                            SaveDialog.Visibility = Visibility.Visible;
                            //var pointerPosition = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;
                            //var pos = Window.Current.CoreWindow.PointerPosition;
                            //CurEmulator.Margin = new Thickness(pos.X - Window.Current.Bounds.X, pos.Y - Window.Current.Bounds.Y + 1, 0, 0);
                            _mcurVisible = true;
                            CurEmulator.Source = dktahgCursor;
                            AnimationBehavior.SetSourceUri(CurEmulator, dktahgCursor.UriSource);
                            CurEmulator.Visibility = Visibility.Visible;
                        }
                        return;
                    case Key.N:
                        if (txtMS.Visibility == Visibility.Visible && txtOffsetMs.Visibility == Visibility.Visible) // Only if debug is visible
                        {
                            VideoView.MediaPlayer.Time -= 15000;
                        }
                        break;
                    case Key.M:
                        if (txtMS.Visibility == Visibility.Visible && txtOffsetMs.Visibility == Visibility.Visible) // Only if debug is visible
                        {
                            VideoView.MediaPlayer.Time += 15000;
                        }
                        break;

                    case Key.C:
                        if (txtMS.Visibility == Visibility.Visible && txtOffsetMs.Visibility == Visibility.Visible) // Only if debug is visible
                        {
                            if (_mainScenePlayer != null)
                            {
                                _mainScenePlayer.JumpToChallenge();
                            }
                        }
                        break;
                    case Key.Enter:
                        if (txtMS.Visibility == Visibility.Visible && txtOffsetMs.Visibility == Visibility.Visible) // Only if debug is visible
                        {
                            float offsetint = 0;
                            if (txtOffsetMs.Text.Length > 0)
                                offsetint = Convert.ToSingle(txtOffsetMs.Text);
                            if (txtMS.Text.Length > 0)
                            {
                                //VideoView.MediaPlayer.Time = (long)(((Convert.ToSingle(txtMS.Text)-2)* 98.14f) + (offsetint));
                                VideoView.MediaPlayer.Time = (long)(Utilities.Frames15fpsToMS(Convert.ToInt32(txtMS.Text) - 2) + (offsetint * 100));

                            }
                        }
                        break;
                    case Key.Oem3: // Accento Debug
                        if (txtMS.Visibility == Visibility.Visible && txtOffsetMs.Visibility == Visibility.Visible)
                        {
                            txtMS.Visibility = Visibility.Collapsed;
                            txtOffsetMs.Visibility = Visibility.Collapsed;
                            lstScene.Visibility = Visibility.Collapsed;
                            lstComputer.Visibility = Visibility.Collapsed;
                            CurEmulator.Visibility = Visibility.Collapsed;
                            _mainScenePlayer.VisualizeRemoveHotspots();
                            if (_clickRectangle != null)_clickRectangle.Visibility = Visibility.Collapsed;
                            _mcurVisible = false;
                        }
                        else
                        {
                            txtMS.Visibility = Visibility.Visible;
                            txtOffsetMs.Visibility = Visibility.Visible;
                            lstScene.Visibility = Visibility.Visible;
                            lstComputer.Visibility = Visibility.Visible;
                            CurEmulator.Visibility = Visibility.Visible;
                            if (_clickRectangle != null) _clickRectangle.Visibility = Visibility.Visible;
                            // Unhook the scene changed event because we don't want it to restart the scene
                            lstScene.SelectionChanged -= lstSceneChanged;
                            var sp = _mainScenePlayer.ScenePlaying;
                            for (int i=0;i<lstScene.Items.Count;i++)
                            {
                                ComboBoxItem item = lstScene.Items[i] as ComboBoxItem;
                                if (item.Content.ToString() == sp)
                                {
                                    lstScene.SelectedIndex = i;
                                    break;
                                }
                            }
                            lstScene.SelectionChanged += lstSceneChanged;
                            //var pointerPosition = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;

                            //var pos = this.PointFromScreen;// PointerPosition;
                            //CurEmulator.Margin = new Thickness(pos.X - Window.Current.Bounds.X, pos.Y - Window.Current.Bounds.Y + 1, 0, 0);
                            ShowCursor();
                            _mainScenePlayer.VisualizeHotspots(VVGrid);// VideoViewGrid);
                            _mcurVisible = true;
                        }
                        break;
                    case Key.Space:
                            SwitchGameModeActiveInfo();
                        break;
                    // OemPlus
                    // OemMinus
                    // Add
                    // Subtract
                    case Key.OemPlus:
                    case Key.Add:
                        _mainScenePlayer.IncreaseVolume();
                        _supportingPlayer.IncreaseVolume();
                        break;
                    case Key.OemMinus:
                    case Key.Subtract:
                        _mainScenePlayer.LowerVolume();
                        _supportingPlayer.LowerVolume();
                        break;
                }
            }
        }
        private void SwitchGameModeActiveInfo()
        {
            if (!_actionTime) // No pausing during active time.  It is too difficult to separate single and double clicks during some scenes that you need to rapid click.
            {
                if (_MainVideoLoaded)
                {
                    // If the Save or load dialog is visible, we want them resuming the video with double click
                    if (SaveDialog.Visibility == Visibility.Collapsed && LoadDialog.Visibility == Visibility.Collapsed)
                    {
                        if (VideoView.MediaPlayer.IsPlaying)
                        {
                            VideoView.MediaPlayer.Pause();
                            CurEmulator.Visibility = Visibility.Visible;

                            CurEmulator.Source = HolodeckCursor;
                            AnimationBehavior.SetSourceUri(CurEmulator, HolodeckCursor.UriSource);
                            //HolodeckCursor.Play();
                            var beep = _holodeckScenes[0];
                            var hum = _holodeckScenes[1];
                            _supportingPlayer.QueueScene(beep, "holodeck");
                            _supportingPlayer.QueueScene(hum, "holodeck", 0, true);
                            


                            //var pointerPosition = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;
                            //var pos = Window.Current.CoreWindow.PointerPosition;
                            //CurEmulator.Margin = new Thickness(pos.X - Window.Current.Bounds.X, pos.Y - Window.Current.Bounds.Y + 1, 0, 0);
                            var point = Mouse.GetPosition(ClickSurface);
                            CurEmulator.Margin = new Thickness(point.X, point.Y + 1, 0, 0);
                            _mcurVisible = true;
                        }
                        else
                        {
                            _supportingPlayer.Pause();
                            _supportingPlayer.ClearQueue();

                            VideoView.MediaPlayer.Play();

                            CurEmulator.Source = dktahgCursor;
                            AnimationBehavior.SetSourceUri(CurEmulator, dktahgCursor.UriSource);
                            //dktahgCursor.Play();
                            CurEmulator.Visibility = Visibility.Collapsed;
                            _mcurVisible = false;
                        }
                    }
                }
            }
        }
        private void Quit()
        {
            SceneDefinition scene = null;
            if (_scenes != null && _scenes.Count == 0)
            {
                PrepPlayer();
            }
            if (_scenes == null)
            {
                PrepPlayer();
            }
            if (_scenes.Count == 0)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Close();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            else
            {
                scene = _scenes[0];
                if (scene != null)
                {
                    _mainScenePlayer.PlayScene(scene);
                }
                scene = _scenes.Where(xy => xy.Name == "LOGO1").FirstOrDefault();
                if (scene != null)
                {
                    _mainScenePlayer.PlayScene(scene);
                }
            }
        }
        private void PrepPlayer()
        {
            btnNewGame.IsEnabled = false;
            grdStartControls.Visibility = Visibility.Collapsed;

            _scenes = SceneLoader.LoadScenesFromAsset("scenes.txt");
            _hotspots = HotspotLoader.LoadHotspotsFromAsset("hotspots.txt");
            _infoScenes = _scenes.Where(xy => xy.SceneType == SceneType.Info).ToList();

            for (int i = 0; i < _hotspots.Count; i++)
            {
                var hotspot = _hotspots[i];
                foreach (var scene in _scenes)
                {
                    if (hotspot.RelativeVideoName.ToLowerInvariant() == scene.Name.ToLowerInvariant())
                    {
                        if (hotspot.ActionVideo.ToLowerInvariant().StartsWith("ip"))
                        {
                            scene.PausedHotspots.Add(hotspot);
                        }
                        else
                        {
                            scene.PlayingHotspots.Add(hotspot);
                        }

                    }
                }
            }

            Load_Scene_List(_scenes);
            _mainScenePlayer = new ScenePlayer(VideoView, _scenes);
            Load_Main_Video();
            //Load_Info_Video();
            ImgStartMain.Visibility = Visibility.Collapsed;
            Mouse.OverrideCursor = Cursors.None;
            //Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = null;
            
            _mainScenePlayer.innerGrid = VVGrid;
            _mainScenePlayer.ActionOn += () =>
            {
                _actionTime = true;
                _clickTimer.Interval = TimeSpan.FromSeconds(0.05);
                ShowCursor();
            };
            _mainScenePlayer.ActionOff += () =>
            {
                _actionTime = false;
                _clickTimer.Interval = TimeSpan.FromSeconds(0.2);
                HideCursor();

            };
            _mainScenePlayer.QuitGame +=  () =>
            {
                // await ApplicationView.GetForCurrentView().TryConsolidateAsync();
                Close();
            };
            _mainScenePlayer.InfoVideoTrigger += (start, end) =>
            {
                SceneDefinition InfoSceneToPlay = _infoScenes.Where(xy => xy.StartMS >= start && xy.EndMS <= end).FirstOrDefault();
                // todo write a way to find the scene by start and end.
                
                if (InfoSceneToPlay != null)
                {
                    var hum = _holodeckScenes[1];
                    _supportingPlayer.QueueScene(InfoSceneToPlay, "info", 0);
                    _supportingPlayer.QueueScene(hum, "holodeck", 0, true);
                }

            };
        }
        private void ShowCursor()
        {
            CurEmulator.Visibility = Visibility.Visible;
            //var pointerPosition = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;

            //var pos = this.Window.Current.CoreWindow.PointerPosition;
            //CurEmulator.Margin = new Thickness(pos.X - this.Width, pos.Y - this.Height + 1, 0, 0);
            var point = Mouse.GetPosition(ClickSurface);
            CurEmulator.Margin = new Thickness(point.X, point.Y + 1, 0, 0);
            _mcurVisible = true;
        }
        private void HideCursor()
        {
            CurEmulator.Visibility = Visibility.Collapsed;

            _mcurVisible = false;
        }

        private void Load_Main_Video()
        {
            var result = _mainScenePlayer.Load_Main_Video(_libVLCMain);
            _OriginalMainVideoHeight = result.OriginalMainVideoHeight;
            _OriginalMainVideoWidth = result.OriginalMainVideoWidth;
            
            _MainVideoLoaded = result.Loaded;
           
        }
        

        private void Load_Scene_List(List<SceneDefinition> defs)
        {
            lstScene.Items.Clear();

            foreach (var def in defs)
            {
                if (def.SceneType == SceneType.Main || def.SceneType == SceneType.Inaction || def.SceneType == SceneType.Bad)
                    lstScene.Items.Add(new ComboBoxItem() { Content = def.Name });
            }
        }
        private void Load_Computer_list(List<SceneDefinition> defs)
        {
            lstComputer.Items.Clear();

            foreach (var def in defs)
            {
                lstComputer.Items.Add(new ComboBoxItem() { Content = def.Name });
            }
        }
        private void Log_Fired(object sender, LogEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine(e.FormattedLog);
        }
    }
}
