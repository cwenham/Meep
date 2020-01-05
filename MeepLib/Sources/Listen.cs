using System;
using System.Net;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using SmartFormat;
using NLog;

using MeepLib.Messages;
using MeepLib.MeepLang;

namespace MeepLib.Sources
{
    /// <summary>
    /// Receive inbound HTTP requests
    /// </summary>
    /// <remarks>This will not respond to inbound requests itself, instead it
    /// saves the HttpListenerContext to the message so another module can
    /// respond.</remarks>
    [Macro(Name = "Listen", DefaultProperty = "Base", Position = MacroPosition.Child)]
    public class Listen : AMessageModule
    {
        /// <summary>
        /// Base URL to bind to
        /// </summary>
        /// <value>The base URL.</value>
        /// <remarks>Defaults to http://127.0.0.1:7780/
        /// 
        /// <para>(77 = 'M' and 80 = 'P' in ASCII table)</para>
        /// </remarks>
        public DataSelector Base { get; set; } = "http://127.0.0.1:7780/";

        protected override IObservable<Message> GetMessagingSource()
        {
            MessageContext context = new MessageContext(null, this);
            string dsBase = Base.SelectString(context);

            _cancelSource = new CancellationTokenSource();

            return Observable.Create<WebRequestMessage>(async observer =>
            {
                var server = new HttpListener();
                if (!String.IsNullOrWhiteSpace(dsBase) && Uri.IsWellFormedUriString(dsBase, UriKind.Absolute))
                    server.Prefixes.Add(dsBase);
                server.Start();

                while (!_cancelSource.IsCancellationRequested)
                {
                    var context = await Task.Run(() => server.GetContext(), _cancelSource.Token);
                    observer.OnNext(new WebRequestMessage
                    {
                        URL = context.Request.RawUrl,
                        Context = context
                    });
                }
            });
        }

        private CancellationTokenSource _cancelSource;

        public override void Dispose()
        {
            base.Dispose();
            _cancelSource?.Cancel();
        }
    }
}
