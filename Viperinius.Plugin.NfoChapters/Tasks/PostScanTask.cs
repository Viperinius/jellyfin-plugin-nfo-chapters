using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Library;
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
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILibraryManager _libraryManager;
        private readonly IItemRepository _itemRepository;
        private readonly IFileSystem _fileSystem;
        private readonly IDirectoryService _directoryService;
        private readonly IChapterManager _chapterManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostScanTask" /> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/> interface.</param>
        /// <param name="chapterManager">Instance of the <see cref="IChapterManager"/> interface.</param>
        public PostScanTask(
            ILogger<PostScanTask> logger,
            ILoggerFactory loggerFactory,
            ILibraryManager libraryManager,
            IItemRepository itemRepository,
            IFileSystem fileSystem,
            IDirectoryService directoryService,
            IChapterManager chapterManager)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _libraryManager = libraryManager;
            _itemRepository = itemRepository;
            _fileSystem = fileSystem;
            _directoryService = directoryService;
            _chapterManager = chapterManager;
        }

        /// <summary>
        /// Runs the specified process.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return new MovieChapterNfoParser(_logger, _loggerFactory, _libraryManager, _itemRepository, _fileSystem, _directoryService, _chapterManager).Run(progress, cancellationToken);
        }
    }
}
