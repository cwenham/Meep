using System;
using System.Net;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

using SmartFormat;
using NLog;

using MeepLib.Messages;

namespace MeepLib.Sources
{
    /// <summary>
    /// Receive inbound HTTP requests
    /// </summary>
    /// <remarks>This will not respond to inbound requests itself, instead it
    /// saves the HttpListenerContext to the message so another module can
    /// respond.</remarks>
    [XmlRoot(ElementName = "Listen", Namespace = "http://meep.example.com/Meep/V1")]
    public class Listen : AMessageModule, IDisposable
    {
        /// <summary>
        /// Base URL to bind to
        /// </summary>
        /// <value>The base URL.</value>
        /// <remarks>Defaults to http://127.0.0.1:7780/
        /// 
        /// <para>(77 = 'M' and 80 = 'P' in ASCII table)</para>
        /// </remarks>
        [XmlAttribute]
        public string Base { get; set; } = "http://127.0.0.1:7780/";

        [XmlIgnore]
        public override IObservable<Message> Pipeline
        {
            get
            {
                if (_pipeline == null)
                {
                    _cancelSource = new CancellationTokenSource();

                    _pipeline = Observable.Create<WebMessage>(async observer =>
                    {
                        var server = new HttpListener();
                        if (!String.IsNullOrWhiteSpace(Base) && Uri.IsWellFormedUriString(Base, UriKind.Absolute))
                            server.Prefixes.Add(Base);
                        server.Start();

                        while (!_cancelSource.IsCancellationRequested)
                        {
                            var context = await Task.Run(() => server.GetContext(), _cancelSource.Token);
                            observer.OnNext(new WebMessage
                            {
                                URL = context.Request.RawUrl,
                                Context = context
                            });
                        }
                    });
                }

                return _pipeline;
            }
            protected set => base.Pipeline = value;
        }
        private IObservable<Message> _pipeline;

        private CancellationTokenSource _cancelSource;

        public void Dispose()
        {
            _cancelSource?.Cancel();
        }
    }
}
