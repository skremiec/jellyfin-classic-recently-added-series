using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;

namespace ClassicRecentlyAddedSeries;

public class DecoratedUserViewManager(IUserViewManager inner, PluginConfiguration? config = null) : IUserViewManager
{
    private readonly IUserViewManager _inner = inner;
    private readonly PluginConfiguration? _config = config;

    public Folder[] GetUserViews(UserViewQuery query) => _inner.GetUserViews(query);

    public UserView GetUserSubView(Guid parentId, CollectionType? type, string localizationKey, string sortName)
        => _inner.GetUserSubView(parentId, type, localizationKey, sortName);

    public List<Tuple<BaseItem, List<BaseItem>>> GetLatestItems(LatestItemsQuery request, DtoOptions options)
    {
        var latestItems = _inner.GetLatestItems(request, options);
        var configuration = _config ?? Plugin.Instance?.Configuration;
        var keepSingleEpisode = configuration?.SingleUnseenEpisodeDisplay == SingleUnseenEpisodeDisplayMode.Episode;

        Dictionary<Guid, Episode?> singleEpisodeCache = [];

        for (var i = 0; i < latestItems.Count; i++)
        {
            var tuple = latestItems[i];

            for (var j = 0; j < tuple.Item2.Count; j++)
            {
                var targetSeries = tuple.Item2[j] switch
                {
                    Episode episode => episode.Series,
                    Season season => season.Series,
                    Series series => series,
                    _ => null
                };

                if (targetSeries is null)
                {
                    continue;
                }

                var singleEpisode = keepSingleEpisode
                    ? GetSingleUnplayedEpisode(targetSeries, request, options, singleEpisodeCache)
                    : null;

                tuple.Item2[j] = singleEpisode ?? (BaseItem)targetSeries;
            }

            var parent = tuple.Item1 switch
            {
                Episode episode when episode.Series is not null =>
                    (keepSingleEpisode && GetSingleUnplayedEpisode(episode.Series, request, options, singleEpisodeCache) is not null)
                        ? tuple.Item1
                        : episode.Series,
                Season season when season.Series is not null => season.Series,
                Series series when keepSingleEpisode && GetSingleUnplayedEpisode(series, request, options, singleEpisodeCache) is not null => null,
                _ => tuple.Item1
            };

            if (parent != tuple.Item1)
            {
                latestItems[i] = new Tuple<BaseItem, List<BaseItem>>(parent!, tuple.Item2);
            }
        }

        return latestItems;
    }

    private static Episode? GetSingleUnplayedEpisode(
        Series series,
        LatestItemsQuery request,
        DtoOptions options,
        Dictionary<Guid, Episode?> cache)
    {
        if (cache.TryGetValue(series.Id, out var result))
        {
            return result;
        }

        if (request.User is null)
        {
            return null;
        }

        Episode? single = null;
        foreach (var item in series.GetEpisodes(request.User, options, false))
        {
            if (item is Episode episode && episode.IsUnplayed(request.User, null))
            {
                if (single is not null)
                {
                    cache[series.Id] = null;
                    return null;
                }

                single = episode;
            }
        }

        cache[series.Id] = single;
        return single;
    }
}
