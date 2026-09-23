using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LazyPan {
    /// <summary>
    /// 行为参数便签：反射读 Behaviour 头上的 MemoDoc（完整用户说明优先显示）/ RequiredPayload / RequiredModules，
    /// 图节点 tooltip 直接显示，不看源码、不手写文档。
    /// </summary>
    public static class BehaviourPayloadDoc {
        public static string Get(string behaviourSign) {
            if (string.IsNullOrEmpty(behaviourSign)) {
                return "参数便签：未知行为";
            }
            Type t = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } })
                .FirstOrDefault(x => x.Name == behaviourSign);
            if (t == null) {
                return $"参数便签：找不到行为 {behaviourSign}";
            }

            // 标题用人话（BehaviourConfig.csv 的中文名），取不到才回退类名
            string displayName = behaviourSign;
            try {
                BehaviourConfig.GetKeys();
                BehaviourConfig cfg = BehaviourConfig.Get(behaviourSign);
                if (cfg != null && !string.IsNullOrEmpty(cfg.Name)) {
                    displayName = cfg.Name.Trim();
                }
            } catch { }

            var sb = new StringBuilder();
            sb.AppendLine($"【{displayName}】");

            FieldInfo memo = t.GetField("MemoDoc", BindingFlags.Public | BindingFlags.Static);
            string memoText = memo?.GetValue(null) as string;
            if (!string.IsNullOrEmpty(memoText)) {
                sb.AppendLine(memoText.Trim());
                sb.AppendLine();
            }

            FieldInfo payload = t.GetField("RequiredPayload", BindingFlags.Public | BindingFlags.Static);
            object[] defs = payload?.GetValue(null) as object[];
            if (defs == null || defs.Length == 0) {
                sb.AppendLine("— 外部参数 —");
                sb.AppendLine("无，本行为开箱即用，无需配置。");
            } else {
                sb.AppendLine("— 需配参数（去 ParamValue 里配）—");
                foreach (object d in defs) {
                    if (d is PayloadContractDef c) {
                        sb.AppendLine($"- {c.Sign}：{c.ValueType}，默认 {DefaultOf(c)}");
                    }
                }
            }

            FieldInfo mods = t.GetField("RequiredModules", BindingFlags.Public | BindingFlags.Static);
            string[] m = mods?.GetValue(null) as string[];
            if (m != null && m.Length > 0) {
                sb.AppendLine();
                sb.AppendLine("— 依赖模块 —");
                sb.AppendLine(string.Join("、", m));
            }
            return sb.ToString().TrimEnd();
        }

        static string DefaultOf(PayloadContractDef c) {
            switch (c.ValueType) {
                case ParamValueType.Bool: return c.BoolDefault.ToString();
                case ParamValueType.Int: return c.IntDefault.ToString();
                case ParamValueType.Float: return c.FloatDefault.ToString();
                case ParamValueType.String: return c.StringDefault ?? "";
                case ParamValueType.Vector3: return c.Vector3Default.ToString();
                default: return "";
            }
        }
    }
}
