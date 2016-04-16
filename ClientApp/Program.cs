using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using StockApp;
using System.Threading.Tasks;
// Add using statements to access AWS SDK for .NET services. 
// Both the Service and its Model namespace need to be added 
// in order to gain access to a service. For example, to access
// the EC2 service, add:
// using Amazon.EC2;
// using Amazon.EC2.Model;

namespace ClientApp
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "Client";
            Console.WriteLine("*********************  Welcome to Stock Summarizer ***************************\n");
            Console.WriteLine("Please wait..Server is processing ..\n");           
            GetObject gobj = new GetObject();                   
            Thread.Sleep(5000);
            Console.WriteLine("Please wait..Fetching results from Amazon Web Services and Yahoo Finance..\n");
            Thread.Sleep(5000);
            string s = "";
            s = gobj.GetOnChangePerc();                    
            Console.WriteLine(s);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}