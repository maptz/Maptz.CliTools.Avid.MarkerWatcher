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

    public class MarkerWatcherEngineSettings
    {
        public string DirectoryPath { get; set; }
    }
}