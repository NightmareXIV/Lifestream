using ECommons.EzIpcManager;

namespace Lifestream.IPC;
public class TextAdvanceIPC
{
    [EzIPC("EnqueueMoveTo2DPoint")] public Action<MoveData, float> EnqueueMoveTo2DPoint;
    [EzIPC("EnqueueMoveTo3DPoint")] public Action<MoveData, float> EnqueueMoveTo3DPoint;
    [EzIPC("Stop")] public Action Stop;
    [EzIPC("IsBusy")] public Func<bool> IsBusy;
    /// <summary>
    /// Enables external control of TextAdvance. 
    /// First argument = your plugin's name. 
    /// Second argument is options. Copy ExternalTerritoryConfig to your plugin. Configure it as you wish: set "null" values to features that you want to keep as configured by user. Set "true" or "false" to forcefully enable or disable feature. 
    /// Returns whether external control successfully enabled or not. When already in external control, it will succeed if called again if plugin name matches with one that already has control and new settings will take effect, otherwise it will fail.
    /// External control completely disables territory-specific settings.
    /// </summary>
    [EzIPC] public Func<string, ExternalTerritoryConfig, bool> EnableExternalControl;
    /// <summary>
    /// Disables external control. Will fail if external control is obtained from other plugin.
    /// </summary>
    [EzIPC] public Func<string, bool> DisableExternalControl;
    /// <summary>
    /// Indicates whether external control is enabled.
    /// </summary>
    [EzIPC] public Func<bool> IsInExternalControl;

    /// <summary>
    /// Indicates whether user has plugin enabled. Respects territory configuration. If in external control, will return true.
    /// </summary>
    [EzIPC] public Func<bool> IsEnabled;
    /// <summary>
    /// Indicates whether plugin is paused by other plugin.
    /// </summary>
    [EzIPC] public Func<bool> IsPaused;

    /// <summary>
    /// All the functions below return currently configured plugin state with respect for territory config and external control. 
    /// However, it does not includes IsEnabled or IsPaused check. A complete check whether TextAdvance is currently ready to process appropriate event will look like: <br></br>
    /// IsEnabled() &amp;&amp; !IsPaused() &amp;&amp; GetEnableQuestAccept()
    /// </summary>
    [EzIPC] public Func<bool> GetEnableQuestAccept;
    [EzIPC] public Func<bool> GetEnableQuestComplete;
    [EzIPC] public Func<bool> GetEnableRewardPick;
    [EzIPC] public Func<bool> GetEnableCutsceneEsc;
    [EzIPC] public Func<bool> GetEnableCutsceneSkipConfirm;
    [EzIPC] public Func<bool> GetEnableRequestHandin;
    [EzIPC] public Func<bool> GetEnableRequestFill;
    [EzIPC] public Func<bool> GetEnableTalkSkip;
    [EzIPC] public Func<bool> GetEnableAutoInteract;

    private TextAdvanceIPC()
    {
        EzIPC.Init(this, "TextAdvance", SafeWrapper.AnyException, reducedLogging: true);
    }

    public class MoveData
    {
        public Vector3 Position;
        public uint DataID;
        public bool NoInteract;
        public bool? Mount = null;
        public bool? Fly = null;
    }

    public class ExternalTerritoryConfig
    {
        public bool? EnableQuestAccept = null;
        public bool? EnableQuestComplete = null;
        public bool? EnableRewardPick = null;
        public bool? EnableRequestHandin = null;
        public bool? EnableCutsceneEsc = null;
        public bool? EnableCutsceneSkipConfirm = null;
        public bool? EnableTalkSkip = null;
        public bool? EnableRequestFill = null;
        public bool? EnableAutoInteract = null;
    }
}
