using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using App.Metrics;
using CommandLine;
using CommandLine.Text;
using GoDaddy.Asherah.AppEncryption;
using GoDaddy.Asherah.AppEncryption.KeyManagement;
using GoDaddy.Asherah.AppEncryption.Persistence;
using GoDaddy.Asherah.Crypto;
using GoDaddy.Asherah.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace GoDaddy.Asherah.ReferenceApp
{
    public class ReferenceApp
    {
        private static readonly int KeyExpirationDays = 30;
        private static readonly int CacheCheckMinutes = 30;

        private static ILogger logger;
        private static ParserResult<Options> cmdOptions;

        public enum MetaStore
        {
            MEMORY,
            ADO,
            DYNAMODB
        }

        public enum Kms
        {
            STATIC,
            AWS
        }

        public static void Main(string[] args)
        {
            // Setup our Logger. This is used by the library as well.
            ILoggerFactory loggerFactory = new LoggerFactory();

            // TODO Not sure if there's a better way to handle LoggerProvider?
#pragma warning disable 618
            loggerFactory.AddProvider(new ConsoleLoggerProvider((category, level) => level >= LogLevel.Information, true));
#pragma warning restore 618
            LogManager.SetLoggerFactory(loggerFactory);
            logger = LogManager.CreateLogger<ReferenceApp>();

            cmdOptions = Parser.Default.ParseArguments<Options>(args);
            cmdOptions.WithParsed(App);
        }

        private static async void App(Options options)
        {
            IMetastorePersistence<JObject> metastorePersistence = null;
            KeyManagementService keyManagementService = null;

            if (options.MetaStore == MetaStore.ADO)
            {
                if (options.AdoConnectionString != null)
                {
                    logger.LogInformation("using ADO-based metastore...");
                    metastorePersistence = AdoMetastorePersistenceImpl
                        .NewBuilder(MySqlClientFactory.Instance, options.AdoConnectionString)
                        .Build();
                }
                else
                {
                    logger.LogError("ADO connection string is a mandatory parameter with MetaStore Type: ADO");
                    Console.WriteLine(HelpText.AutoBuild(cmdOptions, null, null));
                    return;
                }
            }
            else if (options.MetaStore == MetaStore.DYNAMODB)
            {
                logger.LogInformation("using DynamoDB-based metastore...");
                AWSConfigs.AWSRegion = "us-west-2";
                metastorePersistence = DynamoDbMetastorePersistenceImpl.NewBuilder().Build();
            }
            else
            {
                logger.LogInformation("using in-memory metastore...");
                metastorePersistence = new MemoryPersistenceImpl<JObject>();
            }

            if (options.Kms == Kms.AWS)
            {
                if (options.PreferredRegion != null && options.RegionToArnTuples != null)
                {
                    Dictionary<string, string> regionToArnDictionary = new Dictionary<string, string>();
                    foreach (string regionArnPair in options.RegionToArnTuples)
                    {
                        string[] regionArnArray = regionArnPair.Split("=");
                        regionToArnDictionary.Add(regionArnArray[0], regionArnArray[1]);
                    }

                    logger.LogInformation("using AWS KMS...");
                    keyManagementService = AwsKeyManagementServiceImpl
                        .NewBuilder(regionToArnDictionary, options.PreferredRegion).Build();
                }
                else
                {
                    logger.LogError("Preferred region and <region>=<arn> tuples are mandatory with  KMS Type: AWS");
                    Console.WriteLine(HelpText.AutoBuild(cmdOptions, null, null));
                    return;
                }
            }
            else
            {
                logger.LogInformation("using static KMS...");
                keyManagementService = new StaticKeyManagementServiceImpl("secretmasterkey!");
            }

            CryptoPolicy cryptoPolicy = BasicExpiringCryptoPolicy
                .NewBuilder()
                .WithKeyExpirationDays(KeyExpirationDays)
                .WithRevokeCheckMinutes(CacheCheckMinutes)
                .Build();

            // Setup metrics reporters and always include console.
            IMetricsBuilder metricsBuilder = new MetricsBuilder()
                .Report.ToConsole(consoleOptions => consoleOptions.FlushInterval = TimeSpan.FromSeconds(60));

            // CloudWatch metrics generation
            if (options.EnableCloudWatch)
            {
                // Fill in when we open source our App.Metrics cloudwatch reporter separately
            }

            IMetrics metrics = metricsBuilder.Build();

            // Create a session factory for this app. Normally this would be done upon app startup and the
            // same factory would be used anytime a new session is needed for a partition (e.g., shopper).
            // We've split it out into multiple using blocks to underscore this point.
            using (AppEncryptionSessionFactory appEncryptionSessionFactory = AppEncryptionSessionFactory
                .NewBuilder("productId", "reference_app")
                .WithMetaStorePersistence(metastorePersistence)
                .WithCryptoPolicy(cryptoPolicy)
                .WithKeyManagementService(keyManagementService)
                .WithMetrics(metrics)
                .Build())
            {
                // Now create an actual session for a partition (which in our case is a pretend shopper id). This session is used
                // for a transaction and is disposed automatically after use due to the IDisposable implementation.
                using (AppEncryption<byte[], byte[]> appEncryptionBytes =
                    appEncryptionSessionFactory.GetAppEncryptionBytes("shopper123"))
                {
                    const string originalPayloadString = "mysupersecretpayload";
                    foreach (int i in Enumerable.Range(0, options.Iterations))
                    {
                        string dataRowString;

                        // If we get a DRR as a command line argument, we want to directly decrypt it
                        if (options.Drr != null)
                        {
                            dataRowString = options.Drr;
                        }
                        else
                        {
                            // Encrypt the payload
                            byte[] dataRowRecordBytes =
                                appEncryptionBytes.Encrypt(Encoding.UTF8.GetBytes(originalPayloadString));

                            // Consider this us "persisting" the DRR
                            dataRowString = Convert.ToBase64String(dataRowRecordBytes);
                        }

                        logger.LogInformation("dataRowRecord as string = {dataRow}", dataRowString);

                        byte[] newDataRowRecordBytes = Convert.FromBase64String(dataRowString);

                        // Decrypt the payload
                        string decryptedPayloadString =
                            Encoding.UTF8.GetString(appEncryptionBytes.Decrypt(newDataRowRecordBytes));

                        logger.LogInformation("decryptedPayloadString = {payload}", decryptedPayloadString);
                        logger.LogInformation("matches = {result}", originalPayloadString.Equals(decryptedPayloadString));
                    }
                }
            }

            // Force final publish of metrics
            await Task.WhenAll(((IMetricsRoot)metrics).ReportRunner.RunAllAsync());
        }
    }
}
