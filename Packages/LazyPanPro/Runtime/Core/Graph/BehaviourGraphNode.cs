using System;
using UnityEngine;
using GraphProcessor;

namespace LazyPan {
    /// <summary>
    /// 行为图节点基类 子类各自携带该行为在原 Setting 资产中的那条数据(即 XxxSettingData)
    /// 节点即配置 运行时由图直读合成 不再依赖 Setting 资产
    /// </summary>
    [Serializable]
    public abstract class BehaviourGraphNode : BaseNode {
        /// <summary>对应 BehaviourConfig.csv 的 Sign 运行时按此把配置分发给对应行为</summary>
        public abstract string BehaviourSign { get; }

        /// <summary>配置是否已初始化 已初始化后不再从Setting回填</summary>
        [HideInInspector]
        public bool NodeInitialized;

        /// <summary>上次同步进 Setting 的 SourceSign 改名时靠它找到旧条目清掉 不然 Setting 越攒越多</summary>
        [HideInInspector]
        public string LastSyncedSign = "";

        /// <summary>参数便签：节点身上直接显示，不用悬停。内容从 Behaviour 头上 RequiredPayload 反射来</summary>
        [ReadOnly]
        [TextArea(2, 5)]
        public string 参数便签;

        protected BehaviourGraphNode() {
            try { 参数便签 = GetPayloadMemo(BehaviourSign); } catch { 参数便签 = ""; }
        }

        public override void OnNodeCreated() {
            base.OnNodeCreated();
            参数便签 = GetPayloadMemo(BehaviourSign);
        }

        protected override void Enable() {
            base.Enable();
            if (string.IsNullOrEmpty(参数便签)) {
                try { 参数便签 = GetPayloadMemo(BehaviourSign); } catch { }
            }
        }

        /// <summary>旧图里的空便签一键刷出来，Inspector 里点一下即可</summary>
        [ContextMenu("刷新参数便签")]
        public void RefreshMemo() {
            参数便签 = GetPayloadMemo(BehaviourSign);
        }

        /// <summary>
        /// 参数便签反射获取：BehaviourPayloadDoc 是业务侧（Assets）的类型，包内不能直引。
        /// 业务工程里有它就反射拿到，没有（如纯包新项目）就返回空，不报错。
        /// </summary>
        private static string GetPayloadMemo(string sign) {
            try {
                var type = System.Type.GetType("LazyPan.BehaviourPayloadDoc");
                var method = type?.GetMethod("Get", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                object result = method?.Invoke(null, new object[] { sign });
                return result as string ?? "";
            } catch {
                return "";
            }
        }
    }
}
