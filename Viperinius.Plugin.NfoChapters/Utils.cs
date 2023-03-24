using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Entities;

namespace Viperinius.Plugin.NfoChapters
{
    internal class Utils
    {
        /// <summary>
        /// Detect if a chapter list is likely to be a set of generated dummy chapters.
        /// </summary>
        /// <param name="chapters">Chapters to check.</param>
        /// <returns><c>true</c> if chapters are dummies; otherwise, <c>false</c>.</returns>
        public static bool AreDummyChapters(IReadOnlyList<ChapterInfo> chapters)
        {
            // Hardcoded value defined in MediaBrowser.Providers.MediaInfo.FFProbeVideoInfo
            var dummyChapterDuration = TimeSpan.FromMinutes(5).Ticks;

            return !chapters.Select((c, i) => c.StartPositionTicks - (i * dummyChapterDuration)).Distinct().Skip(1).Any();
        }
    }
}
