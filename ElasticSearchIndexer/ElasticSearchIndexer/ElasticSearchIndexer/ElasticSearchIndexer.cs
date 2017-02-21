// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ElasticSearchIndexer.cs" company="Outsell, LLC.">
//   Copyright © Outsell, LLC., All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ElasticSearchIndexer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.DataModel;

    using Nest;

    /// <summary>
    /// Class that handles Elastic search indexing for Inventory items
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public class ElasticSearchIndexer<TEntity>
        where TEntity : class
    {
        private readonly IDynamoDBContext dynamoDbContext;

        private readonly IElasticClient elasticClient;

        private string indexTablename;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElasticSearchIndexer{TEntity}"/> class.
        /// </summary>
        /// <param name="dynamoDbClient">The dynamo database client.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="elasticClient">The elastic client.</param>
        public ElasticSearchIndexer(IAmazonDynamoDB dynamoDbClient, string environment, IElasticClient elasticClient)
        {
            var environmentPrefix = string.IsNullOrEmpty(environment) ? "dev_" : $"{environment}_";
            this.dynamoDbContext = new DynamoDBContext(dynamoDbClient, new DynamoDBContextConfig { TableNamePrefix = environmentPrefix });
            this.elasticClient = elasticClient;

            var attributes = typeof(TEntity).GetTypeInfo().CustomAttributes.ToList();
            this.indexTablename = environmentPrefix + attributes.FirstOrDefault(a => a.AttributeType == typeof(DynamoDBTableAttribute))
                .ConstructorArguments[0]
                .Value;
        }

        /// <summary>
        /// Res the index.
        /// </summary>
        /// <returns>Response data for a bulk insert of items</returns>
        /// <exception cref="System.Exception">Cound not delete log to start re-index - " + deleteResponse.DebugInformation</exception>
        public IBulkResponse ReIndex()
        {
            if (this.elasticClient.IndexExists(this.indexTablename).Exists)
            {
                var response = this.elasticClient.DeleteIndex(this.indexTablename);

                if (!response.IsValid)
                {
                    throw new Exception("Cound not delete log to start re-index - " + response.DebugInformation);
                }
            }

            this.VerifyIndexStatus();

            // this will get all records in the table. Needless to say this is not verify effective so we don't want to do this very often. 
            var results = this.dynamoDbContext.ScanAsync<TEntity>(null);

            var bulkRequest = new BulkRequest(this.indexTablename)
                                  {
                                      Operations = new List<IBulkOperation>()
                                  };

            while (!results.IsDone)
            {
                var batch = results.GetNextSetAsync().Result;

                foreach (var item in batch)
                {
                    bulkRequest.Operations.Add(new BulkIndexOperation<TEntity>(item));
                }
            }

            return this.elasticClient.Bulk(bulkRequest);
        }

        /// <summary>
        /// Inserts the index.
        /// </summary>
        /// <param name="itemToIndex">Index of the item to.</param>
        /// <returns>Response object with details</returns>
        public IIndexResponse InsertIndex(TEntity itemToIndex)
        {
            return this.elasticClient.Index(itemToIndex);
        }

        /// <summary>
        /// Inserts the index.
        /// </summary>
        /// <param name="indexItemId">The index item identifier.</param>
        /// <returns>Response object with details</returns>
        public IDeleteResponse DeleteIndex(Id indexItemId)
        {
            return this.elasticClient.Delete(new DeleteRequest(this.indexTablename, TypeName.From<TEntity>(), indexItemId));
        }

        /// <summary>
        /// Inserts the index.
        /// </summary>
        /// <param name="itemToIndex">Index of the item to.</param>
        /// <param name="itemToUpdateId">The item to update identifier.</param>
        /// <returns>Response object with details</returns>
        public IIndexResponse UpdateIndex(TEntity itemToIndex, Id itemToUpdateId)
        {
            var indexedItemResponse = this.elasticClient.Get<TEntity>(itemToUpdateId);
            var indexItem = indexedItemResponse.Source;
            indexItem = itemToIndex;
            return this.elasticClient.Index(indexItem);
        }

        private void VerifyIndexStatus()
        {
            ////var item = record.Dynamodb.NewImage;
            if (!this.elasticClient.IndexExists(this.indexTablename).Exists)
            {
                // create index
                var response = this.elasticClient.CreateIndex(this.indexTablename);

                if (!response.IsValid)
                {
                    throw new Exception("could not create index -" + response.DebugInformation);
                }
            }
        }
    }
}
