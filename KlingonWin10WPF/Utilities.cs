using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KlingonWin10WPF
{
    public static class Utilities
    {
        public static long Frames15fpsToMS(int frames)
        {
            long result = (long)(frames * 66.666666666666666666666666666667f);
            return result;
        }
        public static long MsTo15fpsFrames(long ms)
        {
            int frames = (int)((float)ms / 66.666666666666666666666666666667f);
            return frames;
        }

        // Find next forward direction scene based on SceneMS
        public static SceneDefinition FindNextMainScene(List<SceneDefinition> options, SceneDefinition existingScene)
        {
            SceneDefinition nextScene = null; // At the end of the game, we won't find one and we want to return null
                                              // so the game knows to end

            // Find our current position in the options array.
            int currentMainSceneArrayPosition = -1; // We didn't find one if -1

            for (int i=0;i<options.Count;i++)
            {
                if (options[i].SceneType != SceneType.Main)
                    continue;

                if (existingScene.Name == options[i].Name )
                {
                    
                    currentMainSceneArrayPosition = i;
                    break;
                }
            }


            if (currentMainSceneArrayPosition > -1) // Make sure to account for V000
            {
                // search for the next main video in the array
                // This will most likely be the next position in the array, but I'm using
                // a less naive search so people can edit the scene file

                int nextScenePosition = currentMainSceneArrayPosition + 1;

                for (int i = nextScenePosition; i<options.Count;i++)
                {
                    if (options[i].SceneType != SceneType.Main)
                        continue;
                    nextScene = options[i];
                    break;
                }

            }

            return nextScene;
        }
        public static string GetReplayingAudioFromSceneName(string sceneName)
        {
            if (sceneName == null)
                return null;
            //V005
            //V018_0
            //R000200
            sceneName = sceneName.ToUpperInvariant();
            var scenenameArr = sceneName.Split('_');
            string chapter = scenenameArr[0].Substring(2, scenenameArr[0].Length - 2);
            return string.Format("R00{0}00", chapter);

        }
        public static AspectRatioMaxResult GetMax(double width, double height, double AspectDecimal)
        {
            var heightbywidth = width / AspectDecimal;
            var widthbyheight = height * AspectDecimal;
            string direction = string.Empty;
            int length = 0;
            
            if (widthbyheight < width)
            {
                direction = "W";
                length = (int)widthbyheight;
            }
            if (heightbywidth < height)
            {
                direction = "H";
                length = (int)heightbywidth;
            }
            System.Diagnostics.Debug.WriteLine(String.Format("\tAspect:({0},{1})-{2},{3}-{4}", width, height, heightbywidth, widthbyheight, direction));
            return new AspectRatioMaxResult() { Direction = direction, Length = length };
            // we know if height is a certain thing and it isn't in ratio
        }

        public static bool ValidateSaveGameName(string SaveName)
        {
            bool GoodYN = true;

            if (SaveName.Contains(","))
                GoodYN = false;

            if (SaveName.Contains("\n"))
                GoodYN = false;

            if (SaveName.Contains("\r"))
                GoodYN = false;

            if (SaveName.Contains("<"))
                GoodYN = false;
            if (SaveName.Contains(">"))
                GoodYN = false;

            if (SaveName.Contains(";"))
                GoodYN = false;


            return GoodYN;
        }
    }
}
