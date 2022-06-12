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
            // set logger

            // TODO: remove this again
            _logger.LogDebug("Hello from NfoChapters!");

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
