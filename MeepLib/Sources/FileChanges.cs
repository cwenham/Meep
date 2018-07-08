using System;
using System.IO;
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

        public override IObservable<Message> Pipeline { 
            get
            {
                if (_Pipeline == null)
                {
                    var fsw = new FileSystemWatcher(Path, Filter);
                    var mergedEvents = Observable.Merge(
                        Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(x => fsw.Changed += x, x => fsw.Changed -= x),
                        Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(x => fsw.Created += x, x => fsw.Created -= x),
                        Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(x => fsw.Deleted += x, x => fsw.Deleted -= x)
                    );

                    _Pipeline = from fev in mergedEvents.Throttle(Throttle, TaskPoolScheduler.Default)
                                let info = new FileInfo(fev.EventArgs.FullPath)
                                select new FileChanged
                                {
                                    ChangeType = fev.EventArgs.ChangeType,
                                    FullPath = fev.EventArgs.FullPath,
                                    Modified = File.GetLastWriteTimeUtc(fev.EventArgs.FullPath),
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
