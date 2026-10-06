using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Querying;
using Moq;

namespace ClassicRecentlyAddedSeries.Tests;

public enum InputKind { Series, Season, Episode }

public enum EpisodeWatchState
{
    MultipleUnwatched,
    SingleUnwatchedWithWatched,
    SingleUnwatchedOnly
}

public class DecoratedUserViewManagerTests
{
    private readonly User _user;
    private readonly Mock<ILibraryManager> _libMock;
    private readonly Mock<IUserDataManager> _userDataMock;

    public DecoratedUserViewManagerTests()
    {
        var configMock = new Mock<IServerConfigurationManager>();
        configMock.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configMock.Object;

        _libMock = new Mock<ILibraryManager>();
        _libMock.Setup(l => l.Sort(It.IsAny<IEnumerable<BaseItem>>(), It.IsAny<User>(), It.IsAny<ItemSortBy[]>(), It.IsAny<SortOrder>()))
            .Returns<IEnumerable<BaseItem>, User, ItemSortBy[], SortOrder>((items, _, _, _) => items);
        _libMock.Setup(l => l.GetLibraryOptions(It.IsAny<BaseItem>())).Returns(new LibraryOptions());
        _libMock.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>())).Returns([]);
        BaseItem.LibraryManager = _libMock.Object;

        _userDataMock = new Mock<IUserDataManager>();
        BaseItem.UserDataManager = _userDataMock.Object;

        _user = new User("test", "pass", "salt") { Id = Guid.NewGuid() };
    }

    private (Series Series, Season Season, List<Episode> Episodes) CreateSeries(EpisodeWatchState state)
    {
        var (unwatchedCount, watchedCount) = state switch
        {
            EpisodeWatchState.MultipleUnwatched => (3, 0),
            EpisodeWatchState.SingleUnwatchedWithWatched => (1, 3),
            EpisodeWatchState.SingleUnwatchedOnly => (1, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };

        var series = new Series { Id = Guid.NewGuid(), Name = "Series" };
        var season = new Season { Id = Guid.NewGuid(), Name = "Season 1", IndexNumber = 1, SeriesId = series.Id };
        var episodes = new List<Episode>();

        _libMock.Setup(l => l.GetItemById(series.Id)).Returns(series);
        _libMock.Setup(l => l.GetItemById(season.Id)).Returns(season);

        var total = unwatchedCount + watchedCount;
        for (var i = 0; i < total; i++)
        {
            var ep = new Episode
            {
                Id = Guid.NewGuid(),
                Name = $"Ep {i + 1}",
                IndexNumber = i + 1,
                ParentIndexNumber = 1,
                SeriesId = series.Id,
                SeasonId = season.Id
            };
            episodes.Add(ep);
            _libMock.Setup(l => l.GetItemById(ep.Id)).Returns(ep);
            _userDataMock.Setup(u => u.GetUserData(_user, ep)).Returns(new UserItemData { Key = ep.Id.ToString(), Played = i >= unwatchedCount });
        }

        _libMock.Setup(l => l.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns([season, ..episodes]);

        return (series, season, episodes);
    }

    [Theory]
    // Input: Series
    [InlineData(InputKind.Series,  EpisodeWatchState.MultipleUnwatched,          SingleUnseenEpisodeDisplayMode.Episode, InputKind.Series)]
    [InlineData(InputKind.Series,  EpisodeWatchState.MultipleUnwatched,          SingleUnseenEpisodeDisplayMode.Series,  InputKind.Series)]
    [InlineData(InputKind.Series,  EpisodeWatchState.SingleUnwatchedWithWatched, SingleUnseenEpisodeDisplayMode.Episode, InputKind.Episode)]
    [InlineData(InputKind.Series,  EpisodeWatchState.SingleUnwatchedWithWatched, SingleUnseenEpisodeDisplayMode.Series,  InputKind.Series)]
    [InlineData(InputKind.Series,  EpisodeWatchState.SingleUnwatchedOnly,        SingleUnseenEpisodeDisplayMode.Episode, InputKind.Episode)]
    [InlineData(InputKind.Series,  EpisodeWatchState.SingleUnwatchedOnly,        SingleUnseenEpisodeDisplayMode.Series,  InputKind.Series)]
    // Input: Season
    [InlineData(InputKind.Season,  EpisodeWatchState.MultipleUnwatched,          SingleUnseenEpisodeDisplayMode.Episode, InputKind.Series)]
    [InlineData(InputKind.Season,  EpisodeWatchState.MultipleUnwatched,          SingleUnseenEpisodeDisplayMode.Series,  InputKind.Series)]
    [InlineData(InputKind.Season,  EpisodeWatchState.SingleUnwatchedWithWatched, SingleUnseenEpisodeDisplayMode.Episode, InputKind.Episode)]
    [InlineData(InputKind.Season,  EpisodeWatchState.SingleUnwatchedWithWatched, SingleUnseenEpisodeDisplayMode.Series,  InputKind.Series)]
    [InlineData(InputKind.Season,  EpisodeWatchState.SingleUnwatchedOnly,        SingleUnseenEpisodeDisplayMode.Episode, InputKind.Episode)]
    [InlineData(InputKind.Season,  EpisodeWatchState.SingleUnwatchedOnly,        SingleUnseenEpisodeDisplayMode.Series,  InputKind.Series)]
    // Input: Episode
    [InlineData(InputKind.Episode, EpisodeWatchState.MultipleUnwatched,          SingleUnseenEpisodeDisplayMode.Episode, InputKind.Series)]
    [InlineData(InputKind.Episode, EpisodeWatchState.MultipleUnwatched,          SingleUnseenEpisodeDisplayMode.Series,  InputKind.Series)]
    [InlineData(InputKind.Episode, EpisodeWatchState.SingleUnwatchedWithWatched, SingleUnseenEpisodeDisplayMode.Episode, InputKind.Episode)]
    [InlineData(InputKind.Episode, EpisodeWatchState.SingleUnwatchedWithWatched, SingleUnseenEpisodeDisplayMode.Series,  InputKind.Series)]
    [InlineData(InputKind.Episode, EpisodeWatchState.SingleUnwatchedOnly,        SingleUnseenEpisodeDisplayMode.Episode, InputKind.Episode)]
    [InlineData(InputKind.Episode, EpisodeWatchState.SingleUnwatchedOnly,        SingleUnseenEpisodeDisplayMode.Series,  InputKind.Series)]
    public void GetLatestItems_ReturnsExpectedItem(
        InputKind inputKind,
        EpisodeWatchState watchState,
        SingleUnseenEpisodeDisplayMode mode,
        InputKind expectedKind)
    {
        var (series, season, episodes) = CreateSeries(watchState);
        BaseItem inputItem = inputKind switch
        {
            InputKind.Series => series,
            InputKind.Season => season,
            InputKind.Episode => episodes[0],
            _ => throw new ArgumentOutOfRangeException(nameof(inputKind))
        };

        var innerMock = new Mock<IUserViewManager>();
        innerMock.Setup(m => m.GetLatestItems(It.IsAny<LatestItemsQuery>(), It.IsAny<DtoOptions>()))
            .Returns([new(null!, [inputItem])]);

        var manager = new DecoratedUserViewManager(innerMock.Object, new PluginConfiguration { SingleUnseenEpisodeDisplay = mode });
        var query = new LatestItemsQuery { User = _user };
        var result = manager.GetLatestItems(query, new DtoOptions())[0].Item2[0];

        Assert.Same(expectedKind == InputKind.Episode ? episodes[0] : series, result);
    }
}
