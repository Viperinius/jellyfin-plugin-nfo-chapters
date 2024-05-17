using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Viperinius.Plugin.NfoChapters
{
    /// <summary>
    /// Entrypoint of the server.
    /// </summary>
    public class Entrypoint : IHostedService
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

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Entrypoint running");

            // TODO: instead of starting the chapter scanning in PostScanTask, subscribe to ILibraryManager.ItemAdded (and maybe ItemUpdated?).
            // could look similar to https://github.com/ConfusedPolarBear/intro-skipper/blob/master/ConfusedPolarBear.Plugin.IntroSkipper/Entrypoint.cs

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
