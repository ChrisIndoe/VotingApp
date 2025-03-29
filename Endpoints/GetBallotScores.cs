using AnnoyedVotingApi.Models;
using Microsoft.Data.SqlClient;

namespace AnnoyedVotingApi.Endpoints;

public static class GetBallotScores
{
    public static void MapGetBallotScores(this WebApplication app)
    {
        app.MapGet("/ballots/{ballotId}/scores", async (int ballotId) =>
        {
            var connectionString = @"Server=tcp:annoyedvoting.database.windows.net,1433;Initial Catalog=AnnoyedVoting;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // First, get the total number of games in the ballot
                var countSql = "SELECT COUNT(*) FROM Games WHERE BallotId = @BallotId";
                await using var countCommand = new SqlCommand(countSql, connection);
                countCommand.Parameters.AddWithValue("@BallotId", ballotId);
                var totalGames = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

                // Get all games and their votes
                var sql = @"
                    SELECT g.ID, g.Name, g.IgdbImageId, v.Rank
                    FROM Games g
                    LEFT JOIN Votes v ON g.ID = v.GameId
                    WHERE g.BallotId = @BallotId";

                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@BallotId", ballotId);

                var gameScores = new Dictionary<int, GameScore>();

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var gameId = reader.GetInt32(0);
                    
                    if (!gameScores.ContainsKey(gameId))
                    {
                        gameScores[gameId] = new GameScore
                        {
                            GameId = gameId,
                            GameName = reader.GetString(1),
                            IgdbImageId = reader.IsDBNull(2) ? null : reader.GetString(2),
                            TotalPoints = 0
                        };
                    }

                    if (!reader.IsDBNull(3))
                    {
                        var rank = reader.GetInt32(3);
                        var points = totalGames + 1 - rank;
                        gameScores[gameId].TotalPoints += points;
                        gameScores[gameId].ReceivedRanks.Add(rank);
                    }
                }

                var sortedScores = gameScores.Values
                    .OrderByDescending(g => g.TotalPoints)
                    .ToList();

                return Results.Ok(sortedScores);
            }
            catch (SqlException e)
            {
                return Results.Problem($"SQL Error: {e.Message}", statusCode: 500);
            }
            catch (Exception e)
            {
                return Results.Problem(e.ToString(), statusCode: 500);
            }
        })
        .WithName("GetBallotScores")
        .WithOpenApi();
    }
}