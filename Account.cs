using System.Collections.Generic;

namespace SFTPupload
{
    /// <summary>
    /// Stores login information for Geotab servers and SFTP server. Each instance represents a seperate company.
    /// </summary>
    class Account
    {
        private Reports report;

        public Account(string userName, string password, string databaseName, string companyName)
        {
            drivers = new List<Driver>();
            this.userName = userName;
            this.password = password;
            this.databaseName = databaseName;
            this.companyName = companyName;
            speedingDuration = 60;
            System.Console.WriteLine("New Account Created:\n" + toString());
        }

        public void Upload(string hostName, string username, string password, string sshHostKeyFingerprint)
        {
            report = new Reports(this);

            report.SpeedingReport(System.DateTime.UtcNow.AddDays(-1));
            report.MileageReport(System.DateTime.UtcNow.AddDays(-1));
            report.GasReport(System.DateTime.UtcNow.AddDays(-1));

            new Server(hostName, username, password, sshHostKeyFingerprint, report.path);
        }

        public void addDriver(Driver driver) { drivers.Add(driver); }

        public void removeDriver(Driver driver) { drivers.Remove(driver); }

        public IList<Driver> drivers { get; }

        public string userName { get; set; }

        public string password { get; set; }

        public string databaseName { get; set; }

        public string companyName { get; set; }

        public int speedingDuration { get; set; }
      
        public string toString()
        {
            return "username: " + userName
               + "\npassword: " + password
               + "\nDatabase Name: " + databaseName
               + "\nCompany Name: " + companyName;
        }
    }
}
