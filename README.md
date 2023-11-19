# CouchDbWikipediaArticleUpload

## Description
.NET Console application to download XML-Dumps from the English Wikipedia and upload them to a CouchDB Database. Per Article there will be one Document created containing the text and metadata of the latest revision.

## Settings
The following Settings can be configured in the "appsettings.json" file:

 - **DumpBaseUrl**: The base url of the wikipedia dump provider. Default value should be used.
 - **DumpVersionToDownload**: Timestamp of the Dump to Upload (example: `20231101`).
 - **DumpDownloadPath**: Local path where the dumps will be stored.
 - **DbEndpoint**: Endpoint of the CouchDb instance (example: `http://ip-address:5984/`).
 - **DbUser**: CouchDb Username.
 - **DbPassword** CouchDb Password.
