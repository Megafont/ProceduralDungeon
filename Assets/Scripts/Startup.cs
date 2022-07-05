using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ToolboxLib_Shared.Diagnostics;
using ToolboxLib_Shared.Diagnostics.Logging;
using ToolboxLib_Shared.Diagnostics.Logging.Loggers;

using ProceduralDungeon.InGame.DevConsoleCommands;


public class Startup : MonoBehaviour
{

    private void Awake()
    {
        // Keep this object around through scene changes so its not destroyed.
        // If it were destroyed, the dev console and anything thing else on this object would be destroyed and no longer work!
        DontDestroyOnLoad(this);

        InitLogging();
    }


    // Use this for initialization
    void Start()
    {

    }



    // Update is called once per frame
    void Update()
    {

    }






    /// <summary>
    /// This event method is a place where NetPeer subclasses should initialize their loggers.
    /// </summary>
    /// <remarks>
    /// \note This method is called automatically just before the UnityAwake() method is called.
    /// </remarks>
    private void InitLogging()
    {
        // Enable logging.
        LogManager.EnableLogging = true;



        // Define a Unity Console logger.
        LogManager.DefineLogger(typeof(UnityConsoleLogger), new string[] { "[MASTER]" });


        // Setup the developer console before we call CreateLoggers() so that messages generated in that method will appear in the developer console.
        InitDevConsole();


        // Define a file logger.
        /*
        FileLoggerSettings settings = new FileLoggerSettings();
        settings.ClearLogFileOnStart = true;
        settings.LogFileName = "Testbed.log";
        settings.LogDirectory = Application.dataPath + "/";
        LogManager.DefineLogger(typeof(TextFileLogger),
                                new string[] { "[MASTER]" },
                                MessageStampFlags.TimeStampUtc | MessageStampFlags.MessageTypeStamp,
                                settings);
        */


        // Define a developer console logger.
        // REMEMBER: Don't enable time stamps on this logger. They will be ignored since the dev console has built-in time stamp functionality that can be toggled with a command in the console.
        LogManager.DefineLogger(typeof(DevConsoleLogger), new string[] { "[MASTER]" }, MessageStampFlags.UseNoMessageStamps);


        // Create the loggers.
        LogManager.CreateLoggers();

    }



    /// <summary>
    /// This method initializes the developer console.
    /// </summary>
    private void InitDevConsole()
    {
        // Instantiate an instance of the DeveloperConsole prefab.
        GameObject g = (GameObject)Instantiate(Resources.Load("ToolboxLib_Shared/Prefabs/DevConsole/DeveloperConsole"));
        g.name = "DeveloperConsole";

        // Make the Developer Console's GameObject a child of this GameObject;
        g.transform.parent = transform;


        DevConsoleCommands.InitDevConsoleCommands();
    }

}
