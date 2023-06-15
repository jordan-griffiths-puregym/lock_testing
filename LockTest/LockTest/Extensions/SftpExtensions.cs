using System.Globalization;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using WinSCP;

public static class SftpExtensions
{
    public static void ConnectSftp(this Session session, SessionOptions sessionOptions)
    {
        Console.WriteLine("Connecting to SFTP...");
        session.Open(sessionOptions);
        Console.WriteLine($"Connected");
    }

    public static void DeleteLockFiles(this Session session, string remoteLockFilesPath, List<LockFileInfo> remoteLockFiles)
    {
        remoteLockFiles.ToList().ForEach(file =>
        {
            bool result = DeleteLockFile(session, $"{remoteLockFilesPath}/{file.FileName}");
            if (!result)
                throw new Exception("Could not delete lock file");            
        });
    }

    public static bool WriteReconFile(this Session session, TransferOptions transferOptions, string tempFileName, string remotePath, int rows)
    {
        new FileWritingService().WriteContentToFileToTransfer(tempFileName, rows);

        Console.WriteLine($"Writing recon file from local: {tempFileName} to remote: {remotePath}");

        TransferOperationResult result = session.PutFiles(tempFileName, remotePath, false, transferOptions);
        if (result.IsSuccess)
            Console.WriteLine("Recon file wrote");        

        return result.IsSuccess;

    }

    public static void WriteLockFile(this Session session, string localLockFilePath, string remoteLockFilePath)
    {
        Console.WriteLine($"Writing lock file to {remoteLockFilePath}...");

        File.WriteAllText(localLockFilePath, string.Empty);

        session.PutFiles(localLockFilePath, remoteLockFilePath);

        Console.WriteLine("Wrote lock file");
    }


    public static List<LockFileInfo> GetExistingLockFiles(this Session session, string remoteLockFilesDirectory, string localDirectory)
    {
        Console.WriteLine($"Checking for existing lock file @ {remoteLockFilesDirectory}...");

        List<LockFileInfo> files = session.GetFiles(remoteLockFilesDirectory, localDirectory).Transfers
           .Select(file =>
           {
               DateTime dt = new DateTime();

               string fileName = new DirectoryInfo(file.FileName).Name;

               Console.WriteLine($"Lock file exists @ {remoteLockFilesDirectory}/{fileName}");

               DateTime.TryParseExact(new Regex("(.+?)(\\.[^.]*$|$)").Match(fileName).Groups[1].Value, "dd-MM-yyyyTHH-mm-ss", null, DateTimeStyles.None, out dt);

               return new LockFileInfo(fileName, dt);
           })
           .Where(lockFileInfo => !string.IsNullOrEmpty(lockFileInfo.FileName))
           .ToList();

        if (files.Count > 0)
            Console.WriteLine("Lock file currently exists");
        else
            Console.WriteLine("No lock file currently exists");

        return files;
    }

    public static bool DeleteLockFile(this Session session, string remoteLockFilesPath)
    {
        Console.WriteLine($"Deleting lock file @ {remoteLockFilesPath}...");

        bool result = session.RemoveFile(remoteLockFilesPath).Error == null;      
        if (result)
        {
            Console.WriteLine($"Lock file deleted");
            return true; 
        }

        return false;
    } 
}
