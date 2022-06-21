using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
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
        private readonly ILibraryManager _libraryManager;
        private readonly IDirectoryService _directoryService;
        private readonly IItemRepository _itemRepository;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractChapterImagesTask"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="encodingManager">Instance of the <see cref="IEncodingManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/> interface.</param>
        /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        public ExtractChapterImagesTask(
            ILogger<ExtractChapterImagesTask> logger,
            IEncodingManager encodingManager,
            ILibraryManager libraryManager,
            IDirectoryService directoryService,
            IItemRepository itemRepository,
            IFileSystem fileSystem)
        {
            _logger = logger;
            _encodingManager = encodingManager;
            _libraryManager = libraryManager;
            _directoryService = directoryService;
            _itemRepository = itemRepository;
            _fileSystem = fileSystem;
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
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (!Plugin.Instance?.Configuration.ExtractChapterImagesToPaths ?? false)
            {
                _logger.LogWarning("Did not execute the chapter image extraction because the setting ExtractChapterImagesToPaths is disabled");
                return;
            }

            var videos = _libraryManager.GetItemList(new InternalItemsQuery
            {
                MediaTypes = new[] { MediaType.Video },
                IsFolder = false,
                Recursive = true,
                SourceTypes = new SourceType[] { SourceType.Library },
                IsVirtualItem = false
            }).OfType<Video>().ToList();

            var numComplete = 0;
            double percent = 0;
            var count = videos.Count;

            foreach (var video in videos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await RunExtractionInternal(video, cancellationToken).ConfigureAwait(false);

                numComplete++;
                percent = numComplete;
                percent /= count;
                percent *= 100;
                progress.Report(percent);
            }
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

        private async Task RunExtractionInternal(Video video, CancellationToken cancellationToken)
        {
            var chapters = _itemRepository.GetChapters(video);
            var success = await RefreshChapterImages(video, chapters, true, true, cancellationToken).ConfigureAwait(false);

            if (!success)
            {
                _logger.LogError("Failed to extract chapter image for {VideoName}", video.Name);
            }
        }

        /// <summary>
        /// Refreshes the chapter images and copy them to the needed location.
        /// </summary>
        /// <param name="video">Video to use.</param>
        /// <param name="chapters">Set of chapters to refresh.</param>
        /// <param name="extractImages">Option to extract images.</param>
        /// <param name="saveChapters">Option to save chapters.</param>
        /// <param name="cancellationToken">CancellationToken to use for operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<bool> RefreshChapterImages(Video video, IReadOnlyList<ChapterInfo> chapters, bool extractImages, bool saveChapters, CancellationToken cancellationToken)
        {
            // Only extract images for items that have "intended" chapters, i.e. not auto generated
            if (AreDummyChapters(chapters))
            {
                return true;
            }

            List<string> currentChapterImagePaths = new List<string>(chapters.Select(c => c.ImagePath));

            if (Plugin.Instance?.Configuration.ForceReplaceChapterImages ?? false)
            {
                var currentInternalChapterImagePaths = GetSavedChapterImages(video);
                foreach (var image in currentInternalChapterImagePaths)
                {
                    try
                    {
                        _fileSystem.DeleteFile(image);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "Error deleting chapter image {Path}", image);
                    }
                }

                if (currentInternalChapterImagePaths.Count > 0)
                {
                    // Clear the file cache of this directory, otherwise bad things happen after just deleting the file
                    _directoryService.GetFilePaths(GetChapterImagesPath(video), true);
                }
            }

            var result = await _encodingManager.RefreshChapterImages(video, _directoryService, chapters, extractImages, saveChapters, cancellationToken).ConfigureAwait(false);
            if (result)
            {
                bool changesMade = false;
                for (int i = 0; i < chapters.Count; i++)
                {
                    var chapter = chapters[i];
                    if (chapter == null)
                    {
                        continue;
                    }

                    var oldChapterPath = currentChapterImagePaths[i];
                    if (chapter.ImagePath != oldChapterPath)
                    {
                        if (!string.IsNullOrEmpty(chapter.ImagePath) && !string.IsNullOrEmpty(oldChapterPath))
                        {
                            File.Copy(chapter.ImagePath, oldChapterPath, true);
                        }

                        chapter.ImagePath = oldChapterPath;
                        changesMade = true;
                    }
                }

                if (changesMade)
                {
                    _itemRepository.SaveChapters(video.Id, chapters);
                }
            }

            return result;
        }

        /// <summary>
        /// Detect if a chapter list is likely to be a set of generated dummy chapters.
        /// </summary>
        /// <param name="chapters">Chapters to check.</param>
        /// <returns><c>true</c> if chapters are dummies; otherwise, <c>false</c>.</returns>
        protected bool AreDummyChapters(IReadOnlyList<ChapterInfo> chapters)
        {
            // Hardcoded value defined in MediaBrowser.Providers.MediaInfo.FFProbeVideoInfo
            var dummyChapterDuration = TimeSpan.FromMinutes(5).Ticks;

            return !chapters.Select((c, i) => c.StartPositionTicks - (i * dummyChapterDuration)).Distinct().Skip(1).Any();
        }

        private static string GetChapterImagesPath(Video video)
        {
            return Path.Combine(video.GetInternalMetadataPath(), "chapters");
        }

        private IReadOnlyList<string> GetSavedChapterImages(Video video)
        {
            var path = GetChapterImagesPath(video);
            if (!Directory.Exists(path))
            {
                return Array.Empty<string>();
            }

            try
            {
                return _directoryService.GetFilePaths(path);
            }
            catch (IOException)
            {
                return Array.Empty<string>();
            }
        }
    }
}
