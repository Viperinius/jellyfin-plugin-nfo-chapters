using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Viperinius.Plugin.NfoChapters.Tasks
{
    /// <summary>
    /// Scheduled task to extract chapter images.
    /// </summary>
    public class ExtractChapterImagesTask : IScheduledTask
    {
        private readonly ILogger<ExtractChapterImagesTask> _logger;
        private readonly IEncodingManager _encodingManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractChapterImagesTask"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="encodingManager">Instance of the <see cref="IEncodingManager"/> interface.</param>
        public ExtractChapterImagesTask(ILogger<ExtractChapterImagesTask> logger, IEncodingManager encodingManager)
        {
            _logger = logger;
            _encodingManager = encodingManager;
        }

        /// <inheritdoc/>
        public string Name => "Extract Chapter Images (NFO)";

        /// <inheritdoc/>
        public string Key => "ViperiniusNfoChaptersExtractChapterImagesTask";

        /// <inheritdoc/>
        public string Description => "Extracts chapter images and saves them to the paths specified in the NFO.";

        /// <inheritdoc/>
        public string Category => "NFO Chapters";

        /// <inheritdoc/>
        public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return Array.Empty<TaskTriggerInfo>();

            /*return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = TimeSpan.FromHours(3).Ticks,
                    MaxRuntimeTicks = TimeSpan.FromHours(4).Ticks,
                }
            };*/
        }
    }
}
