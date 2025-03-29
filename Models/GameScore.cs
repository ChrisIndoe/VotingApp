namespace AnnoyedVotingApi.Models
{
    public class GameScore
    {
        public int GameId { get; set; }
        public string GameName { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public List<int> ReceivedRanks { get; set; } = new();
        public string? IgdbImageId { get; set; }
    }
}