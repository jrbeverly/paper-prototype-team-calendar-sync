using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Api.Org;

public static class TableProvisioner
{
    public static async Task EnsureExistsAsync(IAmazonDynamoDB dynamo, string tableName, CancellationToken ct = default)
    {
        try
        {
            await dynamo.DescribeTableAsync(tableName, ct);
            return;
        }
        catch (ResourceNotFoundException) { }

        await dynamo.CreateTableAsync(new CreateTableRequest
        {
            TableName = tableName,
            BillingMode = BillingMode.PAY_PER_REQUEST,
            KeySchema =
            [
                new KeySchemaElement { AttributeName = "PK", KeyType = KeyType.HASH },
                new KeySchemaElement { AttributeName = "SK", KeyType = KeyType.RANGE },
            ],
            AttributeDefinitions =
            [
                new AttributeDefinition { AttributeName = "PK", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "SK", AttributeType = ScalarAttributeType.S },
            ],
        }, ct);

        TableDescription desc;
        do
        {
            await Task.Delay(200, ct);
            desc = (await dynamo.DescribeTableAsync(tableName, ct)).Table;
        }
        while (desc.TableStatus != TableStatus.ACTIVE);
    }
}
