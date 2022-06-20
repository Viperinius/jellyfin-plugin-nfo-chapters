using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace Viperinius.Plugin.NfoChapters
{
    /// <summary>
    /// Class Video extended with chapter info.
    /// </summary>
    public class VideoWithChapters : Video
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoWithChapters"/> class.
        /// </summary>
        public VideoWithChapters()
        {
            Chapters = Array.Empty<ChapterInfo>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoWithChapters"/> class from an instance of <see cref="Video"/>.
        /// </summary>
        /// <param name="video">Video to initialise from.</param>
        public VideoWithChapters(Video video) : this()
        {
            Type t = typeof(Video);
            foreach (FieldInfo field in t.GetFields())
            {
                field.SetValue(this, field.GetValue(video));
            }

            foreach (PropertyInfo prop in t.GetProperties())
            {
                if (!prop.CanWrite)
                {
                    continue;
                }

                prop.SetValue(this, prop.GetValue(video));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has external chapters.
        /// </summary>
        /// <value><c>true</c> if this instance has chapters; otherwise, <c>false</c>.</value>
        public bool HasChapters { get; set; }

        /// <summary>
        /// Gets the chapters.
        /// </summary>
        public IReadOnlyList<ChapterInfo> Chapters { get; private set; }

        /// <summary>
        /// Sets the chapters.
        /// </summary>
        /// <param name="chapters">The chapters.</param>
        public void SetChapters(IReadOnlyList<ChapterInfo> chapters)
        {
            Chapters = chapters;
        }
    }
}
