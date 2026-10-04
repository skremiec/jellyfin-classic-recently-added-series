using MediaBrowser.Model.Plugins;

namespace ClassicRecentlyAddedSeries;

public enum SingleUnseenEpisodeDisplayMode
{
    Episode,
    Series
}

public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets how a single unseen episode is displayed in the recently added list.
    /// </summary>
    public SingleUnseenEpisodeDisplayMode SingleUnseenEpisodeDisplay { get; set; } = SingleUnseenEpisodeDisplayMode.Episode;
}
