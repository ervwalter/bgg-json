using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BoardGameGeekJsonApi
{
    public class GeekList
    {
        public int GeekListId { get; set; }
        public string Username { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<GeekListItem> Items { get; set; }
    }

    public class GeekListItem
    {
        public int GameId { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public int? ImageId { get; set; }
        public string Description { get; set; }
    }

    public class Challenge
    {
        public int GeekListId { get; set; }
        public string Username { get; set; }
        public string Title { get; set; }
        public List<ChallengeItem> Items { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public int GoalPerGame { get; set; }
        public bool Complete { get; set; }
    }

    public class ChallengeItem
    {
        public string Name { get; set; }
        public int GameId { get; set; }
        public List<int> AdditionalGameIds { get; set; }
        public string Image { get; set; }
        public string Thumbnail { get; set; }
        public List<DateTime> Plays { get; set; }
        public int PlayCount { get; set; }
        public bool Complete { get; set; }
    }

    public class GameDetails
    {
        public int GameId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public string Thumbnail { get; set; }

        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public int PlayingTime { get; set; }
        public List<string> Mechanics { get; set; }

        public bool IsExpansion { get; set; }
        public int YearPublished { get; set; }

        public decimal BGGRating { get; set; }
        public decimal AverageRating { get; set; }
        public int Rank { get; set; }

        public List<string> Designers { get; set; }
        public List<string> Publishers { get; set; }
        public List<string> Artists { get; set; }

        public List<Comment> Comments { get; set; }
        public List<PlayerPollResult> PlayerPollResults { get; set; }

        public List<BoardGameLink> Expansions { get; set; }
        public List<BoardGameLink> Expands { get; set; }

    }

    public class HotGame
    {
        public int Rank { get; set; }
        public int GameId { get; set; }
        public string Name { get; set; }
        public string Thumbnail { get; set; }
        public int YearPublished { get; set; }
    }


    public class CollectionItem
    {
        public int GameId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public string Thumbnail { get; set; }

        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public int PlayingTime { get; set; }
        public List<string> Mechanics { get; set; }

        public bool IsExpansion { get; set; }
        public int YearPublished { get; set; }

        public decimal BGGRating { get; set; }
        public decimal AverageRating { get; set; }
        public int Rank { get; set; }

        public List<string> Designers { get; set; }
        public List<string> Publishers { get; set; }
        public List<string> Artists { get; set; }

        public int NumPlays { get; set; }
        public decimal Rating { get; set; }

        public bool Owned { get; set; }
        public bool PreOrdered { get; set; }
        public bool ForTrade { get; set; }
        public bool PreviousOwned { get; set; }
        public bool Want { get; set; }
        public bool WantToPlay { get; set; }
        public bool WantToBuy { get; set; }
        public bool WishList { get; set; }

        public string UserComment { get; set; }

        public List<CollectionItem> Expansions { get; set; }

        public CollectionItem Clone()
        {
            return new CollectionItem
            {
                GameId = this.GameId,
                Name = this.Name,
                Description = this.Description,
                Image = this.Image,
                Thumbnail = this.Thumbnail,
                MinPlayers = this.MinPlayers,
                MaxPlayers = this.MaxPlayers,
                PlayingTime = this.PlayingTime,
                Mechanics = this.Mechanics,
                IsExpansion = this.IsExpansion,
                YearPublished = this.YearPublished,
                BGGRating = this.BGGRating,
                AverageRating = this.AverageRating,
                Rank = this.Rank,
                Designers = this.Designers,
                Publishers = this.Publishers,
                Artists = this.Artists,
                NumPlays = this.NumPlays,
                Rating = this.Rating,
                Owned = this.Owned,
                PreviousOwned = this.PreviousOwned,
                PreOrdered = this.PreOrdered,
                ForTrade = this.ForTrade,
                Want = this.Want,
                WantToPlay = this.WantToPlay,
                WantToBuy = this.WantToBuy,
                WishList = this.WishList,
                UserComment = this.UserComment,
                Expansions = this.Expansions
            };
        }

    }

    public class BoardGameLink
    {
        public string Name { get; set; }
        public int GameId { get; set; }
    }

    public class Plays
    {
        public int Total { get; set; }
        public IEnumerable<PlayItem> Items { get; set; }
    }

    public class PlayItem
    {
        public int GameId { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public string Thumbnail { get; set; }
        public DateTime PlayDate { get; set; }
        public int NumPlays { get; set; }
        public string Comments { get; set; }
    }

    public class Comment
    {
        public string Username { get; set; }
        public decimal Rating { get; set; }
        public string Text { get; set; }
    }

    public class PlayerPollResult
    {
        public int NumPlayers { get; set; }
        public int Best { get; set; }
        public int Recommended { get; set; }
        public int NotRecommended { get; set; }

        public bool NumPlayersIsAndHigher { get; set; }
    }

    public class SearchResult
    {
        public string Name { get; set; }
        public int GameId { get; set; }
    }

    public class User
    {
        public string Avatar { get; set; }
        public string Username { get; set; }


    }
}