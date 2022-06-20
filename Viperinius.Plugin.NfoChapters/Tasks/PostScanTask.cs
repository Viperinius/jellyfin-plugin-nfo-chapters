using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
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
        private ILibraryManager _libraryManager;
        private IItemRepository _itemRepository;
        private IFileSystem _fileSystem;
        private IDirectoryService _directoryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostScanTask" /> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="directoryService">Instance of the <see cref="DirectoryService"/> interface.</param>
        public PostScanTask(
            ILogger<PostScanTask> logger,
            ILibraryManager libraryManager,
            IItemRepository itemRepository,
            IFileSystem fileSystem,
            IDirectoryService directoryService)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _itemRepository = itemRepository;
            _fileSystem = fileSystem;
            _directoryService = directoryService;
        }

        /// <summary>
        /// Runs the specified process.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return new MovieChapterNfoParser(_logger, _libraryManager, _itemRepository, _fileSystem, _directoryService).Run(progress, cancellationToken);
        }
    }
}
