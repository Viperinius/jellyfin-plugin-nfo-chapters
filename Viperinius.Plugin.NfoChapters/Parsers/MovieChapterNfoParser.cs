using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Viperinius.Plugin.NfoChapters.Parsers
{
    /// <summary>
    /// NFO parser for movies with chapters.
    /// </summary>
    public class MovieChapterNfoParser // : MovieNfoParser
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILibraryManager _libraryManager;
        private readonly IItemRepository _itemRepository;
        private readonly IFileSystem _fileSystem;
        private readonly IDirectoryService _directoryService;
        private readonly IEncodingManager _encodingManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MovieChapterNfoParser"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/> interface.</param>
        /// <param name="encodingManager">Instance of the <see cref="IEncodingManager"/> interface.</param>
        public MovieChapterNfoParser(
            ILogger logger,
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
        public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Running the movie NFO chapter parser");

            var movies = _itemRepository.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie }
            }).Cast<Movie>().ToList();

            var numComplete = 0;
            double percent = 0;
            var count = movies.Count;

            foreach (var movie in movies)
            {
                try
                {
                    var tmpItem = new MetadataResult<VideoWithChapters>
                    {
                        Item = new VideoWithChapters(movie)
                    };
                    var itemInfo = new ItemInfo(movie);

                    var fsMetadata = GetMovieSavePaths(itemInfo).Select(_directoryService.GetFile).FirstOrDefault(i => i != null);
                    if (fsMetadata == null)
                    {
                        continue;
                    }

                    Fetch(tmpItem, fsMetadata.FullName, cancellationToken);

                    if (!tmpItem.Item.HasChapters)
                    {
                        continue;
                    }

                    var chapters = tmpItem.Item.Chapters;
                    var existingChapters = _itemRepository.GetChapters(movie);
                    if (existingChapters != null && existingChapters.Count > 0)
                    {
                        // Check if the NFO chapters differ from the existing ones
                        bool chaptersDiffer = false;

                        if (existingChapters.Count != chapters.Count)
                        {
                            chaptersDiffer = true;
                        }
                        else
                        {
                            foreach (var chapter in chapters)
                            {
                                if (!existingChapters.Where(c => (c.StartPositionTicks == chapter.StartPositionTicks) &&
                                                                 (c.Name == chapter.Name) &&
                                                                 (c.ImagePath == chapter.ImagePath)).Any())
                                {
                                    chaptersDiffer = true;
                                    break;
                                }
                            }
                        }

                        if (!chaptersDiffer)
                        {
                            continue;
                        }
                    }

                    _itemRepository.SaveChapters(movie.Id, chapters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error analysing NFO chapters for {Movie}", movie.Name);
                }

                // Extract images only if not supposed to be done via scheduled task
                if ((Plugin.Instance?.Configuration.ExtractChapterImagesToPaths ?? false) && !(Plugin.Instance?.Configuration.ExtractChapterImagesTask ?? false))
                {
                    await new Tasks.ExtractChapterImagesTask(_loggerFactory.CreateLogger<Tasks.ExtractChapterImagesTask>(), _encodingManager, _libraryManager, _directoryService, _itemRepository, _fileSystem)
                        .RunExtraction(movie, cancellationToken).ConfigureAwait(false);
                }

                numComplete++;
                percent = numComplete;
                percent /= count;
                percent *= 100;
                progress.Report(percent);
            }

            progress.Report(100);
        }

        /// <summary>
        /// Copy of GetMovieSavePaths in MediaBrowser.XbmcMetadata.Savers.MovieNfoSaver.
        /// </summary>
        /// <param name="item">The item info.</param>
        /// <returns>IEnumerable{string}.</returns>
        protected static IEnumerable<string> GetMovieSavePaths(ItemInfo item)
        {
            if (item.VideoType == VideoType.Dvd && !item.IsPlaceHolder)
            {
                var path = item.ContainingFolderPath;

                yield return Path.Combine(path, "VIDEO_TS", "VIDEO_TS.nfo");
            }

            if (!item.IsPlaceHolder && (item.VideoType == VideoType.Dvd || item.VideoType == VideoType.BluRay))
            {
                var path = item.ContainingFolderPath;

                yield return Path.Combine(path, Path.GetFileName(path) + ".nfo");
            }
            else
            {
                yield return Path.ChangeExtension(item.Path, ".nfo");

                if (!item.IsInMixedFolder)
                {
                    yield return Path.Combine(item.ContainingFolderPath, "movie.nfo");
                }
            }
        }

        /// <summary>
        /// Fetches metadata for an item from one xml file.
        /// Based on Fetch() of BaseNfoParser.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentException"><c>item</c> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><c>metadataFile</c> is <c>null</c> or empty.</exception>
        protected void Fetch(MetadataResult<VideoWithChapters> item, string metadataFile, CancellationToken cancellationToken)
        {
            if (item.Item == null)
            {
                throw new ArgumentException("Item can't be null.", nameof(item));
            }

            if (string.IsNullOrEmpty(metadataFile))
            {
                throw new ArgumentException("The metadata filepath was empty.", nameof(metadataFile));
            }

            var xml = File.ReadAllText(metadataFile);

            // Find last closing Tag
            // Need to do this in two steps to account for random > characters after the closing xml
            var index = xml.LastIndexOf(@"</", StringComparison.Ordinal);

            // If closing tag exists, move to end of Tag
            if (index != -1)
            {
                index = xml.IndexOf('>', index);
            }

            if (index == -1)
            {
                return;
            }

            xml = xml.Substring(0, index + 1);

            // These are not going to be valid xml so no sense in causing the provider to fail and spamming the log with exceptions
            try
            {
                using (var stringReader = new StringReader(xml))
                using (var reader = XmlReader.Create(stringReader, GetXmlReaderSettings()))
                {
                    reader.MoveToContent();
                    reader.Read();

                    // Loop through each element
                    while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            FetchDataFromXmlNode(reader, item);
                        }
                        else
                        {
                            reader.Read();
                        }
                    }
                }
            }
            catch (XmlException)
            {
            }
        }

        /// <summary>
        /// Reads from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="itemResult">The result to be populated.</param>
        protected void FetchDataFromXmlNode(XmlReader reader, MetadataResult<VideoWithChapters> itemResult)
        {
            switch (reader.Name)
            {
                case "chapters":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                var videoWithChapters = itemResult.Item;

                                var chapters = GetChaptersFromXmlNode(subtree);
                                if (chapters.Count > 0 && videoWithChapters != null)
                                {
                                    videoWithChapters.SetChapters(chapters);
                                    videoWithChapters.HasChapters = true;
                                }
                            }
                        }

                        break;
                    }

                default:
                    reader.Skip();
                    break;
            }
        }

        /// <summary>
        /// Get the XML reader settings.
        /// </summary>
        /// <returns>XmlReaderSettings.</returns>
        protected static XmlReaderSettings GetXmlReaderSettings()
            => new XmlReaderSettings()
            {
                ValidationType = ValidationType.None,
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true
            };

        /// <summary>
        /// Gets the chapters from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>List{ChapterInfo}.</returns>
        private List<ChapterInfo> GetChaptersFromXmlNode(XmlReader reader)
        {
            List<ChapterInfo> chapters = new List<ChapterInfo>();

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "chapter":
                            {
                                ChapterInfo chapter = new ChapterInfo();

                                var name = reader.GetAttribute("name");
                                if (!string.IsNullOrWhiteSpace(name))
                                {
                                    chapter.Name = name.Trim();
                                }

                                var startSecs = reader.GetAttribute("start");
                                double parsedStartSecs;
                                if (!string.IsNullOrWhiteSpace(startSecs) && double.TryParse(startSecs, out parsedStartSecs))
                                {
                                    chapter.StartPositionTicks = TimeSpan.FromSeconds(parsedStartSecs).Ticks;
                                }

                                var imagePath = reader.ReadElementContentAsString();
                                if (!string.IsNullOrWhiteSpace(imagePath))
                                {
                                    chapter.ImagePath = imagePath.Trim();
                                    chapter.ImageDateModified = _fileSystem.GetLastWriteTimeUtc(chapter.ImagePath);
                                }

                                if (!string.IsNullOrEmpty(chapter.Name))
                                {
                                    chapters.Add(chapter);
                                }

                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            return chapters;
        }
    }
}
