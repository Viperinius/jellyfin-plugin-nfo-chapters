using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.Logging;

namespace Viperinius.Plugin.NfoChapters
{
    /// <summary>
    /// Entrypoint of the server.
    /// </summary>
    public class Entrypoint : IServerEntryPoint
    {
        private readonly ILogger<Entrypoint> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Entrypoint"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        public Entrypoint(ILogger<Entrypoint> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Run setup.
        /// </summary>
        /// <returns>Task.</returns>
        public Task RunAsync()
        {
            _logger.LogDebug("Entrypoint running");

            // TODO: instead of starting the chapter scanning in PostScanTask, subscribe to ILibraryManager.ItemAdded (and maybe ItemUpdated?).
            // could look similar to https://github.com/ConfusedPolarBear/intro-skipper/blob/master/ConfusedPolarBear.Plugin.IntroSkipper/Entrypoint.cs

            return Task.CompletedTask;
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Internal Dispose.
        /// </summary>
        /// <param name="dispose">Should dispose.</param>
        protected virtual void Dispose(bool dispose)
        {
        }
    }
}
