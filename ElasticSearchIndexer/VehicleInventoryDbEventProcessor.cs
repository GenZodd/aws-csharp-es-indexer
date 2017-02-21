// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VehicleInventoryDbEventProcessor.cs" company="Outsell, LLC.">
//   Copyright © Outsell, LLC., All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ElasticSearchIndexer
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

    using Nest;

    using Outsell.Dep.Services.Inventory;

    /// <summary>
    /// Used to managed data exchange between invetory data and elastic search
    /// </summary>
    public class VehicleInventoryDbEventProcessor
    {
        private IElasticClient elasticClient;

        private AwsHttpConnection awsHttpConnection;

        private ElasticSearchIndexer<InventoryItem> indexer;

        private IDynamoDBContext dynamoDbContext;

        private ILambdaContext contenxt;

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleInventoryDbEventProcessor"/> class.
        /// </summary>
        public VehicleInventoryDbEventProcessor()
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

            var config = new DynamoDBContextConfig { ConsistentRead = true, TableNamePrefix = $"dev_" };
            this.dynamoDbContext = new DynamoDBContext(dynamoDbClient, config);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleInventoryDbEventProcessor" /> class.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="dynamoDbContext">The dynamo database context.</param>
        public VehicleInventoryDbEventProcessor(IElasticClient client, IDynamoDBContext dynamoDbContext)
        {
            var config = new DynamoDBContextConfig { ConsistentRead = true, TableNamePrefix = $"dev_" };

            // this is used for more integratin level testing. 
            if (client == null)
            {
                var creds =
                    new StaticCredentialsProvider(
                        new AwsCredentials
                        {
                            AccessKey = string.Empty,
                            SecretKey = string.Empty,
                        });

                var node =
                    new SingleNodeConnectionPool(
                        new Uri(string.Empty));

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
                
                this.dynamoDbContext = new DynamoDBContext(dynamoDbClient, config);
            }
            else
            {
                this.elasticClient = client;
                this.dynamoDbContext = dynamoDbContext;
            }
        }

        /// <summary>
        /// Does the invoke.
        /// </summary>
        /// <param name="theEvent">The event.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        /// The result of the invoke operation
        /// </returns>
        protected string Invoke(DynamoDBEvent theEvent, ILambdaContext context)
        {
            foreach (var record in theEvent.Records)
            {
                var document = Document.FromAttributeMap(record.Dynamodb.NewImage);
                var itemToIndex = this.dynamoDbContext.FromDocument<InventoryItem>(document);

                if (record.EventName == OperationType.INSERT)
                {
                    return this.indexer.InsertIndex(itemToIndex).Result.ToString();
                }

                if (record.EventName == OperationType.MODIFY)
                {
                    return this.indexer.UpdateIndex(itemToIndex, new Id(itemToIndex.VIN)).Result.ToString();
                }

                if (record.EventName == OperationType.REMOVE)
                {
                    return this.indexer.DeleteIndex(new Id(itemToIndex.VIN)).Result.ToString();
                }
            }

            return "nothing processed";
        }
    }
}
