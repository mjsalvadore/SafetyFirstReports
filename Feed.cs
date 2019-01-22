/*
 * @author Maxwell Salvadore
 * @version 1.3
 */

 ///


 ///

using System;
using System.Collections.Generic;
using System.Linq;
using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using Geotab.Checkmate.ObjectModel.Exceptions;
using Google.Maps;
using Google.Maps.TimeZone;
using WeatherNet.Clients;

namespace SFTPupload
{
    /// <summary>
    /// Establishes connection to geotab servers then pulls and organizes data
    /// </summary

    class Feed
    {
        private API api;
        private List<ReverseGeocodeAddress> dir = new List<ReverseGeocodeAddress>();
        private Dictionary<DateTime, float> roadMaxSpeedDict = new Dictionary<DateTime, float>();
        private IList<LogRecord> logRecords;
        private IList<ExceptionEvent> exceptionEvent;
        private IList<StatusData> statusData, fuelStatus, odoStatus;
        private IDictionary<LogRecord, ExceptionEvent> ee;
        private IDictionary<LogRecord, StatusData> sd;
        private LogRecord logRecord;
        private TimeZoneRequest request;
        private TimeZoneResponse response;
        private int orgIndex;
        private bool reachZero;
        float totalOdo = 0, firstFuel = 0, lastFuel = 0,
                  totalFuel = 0, mileage = 0;
        bool isFilled = true;

        private long? token = null;

        public Feed(String userName, String password, String databaseName)
        {
            WeatherNet.ClientSettings.ApiKey = "d85ac6963da4243594023f181738b663";
            logRecords = new List<LogRecord>();
            api = new API(userName, password, null, databaseName);
            GoogleSigned.AssignAllServices(new GoogleSigned("AIzaSyA0kxBfS2bl3sojxKVIsSoEeOMR7MSv5sM"));
            devices = api.Call<List<Device>>("Get", typeof(Device));
            devices.RemoveAll(t => t.SerialNumber.ToString() == "000-000-0000");
        }
        
        private static LogRecord SearchDateTime(List<LogRecord> logRecord, ExceptionEvent ee)
        {
            var start = logRecord.FindIndex(o => o.DateTime >= ee.ActiveFrom);
            
            for(int i = start; i < logRecord.Count; i++)
            {
                if (logRecord.ElementAt(i).Device.CompareTo(ee.Device) == 0 && logRecord.ElementAt(i).DateTime >= ee.ActiveFrom && logRecord.ElementAt(i).DateTime <= ee.ActiveTo)
                {
                    return logRecord.ElementAt(i);
                }
            }
           
            return null;
        }

        private static LogRecord SearchDateTime(IList<LogRecord> logRecord, StatusData ee)
        {
            foreach (LogRecord lr in logRecord)
            {
                

                if (lr.DateTime >= ee.DateTime.Value.AddSeconds(-5) && lr.DateTime <= ee.DateTime.Value.AddSeconds(+5))
                {
                    return lr;
                }
            }

            return null;
        }


        //Getters
        public List<Device> devices { get; }

        public float GetMileage(Device device, DateTime fromDate)
        {
            totalOdo = 0; firstFuel = 0; lastFuel = 0;
            totalFuel = 0; mileage = 0;

            isFilled = true;

            fuelStatus = StatusData(KnownId.DiagnosticFuelLevelId, fromDate, device);
            odoStatus  = StatusData(KnownId.DiagnosticOdometerAdjustmentId, fromDate, device);
            
            totalOdo = (float)(odoStatus.Last().Data - odoStatus.First().Data);

            foreach (StatusData s in fuelStatus)
            {
                if (isFilled)
                {
                    firstFuel = (float)s.Data;
                    isFilled = false;
                }

                //if the next instance of fuel data shows that the gas tank is filled by one gallon more
                if (s.Equals(fuelStatus.Last()) || fuelStatus.ElementAt(fuelStatus.IndexOf(s) + 1).Data * 0.264172 > s.Data * 0.264172 + 2)
                {
                    lastFuel = (float)s.Data;
                    totalFuel += firstFuel - lastFuel;
                    isFilled = true;
                }
            }

            mileage = (float)((totalOdo / totalFuel) * .002352);

            return mileage;
        }

        public String GetWeather(double lat, double lon) => CurrentWeather.GetByCoordinates(lat, lon).Item.Description.ToUpper();

        public String GetTimeZone(LatLng latlng)
        {
            request = new TimeZoneRequest();
            response = new TimeZoneResponse();

            request.Location = latlng;
            request.Timestamp = DateTime.Now;
            request.Language = "en";

            response = new TimeZoneService().GetResponse(request);
     

            if (response.TimeZoneName.Contains("Eastern"))
                return "EASTERN";
            else if (response.TimeZoneName.Contains("Central"))
                return "CENTRAL";
            else if (response.TimeZoneName.Contains("Mountain"))
                return "MOUNTAIN";
            else if(response.TimeZoneName.Contains("Pacific"))
                return "PACIFIC";
            else if (response.TimeZoneName.Contains("Hawaii"))
                return "HAWAII";
            else if (response.TimeZoneName.Contains("Atlantic"))
                return "ATLANTIC";
            else if (response.TimeZoneName.Contains("Alaska"))
                return "ALASKA";
            else
                return response.TimeZoneName;
        }

        public String GetAddress(Coordinate coor)
        {
            dir = api.Call<List<ReverseGeocodeAddress>>("GetAddresses", new
            {
                coordinates = new Coordinate[] { coor }

            });

            return dir[0].FormattedAddress;
        }

        public int GetPostedSpeedLimit(DateTime? from, DateTime? to, Id deviceID)
        {
            reachZero = false;

            //pulls road speed information
            roadMaxSpeedDict = api.Call<Dictionary<DateTime, float>>("GetRoadMaxSpeeds", new
            {
                deviceSearch = new DeviceSearch { Id = deviceID },
                fromDate = from.Value,
                toDate = to.Value
            });

            // keys equals list of datetimes of roadspeeds
            var keys = new List<DateTime>(roadMaxSpeedDict.Keys);

            //binary search to find index where
            var index = keys.BinarySearch(from.Value);

            if (index < 0)
                index = Math.Abs(index) - 1;
        
            try
            {
                while ((int)(roadMaxSpeedDict.Values.ElementAt(index) * 0.621371) == 0)
                {
                    if (!reachZero)
                        index--;
                    else
                        index++;
                }
            }
            catch (Exception e)
            {
                reachZero = true;
                index = orgIndex;
            }


            if (roadMaxSpeedDict.Count > 0)
            {
               // Console.WriteLine((int)(roadMaxSpeedDict.Values.ElementAt(index) * 0.621371) + 1);
                return (int)(roadMaxSpeedDict.Values.ElementAt(index) * 0.621371)+1;
            }
            else
                return 0;

        }

        public IList<LogRecord> LogRecords(DateTime fromDate)
        {
            Console.WriteLine("Pulling LogRecord Data");
            token = null;

            FeedResult<LogRecord> logRecord = api.Call<FeedResult<LogRecord>>("GetFeed", typeof(LogRecord), new
            {
                fromVersion = token,

                //gives results of past 24 hours
                search = new LogRecordSearch { FromDate = fromDate }
            });

            //stores token for further access
            token = logRecord.ToVersion;

            return logRecord.Data;
        }

        public IList<ExceptionEvent> ExceptionEvents(DateTime fromDate)
        {
            Console.WriteLine("Pulling ExceptionEvent Data");

            token = null;

            FeedResult<ExceptionEvent> exceptionEvent = api.Call<FeedResult<ExceptionEvent>>("GetFeed", typeof(ExceptionEvent), new
            {

                fromVersion = token,

                //gives results of past 24 hours
                search = new ExceptionEventSearch { FromDate = DateTime.UtcNow.AddDays(-1) }
            });

            //stores token for further access
            token = exceptionEvent.ToVersion;

            return exceptionEvent.Data;
        }

        public IDictionary<LogRecord, ExceptionEvent> PairedExceptionEvents(DateTime fromDate)
        {
            Console.WriteLine("Pairing ExceptionEvents to LogRecords");
            logRecords = LogRecords(fromDate);
            exceptionEvent = ExceptionEvents(fromDate);
            ee = new Dictionary<LogRecord, ExceptionEvent>();
            
            for(int i = 0; i < exceptionEvent.Count; i++)
            {
                if (exceptionEvent.ElementAt(i).Rule.ToString()[0] != 'a')
                {
                    logRecord = SearchDateTime((List<LogRecord>)logRecords, exceptionEvent.ElementAt(i));

                    if (logRecord != null)
                    {
                        ee.Add(logRecord, exceptionEvent.ElementAt(i));
                        logRecords.Remove(logRecord);
                    }
                }
            }

            return ee;

        }

        public IList<StatusData> StatusData(Id id, DateTime fromDate)
        {

            Console.WriteLine("Pulling StatusData Data");

            token = null;
            
            IList<StatusData> Status = api.Call<IList<StatusData>>("Get", typeof(StatusData), new
            {
                search = new StatusDataSearch
                {
                    FromDate = fromDate,
                    DiagnosticSearch = new DiagnosticSearch
                    {
                        Id = id
                    }
                }
            });

            Status = Status.OrderBy(x => x.Device).ToList();

            return Status;
        }

        public IList<StatusData> StatusData(Id id, DateTime fromDate, Device device)
        {
            token = null;

            IList<StatusData> Status = api.Call<IList<StatusData>>("Get", typeof(StatusData), new
            {
                
                search = new StatusDataSearch
                {
                    DeviceSearch = new DeviceSearch
                    {
                        Id = device.Id
                    },
                    FromDate = fromDate,
                    DiagnosticSearch = new DiagnosticSearch
                    {
                        Id = id
                    }
                }
            });

            Status = Status.OrderBy(x => x.Device).ToList();

            return Status;
        }

        public IDictionary<LogRecord, StatusData> PairedStatusData(Id id, DateTime fromDate)
        {

            //CHANGE LATER TO STORE STATUS DATA THAT CANT PAIR WITH LOG RECORD
            //TRY TO PAIR WITH OTHER COORDINATE DATA...?

            //maybe just find more reliable info than logrecord

            logRecords = LogRecords(fromDate);
            statusData = StatusData(id, fromDate);
            sd = new Dictionary<LogRecord, StatusData>();

            for (int i = 0; i < statusData.Count; i++)
            {
                logRecord = SearchDateTime(logRecords, statusData.ElementAt(i));

                if(logRecord != null)
                {
                    sd.Add(logRecord, statusData.ElementAt(i));
                    logRecords.Remove(logRecord);
                }

            }

            return sd;

        }

        
    }
}
