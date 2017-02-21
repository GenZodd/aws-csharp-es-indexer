// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VehicleInventoryScheduledEventProcessor.cs" company="Outsell, LLC.">
//   Copyright © Outsell, LLC., All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Outsell.Dep.Services.Inventory.ElasticSearch
{
    using System;

    using Amazon;
    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.DataModel;
    using Amazon.DynamoDBv2.DocumentModel;
    using Amazon.Lambda.Core;
    using Amazon.Lambda.DynamoDBEvents;

    using Elasticsearch.Net;
    using Elasticsearch.Net.Aws;

    using ElasticSearchIndexer;

    using Nest;

    /// <summary>
    /// Used to managed data exchange between invetory data and elastic search
    /// </summary>
    public class VehicleInventoryScheduledEventProcessor
    {
        private IElasticClient elasticClient;

        private AwsHttpConnection awsHttpConnection;

        private ElasticSearchIndexer<InventoryItem> indexer;

        private DynamoDBContext dynamoDbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleInventoryScheduledEventProcessor"/> class.
        /// </summary>
        public VehicleInventoryScheduledEventProcessor()
        {
            this.awsHttpConnection = new AwsHttpConnection(RegionEndpoint.USEast1.SystemName);

            var node = new SingleNodeConnectionPool(new Uri(Environment.GetEnvironmentVariable("elasticSearchURL")));

            // setup index settings for each strongly typed object
            var settings = new ConnectionSettings(node, this.awsHttpConnection)
                .DisableDirectStreaming()
                .InferMappingFor<InventoryItem>(m => m.IdProperty(p => p.VIN))
                .MapDefaultTypeIndices(m => m.Add(typeof(InventoryItem), "dev_vehicle_inventory"));

            this.elasticClient = new ElasticClient(settings);

            IAmazonDynamoDB dynamoDbClient = new AmazonDynamoDBClient();
            this.indexer = new ElasticSearchIndexer<InventoryItem>(dynamoDbClient, Environment.GetEnvironmentVariable("environment"), this.elasticClient);

            var config = new DynamoDBContextConfig { ConsistentRead = true, TableNamePrefix = $"{Environment.GetEnvironmentVariable("environment")}_" };
            this.dynamoDbContext = new DynamoDBContext(dynamoDbClient, config);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleInventoryScheduledEventProcessor" /> class.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="environment">The environment.</param>
        public VehicleInventoryScheduledEventProcessor(IElasticClient client, string environment)
        {
            if (client == null)
            {
                var creds =
                    new StaticCredentialsProvider(
                        new AwsCredentials
                        {
                            AccessKey = "",
                            SecretKey = "",
                        });

                var node =
                    new SingleNodeConnectionPool(
                        new Uri(Environment.GetEnvironmentVariable("elasticSearchURL")));

                this.awsHttpConnection = new AwsHttpConnection(RegionEndpoint.USEast1.SystemName, creds);

                // setup index settings for each strongly typed object
                var settings =
                    new ConnectionSettings(node, this.awsHttpConnection)
                        .DisableDirectStreaming()
                        .InferMappingFor<InventoryItem>(m => m.IdProperty(p => p.VIN))
                        .MapDefaultTypeIndices(m => m.Add(typeof(InventoryItem), "dev_vehicle_inventory"));

                this.elasticClient = new ElasticClient(settings);

                IAmazonDynamoDB dynamoDbClient = new AmazonDynamoDBClient(RegionEndpoint.USEast1);
                this.indexer = new ElasticSearchIndexer<InventoryItem>(dynamoDbClient, Environment.GetEnvironmentVariable("environment"), this.elasticClient);

                var config = new DynamoDBContextConfig { ConsistentRead = true, TableNamePrefix = $"{Environment.GetEnvironmentVariable("environment")}_" };
                this.dynamoDbContext = new DynamoDBContext(dynamoDbClient, config);
            }
            else
            {
                this.elasticClient = client;
            }
        }

        /// <summary>
        /// Does the invoke.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        /// The result of the invoke operation
        /// </returns>
        protected string Invoke(ILambdaContext context)
        {
            var response = this.indexer.ReIndex();
            string message = "sucess";

            if (!response.Errors)
            {
                message = $"{response.Items.Count} items have been index";
            }

            if (response.Errors)
            {
                message = "error";
                return "Success";
            }

            return message;
        }
    }
}
