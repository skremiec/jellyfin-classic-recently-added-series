using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;

namespace ClassicRecentlyAddedSeries;

public class DecoratedUserViewManager(IUserViewManager inner) : IUserViewManager
{
    private readonly IUserViewManager _inner = inner;

    public Folder[] GetUserViews(UserViewQuery query) => _inner.GetUserViews(query);

    public UserView GetUserSubView(Guid parentId, CollectionType? type, string localizationKey, string sortName)
        => _inner.GetUserSubView(parentId, type, localizationKey, sortName);

    public List<Tuple<BaseItem, List<BaseItem>>> GetLatestItems(LatestItemsQuery request, DtoOptions options)
    {
        var latestItems = _inner.GetLatestItems(request, options);
        var keepSingleEpisode = Plugin.Instance?.Configuration.SingleUnseenEpisodeDisplay == SingleUnseenEpisodeDisplayMode.Episode;

        Dictionary<Guid, bool> singleEpisodeCache = [];

        for (var i = 0; i < latestItems.Count; i++)
        {
            var tuple = latestItems[i];

            for (var j = 0; j < tuple.Item2.Count; j++)
            {
                if (tuple.Item2[j] is Episode episode && episode.Series is not null)
                {
                    if (keepSingleEpisode && HasSingleUnplayedEpisode(episode.Series, request, options, singleEpisodeCache))
                    {
                        continue;
                    }

                    tuple.Item2[j] = episode.Series;
                }
                else if (tuple.Item2[j] is Season season && season.Series is not null)
                {
                    tuple.Item2[j] = season.Series;
                }
            }

            var parent = tuple.Item1 switch
            {
                Episode episode when episode.Series is not null =>
                    (keepSingleEpisode && HasSingleUnplayedEpisode(episode.Series, request, options, singleEpisodeCache))
                        ? tuple.Item1
                        : episode.Series,
                Season season when season.Series is not null => season.Series,
                _ => tuple.Item1
            };

            if (parent != tuple.Item1)
            {
                latestItems[i] = new Tuple<BaseItem, List<BaseItem>>(parent, tuple.Item2);
            }
        }

        return latestItems;
    }

    private static bool HasSingleUnplayedEpisode(
        Series series,
        LatestItemsQuery request,
        DtoOptions options,
        Dictionary<Guid, bool> cache)
    {
        if (cache.TryGetValue(series.Id, out var result))
        {
            return result;
        }

        if (request.User is null)
        {
            return false;
        }

        var unplayedCount = 0;
        foreach (var episode in series.GetEpisodes(request.User, options, false))
        {
            if (episode.IsUnplayed(request.User, null))
            {
                unplayedCount++;
                if (unplayedCount > 1)
                {
                    result = false;
                    cache[series.Id] = result;
                    return false;
                }
            }
        }

        result = unplayedCount == 1;
        cache[series.Id] = result;
        return result;
    }
}
