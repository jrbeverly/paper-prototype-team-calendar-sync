using System.Text.Json.Serialization;
using Amazon.DynamoDBv2;
using Amazon.Extensions.NETCore.Setup;
using Api.Feed;
using Api.Org;
using Api.Providers;
using Api.Sync;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

builder.Services.ConfigureHttpJsonOptions(opts =>
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Pick up ServiceURL from config so LocalStack is used in Development
var awsOptions = builder.Configuration.GetAWSOptions();
var serviceUrl = builder.Configuration["AWS:ServiceURL"];
if (!string.IsNullOrEmpty(serviceUrl))
    awsOptions.DefaultClientConfig.ServiceURL = serviceUrl;

builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddAWSService<IAmazonDynamoDB>();

var providerType = builder.Configuration["CalendarProvider:Type"] ?? "Mock";
if (providerType == "Mock")
    builder.Services.AddSingleton<ICalendarProvider, MockCalendarProvider>();
else
    throw new InvalidOperationException($"Unknown calendar provider type: '{providerType}'.");

var tableName = builder.Configuration["DynamoDB:TableName"] ?? "TeamCalendarSync";
builder.Services.AddSingleton<IOrgRepository>(sp =>
    new DynamoOrgRepository(sp.GetRequiredService<IAmazonDynamoDB>(), tableName));
builder.Services.AddSingleton<SyncEngine>();

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
    await TableProvisioner.EnsureExistsAsync(app.Services.GetRequiredService<IAmazonDynamoDB>(), tableName);

app.MapGet("/ping", async (IAmazonDynamoDB dynamo) =>
{
    var tables = await dynamo.ListTablesAsync();
    return Results.Ok(new { status = "ok", dynamo = "reachable", tables = tables.TableNames });
});

app.MapGet("/teams", async (IOrgRepository repo) =>
    Results.Ok(await repo.GetTeamsAsync()));

app.MapGet("/teams/{teamId}/members", async (string teamId, IOrgRepository repo) =>
    Results.Ok(await repo.GetMembersAsync(teamId)));

app.MapGet("/teams/{teamId}/observers", async (string teamId, IOrgRepository repo) =>
    Results.Ok(await repo.GetObserversAsync(teamId)));

app.MapGet("/teams/{teamId}/meetings", async (string teamId, IOrgRepository repo) =>
    Results.Ok(await repo.GetMeetingsAsync(teamId)));

app.MapPost("/admin/seed", async (IOrgRepository repo) =>
{
    await OrgSeeder.SeedAsync(repo);
    return Results.Ok(new { seeded = true });
});

app.MapPost("/admin/sync", async (SyncEngine engine) =>
    Results.Ok(await engine.RunAsync()));

app.MapGet("/calendar.ics", async (string? teamId, IOrgRepository repo) =>
{
    var allTeams = await repo.GetTeamsAsync();
    var teams = string.IsNullOrEmpty(teamId)
        ? allTeams
        : allTeams.Where(t => t.Id == teamId).ToList();

    var entries = new List<(RecurringMeeting Meeting, IReadOnlyList<string> Attendees)>();
    foreach (var team in teams)
    {
        var members = await repo.GetMembersAsync(team.Id);
        var observers = await repo.GetObserversAsync(team.Id);
        foreach (var meeting in await repo.GetMeetingsAsync(team.Id))
            entries.Add((meeting, AttendancePolicy.DeriveAttendees(members, observers, meeting.ObserverParticipation)));
    }

    return Results.Text(IcsFeedBuilder.Build(entries), "text/calendar; charset=utf-8");
});

app.Run();
