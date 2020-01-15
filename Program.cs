﻿using System;
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
        private static readonly string[] GameTitles = { "Football", "Basketball", "Baseball", "Hockey" };

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

            GetAllHighScoresForUser("PFZE", client);
            GetHighScoreForUserAndGame("PFZE", "Football", client);
            GetHighScoreForGame("Basketball", client);
            GetMostRecentHighScoreForUser("PFZE", client);
        }

        static void GetAllHighScoresForUser(string user, AmazonDynamoDBClient client)
        {
            Console.WriteLine("GetAllHighScoresForUser");
            // query only using partition key (UserId)
            var request = new QueryRequest
            {
                TableName = "HighScores",
                KeyConditionExpression = "UserId = :v_Id",
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
            // query using partition and sort keys (UserId & GameTitle)
            var request = new QueryRequest
            {
                TableName = "HighScores",
                KeyConditionExpression = "UserId = :v_Id and GameTitle = :v_Game",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                                 { ":v_Id", new AttributeValue { S = user }},
                                 { ":v_Game", new AttributeValue { S = game}}
                             },
                ScanIndexForward = true
            };
            var response = client.QueryAsync(request).Result;
            PrintResults(response.Items);
        }

        static void GetHighScoreForGame(string game, AmazonDynamoDBClient client)
        {
            Console.WriteLine("GetHighScoreForGame");
            // query using Global Secondary Index 
            // partition key is GameTitle and sort key is TopScore
            // note we specify indexName in the query
            // ScanIndexForward is false so results are descending
            // and Limit is 1 so we only get the single high score back
            var request = new QueryRequest
            {
                TableName = "HighScores",
                IndexName = "GameTitleIndex",
                KeyConditionExpression = "GameTitle = :v_Game",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                                 { ":v_Game", new AttributeValue { S = game }},
                             },
                ScanIndexForward = false,
                Limit = 1
            };
            var response = client.QueryAsync(request).Result;
            PrintResults(response.Items);
        }

        static void GetMostRecentHighScoreForUser(string user, AmazonDynamoDBClient client)
        {
            Console.WriteLine("GetMostRecentHighScoreForUser");
            // query using Local Secondary Index 
            // partition key is UserId (default) and sort key is TimeStamp
            // note we specify indexName in the query
            // ScanIndexForward is false so results are descending
            // and Limit is 1 so we only get the single high score back
            var request = new QueryRequest
            {
                TableName = "HighScores",
                IndexName = "TimestampIndex",
                KeyConditionExpression = "UserId = :v_Id",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                                 { ":v_Id", new AttributeValue { S = user }},
                             },
                ScanIndexForward = false,
                Limit = 1
            };
            var response = client.QueryAsync(request).Result;
            PrintResults(response.Items, true);
        }

        static void PopulateDbWithTestData(Table table)
        {
            const Int32 numUsers = 10;
            const Int32 scoreMin = 0;
            const Int32 scoreMax = 100;

            for (Int32 i = 0; i < numUsers; i++)
            {
                string userName = RandomString(4);
                foreach (string game in GameTitles)
                {
                    Int32 score = random.Next(scoreMin, scoreMax);
                    var entry = new Document();
                    entry["UserId"] = userName;
                    entry["GameTitle"] = game;
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
                string s = item["UserId"].S + " - " + item["GameTitle"].S + " - " + item["TopScore"].N;
                if (hasTimestamp)
                    s += " - " + item["Timestamp"].S;
                Console.WriteLine(s);
            }
        }
    }
}