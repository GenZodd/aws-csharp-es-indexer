# AWS DynamoDb to AWS ElasticSearch Index Project

This project is a simple example of how to use a C# Lambda to push DynamoDb records to ElasticSearch in AWS. This project is not really meant to be able to plug directly into any solution
it is meant to help jump start the effort to create this code for indexing. The project consists of 3 main classes. 

This project has a few additional libraries in it. 
* [elasticsearch-net-aws](https://github.com/bcuff/elasticsearch-net-aws) - This is used so that when NEST makes calls to elastic search it can pass the needed authentication 
to AWS. Without this you will always get an anaomous access denied error. 
* [AWS lambda dotnet](https://github.com/aws/aws-lambda-dotnet) - This is used to get the serilized types for DynamoDb events. 
* [NEST](https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/introduction.html) - This is the ElasticSearch .Net client. You can do pretty much everything with your
ElasticSearch REST endpoint via this library. 

Of course, you don't need to break the code down this way. This is just something to get people started. I would like to get this to a point where it can possibly be used for a very 
basic generic library anyone can use to plug into their solution and starting getting some basic indexing going with AWS ElasticSearch. 

## VehicleInventoryDbEventProcessor
This class is used to handle DynamoDb Event streams for updated, inserted, or deleted records. This code simply gets the event and loops through each record it gets. For each record
it sees what type of DynamoDb event it was and then calls the needed NEST command. 

I use the Lambda code to setup the configuration specifics of given DynamoDb type. In this case an "InventoryItem". 

```charp
	var settings = new ConnectionSettings(node, this.awsHttpConnection)
        .DisableDirectStreaming()
        .InferMappingFor<InventoryItem>(m => m.IdProperty(p => p.VIN))
        .MapDefaultTypeIndices(m => m.Add(typeof(InventoryItem), "dev_vehicle_inventory"));

    this.elasticClient = new ElasticClient(settings);
```
By setting up the mapping data here I can keep the indexer more agnostic to what it is trying to do. It allows the ElasticClient to know what the mappings are for different types of
so when it gets one it can simply call delete or index again and NEST + ElasticSearch do an upsert if it finds an IdProperty match or it can do a delete based on the IdProperty. 

## VehicleInventoryScheduledEventProcessor
This class is used to handle the reindexing of an ElasticSearch index based on all the records that are in a DynamoDb. Needless to say this is a heavy operations so should not be run
frequently.  

This class is used to handle DynamoDb Event streams for updated, inserted, or deleted records. This code simply gets the event and loops through each record it gets. For each record
it sees what type of DynamoDb event it was and then calls the needed NEST command. 

I use the Lambda code to setup the configuration specifics of given DynamoDb type. In this case an "InventoryItem". 

```charp
	var settings = new ConnectionSettings(node, this.awsHttpConnection)
        .DisableDirectStreaming()
        .InferMappingFor<InventoryItem>(m => m.IdProperty(p => p.VIN))
        .MapDefaultTypeIndices(m => m.Add(typeof(InventoryItem), "dev_vehicle_inventory"));

    this.elasticClient = new ElasticClient(settings);
```
By setting up the mapping data here I can keep the indexer more agnostic to what it is trying to do. It allows the ElasticClient to know what the mappings are for different types of
so when it gets one it can simply call delete or index again and NEST + ElasticSearch do an upsert if it finds an IdProperty match or it can do a delete based on the IdProperty. 

In the actual invoke method all that is happening is a call to the "Reindex" method. Since the ElasticSearchIndexer is a generic method that has its type defined when an instance is
created, it does not need any parameters. 

## ElasticSearchIndexer
This class is where the abstraction of the ElasticSearch indexing logic has been placed. This simply allows the Lambda's to focus on handling the event and each can 
reuse the indexer as needed. 

This class expects the process to be using the [AWS .NET: Object Persistence Model] (http://docs.aws.amazon.com/amazondynamodb/latest/developerguide/DotNetSDKHighLevel.html) with the 
POCO being decorated with at least the table name. The code then uses that objects to find it table name attribute to marry up to the index it should be creating. 

In AWS prefixing can be used to manage the ALM of the code through Development, QA, UAT and Production. The DynamoDb code hanles setting up this prefix as does the ElasticSearch
code.

```csharp
	var environmentPrefix = string.IsNullOrEmpty(environment) ? "dev_" : $"{environment}_";
    this.dynamoDbContext = new DynamoDBContext(dynamoDbClient, new DynamoDBContextConfig { TableNamePrefix = environmentPrefix });

	var attributes = typeof(TEntity).GetTypeInfo().CustomAttributes.ToList();
	this.indexTablename = environmentPrefix + attributes.FirstOrDefault(a => a.AttributeType == typeof(DynamoDBTableAttribute))
        .ConstructorArguments[0]
        .Value;
````

The process will handle checking the status of indexes and deleting the index as needed. For a reindex it uses the assigned type to query DynamoDb and read all rows for that
table. It will loop through this in batches (so it does not read in the entire table at once). It then creates a bulk request to ElasticSearch to optimize the process. 

