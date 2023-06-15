using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using WinSCP;

internal class LockTest
{
    private static void Main(string[] args)
    {
        SessionOptions sessionOptions = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false)
            .AddUserSecrets("winscp-lock-testing")
            .Build()
            .GetSection("winscp")
            .Get<SessionOptions>();

        using (Session session = new Session())
        {
            session.ConnectSftp(sessionOptions);

            int lockExpiryMins = 60;

            string reconFileName = "2023-14-06_PGSA.csv";
            string lockFileName = $"{DateTime.Now.ToString("dd-MM-yyyyTHH-mm-ss")}.lck";

            string remoteLockFilesDirName = "file_lock_tests";
            string remoteLockFilesPath = $"/{remoteLockFilesDirName}/locks";
            string remoteReconFilesPath = $"/{remoteLockFilesDirName}/writes";
            string remoteReconFilePath = $"{remoteReconFilesPath}/{reconFileName}";
            string remoteLockFilePath = $"{remoteLockFilesPath}/{lockFileName}";

            string localReconFilesPath = "C:\\fv";
            string localDownloadsPath = "downloads";
            string localLockFilesPath = $"{localReconFilesPath}\\locks";
            string localReconFilePath = $"{localReconFilesPath}\\{reconFileName}";
            string localLockFilePath = $"{localLockFilesPath}\\{lockFileName}";

            //1  Get all lock files
            //2    If no lock files exist then skip to step 8
            //3  Get the latest lock file - call this latestLockFile
            //4  Delete all lock files that aren't latestLockFile
            //5  Check is latestLockFile stale (over x mins old)
            //6    If yes then delete it
            //7    If no then throw HyveLockException and quit
            //8  Write new lock file
            //9  Write recon file
            //10 Delete lock file

            try
            {
                List<LockFileInfo> remoteLockFiles = session.GetExistingLockFiles(remoteLockFilesPath, $"{localReconFilesPath}\\{localDownloadsPath}");
                if (remoteLockFiles.Count > 0)
                {
                    LockFileInfo? latestRemoteLockedFile = remoteLockFiles.OrderByDescending(x => x.Timestamp).FirstOrDefault();
                    if (latestRemoteLockedFile == null)
                        throw new Exception("Could not get latest lock file even though it appears to exist");

                    List<LockFileInfo> locksToDelete = remoteLockFiles.Where(file => file.FileName != latestRemoteLockedFile.FileName).ToList();
                    if (locksToDelete.Count > 0)
                        session.DeleteLockFiles(remoteLockFilesPath, locksToDelete);

                    bool lockFileStale = IsLockFileStale(latestRemoteLockedFile.FileName, lockExpiryMins);
                    if (lockFileStale)
                    {
                        bool deleteResult = session.DeleteLockFile($"{remoteLockFilesPath}/{latestRemoteLockedFile.FileName}");
                        if (deleteResult == false)
                            throw new Exception($"Couldn't delete stale lock file");

                        session.WriteLockFile(localLockFilePath, remoteLockFilePath);
                    }
                    else
                        throw new HyveFileLockedException($"Cannot write. Valid lock file currently exists");
                }
                else
                    session.WriteLockFile(localLockFilePath, remoteLockFilePath);

                bool reconWriteResult = session.WriteReconFile(new TransferOptions
                {
                    TransferMode = TransferMode.Ascii,
                    OverwriteMode = OverwriteMode.Overwrite
                }, localReconFilePath, remoteReconFilePath, 500);

                session.DeleteLockFile($"{remoteLockFilesPath}/{lockFileName}"); 
                
                if (reconWriteResult == false)
                    throw new Exception("Could not write SFTP recon file. Deleted lock so other instances can retry");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error occured: {ex.Message}. Exiting");

                Environment.Exit(-1);
            }                              

            Console.WriteLine("All files wrote. Execution completed successfully. Lock deleted");
        }
    }

    private static bool IsLockFileStale(string localLockFilePath, int lockExpiryMins)
    {
        DateTime dt = new DateTime();

        DateTime.TryParseExact(new Regex("(.+?)(\\.[^.]*$|$)").Match(localLockFilePath).Groups[1].Value, "dd-MM-yyyyTHH-mm-ss", null, DateTimeStyles.None, out dt);
        if (dt == DateTime.MinValue)
            throw new Exception($"The lock file: \"{localLockFilePath}\" does not have the correct filename format: <dd-mm-yyyyThh-mm-ss>.*");

        return DateTime.Now.Subtract(dt) > TimeSpan.FromMinutes(lockExpiryMins);
    }   
}