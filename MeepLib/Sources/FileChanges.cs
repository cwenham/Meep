using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Xml.Serialization;

using MeepLib.Messages;

namespace MeepLib.Sources
{
    public class FileChanges : AMessageModule
    {
        /// <summary>
        /// Directory to monitor
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        /// <summary>
        /// Filename filter, honoring "*" wildcards
        /// </summary>
        /// <value>The filter.</value>
        public string Filter { get; set; }

        /// <summary>
        /// Throttling for file change messaging
        /// </summary>
        /// <value>The throttle.</value>
        /// <remarks>This should be set, since FileSystemWatcher can fire
        /// multiple times for one change. Defaults to 250ms.</remarks>
        public TimeSpan Throttle { get; set; } = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Start without firing messages for an initial file listing
        /// </summary>
        /// <value>True to start without initial listing</value>
        /// <remarks>Most uses for this module want it to be "directory listing
        /// plus updates", so that's the default behaviour and it will start by
        /// emitting a message for every matching file before settling down for
        /// updates. If you strictly want updates only, set DryStart to true.</remarks>
        public bool DryStart { get; set; } = false;

        public override IObservable<Message> Pipeline
        {
            get
            {
                if (_Pipeline == null)
                {
                    var fsw = new FileSystemWatcher(Path, Filter);
                    fsw.Error += Fsw_Error;

                    var mergedEvents = Observable.Merge(
                        Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(x => fsw.Changed += x, x => fsw.Changed -= x).Select(x => x.EventArgs),
                        Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(x => fsw.Created += x, x => fsw.Created -= x).Select(x => x.EventArgs),
                        Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(x => fsw.Deleted += x, x => fsw.Deleted -= x).Select(x => x.EventArgs)
                    ).Throttle(Throttle, TaskPoolScheduler.Default);

                    if (!DryStart)
                    {
                        var initialList = from f in Directory.EnumerateFiles(Path, Filter)
                                          let info = new FileInfo(f)
                                          // Since this is injected into a stream of updates, we'll
                                          // order by creation time to match the order implied if
                                          // the files hadn't already existed.
                                          orderby info.CreationTime
                                          select new FileSystemEventArgs(WatcherChangeTypes.Created,
                                                                         Path,
                                                                         System.IO.Path.GetFileName(f));

                        mergedEvents = mergedEvents.StartWith(initialList);
                    }

                    _Pipeline = from fev in mergedEvents
                                let info = fev.ChangeType != WatcherChangeTypes.Deleted ? new FileInfo(fev.FullPath) : null
                                select new FileChanged
                                {
                                    ChangeType = fev.ChangeType,
                                    FullPath = fev.FullPath,
                                    Modified = File.GetLastWriteTimeUtc(fev.FullPath),
                                    Size = info != null ? info.Length : 0
                                };
                    
                    fsw.EnableRaisingEvents = true;
                }

                return _Pipeline;
            }
            protected set => base.Pipeline = value;
        }

        private void Fsw_Error(object sender, ErrorEventArgs e)
        {
            Exception ex = e.GetException();
            if (ex != null)
                logger.Error(ex, "{0} thrown by FileSystemWatcher for {1}: {2}", ex.GetType().Name, this.Name, ex.Message);
            else
                logger.Error("Unknown error thrown by FileSystemWatcher for {0}", this.Name);
        }

        private IObservable<Message> _Pipeline;
    }
}
