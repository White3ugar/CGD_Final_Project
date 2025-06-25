using Vipenti.Singletons;
using System;
using System.IO;
using UnityEngine;
using Photon.Pun;

public class LogManager : Singleton<LogManager>
{
    string filePath;
    string whiteboardPath;

    private void Start()
    {
        filePath = $"{Application.dataPath}/log{DateTime.UtcNow:MM-dd-yyyy}_{DateTime.Now:HH.mm.ss}.txt";
        whiteboardPath = $"{Application.dataPath}/Whiteboard{DateTime.UtcNow:MM-dd-yyyy}_{DateTime.Now:HH.mm.ss}.txt";

        if (!File.Exists(filePath))
        {
            File.Create(filePath).Close();  // ✅ Immediately close after creating
            Debug.Log("✅ Created log file at " + filePath);
        }

        if (!File.Exists(whiteboardPath))
        {
            File.Create(whiteboardPath).Close();  // ✅ Close it here too
            Debug.Log("✅ Created whiteboard log file at " + whiteboardPath);
        }
    }

    public void LogInfo(string msg)
    {
        string text = $"{DateTime.Now:HH:mm:ss}: {msg}";
        try
        {
            using (StreamWriter writer = File.AppendText(filePath))
            {
                writer.WriteLine(text);
            }
        }
        catch (IOException ex)
        {
            Debug.LogError($"❌ Failed to write to log file: {ex.Message}");
        }
    }

    public void SaveNotes(string msg)
    {
        // Placeholder or implement as needed
    }

    public void LogWhiteboard(string msg)
    {
        try
        {
            using (StreamWriter writer = File.AppendText(whiteboardPath))
            {
                writer.WriteLine(msg);
            }
        }
        catch (IOException ex)
        {
            Debug.LogError($"❌ Failed to write to whiteboard log: {ex.Message}");
        }
    }
}
