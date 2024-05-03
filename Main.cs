using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Il2Cpp;
using Il2CppIGame;
using Il2CppLuaInterface;
using Il2CppSystem;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;

namespace AFK_Mod
{
    public class Main : MelonMod
    {
        public static Main Instance { get; private set; } = null!;
        
        private readonly StringBuilder _logBuilder = new StringBuilder();

        private bool _showMenu;
        private Vector2 _menuScrollPosition = Vector2.zero;
        private string _findTxt = "pbsc_avatar_female_main_lod1";
        private string _findLayerTxt = "Player";
        private string _objectsOfTypeTxt = "IGameCollisionBody";
        private bool _startLogging;
        private GameObject _mapPlayer;

        public override void OnInitializeMelon()
        {
            Instance = this;
            
            MelonLogger.Msg("Dog Balls");
            
            ModuleManager.LoadAllModules();
            
            var harmony = new HarmonyLib.Harmony("com.afk.journey.mod");
            harmony.PatchAll();
        }

        public override void OnUpdate()
        {
            if (_startLogging)
            {
                if (!_mapPlayer)
                    _mapPlayer = GameObject.Find("mapscene/object_layer/map_player");

                if (_mapPlayer)
                {
                    var colBody = _mapPlayer.GetComponent<IGameCollisionBody>();
                    // colBody.CurrentPosXZ() is the transform.position.x/z of map_player
                    MelonLogger.Msg($"Movement: {colBody.movement}, CurrentPosXZ: {colBody.CurrentPosXZ()}");
                }
            }
        }
        
        public override void OnGUI()
        {
            GUI.Box(new Rect(5, 5, 400, 700), "AFK Journey Mod");
            
            GUILayout.BeginArea(new Rect(10, 40, 380, 700));
            _menuScrollPosition = GUILayout.BeginScrollView(_menuScrollPosition);

            if (GUILayout.Button("Log Scene Hierarchy"))
            {
                _logBuilder.Clear();
                var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
                if (rootObjects.Length == 0)
                    return;

                foreach (var root in rootObjects)
                {
                    LogAndDraw($"<b>{root.name}</b>");
                    LogChildObjects(root.transform, 1);
                }

                var sceneName = SceneManager.GetActiveScene().name;
                LogAndDraw($"<b>Scene Name:</b> {sceneName}");

            }

            if (GUILayout.Button("Log DontDestroyOnLoad Objects"))
            {
                _logBuilder.Clear();
                LogDontDestroyOnLoadObjects();
            }

            GUILayout.Label("Find:");
            _findTxt = GUILayout.TextField(_findTxt);
            if (GUILayout.Button("Log All Components In Object"))
            {
                _logBuilder.Clear();

                var player = GameObject.Find(_findTxt);
                if (player == null)
                {
                    LogAndDraw("Player not found");
                    return;
                }

                LogAndDraw($"Path: {GetPathToRoot(player)}");
                LogAndDraw($"Tag: {player.tag}");
                LogAndDraw($"Layer: {LayerMask.LayerToName(player.layer)}");

                foreach (var component in player.GetComponents<Component>())
                {
                    LogAndDraw($"<b>{component.ToString()}</b>");
                }
            }

            if (GUILayout.Button("Log Children"))
            {
                _logBuilder.Clear();
                var player = GameObject.Find(_findTxt);
                if (player == null)
                {
                    LogAndDraw("Player not found");
                    return;
                }

                LogChildObjects(player.transform, 0);
            }

            if (GUILayout.Button("Destroy Object"))
            {
                _logBuilder.Clear();
                var player = GameObject.Find(_findTxt);
                if (player == null)
                {
                    LogAndDraw("Player not found");
                    return;
                }

                LogAndDraw($"Destroying {player.name}");
                Object.Destroy(player);

            }

            if (GUILayout.Button("Log All Layers"))
            {
                _logBuilder.Clear();

                for (var i = 0; i < 32; i++)
                {
                    var layerName = LayerMask.LayerToName(i);
                    if (string.IsNullOrEmpty(layerName))
                        continue;

                    LogAndDraw($"<b>Layer {i}:</b> {layerName}");
                }
            }

            _findLayerTxt = GUILayout.TextField(_findLayerTxt);
            if (GUILayout.Button("Find Objects On Layer "))
            {
                var objs = FindObjectsOnLayer(LayerMask.NameToLayer(_findLayerTxt));
                if (objs.Count == 0)
                {
                    LogAndDraw("No objects found on layer");
                    return;
                }

                foreach (var obj in objs)
                {
                    LogAndDraw(obj.name);
                }
            }

            if (GUILayout.Button("Destroy Objects On Layer"))
            {
                var objs = FindObjectsOnLayer(LayerMask.NameToLayer(_findLayerTxt));
                if (objs.Count == 0)
                {
                    LogAndDraw("No objects found on layer");
                    return;
                }

                foreach (var obj in objs)
                {
                    Object.Destroy(obj);
                }
            }

            _objectsOfTypeTxt = GUILayout.TextField(_objectsOfTypeTxt);
            if (GUILayout.Button("Find Objects of Type in Field"))
            {
                _logBuilder.Clear();
                var type = Il2CppSystem.Type.GetType(_objectsOfTypeTxt);
                if (type == null)
                {
                    LogAndDraw($"Type '{_objectsOfTypeTxt}' not found.");
                    return;
                }

                var allObjects = Object.FindObjectsOfType<Object>(true);
                var objectsOfType = new List<UnityEngine.Object>();

                foreach (var obj in allObjects)
                {
                    if (type.IsAssignableFrom(obj.GetIl2CppType()))
                    {
                        objectsOfType.Add(obj);
                    }
                }

                if (objectsOfType.Count == 0)
                {
                    LogAndDraw("No objects of the specified type found.");
                    return;
                }

                foreach (var obj in objectsOfType)
                {
                    var gameObj = obj as GameObject;
                    if (gameObj != null)
                    {
                        var path = GetPathToRoot(gameObj);
                        LogAndDraw(path);
                    }
                    else
                    {
                        LogAndDraw(obj.ToString());
                    }
                }
            }

            _startLogging = GUILayout.Toggle(_startLogging, "Start Logging");

            GUILayout.BeginArea(new Rect(10, 360, 400, 800));
            GUILayout.Label(_logBuilder.ToString());
            GUILayout.EndArea();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void LogChildObjects(Transform parent, int depth)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                _logBuilder.AppendLine($"{new string(' ', depth * 4)}├── {child.name}");
                MelonLogger.Msg($"{new string(' ', depth * 4)}├── {child.name}");
                if (child.childCount > 0)
                {
                    LogChildObjects(child, depth + 1);
                }
            }
        }

        private string GetPathToRoot(GameObject gameObject)
        {
            var path = gameObject.name;
            var parent = gameObject.transform.parent;
            while (parent != null)
            {
                path = $"{parent.name}/{path}";
                parent = parent.parent;
            }
            return path;
        }

        private void LogAndDraw(string txt)
        {
            MelonLogger.Msg(txt);
            _logBuilder.AppendLine(txt);
        }

        public static List<GameObject> FindObjectsOnLayer(int layer)
        {
            var allObjects = Object.FindObjectsOfType<GameObject>();
            var objectsOnLayer = new List<GameObject>();

            foreach (GameObject obj in allObjects)
            {
                if (obj.layer == layer)
                {
                    objectsOnLayer.Add(obj);
                }
            }

            return objectsOnLayer;
        }

        private void LogDontDestroyOnLoadObjects()
        {
            var dontDestroyOnLoadObjects = GetDontDestroyOnLoadObjects();
            if (dontDestroyOnLoadObjects.Length == 0)
            {
                LogAndDraw("No objects found in DontDestroyOnLoad scene.");
                return;
            }

            foreach (var obj in dontDestroyOnLoadObjects)
            {
                LogAndDraw($"<b>{obj.name}</b> - Path: {GetPathToRoot(obj)}");
                LogChildObjects(obj.transform, 1);
            }
        }

        private GameObject[] GetDontDestroyOnLoadObjects()
        {
            var tempGO = new GameObject("TempGO");
            Object.DontDestroyOnLoad(tempGO);
            Scene dontDestroyOnLoadScene = tempGO.scene;
            GameObject.DestroyImmediate(tempGO);

            return dontDestroyOnLoadScene.GetRootGameObjects();
        }

        //[HarmonyPatch(typeof(LuaBehaviour))]
        //public class LuaBehaviour_Patch
        //{
        //    [HarmonyPatch(nameof(LuaBehaviour.InvokeLua)), HarmonyPrefix]
        //    public static void InvokeLua_Hook(ref string fName)
        //    {
        //        MelonLogger.Msg($"InvokeLua: {fName}");
        //    }
        //}

        [HarmonyPatch(typeof(LuaStatePtr))]
        public class LuaStatePtr_Patch
        {
            [HarmonyPatch(nameof(LuaStatePtr.LuaDoString)), HarmonyPrefix]
            public static bool LuaDoString_Hook(ref string chunk, ref string chunkName)
            {
                MelonLogger.Msg($"LuaDoString_Hook:\n   chunk: {chunk}, \n  chunkName: {chunkName}");

                return true;
            }
        }

        [HarmonyPatch(typeof(LuaDLL))]
        public class LuaDLL_Patch
        {
            [HarmonyPatch(nameof(LuaDLL.luaL_loadbuffer)), HarmonyPrefix]
            public static bool luaL_loadbuffer(ref byte[] buff)
            {
                var chunk = Encoding.UTF8.GetString(buff);
                MelonLogger.Msg($"luaL_loadbuffer:\n   chunk: {chunk}");

                return true;
            }

            [HarmonyPatch(nameof(LuaDLL.luaL_dostring)), HarmonyPrefix]
            public static bool luaL_dostring_Hook(ref IntPtr luaState, ref string chunk)
            {
                MelonLogger.Msg($"luaL_dostring_Hook:\n   chunk: {chunk}");

                return true;
            }
        }
    }
}
