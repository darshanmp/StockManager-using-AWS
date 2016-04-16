using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime;
using Amazon.SecurityToken;
namespace StockApp
{
    [DynamoDBTable("tblStock")]
    public class Dynamo
    {
        [DynamoDBProperty]
        public string compname { get; set; }

        [DynamoDBHashKey]
        public string compid { get; set; }

        [DynamoDBProperty]
        public DateTime timestamp { get; set; }
        [DynamoDBProperty]
        public double stockprice { get; set; }
        [DynamoDBProperty]
        public double changeperc { get; set; }
    }
}
