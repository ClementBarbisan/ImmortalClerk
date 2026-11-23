using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Starts the local Forge server (FLUX.1 schnell image generation) when entering Play mode,
/// if it isn't already running. It keeps running after Play mode ends, so later sessions don't wait
/// for the model to load again. Settings are per machine (EditorPrefs), under Tools > Image Server.
/// </summary>
[InitializeOnLoad]
public static class ImageServerLauncher
{
    private const string AutoLaunchKey = "ImmortalClerk.Forge.AutoLaunch";
    private const string FolderKey = "ImmortalClerk.Forge.Folder";
    private const string ProcessIdKey = "ImmortalClerk.Forge.ProcessId";
    private const string MenuAutoLaunch = "Tools/Image Server/Launch On Play";
    private const string Host = "127.0.0.1";
    private const int Port = 7860;

    static ImageServerLauncher()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static bool AutoLaunch
    {
        get => EditorPrefs.GetBool(AutoLaunchKey, true);
        set => EditorPrefs.SetBool(AutoLaunchKey, value);
    }

    /// <summary>Forge folder, the one holding run.bat. Defaults to "forge" next to Assets.</summary>
    private static string Folder
    {
        get => EditorPrefs.GetString(FolderKey,
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "forge")));
        set => EditorPrefs.SetString(FolderKey, value);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode && AutoLaunch)
            StartServer();
    }

    [MenuItem("Tools/Image Server/Start Server")]
    public static void StartServer()
    {
        if (IsServerListening() || IsLaunchedServerAlive())
            return;

        string runBat = Path.Combine(Folder, "run.bat");
        if (!File.Exists(runBat))
        {
            Debug.LogWarning("[ImageServer] No run.bat in " + Folder +
                             ". Set the Forge folder with Tools > Image Server > Set Forge Folder.");
            return;
        }

        try
        {
            // Forge's scripts call each other by bare name, which fails when this variable is set.
            Process process = Process.Start(new ProcessStartInfo("cmd.exe",
                "/c set \"NoDefaultCurrentDirectoryInExePath=\" && run.bat")
            {
                WorkingDirectory = Folder,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized
            });
            if (process != null)
                SessionState.SetInt(ProcessIdKey, process.Id);
            Debug.Log("[ImageServer] Starting Forge from " + Folder +
                      ". It takes a minute; tech images are generated once it's up.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ImageServer] Couldn't start " + runBat + ": " + e.Message);
        }
    }

    [MenuItem("Tools/Image Server/Open Forge")]
    private static void OpenInBrowser()
    {
        Application.OpenURL("http://" + Host + ":" + Port);
    }

    [MenuItem(MenuAutoLaunch)]
    private static void ToggleAutoLaunch()
    {
        AutoLaunch = !AutoLaunch;
    }

    [MenuItem(MenuAutoLaunch, true)]
    private static bool ToggleAutoLaunchValidate()
    {
        Menu.SetChecked(MenuAutoLaunch, AutoLaunch);
        return true;
    }

    [MenuItem("Tools/Image Server/Set Forge Folder...")]
    private static void SetFolder()
    {
        string folder = EditorUtility.OpenFolderPanel("Forge folder (the one with run.bat)", Folder, "");
        if (!string.IsNullOrEmpty(folder))
            Folder = folder;
    }

    private static bool IsServerListening()
    {
        try
        {
            using (TcpClient client = new TcpClient())
            {
                return client.ConnectAsync(Host, Port).Wait(300) && client.Connected;
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// True while a server this editor launched is still starting up (its port isn't open yet).
    /// Stored in SessionState because entering Play mode reloads the scripts.
    /// </summary>
    private static bool IsLaunchedServerAlive()
    {
        int id = SessionState.GetInt(ProcessIdKey, 0);
        if (id == 0)
            return false;
        try
        {
            return !Process.GetProcessById(id).HasExited;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
