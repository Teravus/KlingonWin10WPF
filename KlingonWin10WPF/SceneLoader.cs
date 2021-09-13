using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace KlingonWin10WPF
{
    public static class SceneLoader
    {
        public static List<SceneDefinition> LoadScenesFromAsset(string FileName)
        {
            List<SceneDefinition> defs = new List<SceneDefinition>();
            string[] lines = File.ReadAllLines(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "CDAssets", FileName));
            if (lines.Length > 0)
            {
                for (var i = 0; i < lines.Length; i++)
                {
                    string[] linevals = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (linevals.Length > 0)
                    {
                        if (linevals[0].StartsWith("DN"))
                        {
                            switch (linevals.Length)
                            {
                                case 5:
                                    defs.Add(new SceneDefinition(SceneType.Inaction, linevals[0], Convert.ToInt32(linevals[2]), Convert.ToInt32(linevals[3]), Convert.ToInt32(linevals[1])));
                                    break;
                                case 4: // Older version of DM Line.  No Offset.
                                    defs.Add(new SceneDefinition(SceneType.Inaction, linevals[0], Convert.ToInt32(linevals[1]), Convert.ToInt32(linevals[2]), 0));
                                    break;
                                default:
                                    // I dunno
                                    break;
                            }
                        }
                        else
                        {
                            switch (linevals.Length)
                            {
                                //V012 2 23610 26009 26123 0 25675 v012.clu
                                case 8:
                                    defs.Add(new SceneDefinition(SceneType.Main, linevals[0], Convert.ToInt32(linevals[2]), Convert.ToInt32(linevals[3]), Convert.ToInt32(linevals[1]), Convert.ToInt32(linevals[4]), Convert.ToInt32(linevals[6])));
                                    break;
                                case 7: // Older version that has CDs of above.  No offset.
                                    defs.Add(new SceneDefinition(SceneType.Main, linevals[0], Convert.ToInt32(linevals[1]), Convert.ToInt32(linevals[2]), 0, Convert.ToInt32(linevals[3]), Convert.ToInt32(linevals[5])));
                                    break;
                                case 6:
                                    defs.Add(new SceneDefinition(SceneType.Bad, linevals[0], Convert.ToInt32(linevals[2]), Convert.ToInt32(linevals[3]), Convert.ToInt32(linevals[1]), 0, Convert.ToInt32(linevals[4])));
                                    break;
                                case 5:// Older version of above that has Cds.  No offset.
                                    if (linevals[0].StartsWith("ip"))
                                    {
                                        defs.Add(new SceneDefinition(SceneType.Info, linevals[0], Convert.ToInt32(linevals[1]), Convert.ToInt32(linevals[2]), 0));
                                    }
                                    else
                                        defs.Add(new SceneDefinition(SceneType.Bad, linevals[0], Convert.ToInt32(linevals[1]), Convert.ToInt32(linevals[2]), 0, 0, Convert.ToInt32(linevals[3])));
                                    break;
                                default:
                                    // I dunno
                                    break;
                            }
                        }
                    }
                }
            }
            // Looking for hierarchical main track, alternate track options
            for (int i = 0; i < defs.Count; i++)
            {
                for (int j = 0; j < defs.Count; j++)
                {
                    if (defs[j].Name.Contains(defs[i].Name) && defs[j].SceneType == SceneType.Bad && defs[i].SceneType == SceneType.Main)
                    {
                        defs[i].ParentScene = defs[j];
                    }
                }
            }
            // Fixup CDs
            foreach (var scene in defs)
            {
                scene.CD = GetCDBySceneName(scene.Name);
            }
            return defs;
        }

        public static List<SceneDefinition> LoadSupportingScenesFromAsset(string FileName)
        {
            List<SceneDefinition> defs = new List<SceneDefinition>();
            string[] lines = File.ReadAllLines(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", FileName));
            if (lines.Length > 0)
            {
                for (var i = 0; i < lines.Length; i++)
                {
                    string[] linevals = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (linevals.Length > 0)
                    {
                        defs.Add(new SceneDefinition() { SceneType = SceneType.Info, Name = linevals[0], OffsetTimeMS = Convert.ToInt32(linevals[1]), FrameStart = Convert.ToInt32(linevals[2]), FrameEnd = Convert.ToInt32(linevals[3]) });
                    }
                }
            }

            return defs;
        }

        public static int GetCDBySceneName(string sceneName)
        {
            int result;
            // It isn't strictly required to name every Main/Alternate scene here, but I did it anyway.
            switch (sceneName)
            {
                case "V000":
                case "V001":
                case "V002":
                case "V003":
                case "V004":
                case "V005":
                case "V006":
                case "V007":
                case "V008":
                case "V009":
                case "V010":
                case "V011":
                case "V012":
                case "V013":
                case "V014":
                case "V001A":
                case "V001B":
                case "V002A":
                case "V002B":
                case "V003A":
                case "V003B":
                case "V003C":
                case "V004A":
                case "V004B":
                case "V004C":
                case "V005A":
                case "V005B":
                case "V006A":
                case "V006B":
                case "V007A":
                case "V008A":
                case "V008B":
                case "V008C":
                case "V009A":
                case "V010B":
                case "V010A":
                case "V010C":
                case "V011A":
                case "V011B":
                case "V011C":
                case "V012A":
                case "V013A":
                case "V013B":
                case "LOGO1":
                    result = 1;
                    break;
                case "V014_2":
                case "V015":
                case "V016":
                case "V017":
                case "V017_1":
                case "V018":
                case "V018_1":
                case "V019":
                case "V020":
                case "V021":
                case "V022":
                case "V014A":
                case "V014B":
                case "V014C":
                case "V015A":
                case "V015B":
                case "V016A":
                case "V016B":
                case "V016C":
                case "V016D":
                case "V017A":
                case "V017B":
                case "V017C":
                case "V018A":
                case "V018B":
                case "V019A":
                case "V019B":
                case "V019C":
                case "V019D":
                case "V020B":
                case "V020A":
                    result = 2;
                    break;
                default:
                    result = 1;
                    break;

            }
            return result;
        }

    }
}
