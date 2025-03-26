namespace AnnoyedVotingApi.Models;
public class Game
{
    public int ID { get; set; }
    public int? BallotId { get; set; }
    public string? Name { get; set; }
    public string? IgdbImageId { get; set; }
    public int? IgdbGameId { get; set; }
}