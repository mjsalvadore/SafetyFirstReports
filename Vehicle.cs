using Geotab.Checkmate.ObjectModel;
using System;

namespace SFTPupload
{
    class Vehicle
    {

        public Vehicle(Device device, Feed feed)
        {
            mileageCount = 0;
            this.device = device;
            this.feed = feed;
            lastAvgMileage = mileage();
        }

        private static Account account { get; }
        
        private int mileageCount { get; set; }

        public Feed feed { get; set; }

        public Device device { get; }

        public float mileage()
        {
            /*
            try
            {
                return feed.GetMileage(device, System.DateTime.UtcNow.AddDays(-1));
            }
            catch(NullReferenceException e)
            {
                Console.WriteLine(e.HResult);
                
            }*/
            return feed.GetMileage(device, System.DateTime.UtcNow.AddDays(-1));

        }

        private float lastAvgMileage { get; set; }

        public float avgMileage()
        {
            mileageCount++;
            return ((mileageCount * lastAvgMileage) + mileage()) / (mileageCount + 1);
        }

        public void resetMileage()
        {
            lastAvgMileage = mileage();
            mileageCount = 0;
        }


    }
}