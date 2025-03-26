using AnnoyedVotingApi.Models;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnnoyedVotingApi.Endpoints
{
    public static class AddVotes
    {
        public static void MapAddVotes(this WebApplication app)
        {
            app.MapPost("/votes", async (List<Vote> votes) =>
            {
                var connectionString = @"Server=tcp:annoyedvoting.database.windows.net,1433;Initial Catalog=AnnoyedVoting;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=""Active Directory Default"";";

                try
                {
                    await using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();
 
                    var duplicateRanks = votes.GroupBy(v => v.Rank)
                            .Where(g => g.Count() > 1)
                            .Select(g => g.Key)
                            .ToList();

                    if (duplicateRanks.Any())
                    {
                        return Results.BadRequest($"Duplicate ranks found: {string.Join(", ", duplicateRanks)}");
                    }

                    foreach (var vote in votes)
                    {
                        var sql = "INSERT INTO Votes (BallotId, GameId, UserId, Rank) VALUES (@BallotId, @GameId, @UserId, @Rank)";
                        await using var command = new SqlCommand(sql, connection);
                        command.Parameters.AddWithValue("@BallotId", vote.BallotId);
                        command.Parameters.AddWithValue("@GameId", vote.GameId);
                        command.Parameters.AddWithValue("@UserId", vote.UserId);
                        command.Parameters.AddWithValue("@Rank", vote.Rank);

                        await command.ExecuteNonQueryAsync();
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

                return Results.Ok();
            })
            .WithName("AddVotes")
            .WithOpenApi();
        }
    }
}