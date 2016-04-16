using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockApp
{
    public class DynamoDataOp
    {
        private static AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        private static List<Dynamo> dtLst = new List<Dynamo>();
        string bucketName = "mybucketcse661";
        string logbucket = "mylogbucketcse661";
        string key = "currentComps.csv";
        string sharedFile = "recentCompFile.csv";
        string stockLoggerContent = "";
        public DynamoDataOp()
        {
            DynamoDBContext context = new DynamoDBContext(client);
        }
        public void AddDynamo(Dynamo dyn)
        {
            DynamoDBContext context = new DynamoDBContext(client);
            Dynamo delObj = context.Load<Dynamo>(dyn.compid);
            if (delObj != null)
            {
                stockLoggerContent+= DisplaySingleItem(delObj);                
                context.Delete(delObj);
            }
            context.Save(dyn);
        }
        public void StockLogger()
        {    
            AmazonS3Client loCLient = new AmazonS3Client(Amazon.RegionEndpoint.USEast1);
            PutObjectRequest request1 = new PutObjectRequest()
            {
                BucketName = logbucket,
                Key = "StockLogger" + DateTime.Now + ".csv",
                ContentBody = stockLoggerContent,                
            };
            PutObjectResponse response3 = loCLient.PutObject(request1);
        }
        public void ModifyDynamo(Dynamo newDyn)
        {
            DynamoDBContext context = new DynamoDBContext(client);
            Dynamo oDYN = context.Load<Dynamo>(newDyn.compid);
            oDYN = newDyn;
            if (oDYN == null)
                new Exception("Non - existent object");
            context.Save(oDYN);
        }
        public string GetOnChgPerc()
        {
            var ScanRequest = new ScanRequest
            {
                TableName = "tblStock",
                // Optional parameters.
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                {":val", new AttributeValue { N = "2" }}
                },
                FilterExpression = "changeperc >= :val",
                ProjectionExpression = "compid,compname,changeperc,stockprice"
            };

            var response = client.Scan(ScanRequest);
            dtLst.Clear();
            Console.WriteLine("Important Notice..!!.\n");
            Console.WriteLine("Below company shares have seen a increase in prices! Invest today!\n");
            Console.WriteLine("\n\n\n\n\n");
            foreach (Dictionary<string, AttributeValue> item
                 in response.Items)
            {
                PrintItem(item);
            }
           return DisplayItems();
        }
        public string DisplayItems()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var d in dtLst)
            {
                builder.Append("\nCompany ID : ");
                //Console.Write("\nCompany ID : ");
                builder.Append(d.compid);
               // Console.WriteLine(d.compid);
                builder.Append("\nCompany Name : ");
               // Console.Write("\nCompany Name : ");
                builder.Append(d.compname);
               // Console.WriteLine(d.compname);
                builder.Append("\nStock Price : ");
               // Console.Write("\nStock Price : ");
                builder.Append(d.stockprice);
                //Console.WriteLine(d.stockprice);
                builder.Append("\nChange Percentage(%) : ");
                //Console.Write("\nChange Percentage(%) : ");
                //Console.WriteLine(d.changeperc);
                builder.Append(d.changeperc);
                //Console.WriteLine("\n");
                builder.Append("\n");
            }
            Console.WriteLine(builder.ToString());
            return builder.ToString();
        }

        public string DisplaySingleItem(Dynamo d)
        {
                StringBuilder builder = new StringBuilder();
                builder.Append("\nCompany ID : ");
                builder.Append(d.compid);
                builder.Append("\nCompany Name : ");
                builder.Append(d.compname);
                builder.Append("\nStock Price : ");
                builder.Append(d.stockprice);
                builder.Append("\nChange Percentage(%) : ");
                builder.Append(d.changeperc);
                builder.Append("\n");
            return builder.ToString();
        }
        public void ReadS3File()
        {
            IAmazonS3 client;
            using (client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1))
            {
                GetObjectRequest request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = key
                };
                using (GetObjectResponse response = client.GetObject(request))
                {
                    string dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), key);
                    if (!File.Exists(dest))
                    {
                        response.WriteResponseStreamToFile(dest);
                    }
                }
            }
        }
        public void WriteS3File()
        {
            AmazonS3Client loCLient = new AmazonS3Client(Amazon.RegionEndpoint.USEast1);
            string ftpFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), sharedFile);           
            if(!File.Exists(ftpFile))
            {
                ftpFile = key;
            }
            PutObjectRequest request = new PutObjectRequest()
            {
                BucketName = bucketName,
                Key = key,                
                FilePath = ftpFile
            };
            PutObjectResponse response2 = loCLient.PutObject(request);
        }
        public void RenameS3File()
        {
            AmazonS3Client localclient = new AmazonS3Client(Amazon.RegionEndpoint.USEast1);
            if (ExistsKey(key))
            {
                CopyObjectRequest request = new CopyObjectRequest()
                {
                    SourceBucket = bucketName,
                    SourceKey = key,
                    DestinationBucket = logbucket,                
                    DestinationKey = "logger" + DateTime.Now + ".csv"
                };
                CopyObjectResponse response = localclient.CopyObject(request);
                //Delete the original
                DeleteObjectRequest deleteObjectRequest =
                    new DeleteObjectRequest
                    {
                        BucketName = bucketName,
                        Key = key
                    };
                localclient.DeleteObject(deleteObjectRequest);
            }
        }
        public void SendSQSMessage()
        {
            var sqsCl = new AmazonSQSClient();
            var request = new SendMessageRequest
            {
                DelaySeconds = (int)TimeSpan.FromSeconds(5).TotalSeconds,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                        "MyNameAttribute", new MessageAttributeValue
                        { DataType = "String", StringValue = "Admin" }
                    },
                {
                    "MyClassAttribute", new MessageAttributeValue
                    { DataType = "String", StringValue = "CSE 661 Class" }
                    },
                    {
                        "MyRegionAttribute", new MessageAttributeValue
                    { DataType = "String", StringValue = "Syracuse, United States" }
                }
            },
            MessageBody = EmailMsg(),
            QueueUrl = "https://sqs.us-east-1.amazonaws.com/445960074775/cse661sqs"
            };
            var response = sqsCl.SendMessage(request);
        }
        public string EmailMsg()
        {
            string res = "Please invest in these shares.These shares have seen a increase of more than 2%\n";
            foreach(var i in dtLst)
            {
                res = res+ "CompID:" + i.compid + ";CompName:" + i.compname + ";StockPrice:" + i.stockprice + ";ChangePercentage:" + i.changeperc + "\n";
            }
            return res;
        }
        private static void PrintItem(Dictionary<string, AttributeValue> attributeList)
        {
            Dynamo d = new Dynamo();
            foreach (KeyValuePair<string, AttributeValue> kvp in attributeList)
            {
                string attributeName = kvp.Key;
                AttributeValue value = kvp.Value;
                if (kvp.Key == "compid")
                {
                    d.compid = value.S;
                }
                if (kvp.Key == "compname")
                {
                    d.compname = value.S;
                }
                if (kvp.Key == "stockprice")
                {
                    d.stockprice = double.Parse(value.N);
                }
                if (kvp.Key == "changeperc")
                {
                    d.changeperc = double.Parse(value.N);
                }
            }
            dtLst.Add(d);
        }        
        public bool ExistsKey(string key)
        {
            AmazonS3Client localclient = new AmazonS3Client(Amazon.RegionEndpoint.USEast1);
            var request = new GetObjectMetadataRequest();
            request.BucketName = bucketName;
            request.Key = key;
            try
            {
                GetObjectMetadataResponse response = localclient.GetObjectMetadata(request);
            }
            catch (AmazonS3Exception e)
            {   
                    return false;
            }
            return true;
        }
    }
}
