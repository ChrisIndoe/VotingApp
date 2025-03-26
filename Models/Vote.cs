namespace AnnoyedVotingApi.Models
{
    public class Vote
    {
        public int Id { get; set; }
        public int BallotId { get; set; }
        public int GameId { get; set; }
        public int UserId { get; set; }
        public int Rank { get; set; }
    }
}