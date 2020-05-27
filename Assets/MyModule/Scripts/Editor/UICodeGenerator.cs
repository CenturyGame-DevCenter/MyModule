using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using CenturyGame.Framework.Utility;

namespace CenturyGame.Framework.UI.Editor
{
    public class UICodeGenerator : EditorWindow
    {
        [MenuItem("UITool/CodeGenerator")]
        //[MenuItem("Assets/UITool/CodeGenerator")]
        public static void Create()
        {
            UICodeGenerator window = EditorWindow.GetWindow(typeof(UICodeGenerator), true, "界面代码生成工具", true) as UICodeGenerator;
        }

        private UnityEngine.Object currPrefab;
        private UnityEngine.Object lastPrefab;
        private GameObject uiObject;
        private Transform[] uiTransforms = new Transform[0];
        private UIWndAssetData uiData;
        private Dictionary<string, UIComponentAssetData> componentMap = new Dictionary<string, UIComponentAssetData>();
        private string[] componentNames = new string[0];
        private string[] componentPaths = new string[0];
        private int selectComponentIndex = -1;
        private int lastComponentIndex = -1;
        private int removeEventIndex = -1;
        private UIComponentAssetData selectComponentData;
        private readonly string dataRoot = "Assets/AssetData/UIWnd/";
        private bool IsGenerateLogic = false;
        private static readonly GUIContent eventLabelContent = new GUIContent("事件类型");
        private EUIComponentType lastSelectComponentType = default;
        private EUIComponentType componentFilterType = default;
        #region 组件分类
        private string[] m_GameObjectNames = new string[0];
        private string[] m_GameObjectPaths = new string[0];
        private string[] m_TextNames = new string[0];
        private string[] m_TextPaths = new string[0];
        private string[] m_ImageNames = new string[0];
        private string[] m_ImagePaths = new string[0];
        private string[] m_RawImageNames = new string[0];
        private string[] m_RawImagePaths = new string[0];
        private string[] m_ButtonNames = new string[0];
        private string[] m_ButtonPaths = new string[0];
        private string[] m_ToggleNames = new string[0];
        private string[] m_TogglePaths = new string[0];
        private string[] m_SliderNames = new string[0];
        private string[] m_SliderPaths = new string[0];
        private string[] m_ScrollbarNames = new string[0];
        private string[] m_ScrollbarPaths = new string[0];
        private string[] m_DropdownNames = new string[0];
        private string[] m_DropdownPaths = new string[0];
        private string[] m_InputFieldNames = new string[0];
        private string[] m_InputFieldPaths = new string[0];
        private string[] m_ScrollRectNames = new string[0];
        private string[] m_ScrollRectPaths = new string[0];
        #endregion

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            currPrefab = EditorGUILayout.ObjectField("UI Prefab", currPrefab, typeof(UnityEngine.Object), false);
            if (currPrefab != lastPrefab)
            {
                if (currPrefab != null)
                {
                    Clear();
                    CreateUI();
                }
                else
                    Clear();
                lastPrefab = currPrefab;
            }
            EditorGUILayout.EndHorizontal();

            GUI.enabled = uiObject != null;

            EUIComponentType lastFilterType = componentFilterType;
            float w = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 60f;
            componentFilterType = (EUIComponentType)EditorGUILayout.EnumPopup("筛选类型", componentFilterType, GUILayout.MaxWidth(160));
            EditorGUIUtility.labelWidth = w;
            if (lastFilterType != componentFilterType)
            {
                if (componentFilterType == EUIComponentType.GameObject)
                {
                    componentNames = m_GameObjectNames;
                    componentPaths = m_GameObjectPaths;
                }
                else if (componentFilterType == EUIComponentType.Text)
                {
                    componentNames = m_TextNames;
                    componentPaths = m_TextPaths;
                }
                else if (componentFilterType == EUIComponentType.Image)
                {
                    componentNames = m_ImageNames;
                    componentPaths = m_ImagePaths;
                }
                else if (componentFilterType == EUIComponentType.RawImage)
                {
                    componentNames = m_RawImageNames;
                    componentPaths = m_RawImagePaths;
                }
                else if (componentFilterType == EUIComponentType.Button)
                {
                    componentNames = m_ButtonNames;
                    componentPaths = m_ButtonPaths;
                }
                else if (componentFilterType == EUIComponentType.Toggle)
                {
                    componentNames = m_ToggleNames;
                    componentPaths = m_TogglePaths;
                }
                else if (componentFilterType == EUIComponentType.Slider)
                {
                    componentNames = m_SliderNames;
                    componentPaths = m_SliderPaths;
                }
                else if (componentFilterType == EUIComponentType.Scrollbar)
                {
                    componentNames = m_ScrollbarNames;
                    componentPaths = m_ScrollbarPaths;
                }
                else if (componentFilterType == EUIComponentType.Dropdown)
                {
                    componentNames = m_DropdownNames;
                    componentPaths = m_DropdownPaths;
                }
                else if (componentFilterType == EUIComponentType.InputField)
                {
                    componentNames = m_InputFieldNames;
                    componentPaths = m_InputFieldPaths;
                }
                else if (componentFilterType == EUIComponentType.ScrollRect)
                {
                    componentNames = m_ScrollRectNames;
                    componentPaths = m_ScrollRectPaths;
                }
                selectComponentIndex = -1;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical("box", GUILayout.Width(240), GUILayout.Height(360));
            selectComponentIndex = GUILayout.SelectionGrid(selectComponentIndex, componentNames, 1, GUILayout.Width(200));
            EditorGUILayout.EndVertical();
            if (selectComponentIndex != lastComponentIndex)
            {
                if (selectComponentIndex != -1)
                {
                    string path = componentPaths[selectComponentIndex];
                    selectComponentData = componentMap[path];
                    lastSelectComponentType = selectComponentData.SelectType;
                }
                lastComponentIndex = selectComponentIndex;
            }
            if (selectComponentIndex != -1)
            {
                ComponentView();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("保存", GUILayout.Width(100), GUILayout.Height(25)))
            {
                SaveUIData();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUIUtility.labelWidth = 120f;
            IsGenerateLogic = EditorGUILayout.Toggle("生成Lua侧逻辑代码", IsGenerateLogic, GUILayout.Width(180));
            EditorGUIUtility.labelWidth = 0f;
            if (GUILayout.Button("保存并生成代码", GUILayout.Width(100), GUILayout.Height(25)))
            {
                SaveUIData();
                GenerateCode();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }

        private void ComponentView()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 80f;
            selectComponentData.IsSelect = EditorGUILayout.Toggle("生成Lua成员", selectComponentData.IsSelect, GUILayout.MaxWidth(120));
            EditorGUIUtility.labelWidth = 0f;
            EditorGUILayout.LabelField("路径:" + selectComponentData.ComponentPath, GUILayout.MaxWidth(300));
            EditorGUIUtility.labelWidth = 60f;
            selectComponentData.SelectType = (EUIComponentType)EditorGUILayout.EnumPopup("组件类型", selectComponentData.SelectType, GUILayout.MaxWidth(160));
            EditorGUIUtility.labelWidth = 0f;
            if (selectComponentData.SelectType != lastSelectComponentType)
            {
                ResetSelectComponentEvent();
                lastSelectComponentType = selectComponentData.SelectType;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginVertical("box", GUILayout.Width(600), GUILayout.Height(320));
            removeEventIndex = -1;
            for (int i = 0; i < selectComponentData.EventDatas.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                //selectComponentData.SelectEvents[i] = (EUIEventType)EditorGUILayout.EnumPopup(eventLabelContent, selectComponentData.SelectEvents[i], CheckComponentEventSelectable, false, GUILayout.MaxWidth(200));
                EUIEventType lastEventType = selectComponentData.EventDatas[i].EventType;
                EditorGUIUtility.labelWidth = 60f;
                selectComponentData.EventDatas[i].EventType = (EUIEventType)EditorGUILayout.EnumPopup(eventLabelContent, selectComponentData.EventDatas[i].EventType, CheckComponentEventSelectable, false, GUILayout.MaxWidth(200));
                if (lastEventType != selectComponentData.EventDatas[i].EventType)
                    selectComponentData.EventDatas[i].EventHandlerName = DefaultEventHandlerName(selectComponentData.ComponentName, selectComponentData.EventDatas[i].EventType);
                EditorGUIUtility.labelWidth = 80f;
                selectComponentData.EventDatas[i].EventHandlerName = EditorGUILayout.TextField("事件处理方法", selectComponentData.EventDatas[i].EventHandlerName, GUILayout.MaxWidth(300));
                EditorGUIUtility.labelWidth = 0f;
                if (GUILayout.Button("移除", GUILayout.Width(60)))
                {
                    removeEventIndex = i;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (removeEventIndex != -1)
                selectComponentData.EventDatas.RemoveAt(removeEventIndex);
            EditorGUILayout.EndVertical();
            if (GUILayout.Button("添加事件", GUILayout.Width(120)))
            {
                if (selectComponentData.SelectType >= EUIComponentType.GameObject && selectComponentData.SelectType < EUIComponentType.Button)
                {
                    UIComponentEventAssetData newEvent = new UIComponentEventAssetData();
                    newEvent.EventType = EUIEventType.PointerClick;
                    newEvent.EventHandlerName = DefaultEventHandlerName(selectComponentData.ComponentName, newEvent.EventType);
                    selectComponentData.EventDatas.Add(newEvent);
                }
                else if (selectComponentData.SelectType == EUIComponentType.Button)
                {
                    UIComponentEventAssetData newEvent = new UIComponentEventAssetData();
                    newEvent.EventType = EUIEventType.Click;
                    newEvent.EventHandlerName = DefaultEventHandlerName(selectComponentData.ComponentName, newEvent.EventType);
                    selectComponentData.EventDatas.Add(newEvent);
                    selectComponentData.IsSelect = true;
                }
                else
                {
                    UIComponentEventAssetData newEvent = new UIComponentEventAssetData();
                    newEvent.EventType = EUIEventType.ValueChanged;
                    newEvent.EventHandlerName = DefaultEventHandlerName(selectComponentData.ComponentName, newEvent.EventType);
                    selectComponentData.EventDatas.Add(newEvent);
                    selectComponentData.IsSelect = true;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private string DefaultEventHandlerName(string componentName, EUIEventType eventType)
        {
            return string.Concat("on", eventType.ToString(), "_", componentName);
        }

        private void ResetSelectComponentEvent()
        {
            if (selectComponentData != null)
            {
                List<UIComponentEventAssetData> newEventDatas = new List<UIComponentEventAssetData>();
                for (int i = 0; i < selectComponentData.EventDatas.Count; i++)
                {
                    if (CheckComponentEventSelectable(selectComponentData.EventDatas[i].EventType))
                        newEventDatas.Add(selectComponentData.EventDatas[i]);
                }
                selectComponentData.EventDatas.Clear();
                selectComponentData.EventDatas.AddRange(newEventDatas);
            }
        }

        private bool CheckComponentEventSelectable(Enum arg)
        {
            if (selectComponentData == null)
                return false;
            EUIEventType eventType = (EUIEventType)arg;
            if (selectComponentData.SelectType >= EUIComponentType.GameObject && selectComponentData.SelectType < EUIComponentType.Button)
            {
                if (eventType >= EUIEventType.PointerEnter && eventType <= EUIEventType.Deselect)
                    return true;
            }
            else if (selectComponentData.SelectType.Equals(EUIComponentType.Button))
            {
                if (eventType == EUIEventType.Click)
                    return true;
            }
            else if (selectComponentData.SelectType.Equals(EUIComponentType.InputField))
            {
                if (eventType == EUIEventType.ValueChanged || eventType == EUIEventType.EndEdit)
                    return true;
            }
            else
            {
                if (eventType == EUIEventType.ValueChanged)
                    return true;
            }
            return false;
        }

        private void CreateUI()
        {
            if (uiObject != null)
            {
                DestroyImmediate(uiObject);
                uiObject = null;
            }
            uiObject = PrefabUtility.InstantiatePrefab(currPrefab) as GameObject;
            uiTransforms = uiObject.GetComponentsInChildren<Transform>();
            componentMap.Clear();
            List<UIComponentAssetData> textList = new List<UIComponentAssetData>();
            List<UIComponentAssetData> imageList = new List<UIComponentAssetData>();
            List<UIComponentAssetData> rawimageList = new List<UIComponentAssetData>();
            List<UIComponentAssetData> buttonList = new List<UIComponentAssetData>();
            List<UIComponentAssetData> toggleList = new List<UIComponentAssetData>();
            List<UIComponentAssetData> sliderList = new List<UIComponentAssetData>();
            List<UIComponentAssetData> scrollbarList = new List<UIComponentAssetData>();
            List<UIComponentAssetData> dropdownList = new List<UIComponentAssetData>();
            List<UIComponentAssetData> inputfieldList = new List<UIComponentAssetData>();
            List<UIComponentAssetData> scrollrectList = new List<UIComponentAssetData>();
            foreach (Transform t in uiTransforms)
            {
                if (t == uiObject.transform)
                    continue;
                UIComponentAssetData data = new UIComponentAssetData();
                data.IsSelect = false;
                data.ComponentName = t.name;
                data.ComponentPath = Util.GetTransformPath(t, uiObject.transform);
                data.SelectType = EUIComponentType.GameObject;
                if (!componentMap.ContainsKey(data.ComponentPath))
                {
                    componentMap.Add(data.ComponentPath, data);
                    if (t.GetComponent<Text>() != null)
                        textList.Add(data);
                    if (t.GetComponent<Image>() != null)
                        imageList.Add(data);
                    if (t.GetComponent<RawImage>() != null)
                        rawimageList.Add(data);
                    if (t.GetComponent<Button>() != null)
                        buttonList.Add(data);
                    if (t.GetComponent<Toggle>() != null)
                        toggleList.Add(data);
                    if (t.GetComponent<Slider>() != null)
                        sliderList.Add(data);
                    if (t.GetComponent<Scrollbar>() != null)
                        scrollbarList.Add(data);
                    if (t.GetComponent<Dropdown>() != null)
                        dropdownList.Add(data);
                    if (t.GetComponent<InputField>() != null)
                        inputfieldList.Add(data);
                    if (t.GetComponent<ScrollRect>() != null)
                        scrollrectList.Add(data);
                }
                else
                    EditorUtility.DisplayDialog("提示", string.Format("{0}中已包含路径为{1}的节点，请检查预制体并重新命名", uiObject.name, data.ComponentPath), "确定");
            }
            #region 组件类型筛选
            m_TextNames = new string[textList.Count];
            m_TextPaths = new string[textList.Count];
            for (int i = 0; i < textList.Count; i++)
            {
                m_TextNames[i] = textList[i].ComponentName;
                m_TextPaths[i] = textList[i].ComponentPath;
            }
            m_ImageNames = new string[imageList.Count];
            m_ImagePaths = new string[imageList.Count];
            for (int i = 0; i < imageList.Count; i++)
            {
                m_ImageNames[i] = imageList[i].ComponentName;
                m_ImagePaths[i] = imageList[i].ComponentPath;
            }
            m_RawImageNames = new string[rawimageList.Count];
            m_RawImagePaths = new string[rawimageList.Count];
            for (int i = 0; i < rawimageList.Count; i++)
            {
                m_RawImageNames[i] = rawimageList[i].ComponentName;
                m_RawImagePaths[i] = rawimageList[i].ComponentPath;
            }
            m_ButtonNames = new string[buttonList.Count];
            m_ButtonPaths = new string[buttonList.Count];
            for (int i = 0; i < buttonList.Count; i++)
            {
                m_ButtonNames[i] = buttonList[i].ComponentName;
                m_ButtonPaths[i] = buttonList[i].ComponentPath;
            }
            m_ToggleNames = new string[toggleList.Count];
            m_TogglePaths = new string[toggleList.Count];
            for (int i = 0; i < toggleList.Count; i++)
            {
                m_ToggleNames[i] = toggleList[i].ComponentName;
                m_TogglePaths[i] = toggleList[i].ComponentPath;
            }
            m_SliderNames = new string[sliderList.Count];
            m_SliderPaths = new string[sliderList.Count];
            for (int i = 0; i < sliderList.Count; i++)
            {
                m_SliderNames[i] = sliderList[i].ComponentName;
                m_SliderPaths[i] = sliderList[i].ComponentPath;
            }
            m_ScrollbarNames = new string[scrollbarList.Count];
            m_ScrollbarPaths = new string[scrollbarList.Count];
            for (int i = 0; i < scrollbarList.Count; i++)
            {
                m_ScrollbarNames[i] = scrollbarList[i].ComponentName;
                m_ScrollbarPaths[i] = scrollbarList[i].ComponentPath;
            }
            m_DropdownNames = new string[dropdownList.Count];
            m_DropdownPaths = new string[dropdownList.Count];
            for (int i = 0; i < dropdownList.Count; i++)
            {
                m_DropdownNames[i] = dropdownList[i].ComponentName;
                m_DropdownPaths[i] = dropdownList[i].ComponentPath;
            }
            m_InputFieldNames = new string[inputfieldList.Count];
            m_InputFieldPaths = new string[inputfieldList.Count];
            for (int i = 0; i < inputfieldList.Count; i++)
            {
                m_InputFieldNames[i] = inputfieldList[i].ComponentName;
                m_InputFieldPaths[i] = inputfieldList[i].ComponentPath;
            }
            m_ScrollRectNames = new string[scrollrectList.Count];
            m_ScrollRectPaths = new string[scrollrectList.Count];
            for (int i = 0; i < scrollrectList.Count; i++)
            {
                m_ScrollRectNames[i] = scrollrectList[i].ComponentName;
                m_ScrollRectPaths[i] = scrollrectList[i].ComponentPath;
            }
            m_GameObjectNames = new string[componentMap.Count];
            m_GameObjectPaths = new string[componentMap.Count];
            int index = 0;
            var e = componentMap.GetEnumerator();
            while (e.MoveNext())
            {
                m_GameObjectNames[index] = e.Current.Value.ComponentName;
                m_GameObjectPaths[index] = e.Current.Value.ComponentPath;
                index++;
            }
            #endregion
            componentNames = m_GameObjectNames;
            componentPaths = m_GameObjectPaths;
            string fullPath = string.Concat(dataRoot, currPrefab.name, ".asset");
            if (File.Exists(fullPath))
            {
                uiData = AssetDatabase.LoadAssetAtPath<UIWndAssetData>(fullPath);
                AssetToEdit();
            }
            else
            {
                uiData = CreateUIData(fullPath, currPrefab.name);
            }
        }

        private void OnDestroy()
        {
            Clear();
        }

        private void Clear()
        {
            uiTransforms = new Transform[0];
            uiData = null;
            if (uiObject != null)
                DestroyImmediate(uiObject);
            uiObject = null;
            componentMap.Clear();
            componentNames = new string[0];
            componentPaths = new string[0];
            m_GameObjectNames = new string[0];
            m_GameObjectPaths = new string[0];
            m_TextNames = new string[0];
            m_TextPaths = new string[0];
            m_ImageNames = new string[0];
            m_ImagePaths = new string[0];
            m_RawImageNames = new string[0];
            m_RawImagePaths = new string[0];
            m_ButtonNames = new string[0];
            m_ButtonPaths = new string[0];
            m_ToggleNames = new string[0];
            m_TogglePaths = new string[0];
            m_SliderNames = new string[0];
            m_SliderPaths = new string[0];
            m_ScrollbarNames = new string[0];
            m_ScrollbarPaths = new string[0];
            m_DropdownNames = new string[0];
            m_DropdownPaths = new string[0];
            m_InputFieldNames = new string[0];
            m_InputFieldPaths = new string[0];
            m_ScrollRectNames = new string[0];
            m_ScrollRectPaths = new string[0];
            selectComponentIndex = -1;
            lastComponentIndex = -1;
            removeEventIndex = -1;
            selectComponentData = null;
            lastSelectComponentType = default;
            componentFilterType = default;
            IsGenerateLogic = false;
        }

        private UIWndAssetData CreateUIData(string fullPath, string uiName)
        {
            UIWndAssetData asset = null;
            string path = fullPath.Replace(Application.dataPath, "Assets");
            asset = ScriptableObject.CreateInstance<UIWndAssetData>();
            asset.UIName = uiName;
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private bool CheckConfigValid()
        {
            if (uiObject == null)
            {
                EditorUtility.DisplayDialog("提示", "场景中不存在界面GameObject", "确定");
                return false;
            }
            List<string> addNames = new List<string>();
            var e = componentMap.GetEnumerator();
            while (e.MoveNext())
            {
                UIComponentAssetData cData = e.Current.Value;
                if (cData.IsSelect || cData.EventDatas.Count > 0)
                {
                    if (addNames.Contains(cData.ComponentName))
                    {
                        EditorUtility.DisplayDialog("提示", string.Format("选中的组件变量名重复. 组件名={0}", cData.ComponentName), "确定");
                        return false;
                    }
                    Transform t = uiObject.transform.Find(cData.ComponentPath);
                    if (t == null)
                    {
                        EditorUtility.DisplayDialog("提示", string.Format("指定路径找不到组件. 路径={0}", cData.ComponentPath), "确定");
                        return false;
                    }
                    if (cData.SelectType == EUIComponentType.Text)
                    {
                        Text text = t.GetComponent<Text>();
                        if (text == null)
                        {
                            EditorUtility.DisplayDialog("提示", string.Format("指定路径找不到Text控件. 路径={0}", cData.ComponentPath), "确定");
                            return false;
                        }
                    }
                    else if (cData.SelectType == EUIComponentType.Image)
                    {
                        Image image = t.GetComponent<Image>();
                        if (image == null)
                        {
                            EditorUtility.DisplayDialog("提示", string.Format("指定路径找不到Image控件. 路径={0}", cData.ComponentPath), "确定");
                            return false;
                        }
                    }
                    else if (cData.SelectType == EUIComponentType.RawImage)
                    {
                        RawImage rawImage = t.GetComponent<RawImage>();
                        if (rawImage == null)
                        {
                            EditorUtility.DisplayDialog("提示", string.Format("指定路径找不到RawImage控件. 路径={0}", cData.ComponentPath), "确定");
                            return false;
                        }
                    }
                    else if (cData.SelectType == EUIComponentType.Button)
                    {
                        Button button = t.GetComponent<Button>();
                        if (button == null)
                        {
                            EditorUtility.DisplayDialog("提示", string.Format("指定路径找不到Button控件. 路径={0}", cData.ComponentPath), "确定");
                            return false;
                        }
                    }
                    else if (cData.SelectType == EUIComponentType.Toggle)
                    {
                        Toggle toggle = t.GetComponent<Toggle>();
                        if (toggle == null)
                        {
                            EditorUtility.DisplayDialog("提示", string.Format("指定路径找不到Toogle控件. 路径={0}", cData.ComponentPath), "确定");
                            return false;
                        }
                    }
                    else if (cData.SelectType == EUIComponentType.Slider)
                    {
                        Slider slider = t.GetComponent<Slider>();
                        if (slider == null)
                        {
                            EditorUtility.DisplayDialog("提示", string.Format("指定路径找不到Slider控件. 路径={0}", cData.ComponentPath), "确定");
                            return false;
                        }
                    }
                    else if (cData.SelectType == EUIComponentType.Scrollbar)
                    {
                        Scrollbar sBar = t.GetComponent<Scrollbar>();
                        if (sBar == null)
                        {
                            EditorUtility.DisplayDialog("提示", string.Format("指定路径找不到Scrollbar控件. 路径={0}", cData.ComponentPath), "确定");
                            return false;
                        }
                    }
                    else if (cData.SelectType == EUIComponentType.Dropdown)
                    {
                        Dropdown dropDown = t.GetComponent<Dropdown>();
                        if (dropDown == null)
                        {
                            EditorUtility.DisplayDialog("提示", string.Format("指定路径找不到Dropdown控件. 路径={0}", cData.ComponentPath), "确定");
                            return false;
                        }
                    }
                    else if (cData.SelectType == EUIComponentType.InputField)
                    {
                        InputField input = t.GetComponent<InputField>();
                        if (input == null)
                        {
                            EditorUtility.DisplayDialog("提示", string.Format("指定路径找不到InputField控件. 路径={0}", cData.ComponentPath), "确定");
                            return false;
                        }
                    }
                    else if (cData.SelectType == EUIComponentType.ScrollRect)
                    {
                        ScrollRect sRect = t.GetComponent<ScrollRect>();
                        if (sRect == null)
                        {
                            EditorUtility.DisplayDialog("提示", string.Format("指定路径找不到ScrollRect控件. 路径={0}", cData.ComponentPath), "确定");
                            return false;
                        }
                    }
                    addNames.Add(cData.ComponentName);
                }
            }
            return true;
        }

        private void EditToAsset()
        {
            uiData.UIComponentDatas.Clear();
            var e = componentMap.GetEnumerator();
            while (e.MoveNext())
            {
                if (e.Current.Value.IsSelect || e.Current.Value.EventDatas.Count > 0)
                {
                    UIComponentAssetData data = e.Current.Value.Clone();
                    uiData.UIComponentDatas.Add(data);
                }
            }
        }

        private void AssetToEdit()
        {
            if (uiData != null)
            {
                for (int i = 0; i < uiData.UIComponentDatas.Count; i++)
                {
                    var cData = uiData.UIComponentDatas[i];
                    if (componentMap.ContainsKey(cData.ComponentPath))
                    {
                        var eData = componentMap[cData.ComponentPath];
                        eData.IsSelect = cData.IsSelect;
                        eData.SelectType = cData.SelectType;
                        eData.EventDatas.Clear();
                        for (int j = 0; j < cData.EventDatas.Count; j++)
                            eData.EventDatas.Add(cData.EventDatas[j].Clone());
                    }
                    else
                        EditorUtility.DisplayDialog("提示", string.Format("界面不包含配置中保存的组件. 路径={0}, 类型={1}", cData.ComponentPath, cData.SelectType.ToString()), "确定");
                }
            }
        }

        private void SaveUIData()
        {
            if (uiData != null)
            {
                if (CheckConfigValid())
                {
                    EditToAsset();
                    EditorUtility.SetDirty(uiData);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        private void GenerateCode()
        {
            if (uiData != null)
            {
                int rootEndIndex = Application.dataPath.LastIndexOf('/');
                string luaRoot = Application.dataPath.Substring(0, rootEndIndex);
                string viewPath = string.Concat(luaRoot, "/LuaProject/ui/", uiData.UIName, "View.lua");
                LuaCodeBuilder.GenerateViewCode(uiData, viewPath);
                if (IsGenerateLogic)
                {
                    string logicPath = string.Concat(luaRoot, "/LuaProject/ui/", uiData.UIName, "Logic.lua");
                    if (File.Exists(logicPath))
                    {
                        if (EditorUtility.DisplayDialog("提示", "Lua逻辑代码文件已存在，是否覆盖?", "确定", "取消"))
                            LuaCodeBuilder.MergeLogicCode(uiData, logicPath);
                    }
                    else
                        LuaCodeBuilder.GenerateLogicCode(uiData, logicPath);
                }
            }
        }
    }
}