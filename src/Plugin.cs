using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LethalLib;
using LethalLib.Modules;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using static LethalLib.Modules.Levels;
using static LethalLib.Modules.Enemies;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;

namespace LethalCompanyDalek;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency(Plugin.ModGUID)]
[BepInDependency("linkoid-DissonanceLagFix-1.0.0", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("mattymatty-AsyncLoggers-1.6.2", BepInDependency.DependencyFlags.SoftDependency)]
public class DalekPlugin : BaseUnityPlugin
{
    public const string ModGuid = $"LCM_Dalek|{ModVersion}";
    private const string ModName = "Lethal Company Dalek Mod";
    private const string ModVersion = "1.0.0";
        
    private readonly Harmony _harmony = new(ModGuid);
        
    // ReSharper disable once InconsistentNaming
    private static readonly ManualLogSource _mls = BepInEx.Logging.Logger.CreateLogSource(ModGuid);

    private static DalekPlugin _instance;

    private static EnemyType _dalekEnemyType;
    
    public static DalekConfig DalekConfig { get; internal set; }
        
    private void Awake()
    {
        if (_instance == null) _instance = this;
            
        InitializeNetworkStuff();

        Assets.PopulateAssetsFromFile();
        if (Assets.MainAssetBundle == null)
        {
            _mls.LogError("MainAssetBundle is null");
            return;
        }
            
        _harmony.PatchAll();
        DalekConfig = new DalekConfig(Config);
        SetupDalekEnemy();
        
        _harmony.PatchAll();
        _harmony.PatchAll(typeof(DalekPlugin));
        _mls.LogInfo($"Plugin {ModName} is loaded!");
    }

    private void SetupDalekEnemy()
    {
        _dalekEnemyType = Assets.MainAssetBundle.LoadAsset<EnemyType>("Dalek");
        
        TerminalNode dalekTerminalNode = Assets.MainAssetBundle.LoadAsset<TerminalNode>("DalekTN");
        TerminalKeyword dalekTerminalKeyword = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("DalekTK");
        
        NetworkPrefabs.RegisterNetworkPrefab(_dalekEnemyType.enemyPrefab);
        Utilities.FixMixerGroups(_dalekEnemyType.enemyPrefab);
        RegisterEnemy(
            _dalekEnemyType,
            999,
            LevelTypes.All,
            SpawnType.Default,
            dalekTerminalNode,
            dalekTerminalKeyword
            );
    }
        
    private static void InitializeNetworkStuff()
    {
        Type[] types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (Type type in types)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (MethodInfo method in methods)
            {
                object[] attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length > 0)
                {
                    method.Invoke(null, null);
                }
            }
        }
    }
}
    
[Serializable]
public class SyncedInstance<T>
{
    internal static CustomMessagingManager MessageManager => NetworkManager.Singleton.CustomMessagingManager;
    internal static bool IsClient => NetworkManager.Singleton.IsClient;
    internal static bool IsHost => NetworkManager.Singleton.IsHost;
        
    [NonSerialized]
    protected static int IntSize = 4;

    public static T Default { get; private set; }
    public static T Instance { get; private set; }

    public static bool Synced { get; internal set; }

    protected void InitInstance(T instance) {
        Default = instance;
        Instance = instance;
            
        IntSize = sizeof(int);
    }
        
    private static void RequestSync() {
        if (!IsClient) return;

        using FastBufferWriter stream = new(IntSize, Allocator.Temp);
        MessageManager.SendNamedMessage($"{DalekPlugin.ModGuid}_OnRequestConfigSync", 0uL, stream);
    }
        
    private static void OnRequestSync(ulong clientId, FastBufferReader _) {
        if (!IsHost) return;

        Debug.Log($"Config sync request received from client: {clientId}");

        byte[] array = SerializeToBytes(Instance);
        int value = array.Length;

        using FastBufferWriter stream = new(value + IntSize, Allocator.Temp);

        try {
            stream.WriteValueSafe(in value);
            stream.WriteBytesSafe(array);

            MessageManager.SendNamedMessage($"{DalekPlugin.ModGuid}_OnReceiveConfigSync", clientId, stream);
        } catch(Exception e) {
            Debug.Log($"Error occurred syncing config with client: {clientId}\n{e}");
        }
    }

    private static void OnReceiveSync(ulong _, FastBufferReader reader) {
        if (!reader.TryBeginRead(IntSize)) {
            Debug.LogError("Config sync error: Could not begin reading buffer.");
            return;
        }

        reader.ReadValueSafe(out int val);
        if (!reader.TryBeginRead(val)) {
            Debug.LogError("Config sync error: Host could not sync.");
            return;
        }

        byte[] data = new byte[val];
        reader.ReadBytesSafe(ref data, val);

        SyncInstance(data);

        Debug.Log("Successfully synced config with host.");
    }
        
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
    public static void InitializeLocalPlayer() {
        if (IsHost) {
            MessageManager.RegisterNamedMessageHandler($"{DalekPlugin.ModGuid}_OnRequestConfigSync", OnRequestSync);
            Synced = true;

            return;
        }

        Synced = false;
        MessageManager.RegisterNamedMessageHandler($"{DalekPlugin.ModGuid}_OnReceiveConfigSync", OnReceiveSync);
        RequestSync();
    }
        
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
    public static void PlayerLeave() {
        RevertSync();
    }

    internal static void SyncInstance(byte[] data) {
        Instance = DeserializeFromBytes(data);
        Synced = true;
    }

    internal static void RevertSync() {
        Instance = Default;
        Synced = false;
    }

    public static byte[] SerializeToBytes(T val) {
        BinaryFormatter bf = new();
        using MemoryStream stream = new();

        try {
            bf.Serialize(stream, val);
            return stream.ToArray();
        }
        catch (Exception e) {
            Debug.LogError($"Error serializing instance: {e}");
            return null;
        }
    }

    public static T DeserializeFromBytes(byte[] data) {
        BinaryFormatter bf = new();
        using MemoryStream stream = new(data);

        try {
            return (T) bf.Deserialize(stream);
        } catch (Exception e) {
            Debug.LogError($"Error deserializing instance: {e}");
            return default;
        }
    }
        
    // Got this from the giant specimens mod
    protected void ClearUnusedEntries(ConfigFile configFile) {
        PropertyInfo orphanedEntriesProp = configFile.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);
        if (orphanedEntriesProp != null)
        {
            Dictionary<ConfigDefinition, string> orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(configFile, null);
            orphanedEntries.Clear(); // Clear orphaned entries (Unbounded/Abandoned entries)
        }

        configFile.Save();
    }
}

internal static class Assets
{
    private const string MainAssetBundleName = "dalekbundle";
    public static AssetBundle MainAssetBundle;

    public static void PopulateAssetsFromFile()
    {
        if (MainAssetBundle != null) return;
        string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (assemblyLocation != null)
        {
            MainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(assemblyLocation, MainAssetBundleName));

            if (MainAssetBundle != null) return;
            string assetsPath = Path.Combine(assemblyLocation, "Assets");
            MainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(assetsPath, MainAssetBundleName));
        }

        if (MainAssetBundle == null)
        {
            Plugin.logger.LogError("Failed to load Dalek bundle");
        }
    }
}