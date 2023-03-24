using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Viperinius.Plugin.NfoChapters.Savers
{
    /// <summary>
    /// NFO saver for movies with chapters.
    /// </summary>
    public class MovieChapterNfoSaver : IMetadataFileSaver
    {
        private readonly ILogger<MovieChapterNfoSaver> _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IProviderManager _providerManager;
        private readonly IItemRepository _itemRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="MovieChapterNfoSaver"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
        public MovieChapterNfoSaver(
            ILogger<MovieChapterNfoSaver> logger,
            IFileSystem fileSystem,
            IServerConfigurationManager configurationManager,
            IProviderManager providerManager,
            IItemRepository itemRepository)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
            _providerManager = providerManager;
            _itemRepository = itemRepository;
        }

        /// <summary>
        /// Gets this name.
        /// </summary>
        public static string SaverName => nameof(MovieChapterNfoSaver);

        /// <inheritdoc/>
        public string Name => SaverName;

        /// <summary>
        /// Gets the minimum update type.
        /// </summary>
        private ItemUpdateType MinimumUpdateType
        {
            get
            {
                if (_configurationManager.GetConfiguration<XbmcMetadataOptions>("xbmcmetadata").SaveImagePathsInNfo)
                {
                    return ItemUpdateType.ImageUpdate;
                }

                return ItemUpdateType.MetadataDownload;
            }
        }

        /// <summary>
        /// Get the target path to save to.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The save path.</returns>
        public string GetSavePath(BaseItem item)
        {
            return GetMovieSavePaths(new ItemInfo(item)).FirstOrDefault() ?? Path.ChangeExtension(item.Path, ".nfo");
        }

        /// <summary>
        /// Copy of GetMovieSavePaths in MediaBrowser.XbmcMetadata.Savers.MovieNfoSaver.
        /// </summary>
        /// <param name="item">The item info.</param>
        /// <returns>IEnumerable{string}.</returns>
        private static IEnumerable<string> GetMovieSavePaths(ItemInfo item)
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
        /// Check if item should be saved with this.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">The update reason.</param>
        /// <returns>Check result.</returns>
        public bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            // Check parent for null to avoid running this against things like video backdrops
            if (item is Video video && item is not Episode && !video.ExtraType.HasValue)
            {
                return updateType >= MinimumUpdateType;
            }

            return false;
        }

        /// <summary>
        /// Save the item to file.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>Nothing.</returns>
        public async Task SaveAsync(BaseItem item, CancellationToken cancellationToken)
        {
            if (item is not Video video)
            {
                return;
            }

            var videoWithChapters = new VideoWithChapters(video);
            var chapters = _itemRepository.GetChapters(item);

            // do not save auto generated dummy chapters
            if (!chapters.Any() || Utils.AreDummyChapters(chapters))
            {
                return;
            }

            var path = GetSavePath(item);

            if (!_fileSystem.FileExists(path))
            {
                // try to create it with base nfo saver first
                await _providerManager.SaveMetadataAsync(item, MinimumUpdateType, new[] { "Nfo" }).ConfigureAwait(false);

                if (!_fileSystem.FileExists(path))
                {
                    // nfo saver might be disabled :(
                    _logger.LogError("Failed to save item {Item}! The NFO file was not found!", item.Name);
                    return;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // On Windows, saving the file will fail if the file is hidden or readonly
            _fileSystem.SetAttributes(path, false, false);

            var fileStreamOptions = new FileStreamOptions()
            {
                Mode = FileMode.Open,
                Access = FileAccess.ReadWrite,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous
            };

            var filestream = new FileStream(path, fileStreamOptions);
            await using (filestream.ConfigureAwait(false))
            {
                var xml = LoadExistingNfo(filestream);

                if (!CompareChapters(chapters, xml, out var modifiedServerChapters))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    SetNfoChapters(xml, chapters);

                    filestream.Position = 0;
                    filestream.SetLength(0);
                    await xml.SaveAsync(filestream, SaveOptions.None, cancellationToken).ConfigureAwait(false);
                }

                if (modifiedServerChapters)
                {
                    _itemRepository.SaveChapters(item.Id, chapters);
                }
            }

            if (_configurationManager.Configuration.SaveMetadataHidden)
            {
                SetHidden(path, true);
            }
        }

        private XDocument LoadExistingNfo(FileStream stream)
        {
            var xmlDoc = XDocument.Load(stream);
            return xmlDoc;
        }

        private bool CompareChapters(List<ChapterInfo> serverChapters, XDocument xmlDoc, out bool modifiedServerChapters)
        {
            modifiedServerChapters = false;
            var chapterNodes = xmlDoc.XPathSelectElements("//chapters/chapter");
            if (!chapterNodes.Any())
            {
                return false;
            }

            var matchedCount = 0;
            foreach (var chapterNode in chapterNodes)
            {
                var name = chapterNode.Attribute("name")?.Value;
                var start = chapterNode.Attribute("start")?.Value;

                if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(start) || !double.TryParse(start, out var startSeconds))
                {
                    continue;
                }

                var nfoChapter = new ChapterInfo
                {
                    Name = name,
                    StartPositionTicks = TimeSpan.FromSeconds(startSeconds).Ticks
                };
                if (!string.IsNullOrWhiteSpace(chapterNode.Value))
                {
                    nfoChapter.ImagePath = chapterNode.Value;
                }

                // if nfo entry does not yet exist on server, keep it in the nfo
                // if nfo entry has a different image path than the server, keep the one from the nfo
                var foundChapter = serverChapters.FirstOrDefault(c => c.Name == nfoChapter.Name && c.StartPositionTicks == nfoChapter.StartPositionTicks);
                if (foundChapter != null && foundChapter.ImagePath == nfoChapter.ImagePath)
                {
                    matchedCount++;
                }
                else if (foundChapter != null)
                {
                    serverChapters[serverChapters.IndexOf(foundChapter)].ImagePath = nfoChapter.ImagePath;
                    modifiedServerChapters = true;
                }
                else
                {
                    serverChapters.Add(nfoChapter);
                    modifiedServerChapters = true;
                }
            }

            return serverChapters.Count == matchedCount;
        }

        private void SetNfoChapters(XDocument xmlDoc, List<ChapterInfo> chapters)
        {
            var chaptersNode = xmlDoc.XPathSelectElement("//chapters");
            var hasChaptersNode = false;
            if (chaptersNode != null)
            {
                chaptersNode.RemoveNodes();
                hasChaptersNode = true;
            }
            else
            {
                chaptersNode = new XElement("chapters");
            }

            foreach (var chapter in chapters)
            {
                var elem = new XElement("chapter");
                elem.SetAttributeValue("name", chapter.Name);
                elem.SetAttributeValue("start", TimeSpan.FromTicks(chapter.StartPositionTicks).TotalSeconds);
                elem.SetValue(chapter.ImagePath);

                chaptersNode.Add(elem);
            }

            if (!hasChaptersNode)
            {
                xmlDoc.Element("movie")!.Add(chaptersNode);
            }
        }

        private void SetHidden(string path, bool hidden)
        {
            try
            {
                _fileSystem.SetHidden(path, hidden);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error setting hidden attribute on {Path}", path);
            }
        }
    }
}
