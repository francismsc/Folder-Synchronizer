using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

class FolderSynchronizer
{ 
    //args format sourcefolderPath , replicaFolderPath, logsFolderPath, intervaltime (seconds)
    static void Main(string[] args)
    {

        if (args.Length != 4)
        {
            Console.WriteLine("Please input a command in this format: <sourceFolderPath> <replicaFolderPath> <logsFolderPath> <Synchronization Interval (seconds)>");
            return;
        }

        string sourceFolderPath = args[0];
        string replicaFolderPath = args[1];
        string logsFolderPath = args[2];
        int syncInterval;

        if (!Int32.TryParse(args[3], out syncInterval) )
        {
            Console.WriteLine($"{args[3]} is not a valid number. Please input a positive number");
            return;
        }

        int syncIntervalSeconds = syncInterval * 1000;

        Console.WriteLine($"Synchronization started. Source: {sourceFolderPath}, Replica: {replicaFolderPath}, Logs: {logsFolderPath}, Sync Interval: {syncInterval} seconds");
        
        try
        {
            while (true)
            {
                SynchronizeFolders(sourceFolderPath, replicaFolderPath, logsFolderPath);
                Thread.Sleep(syncIntervalSeconds);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");

        }
    }

    static void SynchronizeFolders(string sourceFolderPath, string replicaFolderPath, string logsFolderPath)
    {
        try
        {
            string logmessage = $"{DateTime.Now} - Started Synchronization.\n";
            Console.WriteLine (logmessage);
            //CreateLog(logsFolderPath, logmessage);

            SyncFiles(sourceFolderPath, replicaFolderPath, logsFolderPath);

            logmessage = $"{DateTime.Now} - Sucessful Synchronization.\n";
            Console.WriteLine(logmessage);
            //CreateLog(logsFolderPath, logmessage);
        }
        catch (Exception ex)
        {
            string failedSyncMessage = $"{DateTime.Now} - Error:{ex.Message} Synchronization was not completed.\n";
            Console.WriteLine(failedSyncMessage);
            //CreateLog(logsFolderPath, failedSyncMessage);
        }
    }

    static void SyncFiles(string sourceFolder, string replicaFolder, string logsFolder)
    {
        HashSet<string> replicaFolderFiles = new HashSet<string>(Directory.GetFiles(replicaFolder));
        HashSet<string> replicaFolderSubdirectoriesFiles = new HashSet<string>(Directory.GetDirectories(replicaFolder));

        //Copy files to replica from source
        foreach(string filePath in Directory.GetFiles(sourceFolder))
        {
            string filename = Path.GetFileName(filePath);
            string destinationPath = Path.Combine(replicaFolder, filename);

            if (!File.Exists(destinationPath) || !AreFilesEqual(filePath, destinationPath)) 
            {

                File.Copy(filePath, destinationPath, true);

                string logMessage = $"{DateTime.Now} - Copied file {filename} to {replicaFolder}\n";
                CreateLog(logsFolder, logMessage);
                Console.WriteLine(logMessage);
                replicaFolderFiles.Remove(destinationPath);
            }else
            {
                replicaFolderFiles.Remove(destinationPath);
            }
           
        }

        //Delete files in replica that don't exist in source
        foreach(string fileToDelete in replicaFolderFiles)
        {
            File.Delete(fileToDelete);
            string logMessage = $"{DateTime.Now} - Deleted file {Path.GetFileName(fileToDelete)} in {replicaFolder}\n";
            CreateLog(logsFolder, logMessage);
            Console.WriteLine(logMessage);
            
        }

        //Recursion to enter folders and copy them to replica folder
        foreach(string subDirectoryPath in Directory.GetDirectories(sourceFolder))
        {
            string subDirectoryName = Path.GetFileName(subDirectoryPath);
            string destinationPath = Path.Combine(replicaFolder, subDirectoryName);

            if(!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);

                string logMessage = $"{DateTime.Now} - Copied Folder {subDirectoryName} to {replicaFolder}\n";
                CreateLog(logsFolder, logMessage);
                Console.WriteLine(logMessage);
            }

            SyncFiles(subDirectoryPath, destinationPath, logsFolder);

            replicaFolderSubdirectoriesFiles.Remove(destinationPath);

        }
        //Delete folders in replica that don't exist in source
        foreach(string subDirectoryToDelete in  replicaFolderSubdirectoriesFiles)
        {
            Directory.Delete(subDirectoryToDelete);
            string logMessage = $"{DateTime.Now} - Deleted Folder {Path.GetFileName(subDirectoryToDelete)} in {replicaFolder}\n";
            CreateLog(logsFolder, logMessage);
            Console.WriteLine(logMessage);
        }

    }

    static void CreateLog(string logsFolder, string message)
    {
        string logFilePath = Path.Combine(logsFolder, "syncLog.txt");
        try
        {
            if(!File.Exists(logFilePath))
            {
                using (StreamWriter sw = File.CreateText(logFilePath))
                {
                    sw.WriteLine("Log file created on: " + DateTime.Now);
                }
            }

            File.AppendAllText(logFilePath, message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing log file: {ex.Message}");
        }
    }

    static bool AreFilesEqual(string filePath1, string filePath2)
    {
        using (MD5 md5 = MD5.Create())
        {
            using (FileStream stream1 = File.OpenRead(filePath1))
            using (FileStream stream2 = File.OpenRead(filePath2))
            {
                byte[] hash1 = md5.ComputeHash(stream1);
                byte[] hash2 = md5.ComputeHash(stream2);

                return StructuralComparisons.StructuralEqualityComparer.Equals(hash1, hash2);
            }
        }
    }

}