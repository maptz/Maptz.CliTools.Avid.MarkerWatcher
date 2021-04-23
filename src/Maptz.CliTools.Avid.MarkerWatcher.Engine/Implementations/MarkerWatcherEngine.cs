using Maptz.Editing.Avid.DS;
using Maptz.Editing.Avid.Markers;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
namespace Maptz.CliTools.Avid.MarkerWatcher.Engine
{

    public class MarkerWatcherEngine : IMarkerWatcherEngine
    {
        public MarkerWatcherEngine(IOptions<MarkerWatcherEngineSettings> settings)
        {
            this.Settings = settings.Value;
        }

        public MarkerWatcherEngineSettings Settings
        {
            get;
        }

        public async Task Start(CancellationToken cancellationToken)
        {

            if (!Directory.Exists(Settings.DirectoryPath))
            {
                Directory.CreateDirectory(Settings.DirectoryPath);
            }

            Console.WriteLine("Starting Marker Watch Engine.");
            foreach (var fi in Directory.GetFiles(this.Settings.DirectoryPath))
            {
                EnqueueFile(fi);
            }

            this.MonitorDirectory(this.Settings.DirectoryPath);
            while (!cancellationToken.IsCancellationRequested)
            {

                while (FilesQueue.Count > 0)
                {
                    var file = FilesQueue.Dequeue();
                    await ProcessFile(file);
                }

                await Task.Delay(2000);
            }
        }



        private void MonitorDirectory(string path)
        {
            FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();
            fileSystemWatcher.Path = path;
            fileSystemWatcher.Created += (s, e) =>
             {
                 OnFileCreated(e.FullPath);
             };
            fileSystemWatcher.Renamed += (s, e) => { };
            fileSystemWatcher.Deleted += (s, e) => { };
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        private Queue<string> FilesQueue { get; set; } = new Queue<string>();

        private void OnFileCreated(string fullPath)
        {
            EnqueueFile(fullPath);
        }

        private void EnqueueFile(string fullPath)
        {
            if (!FilesQueue.Contains(fullPath))
                FilesQueue.Enqueue(fullPath);
        }

        private bool HasProcessed(string fullPath)
        {
            var tempDsCreated =  File.Exists(Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath) + ".temp"));
            var dsCreated = File.Exists(Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath) + ".ds.txt"));
            var outCreated = File.Exists(Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath) + ".out.txt"));

            return dsCreated;

        }

        private async Task ProcessFile(string fullPath)
        {
            if (fullPath.EndsWith(".temp")) { return; }
            if (fullPath.EndsWith(".ds.txt")) { return; }
            if (fullPath.EndsWith(".out.txt")) { return; }

            if (HasProcessed(fullPath)) return;


            Console.WriteLine($"Processing {fullPath}");
            //await this.UpdateMarkerTracksAsync(fullPath);
            await SaveMarkersAsDsAsync(fullPath);
        }

        private async Task SaveMarkersAsDsAsync(string fullPath)
        {
            Console.WriteLine($"Saving markers as ds for {fullPath}");
            var tempFilePath = Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath) + ".temp");
            var dsFilePath = Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath) + ".ds.txt");

            //var doneDirPath = Path.Combine(Path.GetDirectoryName(fullPath), "done");
            //if (!Directory.Exists(doneDirPath)) Directory.CreateDirectory(doneDirPath);

            if (File.Exists(tempFilePath)) return;

            var mr = new MarkersReader();
            var markers = await mr.ReadFromTextFileAsync(fullPath);


            var sb = new StringBuilder();
            foreach (var marker in markers)
            {
                sb.AppendLine(marker.Timecode);
                sb.AppendLine(marker.Content);
                sb.AppendLine();
            }

            File.WriteAllText(tempFilePath, sb.ToString());

            //var dsComponents = new List<AvidDSComponent>();
            //foreach (var marker in markers)
            //{
            //    dsComponents.Add(new AvidDSComponent
            //    {
            //        Content = marker.Content,
            //        In = marker.Timecode,
            //    });
            //}

            //var dsDoc = new AvidDSDocument()
            //{
            //    Components = dsComponents
            //};
            //var dw = new AvidDSDocumentWriter();
            //dw.WriteToFile(dsDoc, outputFilePath);
            await Task.Delay(200);

            var p = Process.Start(@"C:\Users\steph\OneDrive\Data\SCRIPTS\mparse.bat", $"convert -i \"{tempFilePath}\"");
            p.WaitForExit();

            //File.Delete(tempFilePath);
        }


        private async Task UpdateMarkerTracksAsync(string fullPath)
        {

            if (!string.Equals(Path.GetExtension(fullPath), ".txt", StringComparison.OrdinalIgnoreCase)) return;
            if (fullPath.EndsWith(".out.txt", StringComparison.OrdinalIgnoreCase)) return;
            var outputFilePath = Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath) + ".out.txt");
            if (File.Exists(outputFilePath)) return;


            string content;
            using (var sr = File.OpenText(fullPath)) { content = sr.ReadToEnd(); }

            var matches = ((IEnumerable<Match>)Regex.Matches(content, "\t[AV][0-9]+\t")).Reverse().ToArray();

            foreach (Match match in matches)
            {
                var prefix = content.Substring(0, match.Index);
                var suffix = content.Substring(match.Index + match.Length);
                content = prefix + "\tV1\t" + suffix;
            }

            Console.WriteLine($"Updating markers for file {fullPath}");
            using (var sw = File.CreateText(outputFilePath)) { sw.Write(content); }
            await Task.CompletedTask;
        }

    }
}