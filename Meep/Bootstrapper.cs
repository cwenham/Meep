﻿using System;
using System.IO;
using System.Reactive.Linq;
using System.ComponentModel;
using System.Threading;

using MeepLib;
using MeepLib.Sources;
using MeepLib.MeepLang;
using MeepLib.Messages;
using MeepGit;

namespace Meep
{
    /// <summary>
    /// A hard-coded Meep pipeline that loads and maintains a soft-coded Meep pipeline
    /// </summary>
    /// <remarks>Loads and maintains one of the following:
    /// 
    /// <list type="bullet">
    ///     <item>File on disk, monitoring and reloading with FileChanges.</item>
    ///     <item>File at URI, monitoring and reloading with a timer checking HEAD.</item>
    ///     <item>Git repository, pulling and reloading with a timer or push notifications.</item>
    ///     <item>Definition from a PipeReader (typically STDIN), reloading on new definitions.</item>
    /// </list>
    /// 
    /// <para>This is an example of Meep self-hosting.</para>
    /// 
    /// </remarks>
    public class Bootstrapper : INotifyPropertyChanging, INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// The root of the loaded Meep pipeline, ready for subscription when not null
        /// </summary>
        /// <value>The pipeline root.</value>
        /// <remarks>Subscribe to PropertyChanged to know when to dispose of
        /// old subscription and re-subscribe.</remarks>
        public AMessageModule PipelineRoot
        {
            get => _root;
            set
            {
                if (PropertyChanging != null)
                    PropertyChanging(this, new PropertyChangingEventArgs(nameof(PipelineRoot)));

                _root = value;

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(PipelineRoot)));

                if (PipelineRefreshed != null)
                    PipelineRefreshed(this, new PipelineRefreshEventArgs());
            }
        }
        private AMessageModule _root;


        /// <summary>
        /// Root of the bootstrapper pipeline
        /// </summary>
        private AMessageModule _laces;

        /// <summary>
        /// Subscription to bootstrap pipeline
        /// </summary>
        private CancellationToken _subscription;

        /// <summary>
        /// Bootstrap from a file
        /// </summary>
        /// <param name="filename">Filename.</param>
        public Bootstrapper(string filename)
        {
            FileChanges changes = new FileChanges
            {
                Path = Path.GetDirectoryName(filename),
                Filter = Path.GetFileName(filename)
            };

            Load load = new Load
            {
                From = "{msg.FullPath}"
            };
            load.AddUpstream(changes);

            DeserialisePipeline deserialiser = new DeserialisePipeline();
            deserialiser.AddUpstream(load);

            _laces = deserialiser;
        }

        /// <summary>
        /// Bootstrap pipeline from a URI
        /// </summary>
        /// <param name="uri">URI.</param>
        public Bootstrapper(Uri uri, TimeSpan recheckAfter)
        {
            Get getter = new Get
            {
                URL = uri.AbsoluteUri
            };

            getter.AddUpstream(new MeepLib.Sources.Timer
            {
                Interval = recheckAfter
            });

            DeserialisePipeline deserialiser = new DeserialisePipeline();
            deserialiser.AddUpstream(getter);

            _laces = deserialiser;
        }

        /// <summary>
        /// Boostrap from a PipeReader
        /// </summary>
        /// <param name="reader">Reader.</param>
        /// <remarks>PipeReaders were introduced to the BCL as a successor to
        /// reading discrete messages from Streams. We will treat a discrete
        /// message as a pipeline definition, and re-load each time a new message
        /// is received.</remarks>
        //public Bootstrapper(PipeReader reader)
        //{
        //}

        /// <summary>
        /// Bootstrap from a Git repository
        /// </summary>
        /// <param name="repository">Address of the repository</param>
        /// <param name="file">File in the repository to load</param>
        public Bootstrapper(Uri repository, string file, TimeSpan recheckAfter)
        {
            string repoName = Path.GetFileName(repository.AbsoluteUri).Replace(".git", "");

            MeepLib.Sources.Timer timer = new MeepLib.Sources.Timer
            {
                Interval = recheckAfter
            };

            Clone cloner = new Clone
            {
                Repo = repository.AbsoluteUri
            };
            cloner.AddUpstream(timer);

            Fetch fetcher = new Fetch();
            fetcher.AddUpstream(cloner);

            Load loader = new Load
            {
                From = Path.Combine("{msg.Local}", file)
            };
            loader.AddUpstream(fetcher);

            DeserialisePipeline deserialiser = new DeserialisePipeline();
            deserialiser.AddUpstream(loader);

            _laces = deserialiser;
        }

        public void Start(CancellationToken stoppingToken)
        {
            if (_laces == null)
                throw new InvalidOperationException("Bootstrapper has not been initialised");

            _subscription = stoppingToken;

            _laces.Pipeline.Subscribe<Message>(
                msg => PipelineRoot = ((DeserialisedPipeline)msg).Tree,
                _subscription
            );
        }

        public void Dispose()
        {
            // Now obsolete after switching to a CancellationToken
        }

        public event EventHandler<PipelineRefreshEventArgs> PipelineRefreshed;

        public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class PipelineRefreshEventArgs : EventArgs
    {
    }
}
