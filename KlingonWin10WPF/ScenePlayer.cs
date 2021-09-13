using LibVLCSharp;
using LibVLCSharp.WPF;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xaml;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace KlingonWin10WPF
{
    public class ScenePlayer
    {
        public delegate void UserActionRequired();
        public delegate void InfoAction(long framestart, long frameend);
        public event UserActionRequired ActionOn;
        public event UserActionRequired ActionOff;
        public event UserActionRequired QuitGame;
        public event InfoAction InfoVideoTrigger;

        private VideoView _displayElement = null;
        private SceneDefinition _currentScene { get; set; }
        private List<SceneDefinition> _allSceneOptions { get; set; }
        private SceneDefinition _lastScene { get; set; }
        private long _challengeStartMS = 0;
        private long _challengeEndMS = 0;
        private long _lastPlayheadMS = 0;
        private long _retryMS = 0;
        private int timerMS = 50;
        private bool _challengeSectionNotificationComplete = false;
        private bool _visualizationEnabled = false;
        private int _inactionacount = 0;

        private int _bombattemptcount = 1;
        private LibVLC _vlcInstance = null;
        private int _multi_click_count = 0;
        private HotspotDefinition _multi_click_lastAction = null;
        private int _multi_click_Item_Sequence_id = -2;
        private MultiAction _special_case_multi_action = null;
        private SupportingPlayer _supportingPlayer = null;
        private string _ReplayingFromTimeStopVideo = null;  // We can't play these unless it is a main video, but we need to be able to control the fact that they should be played when playing an alternate.
                                                            // This is a bit like queuing it up for the next main scene.
        private string _loadedVideoFile = string.Empty;
        private float hotspotscale = 1.2f;

        public float HotspotScale 
        {
            get { return hotspotscale; } 
            set { hotspotscale = value;
                foreach (var item in _currentScene.PlayingHotspots)
                    item.HotspotScale = value;

                foreach (var item in _currentScene.PausedHotspots)
                    item.HotspotScale = value;
            }
        }

        private long originalVideoAudioDelayMicroseconds = -10000000;
        private NAudioBufferer _duckBufferer;

        internal Grid innerGrid { get; set; }
        public SupportingPlayer TheSupportingPlayer
        {
            set
            {
                if (_supportingPlayer == null)
                {
                    _supportingPlayer = value;
                    _supportingPlayer.SceneComplete += (o, s, t) =>
                    {
                        // We may need specific exceptions for the bomb buttons in V018_0 because they're in the computer video
                        if (t == "computer")
                        {
                            if (s == "BB2" || s == "BB1")
                            {
                                ++_bombattemptcount;
                            }
                            if (s == "BB3") // Final bomb blast attempt.
                            {
                                _bombattemptcount = 1;
                                _inactionacount = 0;
                                _ReplayingFromTimeStopVideo = null;
                                // This is a game reset.
                                PlayScene(_allSceneOptions[0]);
                                return;
                            }

                            if (_displayElement.MediaPlayer.WillPlay)
                                _displayElement.MediaPlayer.Play();
                        }
                    };
                }
            }
        }

        List<SceneDefinition> DoNothingVideos = new List<SceneDefinition>();

        // I'm using the DispatcherTimer so that it is always on the main thread and we can interact with the unmanaged library.
        private DispatcherTimer _PlayHeadTimer = new DispatcherTimer();
        public ScenePlayer(VideoView displayElement, List<SceneDefinition> allScenes)
        {
            _displayElement = displayElement;
            _allSceneOptions = allScenes;
            for (int i = 0; i < allScenes.Count; i++)
            {
                if (allScenes[i].SceneType == SceneType.Inaction)
                    DoNothingVideos.Add(allScenes[i]);
            }
            _PlayHeadTimer.Interval = new TimeSpan(0, 0, 0, 0, timerMS);
            _PlayHeadTimer.Tick += (o, e) =>
            {
                TimerTickAction();

            };
        }

        public void LoadSave(SaveDefinition def)
        {
            string scene = def.SaveScene;
            int frame = def.SaveFrame;
            var loadscene = _allSceneOptions.Where(xy => xy.Name == scene).FirstOrDefault();
            if (loadscene != null)
            {
                _inactionacount = def.DoNothingCount;
                var framems = Utilities.Frames15fpsToMS(frame - 2);
                PlayScene(loadscene, framems);
            }
        }
        public void JumpToChallenge()
        {
            if (_displayElement.MediaPlayer.IsPlaying)
            {
                if (_challengeEndMS > 50000)
                {
                    _displayElement.MediaPlayer.Time = _challengeEndMS - 5000;
                }
            }
        }

        /// <summary>
        /// Play a scene.  Start from the beginning of the scene or optionally have a time
        /// </summary>
        /// <param name="def"></param>
        /// <param name="specifictimecode"></param>
        public void PlayScene(SceneDefinition def, long specifictimecode = 0, string ReplayingFromTimeStop = null)
        {
            bool _visualizations = _visualizationEnabled;

            if (_currentScene != null && _visualizations)
                VisualizeRemoveHotspots();

            UserActionRequired userAction = ActionOff;
            if (userAction != null)
            {
                userAction();
            }

            _multi_click_count = 0;
            _multi_click_lastAction = null;
            _multi_click_Item_Sequence_id = -2;

            //  If we play the Logo screen.  We are ending.  Quit now!
            var getLogo = _allSceneOptions.Where(xy => xy.Name == "LOGO1").FirstOrDefault();
            var GowronEndsProgram = _allSceneOptions.Where(xy => xy.Name == "V008C").FirstOrDefault();

            if (_lastScene == getLogo)
            {
                UserActionRequired quituserAction = QuitGame;
                if (quituserAction != null)
                {
                    quituserAction();
                }
                return;
            }
            
            // At the end of the game, def becomes null
            if (def == null)
            {

                if (_lastScene == getLogo)
                {

                }
                else
                {
                    PlayScene(getLogo);
                    return;
                }
            }


            _lastScene = _currentScene;
            // Quit after last scene is logo.
            if (_lastScene == getLogo)
            {
                UserActionRequired quituserAction = QuitGame;
                if (quituserAction != null)
                {
                    quituserAction();
                }
                return;
            }
            
            _challengeSectionNotificationComplete = false;
            _challengeEndMS = 0;
            _challengeStartMS = 0;
            _lastPlayheadMS = 0;
            _PlayHeadTimer.Stop();
            _currentScene = def;

            // Swap videos if necessary.
            SwitchVideo(def.SceneType, def.CD);


            if (_lastScene == GowronEndsProgram) // Gowron ends program
            {
                //_lastScene = _currentScene;
                PlayScene(null);
                return;
            }

            _PlayHeadTimer.Start();

            _challengeStartMS = def.EndMS;
            _challengeEndMS = def.SuccessMS - timerMS;
            _retryMS = def.retryMS;

            _displayElement.MediaPlayer.Time = def.StartMS;
            Task.Delay(50).Wait();
            if (!_displayElement.MediaPlayer.IsPlaying)
            {
                Task.Delay(50).Wait();
                _displayElement.MediaPlayer.Play();
            }


            if (specifictimecode > 0 && _displayElement.MediaPlayer.Media.Duration > specifictimecode)
                _displayElement.MediaPlayer.Time = specifictimecode;

            if (def.SceneType == SceneType.Main)
            {

                if (ReplayingFromTimeStop != null) // We can't play these unless it is a prior to a main video
                {
                    // we need to let it play for about 300 ms to let VLC pause the image on the correct scene.
                    Task.Delay(50).Wait();
                    // We need to play 'Replaying from timestop X:X:X', so pause the main video until that one is complete and triggers the event handler.
                    _displayElement.MediaPlayer.Pause();
                    _supportingPlayer.QueueComputerScene(ReplayingFromTimeStop);
                }
                else if (_ReplayingFromTimeStopVideo != null) // We can't play these unless it is a main video, but we need to be able to control the fact that they should be played when playing an alternate.
                {
                    // So this is how I'm handling it...  
                    string rts = _ReplayingFromTimeStopVideo;
                    _ReplayingFromTimeStopVideo = null;

                    Task.Delay(50).Wait();
                    // We need to play 'Replaying from timestop X:X:X', so pause the main video until that one is complete and triggers the event handler.
                    _displayElement.MediaPlayer.Pause();
                    _supportingPlayer.QueueComputerScene(rts);
                }
            }
            else
            {
                if (ReplayingFromTimeStop != null)
                    _ReplayingFromTimeStopVideo = ReplayingFromTimeStop;
            }


            System.Diagnostics.Debug.WriteLine(string.Format("\tPlaying Scene {0}", def.Name));

            if (_visualizations)
            {
                Grid parentgrid = innerGrid;// _displayElement.Parent as Grid;
                VisualizeHotspots(parentgrid);
            }



        }

        public void LowerVolume()
        {
            var vol = _displayElement.MediaPlayer.Volume;
            vol -= 15;
            if (vol < 0)
            {
                vol = 0;
            }
            _displayElement.MediaPlayer.Volume = vol;
        }

        public void IncreaseVolume()
        {
            var vol = _displayElement.MediaPlayer.Volume;
            vol += 15;
            if (vol > 100)
            {
                vol = 100;
            }
            _displayElement.MediaPlayer.Volume = vol;
        }

        public string ScenePlaying
        {
            get
            {
                if (_currentScene == null)
                    return string.Empty;
                return _currentScene.Name;
            }
        }

        public void MouseClick(int X, int Y, bool PausedState)
        {
            if (_currentScene == null)
                return;
            List<HotspotDefinition> inFrame = new List<HotspotDefinition>();
            
            //item.Draw(ParentGrid, _displayElement.MediaPlayer.Time, _currentScene);
            
            var currtime = _displayElement.MediaPlayer.Time;
            bool playing = _displayElement.MediaPlayer.IsPlaying;
            List<HotspotDefinition> hotspotstocheck = playing ? _currentScene.PlayingHotspots : _currentScene.PausedHotspots;

            if (!playing || _lastPlayheadMS >= _challengeStartMS) // to save processing we should only run queries when the user is allowed to interact.
            {
                foreach (var hotspot in hotspotstocheck)
                {

                    var FrameStartMS = Utilities.Frames15fpsToMS(_currentScene.FrameStart + hotspot.FrameStart) + _currentScene.OffsetTimeMS;
                    var FrameEndMS = Utilities.Frames15fpsToMS(_currentScene.FrameStart + hotspot.FrameEnd) + _currentScene.OffsetTimeMS;

                    if (currtime >= FrameStartMS && currtime <= FrameEndMS)
                    {
                        inFrame.Add(hotspot);
                    }
                }

                for (int i = 0; i < inFrame.Count; i++)
                {
                    var hittestresults = (inFrame[i].HitTest(X, Y, currtime, _currentScene));
                    System.Diagnostics.Debug.WriteLine(string.Format("\t[{0}]: Hit test {1},{2}-{7}.  Box ({3},{4},{5},{6})", inFrame[i].Name + "/" + inFrame[i].ActionVideo, X, Y, inFrame[i].Area[0].TopLeft.X * 4, inFrame[i].Area[0].TopLeft.Y * 4, inFrame[i].Area[0].BottomRight.X * 4, inFrame[i].Area[0].BottomRight.Y * 4, hittestresults));
                    if (hittestresults)
                    {

                        if (playing)
                        {
                            string FrameActionVideo = inFrame[i].ActionVideo;
                            bool skipFrameActionVideodefault = false;

                            if (inFrame[i].HType != HotspotType.Multi)
                            {
                                // no-name action video means that it is a success trigger.
                                if (string.IsNullOrEmpty(FrameActionVideo))
                                {
                                    var scene = Utilities.FindNextMainScene(_allSceneOptions, _currentScene);
                                    PlayScene(scene);
                                    return;
                                }
                            }
                            else
                            {
                                string playMulti_TriggerVideo = null;
                                bool MainAlternateYN = true;
                                _displayElement.MediaPlayer.Pause();
                                ++_multi_click_count;
                                if (_multi_click_lastAction == null)
                                {
                                    _multi_click_lastAction = inFrame[i];
                                    _multi_click_Item_Sequence_id = _multi_click_count;
                                    var debugsequence = _multi_click_lastAction.multiAction.Where(xy => xy.ClickIndex == _multi_click_Item_Sequence_id).FirstOrDefault();
                                    string debugclicksequence = string.Empty;

                                    if (debugsequence != null)
                                        debugclicksequence = string.Join(",", debugsequence.NextButtonSequence.Select(x => x.ToString()).ToArray());

                                    if (!string.IsNullOrEmpty(inFrame[i].ActionVideo))
                                        _special_case_multi_action = _multi_click_lastAction.multiAction.Where(xy => xy.ClickIndex == _multi_click_Item_Sequence_id).FirstOrDefault();

                                    System.Diagnostics.Debug.WriteLine(string.Format("\t\tFirst action in Multi-Click. Starting with {0}. ClickAction:{1}. SequenceText: {2}", _multi_click_lastAction.Name, _multi_click_count, debugclicksequence));
                                }

                                // 1,0/-2/V016A,2/-2/V016A,3/-2/,4/-2/V016D

                                //1,4/4/4/3/-2/V019,2/-2/V018A,3/-2/V018A,4/0/-2/V018A,0/-2/V018A

                                var buttonid = inFrame[i].Area.Last().ActionId;
                                MultiAction activesequence = null;


                                if (_special_case_multi_action != null)
                                {
                                    RollBackFrameWithinChallenge(500);
                                }

                                if (",green button,orange button,red button,blue button,yellow button,".IndexOf("," + inFrame[i].Name + ",") > -1 )
                                {
                                    _supportingPlayer.QueueComputerScene(string.Format("B{0}", buttonid));
                                }

                                // Special case to feed to the Hotspot processor.  This is only for the green button hotspot right now.

                                var idCheck = buttonid;
                                if (_special_case_multi_action != null)
                                    idCheck = _multi_click_Item_Sequence_id;

                                activesequence = _multi_click_lastAction.multiAction.Where(xy => xy.ClickIndex == idCheck).FirstOrDefault();
                                if (_special_case_multi_action != null && _multi_click_count == 1)
                                {
                                    activesequence = null;

                                }
                                else if (_special_case_multi_action != null)
                                {
                                    activesequence = _special_case_multi_action;

                                }

                                if (activesequence != null) // Some click actions won't exist and those are the 'correct' ones with no immediate action. sometimes.  Other times, the correct action is a sequence that exists.
                                {
                                    // This is minus 2 because the click array starts at 1, and by the time we get here, we've already clicked once to get the start button in _multi_click_lastAction, so we're automatically on the second click.
                                    if (activesequence.NextButtonSequence[_multi_click_count - 2] == buttonid)
                                    {
                                        if (activesequence.NextButtonSequence.Count == _multi_click_count - 1) // we have reached the end of the sequence
                                        {
                                            playMulti_TriggerVideo = activesequence.ResultVideo;
                                            if (playMulti_TriggerVideo.Length == 5)
                                                MainAlternateYN = false;
                                            System.Diagnostics.Debug.WriteLine(string.Format("\t\tClicked through to the end of the sequence. Playing {0}", playMulti_TriggerVideo));
                                        }
                                        // If it isn't the end, don't trigger anything else.  Just accumulate clicks to move through the button sequence.
                                        // Give them some more time to click within the challenge time.
                                        RollBackFrameWithinChallenge(100);
                                    }
                                    else // They pressed a button that isn't in the sequence, Reset!
                                    {

                                        _multi_click_Item_Sequence_id = buttonid;

                                        if (_special_case_multi_action != null)
                                        {
                                            _multi_click_lastAction = inFrame[i];
                                            _multi_click_count = 1;
                                            activesequence = _multi_click_lastAction.multiAction.Where(xy => xy.ClickIndex == _multi_click_Item_Sequence_id).FirstOrDefault();
                                        }
                                        else
                                        {
                                            activesequence = _multi_click_lastAction.multiAction.Where(xy => xy.ClickIndex == _multi_click_Item_Sequence_id).FirstOrDefault();
                                        }
                                        System.Diagnostics.Debug.WriteLine(string.Format("\t\tPressed a button that isnt in sequence, resetting to {0}. Sequence has clickindex?{1}?{2}", _multi_click_lastAction.Name, _multi_click_Item_Sequence_id, activesequence != null));

                                        if (activesequence != null && activesequence.NextButtonSequence.Count == _multi_click_count) // we have reached the end of the sequence
                                        {
                                            playMulti_TriggerVideo = activesequence.ResultVideo;
                                            if (playMulti_TriggerVideo.Length == 5)
                                                MainAlternateYN = false;
                                            System.Diagnostics.Debug.WriteLine(string.Format("\t\tIncorrect button, but alternate.. Clicked through to the end of the sequence. Playing {0}", playMulti_TriggerVideo));
                                        }


                                        if (playMulti_TriggerVideo == null)
                                        {
                                            var defaultvideo = _multi_click_lastAction.multiAction.Where(xy => xy.ClickIndex == -1).FirstOrDefault();
                                            if (defaultvideo != null)
                                            {
                                                playMulti_TriggerVideo = defaultvideo.ResultVideo;
                                                if (playMulti_TriggerVideo.Length == 5)
                                                    MainAlternateYN = false;
                                                System.Diagnostics.Debug.WriteLine(string.Format("\t\tDefault (-1 state). Playing {0}", playMulti_TriggerVideo));
                                            }

                                        }
                                    }


                                }
                                else
                                {
                                    skipFrameActionVideodefault = true;
                                }
                                // We've done some housekeeping but now we have to deal with when there is a -1(default)
                                if (playMulti_TriggerVideo == null)
                                {
                                    var defaultvideo = _multi_click_lastAction.multiAction.Where(xy => xy.ClickIndex == -1).FirstOrDefault();
                                    if (defaultvideo != null)
                                    {
                                        playMulti_TriggerVideo = defaultvideo.ResultVideo;
                                        if (playMulti_TriggerVideo.Length == 5)
                                            MainAlternateYN = false;
                                        System.Diagnostics.Debug.WriteLine(string.Format("\t\tDefault (-1 state). Playing {0}", playMulti_TriggerVideo));
                                    }

                                }
                                if (playMulti_TriggerVideo != null)
                                {
                                    string PlayTimeStopVideo = null;

                                    if (playMulti_TriggerVideo == "V018A")
                                    {
                                        PlayTimeStopVideo = string.Format("BB{0}", _bombattemptcount);
                                    }

                                    if (playMulti_TriggerVideo == string.Empty)
                                    {
                                        var scene = Utilities.FindNextMainScene(_allSceneOptions, _currentScene);
                                        System.Diagnostics.Debug.WriteLine(string.Format("\t\tSuccess State. Playing {0}", scene.Name));
                                        _displayElement.MediaPlayer.Play();
                                        PlayScene(scene, 0, PlayTimeStopVideo);

                                        return;
                                    }
                                    else
                                    {
                                        var multi_scene = _allSceneOptions.Where(sc => sc.Name.ToUpperInvariant() == playMulti_TriggerVideo.ToUpperInvariant()).FirstOrDefault();
                                        if (multi_scene != null)
                                        {
                                            System.Diagnostics.Debug.WriteLine(string.Format("\t\tClick Triggered scene change. Playing {0}", multi_scene.Name));
                                            _currentScene.LastHotspotTrigger = inFrame[i];
                                            _displayElement.MediaPlayer.Play();
                                            if (MainAlternateYN)
                                                PlayScene(multi_scene);
                                            else
                                                PlayScene(multi_scene, 0, PlayTimeStopVideo);//, _currentScene);
                                            return;
                                        }


                                    }
                                }
                                _displayElement.MediaPlayer.Play();

                            }


                            //}
                            string _PlayTimeStopVideo = null;
                            if (FrameActionVideo == "V018A")
                            {
                                _PlayTimeStopVideo = string.Format("BB{0}", _bombattemptcount);
                            }

                            var alternatescene = _allSceneOptions.Where(sc => sc.Name == FrameActionVideo).FirstOrDefault();
                            if (alternatescene != null && !skipFrameActionVideodefault)
                            {
                                _currentScene.LastHotspotTrigger = inFrame[i];
                                PlayScene(alternatescene, 0, _PlayTimeStopVideo);
                                return;
                            }
                        }
                        else
                        {
                            TriggerInfoScene(inFrame[i].ActionVideo);
                        }
                    }
                }
            }

        }
        private void RollBackFrameWithinChallenge(int ms)
        {
            var currtime = _displayElement.MediaPlayer.Time;
            bool playing = _displayElement.MediaPlayer.IsPlaying;

            var startChallenge = _currentScene.EndMS + 1200;
            bool beyondstart = currtime - ms > startChallenge;


            if (playing && beyondstart)
                _displayElement.MediaPlayer.Time = currtime - ms;
            else
                _displayElement.MediaPlayer.Time = startChallenge;
        }
        private void TriggerInaction()
        {
            // Multi-action
            if (_multi_click_count > 0)
            {
                // Bad options
                string alternatevideo = null;
                if (_multi_click_lastAction != null)
                {
                    var multiactions = _multi_click_lastAction.multiAction;
                    var noaction = multiactions.Where(xy => xy.ClickIndex == 0).FirstOrDefault();
                    if (noaction != null)
                    {
                        alternatevideo = noaction.ResultVideo;
                    }
                }

                //var AlternateVideo = _allSceneOptions.Where(xy => xy.Name.ToUpperInvariant() == _multi_click_lastAction.ResultVideo.ToUpperInvariant()).FirstOrDefault();
                //if (AlternateVideo != null)
                //{
                if (alternatevideo != null)
                {
                    var AlternateVideo = _allSceneOptions.Where(xy => xy.Name.ToUpperInvariant() == alternatevideo.ToUpperInvariant()).FirstOrDefault();
                    if (AlternateVideo != null)
                    {
                        AlternateVideo.ParentScene = _currentScene;
                        PlayScene(AlternateVideo);
                        return;
                    }
                }

                if (alternatevideo == null && _currentScene.Name == "V018_1") // If you pressed at least one button, just explode, don't do inaction failure.
                {
                    //++_bombattemptcount;
                    var AlternateVideo = _allSceneOptions.Where(xy => xy.Name.ToUpperInvariant() == "V018A").FirstOrDefault();
                    if (AlternateVideo != null)
                    {
                        AlternateVideo.ParentScene = _currentScene;
                        PlayScene(AlternateVideo, 0, string.Format("BB{0}", _bombattemptcount));
                        return;
                    }
                }//

                // Success option
                if (alternatevideo == string.Empty)
                {
                    var scene = Utilities.FindNextMainScene(_allSceneOptions, _currentScene);
                    PlayScene(scene);
                    return;
                }
                // Nothing
                if (_inactionacount > DoNothingVideos.Count - 1)
                    _inactionacount = DoNothingVideos.Count - 1;

                var AlternateVideom = DoNothingVideos[_inactionacount++];

                AlternateVideom.ParentScene = _currentScene;
                PlayScene(AlternateVideom);

            }
            else
            {
                if (_currentScene.Name == "V018_1") // Bomb blast has a special ending for inaction that isn't documented anywhere in the scene or hotspot files
                {
                    //++_bombattemptcount;
                    var AlternateVideo_ = _allSceneOptions.Where(xy => xy.Name.ToUpperInvariant() == "V018B").FirstOrDefault();
                    if (AlternateVideo_ != null)
                    {
                        AlternateVideo_.ParentScene = _currentScene;
                        PlayScene(AlternateVideo_, 0, string.Format("BB{0}", _bombattemptcount));
                        return;
                    }
                }//


                if (_inactionacount > DoNothingVideos.Count - 1)
                    _inactionacount = DoNothingVideos.Count - 1;

                var AlternateVideo = DoNothingVideos[_inactionacount++];

                AlternateVideo.ParentScene = _currentScene;
                PlayScene(AlternateVideo);
            }
        }

        internal SaveDefinition GetSaveInfo()
        {

            int SceneArrayPosition = -1;
            for (int scenei = 0; scenei < _allSceneOptions.Count; scenei++)
            {
                if (_allSceneOptions[scenei].Name == _currentScene.Name)
                {
                    SceneArrayPosition = scenei;
                    break;
                }
            }
            SaveDefinition result = new SaveDefinition()
            {
                DoNothingCount = _inactionacount,
                SaveFrame = (int)Utilities.MsTo15fpsFrames(_displayElement.MediaPlayer.Time),
                SaveName = string.Empty,
                SaveRowType = "g",
                SaveScene = _currentScene.Name,
                SaveSceneInt = SceneArrayPosition
            };
            return result;
        }

        private void PlayAlternate(SceneDefinition alternate, SceneDefinition RetryScene)
        {
            PlayScene(alternate);
        }

        public void VisualizeHotspots(Grid ParentGrid)
        {
            if (_visualizationEnabled)
                return;

            foreach (var item in _currentScene.PlayingHotspots)
                item.Draw(ParentGrid, _displayElement.MediaPlayer.Time, _currentScene);

            foreach (var item in _currentScene.PausedHotspots)
                item.Draw(ParentGrid, _displayElement.MediaPlayer.Time, _currentScene);

            _visualizationEnabled = true;
        }

        public void VisualizeRemoveHotspots()
        {
            if (!_visualizationEnabled)
                return;

            _visualizationEnabled = false;

            foreach (var item in _currentScene.PlayingHotspots)
                item.ClearVisualization();

            foreach (var item in _currentScene.PausedHotspots)
                item.ClearVisualization();


        }

        private void TriggerInfoScene(string scenename)
        {
            var infoscene = _allSceneOptions.Where(xy => xy.Name.ToLowerInvariant() == scenename.ToLowerInvariant()).FirstOrDefault();
            if (infoscene != null)
            {
                InfoAction act = InfoVideoTrigger;
                if (act != null)
                {
                    act(infoscene.StartMS, infoscene.EndMS);
                }

            }
            System.Diagnostics.Debug.WriteLine(string.Format("\tTrigger Scene: {0}", scenename));
        }
        private void TimerTickAction()
        {
            if (_challengeStartMS > 0 || _challengeEndMS > 0)
            {
                // 
                if (_displayElement != null && _displayElement.MediaPlayer != null)
                {
                    if (_displayElement.MediaPlayer.IsPlaying)
                    {
                        _lastPlayheadMS = _displayElement.MediaPlayer.Time;
                    }
                    else
                    {
                        _lastPlayheadMS = 0;
                    }
                }
            }
            switch (_currentScene.SceneType)
            {
                case SceneType.Main:
                    if (_lastPlayheadMS >= _challengeEndMS)
                    {
                        System.Diagnostics.Debug.WriteLine(string.Format("\tPlaying Scene Cursor Position {0} exceeds EndPos {1}", _lastPlayheadMS, _challengeEndMS));
                        // What do?
                        if (_currentScene.retryMS <= 0)
                        {
                            // Go on to the success frames.
                            var scene = Utilities.FindNextMainScene(_allSceneOptions, _currentScene);
                            PlayScene(scene);
                        }
                        else
                        {
                            if (_currentScene.InactionBad)
                            {
                                TriggerInaction();
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("\tFake Success");
                                // Trigger inaction
                                var scene = Utilities.FindNextMainScene(_allSceneOptions, _currentScene);
                                PlayScene(scene);
                            }
                        }
                    }
                    if (_lastPlayheadMS >= _challengeStartMS && !_challengeSectionNotificationComplete)
                    {
                        _challengeSectionNotificationComplete = true;
                        _multi_click_count = 0;
                        _multi_click_lastAction = null;
                        _multi_click_Item_Sequence_id = -2;
                        _special_case_multi_action = null;

                        System.Diagnostics.Debug.WriteLine(string.Format("\tIn Challenge time {0} End {1}", _lastPlayheadMS, _challengeEndMS));
                        UserActionRequired userAction = ActionOn;
                        if (userAction != null)
                        {
                            userAction();
                        }

                    }

                    break;
                case SceneType.Bad:
                    if (_currentScene.retryMS == Utilities.Frames15fpsToMS(999999))
                    {
                        UserActionRequired userAction = QuitGame;
                        if (userAction != null)
                        {
                            userAction();
                        }
                        // This is a quit game
                    }

                    bool shouldShortenEnd = false;
                    shouldShortenEnd = _currentScene.Name == "V018A" && _bombattemptcount <= 2;
                    // special case for bomb blast

                    if ((_lastPlayheadMS >= _currentScene.EndMS && _lastScene != null) || (shouldShortenEnd && _lastPlayheadMS >= (_currentScene.EndMS - 17000) && _lastScene != null))
                    {
                        _PlayHeadTimer.Stop(); // Stop the timer so we don't get ReplayingFromTimeStop Twice.
                        long retryms = _lastScene.retryMS;
                        if (retryms <= 0)
                        // Special case.  0 is used to determine that nothing bad should happen if you do nothing. 

                        {
                            retryms = Utilities.Frames15fpsToMS(_currentScene.OriginalRetryFrames - 2) + _lastScene.OffsetTimeMS;
                        }

                        string ReplayFromTimeStop = Utilities.GetReplayingAudioFromSceneName(_lastScene.Name);
                        if (_ReplayingFromTimeStopVideo != null)
                        {
                            ReplayFromTimeStop = _ReplayingFromTimeStopVideo;
                        }
                        PlayScene(_lastScene, retryms, ReplayFromTimeStop);
                        return;
                    }
                    break;
                case SceneType.Inaction:
                    if (_lastPlayheadMS >= _currentScene.EndMS - 2000)
                    {
                        bool exceededinactiontries = _inactionacount >= DoNothingVideos.Count;
                        if (exceededinactiontries) // Restart!
                        {
                            _inactionacount = 0;
                            _PlayHeadTimer.Stop(); // Stop the timer so we don't get ReplayingFromTimeStop Twice.
                            PlayScene(_allSceneOptions[1], _allSceneOptions[1].StartMS, "P000100");
                        }
                        else
                        {
                            _PlayHeadTimer.Stop(); // Stop the timer so we don't get ReplayingFromTimeStop Twice.
                            string ReplayFromTimeStop = Utilities.GetReplayingAudioFromSceneName(_lastScene.Name);
                            if (_ReplayingFromTimeStopVideo != null)
                            {
                                ReplayFromTimeStop = _ReplayingFromTimeStopVideo;
                            }
                            PlayScene(_lastScene, _lastScene.retryMS, ReplayFromTimeStop);
                        }
                    }

                    break;
            }




            if (_visualizationEnabled)
            {
                Grid ParentGrid = _displayElement.Parent as Grid;
                foreach (var item in _currentScene.PlayingHotspots)
                    item.Draw(ParentGrid, _displayElement.MediaPlayer.Time, _currentScene);

                foreach (var item in _currentScene.PausedHotspots)
                    item.Draw(ParentGrid, _displayElement.MediaPlayer.Time, _currentScene);
            }
        }
        public LoadedVideoInfo Load_Main_Video(LibVLC _libVLCMain, int CD = 1)
        {
            _vlcInstance = _libVLCMain;
            LoadedVideoInfo result = null;
            string videopath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(),"CDAssets", string.Format("MAIN_{0}X.AVI",CD));
            var filters = _vlcInstance.AudioFilters;

            
            using (var media = new Media(_libVLCMain, videopath, FromType.FromPath))
            {
                //media.AddOption("start-time=120.0");
                //media.AddOption("stop-time=180.0");
                if (media == null)
                {
                    throw new Exception("We couldn't find the video");
                }
                result = new LoadedVideoInfo();
                _displayElement.MediaPlayer.Play(media);
                WaitWhileLoading();
                _displayElement.MediaPlayer.Time += 2000;
                //_displayElement.MediaPlayer.Pause();
               
                //_mainVideoMedia = media;   // This media shouldn't ever be disposed.

                foreach (var track in media.Tracks)
                {
                    if (track.TrackType == TrackType.Video)
                    {
                        result.OriginalMainVideoHeight = (int)media.Tracks[0].Data.Video.Height;
                        result.OriginalMainVideoWidth = (int)media.Tracks[0].Data.Video.Width;
                        
                    }
                    if (track.TrackType == TrackType.Audio)
                    {
                        var codecinfo = track.Codec;
                        if (track.Codec == 1627419501) // Original video
                        {
                            //_duckBufferer = new NAudioBufferer(_displayElement.MediaPlayer);
                        }
                    }
                }
                result.MaxVideoMS = media.Duration;
                result.Loaded = true;
                
            }

            return result;
        }
        private void SwitchVideo(SceneType type, int CD)
        {
            string filename = string.Empty;
            switch (type)
            {
                case SceneType.Main:
                    filename = string.Format("MAIN_{0}X.AVI",CD);
                    break;
                case SceneType.Bad:
                case SceneType.Inaction:
                    filename = string.Format("SS_{0}X.AVI", CD);
                    break;
            }
            string assetpath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "CDAssets", filename);
            if (!string.IsNullOrEmpty(filename) && filename != _loadedVideoFile)
            {
                _loadedVideoFile = filename;
                
                SwitchVideo(assetpath);
            }
            else
            {
                // libVLC player reacts weirdly when the media has ended.
                // (and no, we do not use the Ended video event so are not subject to deadlocks of using it)
                // If we're in an Ended state, the video must be reloaded anyway.

                if (_displayElement.MediaPlayer.State == VLCState.Ended)
                {
                    SwitchVideo(assetpath);
                }
            }

        }
        private void SwitchVideo(string path)
        {
            using (var media = new Media(_vlcInstance, path, FromType.FromPath))
            {
                _displayElement.MediaPlayer.Play(media);
                WaitWhileLoading();
                //_displayElement.MediaPlayer.Pause();

                int originalMainVideoHeight = 0;
                int originalMainVideoWidth = 0;
                long maxVideoMS = 0;

                foreach (var track in media.Tracks)
                {
                    if (track.TrackType == TrackType.Video)
                    {
                        originalMainVideoHeight = (int)media.Tracks[0].Data.Video.Height;
                        originalMainVideoWidth = (int)media.Tracks[0].Data.Video.Width;
                        
                    }
                }
                maxVideoMS = media.Duration;

            }
        }

        private void WaitWhileLoading()
        {
            int whilelooptimeout = 0;
            while (!_displayElement.MediaPlayer.IsPlaying)
            {
                Task.Delay(50).Wait();
                if (++whilelooptimeout > 10)
                {
                    throw new Exception("The Player cannot be initialized");
                }
            }
        }

    }
    public class LoadedVideoInfo
    {
        public bool Loaded { get; set; }
        public long MaxVideoMS { get; set; }
        public int OriginalMainVideoHeight { get; set; }
        public int OriginalMainVideoWidth { get; set; }
    }
}
