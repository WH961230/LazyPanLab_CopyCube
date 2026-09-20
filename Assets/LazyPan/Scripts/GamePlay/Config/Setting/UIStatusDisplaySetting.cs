using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 屏幕状态展示 — 把任意实体 Data 刷到屏幕 UI 上。
    /// 跟 EntityUIBinder 是两兄弟：EntityUIBinder 是挂头顶血条（世界坐标，跟实体走），
    /// 这个是刷主界面 HUD（屏幕坐标，比如 UI_SceneC 显示玩家血量/等级/经验/波次）。
    /// 只读不写，不改任何数值，数值归 ParamValue / StageProgress / Death 管。
    /// 配置来源 Setting/UIStatusDisplaySetting，一个实体一条，里面可配多个屏幕 UI 块。
    /// </summary>
    [CreateAssetMenu(fileName = "UIStatusDisplaySetting", menuName = "LazyPan/UIStatusDisplaySetting")]
    public class UIStatusDisplaySetting : Setting {
        public List<UIStatusDisplaySettingData> Datas = new List<UIStatusDisplaySettingData>();

        public bool TryGet(string sourceSign, out UIStatusDisplaySettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("UIStatusDisplaySetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public struct UIStatusDisplaySettingData {
        [EntitySign]
        [Header("挂这个行为的实体类型 SourceSign")]
        [Tooltip("挂这个行为的实体类型，必须与 ObjConfig.Sign 一致，如 Obj_Player_SceneC_Player。行为挂谁身上，刷新就由谁驱动")]
        public string SourceSign;

        [Header("屏幕UI块列表")]
        [Tooltip("屏幕UI块列表，一般配1块就够。想同时刷两个界面才配多块")]
        public List<UIStatusDisplayItem> Displays;
    }

    [Serializable]
    public class UIStatusDisplayItem {
        [Header("屏幕UI名 为空=当前流程主界面")]
        [Tooltip("屏幕UI名，如 UI_SceneC。留空=自动取当前流程的 GetUI()，一般留空就行。填了就按名字去 UI.Instance.Get 取")]
        public string UIName;

        [Header("数值绑定列表")]
        [Tooltip("数值绑定列表，组件从屏幕UI的Comp按标签取，数值从任意实体Data按标签取")]
        public List<UIStatusDisplayBind> DataBinds = new List<UIStatusDisplayBind>();
    }

    [Serializable]
    public class UIStatusDisplayBind {
        [Header("组件标签 屏幕UI的Comp里配置的Sign")]
        [Tooltip("组件标签，屏幕UI的Comp里配置的Sign，如 Slider / HealthText。无Comp时按子物体名兜底")]
        public string ComponentSign;

        [Header("组件类型")]
        [Tooltip("绑定组件类型：Slider=滑条/血条，Text=文本，Image=填充图")]
        public UIDataBindComponentType ComponentType;

        [Header("取值模式 比例=当前/最大 直接=当前值")]
        [Tooltip("Ratio=当前/最大(如 Health/MaxHealth)；Direct=直接取值")]
        public UIDataBindValueMode Mode;

        [EntitySign]
        [Header("取数实体 Self=自己")]
        [Tooltip("去谁身上取数。Self=挂行为的自己。想在玩家界面上显示波次就填 Obj_Wave_SceneC_WaveManager")]
        public string SourceEntitySign = BehaviourSigns.Self;

        [Header("数据标签 如Health")]
        [Tooltip("数据标签，取数实体Data里的Sign，如 Health / Level / WaveIndex")]
        public string DataSign;

        [Header("最大值数据标签 比例模式用 如MaxHealth")]
        [Tooltip("比例模式的最大值标签，如 MaxHealth。Direct 模式可空")]
        public string MaxDataSign;

        [Header("文本格式 F0整数 F1一位小数 空则F0")]
        [Tooltip("文本格式化，F0=整数/F1=一位小数，空则F0（仅 Text 用）")]
        public string Format;
    }
}
