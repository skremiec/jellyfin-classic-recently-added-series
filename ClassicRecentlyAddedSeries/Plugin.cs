using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace ClassicRecentlyAddedSeries;

public class Plugin : BasePlugin<BasePluginConfiguration>
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public override string Name => "Classic Recently Added Series";

    public override Guid Id => Guid.Parse("f9f0402b-a010-4ecb-99f6-6c84a56a64e2");

    public static Plugin? Instance { get; private set; }
}
