using System;
using System.Collections.Generic;
using UnityEngine;

namespace LazyPan {
    /// <summary>死亡 — 只做生命归零后的死亡处理与延迟销毁，不管血量数值。数值参数 Health/MaxHealth/Dead 归实体参数值(ParamValue)配置初始化，本行为只读写。</summary>
    [CreateAssetMenu(fileName = "DeathSetting", menuName = "LazyPan/DeathSetting")]
    public class DeathSetting : Setting {
        public List<DeathSettingData> Datas = new List<DeathSettingData>();

        public bool TryGet(string sourceSign, out DeathSettingData data) {
            foreach (var tmp in Datas) {
                if (tmp.SourceSign == sourceSign) {
                    data = tmp;
                    return true;
                }
            }

            data = default;
            LogUtil.LogErrorFormat("DeathSetting 缺少 SourceSign:{0} 的配置条目", sourceSign);
            return false;
        }
    }

    [Serializable]
    public struct DeathSettingData {
        [Header("发起实体的类型标识 SourceSign")]
        [Tooltip("发起实体的类型标识，必须与 ObjConfig.Sign 一致，如 Obj_Enemy_Enemy1")]
        public string SourceSign;

        [Header("死亡延迟销毁时长 0=立即执行")]
        [Tooltip("死亡后延迟几秒再执行 DeathAction，0 立即执行")]
        public float DeathDelay;

        [Header("死亡后处理")]
        [Tooltip("死亡后处理：None=仅置 Dead 标记，DestroyEntity=销毁实体，DisableEntity=失活预制体")]
        public DeathAction DeathAction;
    }

    public enum DeathAction {
        None = 0,
        DestroyEntity = 1,
        DisableEntity = 2,
    }
}
