
namespace SFTPupload
{
    class Driver
    {
        public Driver(string driverName)
        {
            this.driverName = driverName;
        }

        public string driverName { get; set; }

        public int preformanceScore() => CalculatePS();
        
        public float mileageScore() => (vehicle.mileage() / vehicle.avgMileage()) * 100;

        public Vehicle vehicle { get; set; }

        private int CalculatePS()
        {
            return 0;
        }

    }
}
