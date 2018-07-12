using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Xml.Serialization;

using MeepLib.Messages;

namespace MeepLib.Sources
{
    [XmlRoot(ElementName = "FileChanges", Namespace = "http://meep.example.com/Meep/V1")]
    public class FileChanges : AMessageModule
    {
        /// <summary>
        /// Directory to monitor
        /// </summary>
        /// <value>The path.</value>
        [XmlAttribute]
        public string Path { get; set; }

        /// <summary>
        /// Filename filter, honoring "*" wildcards
        /// </summary>
        /// <value>The filter.</value>
        [XmlAttribute]
        public string Filter { get; set; }

        /// <summary>
        /// Throttling for file change messaging
        /// </summary>
        /// <value>The throttle.</value>
        /// <remarks>This should be set, since FileSystemWatcher can fire
        /// multiple times for one change. Defaults to 250ms.</remarks>
        [XmlAttribute]
        public TimeSpan Throttle { get; set; } = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Start without firing messages for an initial file listing
        /// </summary>
        /// <value>True to start without initial listing</value>
        /// <remarks>Most uses for this module want it to be "directory listing
        /// with updates", so that's the default behaviour and it will start by
        /// emitting a message for every matching file before settling down for
        /// updates. If you strictly want updates only, set DryStart to true.</remarks>
        [XmlAttribute]
        public bool DryStart { get; set; } = false;

        [XmlIgnore]
        public override IObservable<Message> Pipeline
        {
            get
            {
                if (_Pipeline == null)
                {
                    var fsw = new FileSystemWatcher(Path, Filter);

                    var mergedEvents = Observable.Merge(
                        Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(x => fsw.Changed += x, x => fsw.Changed -= x).Select(x => x.EventArgs),
                        Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(x => fsw.Created += x, x => fsw.Created -= x).Select(x => x.EventArgs),
                        Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(x => fsw.Deleted += x, x => fsw.Deleted -= x).Select(x => x.EventArgs)
                    );

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
                        mergedEvents = Observable.Merge(
                            mergedEvents,
                            initialList.ToObservable()
                        );
                    }

                    _Pipeline = from fev in mergedEvents.Throttle(Throttle, TaskPoolScheduler.Default)
                                let info = new FileInfo(fev.FullPath)
                                select new FileChanged
                                {
                                    ChangeType = fev.ChangeType,
                                    FullPath = fev.FullPath,
                                    Modified = File.GetLastWriteTimeUtc(fev.FullPath),
                                    Size = info.Length
                                };

                    fsw.EnableRaisingEvents = true;
                }

                return _Pipeline;
            }
            protected set => base.Pipeline = value;
        }
        private IObservable<Message> _Pipeline;
    }
}
