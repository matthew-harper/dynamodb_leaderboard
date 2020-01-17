using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime.CredentialManagement;

namespace dynamo_leaderboard_example
{
    class Program
    {
        private static Random random = new Random();
        private static readonly string[] Games = { "Super Mario Bros", "Donkey Kong", "Legend of Zelda", "Tetris" };

        static void Main(string[] args)
        {
            Console.WriteLine("Launched!");

            var sharedFile = new SharedCredentialsFile();
            // replace profile name with your own shared credentials
            sharedFile.TryGetProfile("adminuser", out var profile);
            AWSCredentialsFactory.TryGetAWSCredentials(profile, sharedFile, out var credentials);

            AmazonDynamoDBClient client = new AmazonDynamoDBClient(credentials);
            DynamoDBContext context = new DynamoDBContext(client);
            Table table = Table.LoadTable(client, "HighScores");

            bool populateTable = false;
            if (populateTable)
                PopulateDbWithTestData(table);

            // replace user after you populate your DB with random data
            string userToQuery = "CFGV";
            // query with partition key
            GetAllHighScoresForUser(userToQuery, client);
            // query with partition and sort key
            GetHighScoreForUserAndGame(userToQuery, "Tetris", client);
            // query against local secondary index
            GetMostRecentHighScoreForUser(userToQuery, client);
            // query against global secondary index
            GetHighScoreForGame("Legend of Zelda", client);
        }

        static void GetAllHighScoresForUser(string user, AmazonDynamoDBClient client)
        {
            Console.WriteLine("GetAllHighScoresForUser");
            // query only using partition key (Username)
            var request = new QueryRequest
            {
                TableName = "HighScores",
                KeyConditionExpression = "Username = :v_Id",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                                 { ":v_Id", new AttributeValue { S = user } }
                             },
                ScanIndexForward = true
            };
            var response = client.QueryAsync(request).Result;
            PrintResults(response.Items);
        }

        static void GetHighScoreForUserAndGame(string user, string game, AmazonDynamoDBClient client)
        {
            Console.WriteLine("GetHighScoreForUserAndGame");
            // query using partition and sort keys (Username & Game)
            var request = new QueryRequest
            {
                TableName = "HighScores",
                KeyConditionExpression = "Username = :v_Id and Game = :v_Game",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                                 { ":v_Id", new AttributeValue { S = user }},
                                 { ":v_Game", new AttributeValue { S = game}}
                             },
                ScanIndexForward = true
            };
            var response = client.QueryAsync(request).Result;
            PrintResults(response.Items);
        }

        static void GetMostRecentHighScoreForUser(string user, AmazonDynamoDBClient client)
        {
            Console.WriteLine("GetMostRecentHighScoreForUser");
            // query using Local Secondary Index 
            // partition key is Username (default) and sort key is TimeStamp
            // note we specify indexName in the query
            // ScanIndexForward is false so results are descending
            // and Limit is 1 so we only get the single high score back
            var request = new QueryRequest
            {
                TableName = "HighScores",
                IndexName = "TimestampIndex",
                KeyConditionExpression = "Username = :v_Id",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                                 { ":v_Id", new AttributeValue { S = user }},
                             },
                ScanIndexForward = false,
                Limit = 1
            };
            var response = client.QueryAsync(request).Result;
            PrintResults(response.Items, true);
        }

        static void GetHighScoreForGame(string game, AmazonDynamoDBClient client)
        {
            Console.WriteLine("GetHighScoreForGame");
            // query using Global Secondary Index 
            // partition key is Game and sort key is TopScore
            // note we specify indexName in the query
            // ScanIndexForward is false so results are descending
            // and Limit is 1 so we only get the single high score back
            var request = new QueryRequest
            {
                TableName = "HighScores",
                IndexName = "GameIndex",
                KeyConditionExpression = "Game = :v_Game",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                                 { ":v_Game", new AttributeValue { S = game }},
                             },
                ScanIndexForward = false,
                Limit = 1
            };
            var response = client.QueryAsync(request).Result;
            PrintResults(response.Items);
        }

        static void PopulateDbWithTestData(Table table)
        {
            const Int32 numUsers = 10;
            const Int32 scoreMin = 0;
            const Int32 scoreMax = 100;

            for (Int32 i = 0; i < numUsers; i++)
            {
                string userName = RandomString(4);
                foreach (string game in Games)
                {
                    Int32 score = random.Next(scoreMin, scoreMax);
                    var entry = new Document();
                    entry["Username"] = userName;
                    entry["Game"] = game;
                    entry["TopScore"] = score;
                    entry["Timestamp"] = DateTime.Now;
                    _ = table.PutItemAsync(entry).Result;
                }
            }
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        static void PrintResults(List<Dictionary<string, AttributeValue>> items, bool hasTimestamp = false)
        {
            foreach (var item in items)
            {
                string s = item["Username"].S + " - " + item["Game"].S + " - " + item["TopScore"].N;
                if (hasTimestamp)
                    s += " - " + item["Timestamp"].S;
                Console.WriteLine(s);
            }
        }
    }
}
