using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>实体挂载界面 — 把 UI 预制体挂到实体节点并按 Data 自动刷新数值。</summary>
    [CreateAssetMenu(fileName = "EntityUIBinderSetting", menuName = "LazyPan/EntityUIBinderSetting")]
    public class EntityUIBinderSetting : Setting {
        public List<EntityUIBindSettingData> Datas = new List<EntityUIBindSettingData>();

        public bool TryGet(string sourceSign, out EntityUIBindSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("EntityUIBinderSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public struct EntityUIBindSettingData {
        [Header("发起绑定的实体类型")]
        [Tooltip("发起绑定的实体类型，必须与 ObjConfig.Sign 一致，如 Obj_Enemy_Enemy1")]
        public string SourceSign;

        [Header("绑定UI列表")]
        [Tooltip("绑定UI列表，一个实体可挂多个UI")]
        public List<EntityUIBindItem> Items;
    }

    [Serializable]
    public class EntityUIBindItem {
        [Header("UI预制体标识 Bundles/Prefabs/UI下")]
        [Tooltip("UI预制体标识，Bundles/Prefabs 下相对路径，如 UI/UI_HealthBar")]
        public string UIPrefabSign;

        [Header("挂点标签 留空挂实体根节点 如Foot")]
        [Tooltip("挂点标签（实体Comp里的Transform Sign），留空挂实体根节点，如 UIRoot/Foot/Body")]
        public string AttachLabel;

        [Header("挂点局部偏移")]
        [Tooltip("挂点局部偏移")]
        public Vector3 Offset;

        [Header("是否始终面向相机")]
        [Tooltip("勾上则始终面向相机（Billboard）")]
        public bool Billboard;

        [Header("数值绑定列表")]
        [Tooltip("数值绑定列表，组件从UI预制体Comp按标签取，数值从实体Data按标签取")]
        public List<EntityUIDataBind> DataBinds = new List<EntityUIDataBind>();
    }

    [Serializable]
    public class EntityUIDataBind {
        [Header("组件标签 预制体Comp里配置的Sign")]
        [Tooltip("组件标签，UI预制体Comp里配置的Sign，如 Slider；无Comp时按子物体名兜底")]
        public string ComponentSign;

        [Header("组件类型")]
        [Tooltip("绑定组件类型：Slider=滑条/血条，Text=文本，Image=填充图")]
        public UIDataBindComponentType ComponentType;

        [Header("取值模式 比例=当前/最大 直接=当前值")]
        [Tooltip("Ratio=当前/最大(如 Health/MaxHealth)；Direct=直接取值")]
        public UIDataBindValueMode Mode;

        [Header("数据标签 实体Data里的Sign 如Health")]
        [Tooltip("数据标签，实体Data里的Sign，如 Health")]
        public string DataSign;

        [Header("最大值数据标签 比例模式用 如MaxHealth")]
        [Tooltip("比例模式的最大值标签，如 MaxHealth")]
        public string MaxDataSign;

        [Header("文本格式 F0整数 F1一位小数 空则F0")]
        [Tooltip("文本格式化，F0=整数/F1=一位小数，空则F0（仅 Text 用）")]
        public string Format;
    }

    public enum UIDataBindComponentType {
        Slider = 0,
        Text = 1,
        Image = 2,
    }

    public enum UIDataBindValueMode {
        Ratio = 0,
        Direct = 1,
    }
}
