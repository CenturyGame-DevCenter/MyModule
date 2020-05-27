using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
//using XLua;

namespace CenturyGame.Framework.UI
{
    public class UIWnd : MonoBehaviour
    {
        //private LuaTable logicEnv;
        private readonly static string BindMethod = "BindView";
        public readonly static int GroupDepthFactor = 1000;
        public readonly static int DepthFactor = 10;
        private Canvas canvas;

        public string UIName { get; private set; }

        public int Depth
        {
            get { return canvas.sortingOrder; }
            set { canvas.sortingOrder = value; }
        }

        public int DepthInGroup { get; set; }

        [HideInInspector]
        public UnityEvent OnInit = new UnityEvent();

        [HideInInspector]
        public UnityEvent OnEnter = new UnityEvent();

        [HideInInspector]
        public UnityEvent OnExit = new UnityEvent();

        [HideInInspector]
        public UnityEvent OnClose = new UnityEvent();

        [HideInInspector]
        public UnityEvent OnUpdate = new UnityEvent();

        private void Awake()
        {
            UIName = gameObject.name;
            canvas = GetComponent<Canvas>();
            //根据命名约束判断是否是CS逻辑界面，CS逻辑界面不绑定Lua脚本
            if (!UIName.EndsWith("_CS"))
            {
                BindLuaScript();
            }
            OnInit?.Invoke();
        }

        private void BindLuaScript()
        {
            //LuaManager luaMgr = FrameworkEntry.GetModule<LuaManager>();
            //LuaTable scriptEnv = luaMgr.luaEnv.NewTable();
            //LuaTable meta = luaMgr.luaEnv.NewTable();
            //meta.Set("__index", luaMgr.luaEnv.Global);
            //scriptEnv.SetMetaTable(meta);
            //meta.Dispose();
            //string luaFileName = string.Concat(UIName + "Logic");
            //string cmd = string.Concat("require 'ui/", luaFileName, "'");
            //luaMgr.luaEnv.DoString(cmd, luaFileName, scriptEnv);
            //logicEnv = scriptEnv.Get<LuaTable>(UIName);
            ////string bindMethodName = string.Concat(UIName, ".", BindMethod);
            ////scriptEnv.GetInPath<Action<LuaTable, UIWnd>>(bindMethodName)?.Invoke(logicEnv, this);
            //logicEnv.Get<Action<LuaTable, UIWnd>>(BindMethod)?.Invoke(logicEnv, this);
        }

        private void OnEnable()
        {
            OnEnter?.Invoke();
        }

        private void OnDisable()
        {
            OnExit?.Invoke();
        }

        private void Update()
        {
            OnUpdate?.Invoke();
        }

        private void OnDestroy()
        {
            OnClose?.Invoke();
        }

        public void RefreshDepth(int groupDepth, int depthInGroup)
        {
            DepthInGroup = depthInGroup;
            int oldDepth = Depth;
            int deltaDepth = GroupDepthFactor * groupDepth + DepthFactor * DepthInGroup - oldDepth;
            Depth += deltaDepth;
            //Canvas[] canvas = GetComponentsInChildren<Canvas>(true);
            //for (int i = 0; i < canvas.Length; i++)
            //{
            //    canvas[i].sortingOrder += deltaDepth;
            //}
        }
    }
}