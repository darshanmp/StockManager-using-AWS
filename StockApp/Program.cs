using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Net;
using ReadWriteCsv;

namespace StockApp
{
    public class GetObject
    {
        static List<CSVFile> csvfileData = new List<CSVFile>();
        static List<Dynamo> stockLst = new List<Dynamo>();
        static string FinP1 = "http://download.finance.yahoo.com/d/quotes.csv?s=";
        static string FinP2 = "";
        static string FinP3 = "&f=sl1d1t1p2";
        static Dictionary<string, string> compLookup = new Dictionary<string, string>();
        static int MaxCap = 100;
        string key = "currentComps.csv";
        public static void Main(string[] args)
        {
            GetObject gobj = new GetObject();
            try
            {                              
                gobj.ReadS3FromFTP();
                gobj.GetCompanyTickers();
                int j = 0;
                for (int i = 0; i < MaxCap; i++)
                {
                    compLookup[csvfileData[j].CompID] = csvfileData[j].CompName;
                    if (i != (MaxCap - 1))
                        FinP2 = FinP2 + csvfileData[j].CompID + "+";
                    else
                        FinP2 = FinP2 + csvfileData[j].CompID;
                    j++;
                }
                if (FinP2 == "")
                    FinP2 = "YHOO+GOOG+MSFT+TFSL+AAL+AAPL+ADI+BBBY+KMI+BAC+PFE+FCX+GE+SUNE+AA";
                string url = FinP1 + FinP2 + FinP3;
                SplitCSV(url);
                //Dynamo db insert      
                gobj.DynamoInsert();
               //gobj.GetOnChangePerc();
                gobj.SendSQSMessage();
            }
            catch (AmazonS3Exception)
            {
                //Console.WriteLine(s3Exception.Message,
                //                  s3Exception.InnerException);
            }
            //Console.WriteLine("Press any key to continue...");
            //Console.ReadKey();
        }      
        public void GetCompanyTickers()
        {
            // Read sample data from CSV file
            DynamoDataOp opLib = new DynamoDataOp();
            opLib.ReadS3File();
            string dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), key);
            using (CsvFileReader reader = new CsvFileReader(dest))
            {
                CsvRow row = new CsvRow();
                while (reader.ReadRow(row))
                {
                    CSVFile stoK = new CSVFile();
                    stoK.CompID = row[0];
                    stoK.CompName = row[1];
                    csvfileData.Add(stoK);
                }
            }
        }
        public string GetCSV(string url)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            StreamReader sr = new StreamReader(resp.GetResponseStream());
            string results = sr.ReadToEnd();
            sr.Close();
            return results;
        }
        public static void SplitCSV(string url)
        {
            List<string> splitted = new List<string>();
            GetObject gobj = new GetObject();
            string fileList = gobj.GetCSV(url);
            string[] tempStr;
            string[] finalStr =new string[9999999];
            int coun = 0;
            tempStr = fileList.Split('%');
            foreach(string h in tempStr)
            {
                foreach(var r in h.Split(','))
                {
                    finalStr[coun] = r;
                    coun++;
                }
            }
            int i = 0;
            foreach (string item in finalStr)
            {
                if (item != null)
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        splitted.Add(item);
                    }
                    if (i == 4)
                    {
                        Dynamo db = new Dynamo();
                        db.compid = splitted[0].Replace("\"", "").TrimStart('\n');
                        db.stockprice = double.Parse(splitted[1]);
                        db.compname = compLookup[db.compid];
                        DateTime dt = Convert.ToDateTime(splitted[2].Replace("\"", "") + " " + splitted[3].Replace("\"", ""));                     
                        db.timestamp = dt;
                        db.changeperc = double.Parse(splitted[4].Replace("\"", ""));
                        stockLst.Add(db); //add items to the lst   
                        splitted.Clear();                                       
                        i = 0;
                    }
                    else
                        i++;
                }
            }

        }
        public void DynamoInsert()
        {
            //Sort and insert
            //stockLst = stockLst.OrderBy(o => o.stockprice).ToList();
            DynamoDataOp opLib = new DynamoDataOp();
            foreach (var  i in stockLst)
            {
                opLib.AddDynamo(i);
            }
            opLib.StockLogger();
        }
        public string GetOnChangePerc()
        {
           DynamoDataOp opLib = new DynamoDataOp();
           return opLib.GetOnChgPerc();
        }
        public void SendSQSMessage()
        {
            DynamoDataOp opLib = new DynamoDataOp();
            opLib.SendSQSMessage();
        }
        public void ReadS3FromFTP()
        {
            DynamoDataOp opLib = new DynamoDataOp();
            opLib.RenameS3File();
            opLib.WriteS3File();
        }
    }
}

namespace ReadWriteCsv
{
    /// <summary>
    /// Class to store one CSV row
    /// </summary>
    public class CsvRow : List<string>
    {
        public string LineText { get; set; }
    }
    /// <summary>
    /// Class to read data from a CSV file
    /// </summary>
    public class CsvFileReader : StreamReader
    {
        public CsvFileReader(Stream stream)
            : base(stream)
        {
        }

        public CsvFileReader(string filename)
            : base(filename)
        {
        }

        /// <summary>
        /// Reads a row of data from a CSV file
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool ReadRow(CsvRow row)
        {
            row.LineText = ReadLine();
            if (String.IsNullOrEmpty(row.LineText))
                return false;

            int pos = 0;
            int rows = 0;

            while (pos < row.LineText.Length)
            {
                string value;

                // Special handling for quoted field
                if (row.LineText[pos] == '"')
                {
                    // Skip initial quote
                    pos++;

                    // Parse quoted value
                    int start = pos;
                    while (pos < row.LineText.Length)
                    {
                        // Test for quote character
                        if (row.LineText[pos] == '"')
                        {
                            // Found one
                            pos++;

                            // If two quotes together, keep one
                            // Otherwise, indicates end of value
                            if (pos >= row.LineText.Length || row.LineText[pos] != '"')
                            {
                                pos--;
                                break;
                            }
                        }
                        pos++;
                    }
                    value = row.LineText.Substring(start, pos - start);
                    value = value.Replace("\"\"", "\"");
                }
                else
                {
                    // Parse unquoted value
                    int start = pos;
                    while (pos < row.LineText.Length && row.LineText[pos] != ',')
                        pos++;
                    value = row.LineText.Substring(start, pos - start);
                }

                // Add field to list
                if (rows < row.Count)
                    row[rows] = value;
                else
                    row.Add(value);
                rows++;

                // Eat up to and including next comma
                while (pos < row.LineText.Length && row.LineText[pos] != ',')
                    pos++;
                if (pos < row.LineText.Length)
                    pos++;
            }
            // Delete any unused items
            while (row.Count > rows)
                row.RemoveAt(rows);

            // Return true if any columns read
            return (row.Count > 0);
        }
    }
}