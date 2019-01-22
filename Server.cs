using System;
using WinSCP;

namespace SFTPupload
{
    /// <summary>
    /// Stores information about the SFTP server that reports are uploaded to and establishes connection to said server.
    /// </summary>
    class Server
    {
        //init
        private SessionOptions sessionOptions;
        private TransferOptions tO;
        private TransferOperationResult tR;

        public Server(String HostName, String UserName, String Password, String SshHostKeyFingerprint, String path)
        {
            this.HostName = HostName;
            this.UserName = UserName;
            this.Password = Password;
            this.SshHostKeyFingerprint = SshHostKeyFingerprint;

            Upload(path);
        }

        private int Upload(String path)
        {
            try
            {
                //SFTP Server Settings
                sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Sftp,
                    HostName = this.HostName,
                    UserName = this.UserName,
                    Password = this.Password,
                    SshHostKeyFingerprint = this.SshHostKeyFingerprint
                };

                using (Session s = new Session())
                {
                    Console.WriteLine("Establishing connection");
                    s.Open(sessionOptions);

                    //Upload


                    tO = new TransferOptions();
                    tO.TransferMode = TransferMode.Binary;

                    //File Location
                    Console.WriteLine("Pulling from directory {0}", path);
                    tR = s.PutFiles(path + @"\*", "/", true, tO);

                    //error
                    tR.Check();

                    //print out
                    foreach(TransferEventArgs t in tR.Transfers)
                    {
                        Console.WriteLine("Upload {0} SUCCESS\n", t.FileName);
                    }

                }

                return 0;
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: {0}", e);
                return 1;
            }
        }

        public String HostName { get; }
    
        public String UserName { get; }
     
        public String Password { get; }
     
        public String SshHostKeyFingerprint { get; }
   
    }
}
