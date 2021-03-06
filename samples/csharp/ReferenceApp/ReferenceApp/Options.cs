using System.Collections.Generic;
using CommandLine;
using static GoDaddy.Asherah.ReferenceApp.ReferenceApp;

namespace GoDaddy.Asherah.ReferenceApp
{
    public class Options
    {
        [Option('m', "metastore-type", Required = false,  Default = ReferenceApp.MetaStore.MEMORY, HelpText = "Type of metastore persistence to use. Enum values: MEMORY, JDBC, DYNAMODB")]
        public ReferenceApp.MetaStore MetaStore { get; set; }

        [Option('a', "ado-connection-string", Required = false, HelpText = "ADO connection string to use for ADO metastore persistence. Required for ADO metastore.")]
        public string AdoConnectionString { get; set; }

        [Option('k', "kms-type", Required = false, Default = ReferenceApp.Kms.STATIC, HelpText = "Type of key management service to use. Enum values: STATIC, AWS")]
        public ReferenceApp.Kms Kms { get; set; }

        [Option('p', "preferred-region", Required = false, HelpText = "Preferred region to use for KMS if using AWS KMS. Required for AWS KMS.")]
        public string PreferredRegion { get; set; }

        [Option('r', "region-arn-tuples", Required = false, Separator = ',', HelpText = "Comma separated list of <region>=<kms_arn> tuples. Required for AWS KMS.")]
        public IEnumerable<string> RegionToArnTuples { get; set; }

        [Option('i', "iterations", Required = false, HelpText = "Number of encrypt/decrypt iterations to run", Default = 1)]
        public int Iterations { get; set; }

        [Option('c', "enable-cw", Required = false, HelpText = "Enable CloudWatch Metrics output")]
        public bool EnableCloudWatch { get; set; }

        [Option('d', "drr", Required = false, HelpText = "DRR to be decrypted", Default = null)]
        public string Drr { get; set; }
    }
}