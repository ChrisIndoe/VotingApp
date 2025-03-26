using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace AnnoyedVotingApi.Endpoints
{
    public static class GetImage
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static void MapGetImage(this WebApplication app)
        {
            app.MapGet("/images", async (string IgdbImageId) =>
            {
                try
                {
                    var url = $"https://images.igdb.com/igdb/image/upload/t_cover_big/{IgdbImageId}.jpg";
                    
                    var imageBytes = await _httpClient.GetByteArrayAsync(url);
                    
                    // Try to determine content type from URL
                    string contentType = "image/jpeg"; // default
                    if (url.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        contentType = "image/png";
                    else if (url.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                        contentType = "image/gif";
                    else if (url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                        contentType = "image/webp";

                    return Results.File(imageBytes, contentType);
                }
                catch (HttpRequestException e)
                {
                    return Results.Problem($"Failed to retrieve image: {e.Message}", statusCode: 500);
                }
            })
            .WithName("GetImage")
            .WithOpenApi();
        }
    }
}