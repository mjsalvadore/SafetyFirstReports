/*
 * @author Maxwell Salvadore
 * @version 1.0
 */

using System;
using System.Collections.Generic;
using System.IO;
using Geotab.Checkmate.ObjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SFTPupload
{
    class main
    {

        static void Main(string[] args)
        {

            //path = Path.Combine(Directory.GetCurrentDirectory(), @"\ACCOUNT_DATA");
            //Directory.CreateDirectory(path); 

          //  load();



            //Console.WriteLine(accounts.Count);

            //Console.ReadLine();

            String userName = "mjsalvadore@comcast.net";
            String password = "1234$RFV";
            String databaseName = "safetyfirst_demo_site";
            String companyName = "SafetyFirst";
            Feed feed;
            Device device;
            Account acc;
            Driver driver;
            Vehicle vehicle;

            feed = new Feed(userName, password, databaseName);
            device = feed.devices.Find(x => x.Id.ToString() == "b5D");



            acc = new Account(userName, password, databaseName, companyName);

            driver = new Driver("Maxwell Salvadore");

            vehicle = new Vehicle(device, feed);

            

            driver.vehicle = vehicle;

            acc.addDriver(driver);

            accounts.Add(acc);

            






            Console.WriteLine("UPLOADING FILES TO SERVER");
            accounts[0].Upload("47.22.67.26", "sf_geo_tab_sftp", "GeotabSF@Max", "ssh-rsa 1024 cf:0e:b8:b4:4e:cc:fc:f8:58:10:ff:28:b7:e3:13:ae");

           // save();

        }

        private static void save()
        {


            Console.WriteLine("Serializing...");
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.TypeNameHandling = TypeNameHandling.Auto;
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(path + @"\" + filename + ".txt"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, accounts, typeof(Account));
            }
        }

        private static void load()
        {
            Console.WriteLine("Deserializing...\n" + path);

            try
            {
                accounts = JsonConvert.DeserializeObject<IList<Account>>(File.ReadAllText(path + @"\" + filename + ".txt"), new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    NullValueHandling = NullValueHandling.Include,
                });
            }
            catch(FileNotFoundException e)
            {
                Console.WriteLine("HIT EXCEPTION");
            }

        }

        private static IList<Account> accounts { get; set; } = new List<Account>();

        private static string path { get; set; }

        private static string filename { get; } = "ACCOUNT_DATA";

    }
}