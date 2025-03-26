using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AnnoyedVotingApi.Configuration;
using AnnoyedVotingApi.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AnnoyedVotingApi.Endpoints
{
    public static class AddGameFromIgdb
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string? _accessToken;
        private static DateTime _tokenExpiration = DateTime.MinValue;

        public static void MapAddGameFromIgdb(this WebApplication app)
        {
            app.MapPost("/ballots/{ballotId}/games", async (int ballotId, int igdbGameId, IOptions<IgdbConfig> igdbConfig) =>
            {
                try
                {
                    // Get or refresh access token
                    if (_accessToken == null || DateTime.UtcNow >= _tokenExpiration)
                    {
                        var tokenResponse = await GetTwitchAccessToken(igdbConfig.Value);
                        _accessToken = tokenResponse.access_token;
                        _tokenExpiration = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in - 60);
                    }

                    // Get game details from IGDB
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                    _httpClient.DefaultRequestHeaders.Add("Client-ID", igdbConfig.Value.ClientId);

                    var gameQuery = $"fields name,cover.*; where id = {igdbGameId};";
                    var response = await _httpClient.PostAsync(
                        "https://api.igdb.com/v4/games",
                        new StringContent(gameQuery, Encoding.UTF8, "text/plain")
                    );

                    response.EnsureSuccessStatusCode();
                    var games = await JsonSerializer.DeserializeAsync<List<IgdbGame>>(
                        await response.Content.ReadAsStreamAsync()
                    );

                    if (games == null || !games.Any())
                    {
                        return Results.NotFound($"Game with ID {igdbGameId} not found in IGDB");
                    }

                    var game = games.First();

                    // Insert into database
                    var connectionString = @"Server=tcp:annoyedvoting.database.windows.net,1433;Initial Catalog=AnnoyedVoting;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=""Active Directory Default"";";
                    await using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();

                    var sql = @"
                        INSERT INTO Games (BallotId, Name, IgdbImageId, IgdbGameId) 
                        VALUES (@BallotId, @Name, @IgdbImageId, @IgdbGameId);
                        SELECT SCOPE_IDENTITY();";

                    await using var command = new SqlCommand(sql, connection);
                    command.Parameters.AddWithValue("@BallotId", ballotId);
                    command.Parameters.AddWithValue("@Name", game.name);
                    command.Parameters.AddWithValue("@IgdbImageId", game.cover?.image_id ?? "");
                    command.Parameters.AddWithValue("@IgdbGameId", igdbGameId);

                    var newId = Convert.ToInt32(await command.ExecuteScalarAsync());

                    return Results.Ok(new Game
                    {
                        ID = newId,
                        BallotId = ballotId,
                        Name = game.name,
                        IgdbImageId = game.cover?.image_id,
                        IgdbGameId = igdbGameId
                    });
                }
                catch (HttpRequestException e)
                {
                    return Results.Problem($"IGDB API Error: {e.Message}", statusCode: 500);
                }
                catch (SqlException e)
                {
                    return Results.Problem($"Database Error: {e.Message}", statusCode: 500);
                }
                catch (Exception e)
                {
                    return Results.Problem($"Error: {e.Message}", statusCode: 500);
                }
            })
            .WithName("AddGameFromIgdb")
            .WithOpenApi();
        }

        private static async Task<TwitchToken> GetTwitchAccessToken(IgdbConfig config)
        {
            var response = await _httpClient.PostAsync(
                $"https://id.twitch.tv/oauth2/token?client_id={config.ClientId}&client_secret={config.ClientSecret}&grant_type=client_credentials",
                null
            );

            response.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<TwitchToken>(
                await response.Content.ReadAsStreamAsync()
            ) ?? throw new Exception("Failed to deserialize Twitch token response");
        }

        private class TwitchToken
        {
            public string access_token { get; set; } = string.Empty;
            public int expires_in { get; set; }
        }

        private class IgdbGame
        {
            public string name { get; set; } = string.Empty;
            public IgdbCover? cover { get; set; }
        }

        private class IgdbCover
        {
            public string image_id { get; set; } = string.Empty;
        }
    }
}