using AnnoyedVotingApi.Models;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnnoyedVotingApi.Endpoints;
using Microsoft.AspNetCore.Builder;


namespace AnnoyedVotingApi.Endpoints;

public static class GetGameByBallotId
{
    public static void MapGetGameByBallotId(this WebApplication app)
    {
        app.MapGet("/games/{ballotId}", async (int ballotId) =>
        {
            var games = new List<Game>();
            var connectionString = @"Server=tcp:annoyedvoting.database.windows.net,1433;Initial Catalog=AnnoyedVoting;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=""Active Directory Default"";";

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sql = "SELECT ID, BallotId, Name, IgdbImageId, IgdbGameId FROM Games WHERE BallotId = @BallotId";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@BallotId", ballotId);

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var game = new Game
                    {
                        ID = reader.GetInt32(0),
                        BallotId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                        Name = reader.IsDBNull(2) ? null : reader.GetString(2),
                        IgdbImageId = reader.IsDBNull(3) ? null : reader.GetString(3),
                        IgdbGameId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4)
                    };
                    games.Add(game);
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

            return Results.Ok(games);
        })
        .WithName("GetGamesByBallotId")
        .WithOpenApi();

    }

}