using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            Console.WriteLine("Starting Marker Watch Engine.");
            foreach (var fi in Directory.GetFiles(this.Settings.DirectoryPath))
            {
                await this.UpdateMarkersAsync(fi);
            }

            this.MonitorDirectory(this.Settings.DirectoryPath);
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(2000);
            }
        }



        private void MonitorDirectory(string path)
        {
            FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();
            fileSystemWatcher.Path = path;
            fileSystemWatcher.Created += async (s, e) => { await OnFileCreated(e.FullPath); };
            fileSystemWatcher.Renamed += (s, e) => { };
            fileSystemWatcher.Deleted += (s, e) => { };
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        private async Task OnFileCreated(string fullPath)
        {
            await Task.Delay(200);
            await this.UpdateMarkersAsync(fullPath);
        }

        private async Task UpdateMarkersAsync(string fullPath)
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