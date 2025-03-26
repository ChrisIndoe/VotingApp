using AnnoyedVotingApi.Models;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnnoyedVotingApi.Endpoints
{
    public static class GetGamesAndVotesByBallotId
    {
        public static void MapGetGamesAndVotesByBallotId(this WebApplication app)
        {
            app.MapGet("/ballots/{ballotId}/games-votes", async (int ballotId) =>
            {
                var gamesAndVotes = new List<object>();
                var connectionString = @"Server=tcp:annoyedvoting.database.windows.net,1433;Initial Catalog=AnnoyedVoting;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=""Active Directory Default"";";

                try
                {
                    await using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT g.ID, g.BallotId, g.Name, g.IgdbImageId, g.IgdbGameId, v.UserId, v.Rank
                        FROM Games g
                        LEFT JOIN Votes v ON g.ID = v.GameId
                        WHERE g.BallotId = @BallotId";

                    await using var command = new SqlCommand(sql, connection);
                    command.Parameters.AddWithValue("@BallotId", ballotId);

                    await using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var gameAndVote = new
                        {
                            GameId = reader.GetInt32(0),
                            BallotId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                            Name = reader.IsDBNull(2) ? null : reader.GetString(2),
                            IgdbImageId = reader.IsDBNull(3) ? null : reader.GetString(3),
                            IgdbGameId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                            UserId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                            Rank = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6)
                        };
                        gamesAndVotes.Add(gameAndVote);
                    }
                }
                catch (SqlException e)
                {
                    return Results.Problem($"SQL Error: {e.Message}", statusCode: 500);
                }
                catch (Exception e)
                {
                    return Results.Problem(e.ToString(), statusCode: 500);
                }

                return Results.Ok(gamesAndVotes);
            })
            .WithName("GetGamesAndVotesByBallotId")
            .WithOpenApi();
        }
    }
}