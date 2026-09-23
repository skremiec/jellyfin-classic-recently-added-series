using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;

namespace ClassicRecentlyAddedSeries;

public class DecoratedUserViewManager : IUserViewManager
{
    private readonly IUserViewManager _inner;

    public DecoratedUserViewManager(IUserViewManager inner) => _inner = inner;

    public Folder[] GetUserViews(UserViewQuery query) => _inner.GetUserViews(query);

    public UserView GetUserSubView(Guid parentId, CollectionType? type, string localizationKey, string sortName)
        => _inner.GetUserSubView(parentId, type, localizationKey, sortName);

    public List<Tuple<BaseItem, List<BaseItem>>> GetLatestItems(LatestItemsQuery request, DtoOptions options)
    {
        var latestItems = _inner.GetLatestItems(request, options);

        for (var i = 0; i < latestItems.Count; i++)
        {
            var tuple = latestItems[i];
            for (var j = 0; j < tuple.Item2.Count; j++)
            {
                if (tuple.Item2[j] is MediaBrowser.Controller.Entities.TV.Episode ep && ep.Series is not null)
                {
                    tuple.Item2[j] = ep.Series;
                }
                else if (tuple.Item2[j] is MediaBrowser.Controller.Entities.TV.Season s && s.Series is not null)
                {
                    tuple.Item2[j] = s.Series;
                }
            }

            var parent = tuple.Item1 switch
            {
                MediaBrowser.Controller.Entities.TV.Episode ep when ep.Series is not null => ep.Series,
                MediaBrowser.Controller.Entities.TV.Season s when s.Series is not null => s.Series,
                _ => tuple.Item1
            };

            if (parent != tuple.Item1)
            {
                latestItems[i] = new Tuple<BaseItem, List<BaseItem>>(parent, tuple.Item2);
            }
        }

        return latestItems;
    }
}
