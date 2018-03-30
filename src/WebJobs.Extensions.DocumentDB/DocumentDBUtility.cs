﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Microsoft.Azure.WebJobs.Extensions.DocumentDB
{
    internal static class DocumentDBUtility
    {
        private const int CosmosDBMinimumCollectionRU = 400;

        internal static bool TryGetDocumentClientException(Exception originalEx, out DocumentClientException documentClientEx)
        {
            documentClientEx = originalEx as DocumentClientException;

            if (documentClientEx != null)
            {
                return true;
            }

            AggregateException ae = originalEx as AggregateException;
            if (ae == null)
            {
                return false;
            }

            documentClientEx = ae.InnerException as DocumentClientException;

            return documentClientEx != null;
        }

        internal static async Task CreateDatabaseAndCollectionIfNotExistAsync(DocumentDBContext context)
        {
            await CreateDatabaseAndCollectionIfNotExistAsync(context.Service, context.ResolvedAttribute.DatabaseName, context.ResolvedAttribute.CollectionName,
                context.ResolvedAttribute.PartitionKey, context.ResolvedAttribute.CollectionThroughput);
        }

        internal static async Task CreateDatabaseAndCollectionIfNotExistAsync(IDocumentDBService service, string databaseName, string collectionName, string partitionKey, int throughput)
        {
            await service.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseName });

            await CreateDocumentCollectionIfNotExistsAsync(service, databaseName, collectionName, partitionKey, throughput);
        }

        private static async Task<DocumentCollection> CreateDocumentCollectionIfNotExistsAsync(IDocumentDBService service, string databaseName, string collectionName,
            string partitionKey, int throughput)
        {
            Uri databaseUri = UriFactory.CreateDatabaseUri(databaseName);

            DocumentCollection documentCollection = new DocumentCollection
            {
                Id = collectionName
            };

            if (!string.IsNullOrEmpty(partitionKey))
            {
                documentCollection.PartitionKey.Paths.Add(partitionKey);
            }

            if (throughput == 0)
            {
                throughput = CosmosDBMinimumCollectionRU;
            }

            // If there is any throughput specified, pass it on. DocumentClient will throw with a 
            // descriptive message if the value does not meet the collection requirements.
            RequestOptions collectionOptions = new RequestOptions
            {
                OfferThroughput = throughput
            };

            return await service.CreateDocumentCollectionIfNotExistsAsync(databaseUri, documentCollection, collectionOptions);
        }
    }
}