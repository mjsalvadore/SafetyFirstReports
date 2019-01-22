using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using Geotab.Checkmate.ObjectModel.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SFTPupload
{

    /// <summary>
    ///  Organizes the data freom GeotabFeed into readable csv files and stores them at "path"
    /// </summary>
    class Reports
    {

        //init
        private Coordinate coor;
        private Account account;
        private Feed dataFeed;
        private LogRecord logRecord;
        private ExceptionEvent exceptionEvent;
        private StatusData statusData;
        private IDictionary<LogRecord, ExceptionEvent> PairEE;
        private IDictionary<LogRecord, StatusData> PairSD;
        private IList<String> filecontent;
        private IDictionary<Device, TimeSpan> SpeedCulmination;
        private TimeSpan speedingTime;
        private Device device;
        private double firstFuel;
        private double secondFuel;

        public Reports(Account account)
        {
            SpeedCulmination = new Dictionary<Device, TimeSpan>();
            filecontent = new List<String>();
            PairSD = new Dictionary<LogRecord, StatusData>();
            PairEE = new Dictionary<LogRecord, ExceptionEvent>();

            this.account = account;
            dataFeed = new Feed(account.userName, account.password, account.databaseName);

            path = Path.Combine(Directory.GetCurrentDirectory(), @"\Reports");
            Console.WriteLine("Creating files @ path {0}", path);

            Directory.CreateDirectory(path);
        }

        public Reports(Account account, String path)
        {
            this.path = path;
            this.account = account;
            dataFeed = new Feed(account.userName, account.password, account.databaseName);
        }

        public void SpeedingReport(DateTime fromDate)
        {
            speedingTime = new TimeSpan(account.speedingDuration * 10000000);
            filecontent.Clear();
            filecontent.Add("Time Triggered,TIME ZONE, Vehicle, Location, Weather, Marker, Severity, Speed, Posted Limit, Duration");
            PairEE = dataFeed.PairedExceptionEvents(fromDate);


            //Removes all non speeding exceptions
            foreach(var e in PairEE.ToList())
            {
                if (e.Value.Rule.ToString() != "RulePostedSpeedingId")
                    PairEE.Remove(e);
            }

            //iterates through the exceptionEvent and logRecord dictionary
            for (int i = 0; i < PairEE.Count; i++)
            {
                //Gets instance of LogRecord, ExceptionEvent, Location, and Device for this element in Dictionary
                logRecord = PairEE.ElementAt(i).Key;
                device = dataFeed.devices.ElementAt(dataFeed.devices.IndexOf(logRecord.Device));
                exceptionEvent = PairEE.ElementAt(i).Value;
                coor = new Coordinate(logRecord.Longitude, logRecord.Latitude);
                
                //If the device isnt currently being documented in the SpeedCulmination and adding speeding duration, add to SpeedCulmination
                if (!SpeedCulmination.Keys.Contains(device))
                {
                    SpeedCulmination.Add(device, exceptionEvent.Duration.Value);
                }

                //If the device IS in SpeedCulmination, then add duration to device in SpeedCulmination
                else
                {
                    SpeedCulmination[device] = SpeedCulmination[device].Add(exceptionEvent.Duration.Value);
                }
                
                //If the total speeding duration for the device is greater than or equal to the set maximum speeding time then it will create a report and remove it from the list
                if (SpeedCulmination[device] >= speedingTime) {
                    
                    //creates report
                    filecontent.Add("\"" + exceptionEvent.ActiveFrom.Value.AddHours(-4) + "\",\""
                    + dataFeed.GetTimeZone(new Google.Maps.LatLng(logRecord.Latitude, logRecord.Longitude)) + "\",\"" + device.SerialNumber + "\",\"" + dataFeed.GetAddress(coor) + "\",\"" + dataFeed.GetWeather(logRecord.Latitude, logRecord.Longitude) + "\",\"\",\"\",\"" + (int)(logRecord.Speed * 0.621371) + "\",\""
                    + dataFeed.GetPostedSpeedLimit(exceptionEvent.ActiveFrom, exceptionEvent.ActiveTo, device.Id) + "\",\"" + SpeedCulmination[logRecord.Device] + "\"");

                    //removes device from list
                    SpeedCulmination.Remove(device);
                }
            }

            WriteToFile(DateTime.Now.ToString("yyyy-dd-M_HH-mm-ss") + "_SpeedingReport");

        }

        public void GasReport(DateTime fromDate)
        {
            firstFuel = 0;
            secondFuel = 0;
            filecontent.Clear();

            filecontent.Add("Time Triggered, Location, Vehicle Id, Amount Filled");

            PairSD = dataFeed.PairedStatusData(KnownId.DiagnosticFuelLevelId, fromDate);
            
            foreach (var s in PairSD)
            {
                device = dataFeed.devices.ElementAt(dataFeed.devices.IndexOf(logRecord.Device));

                if (!s.Equals(PairSD.First()))
                {
                    firstFuel = s.Value.Data.Value * 0.264172;
                    secondFuel = PairSD.ElementAt(PairSD.Values.ToList().IndexOf(s.Value) - 1).Value.Data.Value * 0.264172;
                }

                //if the next instance of fuel data shows that the gas tank is filled by one gallon more
                if (secondFuel < firstFuel - 2)
                {
                    filecontent.Add("\"" + s.Value.DateTime.Value.AddHours(-4) + "\",\"" + dataFeed.GetAddress(new Coordinate(s.Key.Longitude, s.Key.Latitude)) + "\",\"" + device.SerialNumber + "\",\"" + (firstFuel - secondFuel) + "\"");
                }
            }

            WriteToFile(DateTime.Now.ToString("yyyy-dd-M_HH-mm-ss") + "_GasReport");

        }

        public void MileageReport(DateTime fromDate)
        {
            filecontent.Clear();

            filecontent.Add("Vehicle Id, Mileage");

            foreach (Device d in dataFeed.devices)
            {
                filecontent.Add("\"" + d.SerialNumber + "\",\"" + dataFeed.GetMileage(d, fromDate) + "\"");
            }

            WriteToFile(DateTime.Now.ToString("yyyy-dd-M_HH-mm-ss") + "_MileageReport");

        }

        public void PositiveRecognitionReport(DateTime fromDate)
        {

        }

        private void WriteToFile(String filename)
        {
            try
            {
                Console.WriteLine("Writing " + filename + " to path {0}", path);
                File.WriteAllLines(path + @"\" + filename + ".csv", filecontent);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                path = Path.Combine(Directory.GetCurrentDirectory(), @"\Reports");
                Console.WriteLine("ERROR: Path invalid. Now defaulting to {0}", path);
                Directory.CreateDirectory(path);
            }
        }

        public String path { get; set; }
    }
}
