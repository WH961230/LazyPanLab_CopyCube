using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>追踪实体配置：以 SourceSign 为键。行为只消费自身配置。</summary>
    [CreateAssetMenu(fileName = "TrackingEntitySetting", menuName = "LazyPan/TrackingEntitySetting")]
    public class TrackingEntitySetting : Setting {
        [Header("节点便签说明 自由修改")]
        [Tooltip("追踪节点上的行为说明书，改这里就行，不用改代码。清空则回退到代码里的默认文案")]
        [TextArea(5, 15)]
        public string MemoDoc =
            "【追踪实体】管一个怪自动找路追人，目标没了就原地等。\n" +
            "停走命令看实体级 MoveAttr，谁置停都停。\n" +
            "— 配置参数（TrackingEntitySetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>TargetType</color>：追谁，按实体类型名填，找不到就原地等\n" +
            "- <color=#FFD54F>NavMeshTerrainSign</color>：寻路用的地形实体，如 Obj_Terrain_SceneC_Terrain，不填不查 NavMesh\n" +
            "- <color=#FFD54F>TrackingSpeed</color>：追多快，建议 2~6\n" +
            "- <color=#FFD54F>TrackingStop</color>：true=这个追踪自己先停住，false=跟着大家一起走";
        public List<TrackingEntitySettingData> Datas = new List<TrackingEntitySettingData>();

        /// <summary>
        /// 按来源类型获取行为配置 条目不存在时输出错误 避免静默失败
        /// </summary>
        public bool TryGet(string sourceSign, out TrackingEntitySettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("TrackingEntitySetting 缺少 SourceType:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    /// <summary>
    /// 追踪行为配置 以来源类型为键 行为不感知实体业务
    /// </summary>
    [Serializable]
    public struct TrackingEntitySettingData {
        [EntitySign]
        [Tooltip("发起追踪的实体类型，与 ObjConfig.Sign 一致，如 Obj_Enemy_SceneC_Enemy")]
        [Header("发起追踪的实体类型")] public string SourceSign;
        [Tooltip("追踪速度，代理速度，如 3.5")]
        [Header("追踪速度")] public float TrackingSpeed;
        [Tooltip("配置级停止，勾上后行为不再追踪")]
        [Header("追踪是否停止")] public bool TrackingStop;
        [Tooltip("被追踪的实体类型，如 Player 或 Obj_Player_SceneC_Player")]
        [Header("被追踪的实体类型")] public string TargetType;
        [EntitySign]
        [Tooltip("寻路用的地形实体，如 Obj_Terrain_SceneC_Terrain。检查时真去它所在场景查 NavMesh 烘没烘焙")]
        [Header("寻路地形实体")] public string NavMeshTerrainSign;
    }
}