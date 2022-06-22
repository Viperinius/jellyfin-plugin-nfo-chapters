using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Viperinius.Plugin.NfoChapters.Parsers;

namespace Viperinius.Plugin.NfoChapters.Tasks
{
    /// <summary>
    /// Task triggered after library scan.
    /// </summary>
    public class PostScanTask : ILibraryPostScanTask
    {
        private readonly ILogger<PostScanTask> _logger;
        private ILoggerFactory _loggerFactory;
        private ILibraryManager _libraryManager;
        private IItemRepository _itemRepository;
        private IFileSystem _fileSystem;
        private IDirectoryService _directoryService;
        private IEncodingManager _encodingManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostScanTask" /> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/> interface.</param>
        /// <param name="encodingManager">Instance of the <see cref="IEncodingManager"/> interface.</param>
        public PostScanTask(
            ILogger<PostScanTask> logger,
            ILoggerFactory loggerFactory,
            ILibraryManager libraryManager,
            IItemRepository itemRepository,
            IFileSystem fileSystem,
            IDirectoryService directoryService,
            IEncodingManager encodingManager)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _libraryManager = libraryManager;
            _itemRepository = itemRepository;
            _fileSystem = fileSystem;
            _directoryService = directoryService;
            _encodingManager = encodingManager;
        }

        /// <summary>
        /// Runs the specified process.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return new MovieChapterNfoParser(_logger, _loggerFactory, _libraryManager, _itemRepository, _fileSystem, _directoryService, _encodingManager).Run(progress, cancellationToken);
        }
    }
}
