using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KlingonWin10WPF
{
    public static class SaveLoader
    {
        public static async Task<List<SaveDefinition>> LoadSavesFromAsset(string FileName)
        {
            List<SaveDefinition> defs = new List<SaveDefinition>();

        //    StorageFolder appFolder = ApplicationData.Current.LocalFolder; //StorageFolder.GetFolderFromPathAsync(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets"));// Windows.Storage.ApplicationData.Current.LocalFolder;
        //    StorageFile file = await appFolder.CreateFileAsync(FileName,
        //Windows.Storage.CreationCollisionOption.OpenIfExists);


        //    IList<string> linesList = await PathIO.ReadLinesAsync(file.Path);
            string[] lines = null;// linesList.ToArray();
            try
            {
                lines = File.ReadAllLines(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", FileName));
            }
            catch
            {
                lines = new string[0];
            }
            if (lines.Length > 0)
            {
                for (var i = 0; i < lines.Length; i++)
                {
                    string[] linevals = lines[i].Split(new char[] { ',' }, StringSplitOptions.None);
                    if (linevals.Length > 0)
                    {
                        defs.Add(new SaveDefinition()
                        {
                            SaveRowType = linevals[0],
                            SaveName = linevals[1],
                            SaveScene = linevals[2],
                            SaveSceneInt = Convert.ToInt32(linevals[3]),
                            SaveFrame = Convert.ToInt32(linevals[4]),
                            DoNothingCount = Convert.ToInt32(linevals[5])

                        });
                    }
                }
            }
            return defs;
        }

        internal static async Task SaveSavesToAsset(List<SaveDefinition> saves, string FileName)
        {
        //    StorageFolder appFolder = ApplicationData.Current.LocalFolder; //StorageFolder.GetFolderFromPathAsync(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets"));// Windows.Storage.ApplicationData.Current.LocalFolder;
        //    StorageFile file = await appFolder.CreateFileAsync(FileName,
        //Windows.Storage.CreationCollisionOption.OpenIfExists);//await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/RIVER.TXT"));




            StringBuilder saveSB = new StringBuilder();
            foreach (var save in saves)
            {
                saveSB.AppendLine(
                   string.Format("{0},{1},{2},{3},{4},{5}",
                    save.SaveRowType,
                    save.SaveName,
                    save.SaveScene,
                    save.SaveSceneInt,
                    save.SaveFrame,
                    save.DoNothingCount));
            }
            ////await Windows.Storage.FileIO.WriteTextAsync(file, saveSB.ToString());

            //using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            //{
            //    var fileWriter = new StreamWriter(fileStream.AsStreamForWrite());
            //    fileWriter.Write(saveSB.ToString());
            //    await fileWriter.FlushAsync();
            //    fileWriter.Close();
            //}
            System.Diagnostics.Debug.WriteLine(string.Format("Saved to: {0}", Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", FileName)));

            File.WriteAllText(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", FileName), saveSB.ToString());
        }
    }
}
