using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CenturyGame.Framework.UI
{
    /// <summary>
    /// 窗体数据
    /// </summary>
    public class UIWndAssetData : ScriptableObject
    {
        /// <summary>
        /// UI名称
        /// </summary>
        public string UIName;

        /// <summary>
        /// 组件配置数据
        /// </summary>
        public List<UIComponentAssetData> UIComponentDatas = new List<UIComponentAssetData>();
    }

    public enum EUIComponentType
    {
        GameObject,
        Text,
        Image,
        RawImage,
        Button,
        Toggle,
        Slider,
        Scrollbar,
        Dropdown,
        InputField,
        ScrollRect,
    }

    [Serializable]
    public class UIComponentAssetData
    {
        /// <summary>
        /// 是否生成组件对应的Lua侧变量
        /// </summary>
        public bool IsSelect;

        /// <summary>
        /// 组件名称
        /// </summary>
        public string ComponentName;

        /// <summary>
        /// 组件路径
        /// </summary>
        public string ComponentPath;

        /// <summary>
        /// 组件类型
        /// </summary>
        public EUIComponentType SelectType = default;

        /// <summary>
        /// 组件上关联的事件
        /// </summary>
        public List<UIComponentEventAssetData> EventDatas = new List<UIComponentEventAssetData>();

        public UIComponentAssetData Clone()
        {
            UIComponentAssetData newData = new UIComponentAssetData
            {
                IsSelect = IsSelect,
                ComponentName = ComponentName,
                ComponentPath = ComponentPath,
                SelectType = SelectType
            };
            for (int i = 0; i < EventDatas.Count; i++)
            {
                UIComponentEventAssetData newEventData = EventDatas[i].Clone();
                newData.EventDatas.Add(newEventData);
            }
            return newData;
        }
    }

    [Serializable]
    public class UIComponentEventAssetData
    {
        /// <summary>
        /// 事件类型
        /// </summary>
        public EUIEventType EventType;

        /// <summary>
        /// 事件处理方法名
        /// </summary>
        public string EventHandlerName;

        public UIComponentEventAssetData Clone()
        {
            UIComponentEventAssetData newData = new UIComponentEventAssetData
            {
                EventType = EventType,
                EventHandlerName = EventHandlerName
            };
            return newData;
        }
    }

    public enum EUIEventType
    {
        PointerEnter,
        PointerExit,
        PointerUp,
        PointerDown,
        PointerClick,
        InitializeDrag,
        BeginDrag,
        Drag,
        EndDrag,
        Drop,
        Scroll,
        UpdateSelected,
        Select,
        Deselect,
        Click,
        ValueChanged,
        EndEdit,
    }
}