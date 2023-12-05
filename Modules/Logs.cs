namespace DojoArenaKT;
using System;
using System.IO;
using System.Text;
using static Cpp2IL.Core.Logging.Logger;

public static class Logs
{
    public static string LogFolder = "";
    public const string CompleteLogFileName = "CompleteLog";
    static string FixedDateTime = "";

    [EventSubscriber(Events.Preload, -9001)] // It's OVER 9000!!!!!!!!!!!
    public static void SetupLogs()
    {
        FixedDateTime = DateTime.Now.ToString("yyyy-MM-dd");
        LogFolder = Main.Folder + "Logs/" + FixedDateTime + "/";
        if (!Directory.Exists(LogFolder)) Directory.CreateDirectory(LogFolder);
    }

    public static void Write(string logMessage, string LogType)
    {
        if (!Directory.Exists(LogFolder)) Directory.CreateDirectory(LogFolder);

        var curDate = DateTime.Now;

        string logEntry = new StringBuilder()
            .Append('[')
            .Append(curDate.ToString("yyyy-MM-dd HH:mm"))
            .Append("] ")
            .Append(logMessage)
            .ToString(); // felt cute, might delete and replace with $-string later


        using (StreamWriter SR = new(LogFolder + LogType + " " + FixedDateTime + ".txt", true))
            SR.WriteLine(logEntry);

        if (LogType != CompleteLogFileName)
        {
            using (StreamWriter SR = new(LogFolder + CompleteLogFileName + " " + FixedDateTime + ".txt", true))
                SR.WriteLine(logEntry);
        }
    }
}