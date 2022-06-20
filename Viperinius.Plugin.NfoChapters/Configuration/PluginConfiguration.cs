using MediaBrowser.Model.Plugins;

namespace Viperinius.Plugin.NfoChapters.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        ExtractChapterImagesToPaths = false;
        ExtractChapterImagesTask = false;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to extract chapter images based on NFO chapters.<br/>
    /// If true:<br/>
    /// Paths in NFO can be non existent and will be created.
    /// If false:<br/>
    /// Paths in NFO need to exist to be used.
    /// </summary>
    public bool ExtractChapterImagesToPaths { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to extract chapter images using its own scheduled task instead of running after a library scan.
    /// </summary>
    public bool ExtractChapterImagesTask { get; set; }
}
