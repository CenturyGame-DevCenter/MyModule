using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace CenturyGame.Framework.UI
{
    public enum EWindowLayer
    {
        Bottom = 0,
        Background,
        Common,
        Animation,
        Pop,
        Guide,
        Const,
        Top,
    }

    public class UIManager : FrameworkModule
    {
        private Dictionary<EWindowLayer, UIGroup> groupMap = new Dictionary<EWindowLayer, UIGroup>(8);

        public override void Init()
        {
            LuaManager luaMgr = FrameworkEntry.GetModule<LuaManager>();
            luaMgr.luaEnv.AddLoader(CustomLuaLoad);
        }

        public override void Update(float elapseTime, float realElapseTime)
        {
        }

        public override void LateUpdate()
        {
        }

        public override void Shutdown()
        {
        }

        private byte[] CustomLuaLoad(ref string fileName)
        {
            int rootEndIndex = Application.dataPath.LastIndexOf('/');
            string luaRoot = Application.dataPath.Substring(0, rootEndIndex);
            string filePath = fileName.Replace('.', '/');
            string fullPath = string.Concat(luaRoot, "/LuaProject/", filePath, ".lua");
            if (File.Exists(fullPath))
                return File.ReadAllBytes(fullPath);
            return null;
        }

        public void SetRoot(Transform root)
        {
            foreach (Transform t in root)
            {
                int depth = t.GetSiblingIndex();
                EWindowLayer layer = (EWindowLayer)depth;
                UIGroup uiGroup = new UIGroup(layer, t, depth);
                groupMap.Add(layer, uiGroup);
            }
        }

        public void OpenWindow(string uiName, EWindowLayer layer)
        {
            if (!groupMap.ContainsKey(layer))
            {
                Debug.LogError("找不到指定的层级:" + layer);
                return;
            }
            groupMap[layer].OpenUIWnd(uiName);
            groupMap[layer].RefreshDepth();
        }

        public void CloseWindow(string uiName, bool isDestroy)
        {
            var e = groupMap.GetEnumerator();
            while (e.MoveNext())
            {
                if (e.Current.Value.CloseUIWnd(uiName, isDestroy))
                {
                    if (isDestroy)
                        e.Current.Value.RefreshDepth();
                    break;
                }
            }
        }
    }
}