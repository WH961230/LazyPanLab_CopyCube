using System;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 传话包认词单三件套 词名+类型+默认值三位一体 行为头上贴一张 开火节点照单补齐
    /// 词的唯一出处永远是行为代码 配置只是投影 配的人只改数不打字
    /// </summary>
    [Serializable]
    public class PayloadContractDef {
        public string Sign;
        public ParamValueType ValueType;
        public bool BoolDefault;
        public int IntDefault;
        public float FloatDefault;
        public string StringDefault;
        public Vector3 Vector3Default;
    }
}
