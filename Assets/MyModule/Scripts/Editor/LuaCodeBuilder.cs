using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace CenturyGame.Framework.UI.Editor
{
    public static class LuaCodeBuilder
    {
        public static StringBuilder sb = new StringBuilder();

        private static void BeginBuild()
        {
            sb.Length = 0;
        }

        private static void InsertTab()
        {
            sb.Append('\t');
        }

        private static void InsertEnd()
        {
            sb.AppendLine("end");
            sb.AppendLine();
        }

        private static void InsertFunction(string className, string funcName, bool isStatic, params string[] args)
        {
            sb.AppendLine(string.Format("function {0}{1}{2}({3})", className, isStatic ? "." : ":", funcName, string.Join(",", args)));
            InsertEnd();
        }

        private static void InsertBindViewFunction(UIWndAssetData data)
        {
            sb.AppendLine(string.Format("function {0}:BindView(uiWnd)", data.UIName));
            InsertTab();
            sb.AppendLine("self.uiWnd = uiWnd");
            InsertTab();
            sb.AppendLine("self.uiref = {}");
            for (int i = 0; i < data.UIComponentDatas.Count; i++)
            {
                var cData = data.UIComponentDatas[i];
                string typeCode = GetUIComponentCode(cData.SelectType);
                if (cData.IsSelect)
                {
                    InsertTab();
                    sb.AppendLine(string.Format("self.uiref.{0} = uiWnd.transform:Find(\"{1}\"){2}", cData.ComponentName, cData.ComponentPath, typeCode));
                }
                for (int j = 0; j < cData.EventDatas.Count; j++)
                {
                    string eventName = string.Concat("on", cData.EventDatas[j].EventType.ToString());
                    string eventHandlerName = string.Concat("self.", cData.EventDatas[j].EventHandlerName);
                    string goCode = string.Format("uiWnd.transform:Find(\"{0}\").gameObject", cData.ComponentPath);
                    if (cData.IsSelect)
                        goCode = string.Format("self.uiref.{0}.gameObject", cData.ComponentName);
                    if (cData.EventDatas[j].EventType > EUIEventType.Deselect)
                    {
                        if (cData.IsSelect)
                        {
                            InsertTab();
                            sb.AppendLine(string.Format("self.uiref.{0}.{1}:AddListener({2})", cData.ComponentName, eventName, eventHandlerName));
                        }
                        else
                        {
                            InsertTab();
                            sb.AppendLine(string.Format("self.uiref.{0} = uiWnd.transform:Find(\"{1}\"){2}", cData.ComponentName, cData.ComponentPath, typeCode));
                            InsertTab();
                            sb.AppendLine(string.Format("self.uiref.{0}.{1}:AddListener({2})", cData.ComponentName, eventName, eventHandlerName));
                        }
                    }
                    else
                    {
                        InsertTab();
                        sb.AppendLine(string.Format("CS.CenturyGame.Framework.UI.EventTriggerListener.GetListener({0}):{1}('+', {2})", goCode, eventName, eventHandlerName));
                    }
                }
            }
            InsertTab();
            sb.AppendLine("self.uiWnd.OnInit:AddListener(self.onInit)");
            InsertTab();
            sb.AppendLine("self.uiWnd.OnEnter:AddListener(self.onEnter)");
            InsertTab();
            sb.AppendLine("self.uiWnd.OnExit:AddListener(self.onExit)");
            InsertTab();
            sb.AppendLine("self.uiWnd.OnClose:AddListener(self.onClose)");
            InsertTab();
            sb.AppendLine("self.uiWnd.OnUpdate:AddListener(self.onUpdate)");
            InsertEnd();
        }

        private static string GetUIComponentCode(EUIComponentType type)
        {
            string s = string.Empty;
            if (type == EUIComponentType.GameObject)
                s = ".gameObject";
            else
                s = string.Format(":GetComponent(\"UnityEngine.UI.{0}\")", type.ToString());
            return s;
        }

        public static void GenerateViewCode(UIWndAssetData data, string path)
        {
            BeginBuild();
            sb.AppendLine(string.Format("{0} = {1}", data.UIName, "{}"));
            sb.AppendLine();
            InsertBindViewFunction(data);
            File.WriteAllText(path, sb.ToString());
        }

        public static void GenerateLogicCode(UIWndAssetData data, string path)
        {
            BeginBuild();
            WriteLogic(data);
            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteLogic(UIWndAssetData data)
        {
            sb.AppendLine(string.Format("require 'ui/{0}View'", data.UIName));
            sb.AppendLine();
            sb.AppendLine(string.Format("local this = {0}", data.UIName));
            sb.AppendLine();
            List<string> functionList = new List<string>();
            for (int i = 0; i < data.UIComponentDatas.Count; i++)
            {
                for (int j = 0; j < data.UIComponentDatas[i].EventDatas.Count; j++)
                {
                    string funcName = data.UIComponentDatas[i].EventDatas[j].EventHandlerName;
                    if (!functionList.Contains(funcName))
                    {
                        string[] args = new string[0];
                        if (data.UIComponentDatas[i].EventDatas[j].EventType < EUIEventType.Click)
                        {
                            args = new string[2];
                            args[0] = "go";
                            args[1] = "evtData";
                        }
                        else if (data.UIComponentDatas[i].EventDatas[j].EventType > EUIEventType.Click)
                        {
                            args = new string[1];
                            args[0] = "v";
                        }
                        InsertFunction(data.UIName, funcName, true, args);
                        functionList.Add(funcName);
                    }
                }
            }
            InsertFunction(data.UIName, "onInit", true);
            InsertFunction(data.UIName, "onEnter", true);
            InsertFunction(data.UIName, "onExit", true);
            InsertFunction(data.UIName, "onClose", true);
            InsertFunction(data.UIName, "onUpdate", true);
        }

        public static void OverrideLogicCode(UIWndAssetData data, string path)
        {
            BeginBuild();
            string[] readText = File.ReadAllLines(path);
            foreach (string s in readText)
            {
                if (string.IsNullOrEmpty(s))
                    sb.AppendLine();
                else
                    sb.AppendLine(s.Insert(0, "--"));
            }
            sb.AppendLine("----------注释前一次代码的分割线----------");
            sb.AppendLine();
            WriteLogic(data);
            File.WriteAllText(path, sb.ToString());
        }

        public static void MergeLogicCode(UIWndAssetData data, string path)
        {
            BeginBuild();
            string[] readText = File.ReadAllLines(path);
            for (int i = 0; i < readText.Length; i++)
            {
                sb.AppendLine(readText[i]);
            }
            List<string> functionLines = new List<string>();
            ParseLuaFunction(readText, out functionLines);
            MergeLogic(data, readText, functionLines);
            File.WriteAllText(path, sb.ToString());
        }

        public static void ParseLuaFunction(string[] codeLines, out List<string> functionLines)
        {
            functionLines = new List<string>();
            for (int i = 0; i < codeLines.Length; i++)
            {
                string codeLine = codeLines[i];
                if (codeLine.StartsWith("function"))
                    functionLines.Add(codeLine);
            }
        }

        private static void MergeLogic(UIWndAssetData data, string[] oldCodes, List<string> existFuncLines)
        {
            List<string> functionList = new List<string>();
            for (int i = 0; i < data.UIComponentDatas.Count; i++)
            {
                for (int j = 0; j < data.UIComponentDatas[i].EventDatas.Count; j++)
                {
                    string funcName = data.UIComponentDatas[i].EventDatas[j].EventHandlerName;
                    bool isExistFunc = false;
                    for (int k = 0; k < existFuncLines.Count; k++)
                    {
                        if (existFuncLines[k].Contains(funcName))
                        {
                            isExistFunc = true;
                            break;
                        }
                    }
                    if (isExistFunc)
                        continue;
                    if (!functionList.Contains(funcName))
                    {
                        string[] args = new string[0];
                        if (data.UIComponentDatas[i].EventDatas[j].EventType < EUIEventType.Click)
                        {
                            args = new string[2];
                            args[0] = "go";
                            args[1] = "evtData";
                        }
                        else if (data.UIComponentDatas[i].EventDatas[j].EventType > EUIEventType.Click)
                        {
                            args = new string[1];
                            args[0] = "v";
                        }
                        InsertFunction(data.UIName, funcName, true, args);
                        functionList.Add(funcName);
                    }
                }
            }
        }
    }
}