
using AnnoyedVotingApi.Endpoints;
using AnnoyedVotingApi.Configuration;
  
var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();

builder.Services.Configure<IgdbConfig>(builder.Configuration.GetSection("Igdb"));

var app = builder.Build();
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapAddGameFromIgdb();
app.MapAddVotes();
app.MapGetGameByBallotId();
app.MapGetGamesAndVotesByBallotId();
app.MapGetImage();

app.Run();
