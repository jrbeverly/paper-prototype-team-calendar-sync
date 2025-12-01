using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Api.Org;

// Scan is acceptable at prototype scale; replace with a GSI-backed query when team count grows.
public sealed class DynamoOrgRepository(IAmazonDynamoDB dynamo, string tableName) : IOrgRepository
{
    public async Task<IReadOnlyList<OrgTeam>> GetTeamsAsync(CancellationToken ct = default)
    {
        var response = await dynamo.ScanAsync(new ScanRequest
        {
            TableName = tableName,
            FilterExpression = "#t = :t",
            ExpressionAttributeNames = new Dictionary<string, string> { ["#t"] = "Type" },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":t"] = new AttributeValue { S = "Team" },
            },
        }, ct);

        return response.Items
            .Select(i => new OrgTeam(i["Id"].S, i["Name"].S, i["Description"].S))
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<TeamMember>> GetMembersAsync(string teamId, CancellationToken ct = default)
    {
        var response = await dynamo.QueryAsync(new QueryRequest
        {
            TableName = tableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :prefix)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = $"TEAM#{teamId}" },
                [":prefix"] = new AttributeValue { S = "MEMBER#" },
            },
        }, ct);

        return response.Items
            .Select(i => new TeamMember(teamId, i["Email"].S, i["Name"].S, i["Role"].S))
            .OrderBy(m => m.Email, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<TeamObserver>> GetObserversAsync(string teamId, CancellationToken ct = default)
    {
        var response = await dynamo.QueryAsync(new QueryRequest
        {
            TableName = tableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :prefix)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = $"TEAM#{teamId}" },
                [":prefix"] = new AttributeValue { S = "OBSERVER#" },
            },
        }, ct);

        return response.Items
            .Select(i => new TeamObserver(teamId, i["Email"].S, i["Name"].S))
            .OrderBy(o => o.Email, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<RecurringMeeting>> GetMeetingsAsync(string teamId, CancellationToken ct = default)
    {
        var response = await dynamo.QueryAsync(new QueryRequest
        {
            TableName = tableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :prefix)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = $"TEAM#{teamId}" },
                [":prefix"] = new AttributeValue { S = "MEETING#" },
            },
        }, ct);

        return response.Items
            .Select(i => new RecurringMeeting(
                i["Id"].S,
                teamId,
                i["Title"].S,
                i["Recurrence"].S,
                Enum.Parse<ObserverParticipation>(i["ObserverParticipation"].S)))
            .OrderBy(m => m.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Task SaveTeamAsync(OrgTeam team, CancellationToken ct = default) =>
        dynamo.PutItemAsync(new PutItemRequest
        {
            TableName = tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"TEAM#{team.Id}" },
                ["SK"] = new AttributeValue { S = $"TEAM#{team.Id}" },
                ["Type"] = new AttributeValue { S = "Team" },
                ["Id"] = new AttributeValue { S = team.Id },
                ["Name"] = new AttributeValue { S = team.Name },
                ["Description"] = new AttributeValue { S = team.Description },
            },
        }, ct);

    public Task SaveMemberAsync(TeamMember member, CancellationToken ct = default) =>
        dynamo.PutItemAsync(new PutItemRequest
        {
            TableName = tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"TEAM#{member.TeamId}" },
                ["SK"] = new AttributeValue { S = $"MEMBER#{member.Email.ToLowerInvariant()}" },
                ["Type"] = new AttributeValue { S = "Member" },
                ["Email"] = new AttributeValue { S = member.Email },
                ["Name"] = new AttributeValue { S = member.Name },
                ["Role"] = new AttributeValue { S = member.Role },
            },
        }, ct);

    public Task SaveObserverAsync(TeamObserver observer, CancellationToken ct = default) =>
        dynamo.PutItemAsync(new PutItemRequest
        {
            TableName = tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"TEAM#{observer.TeamId}" },
                ["SK"] = new AttributeValue { S = $"OBSERVER#{observer.Email.ToLowerInvariant()}" },
                ["Type"] = new AttributeValue { S = "Observer" },
                ["Email"] = new AttributeValue { S = observer.Email },
                ["Name"] = new AttributeValue { S = observer.Name },
            },
        }, ct);

    public Task SaveMeetingAsync(RecurringMeeting meeting, CancellationToken ct = default) =>
        dynamo.PutItemAsync(new PutItemRequest
        {
            TableName = tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"TEAM#{meeting.TeamId}" },
                ["SK"] = new AttributeValue { S = $"MEETING#{meeting.Id}" },
                ["Type"] = new AttributeValue { S = "Meeting" },
                ["Id"] = new AttributeValue { S = meeting.Id },
                ["Title"] = new AttributeValue { S = meeting.Title },
                ["Recurrence"] = new AttributeValue { S = meeting.Recurrence },
                ["ObserverParticipation"] = new AttributeValue { S = meeting.ObserverParticipation.ToString() },
            },
        }, ct);
}
