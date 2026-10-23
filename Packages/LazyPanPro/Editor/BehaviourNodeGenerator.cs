using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为节点一键生成 按 Behaviour 源码反推 Setting 类型与 Data 类型 追加到 BehaviourGraphNodes.Generated.cs
    /// 行为只管写好三件套(Behaviour + Setting + Data)并在 BehaviourConfig.csv 登记中文名 其余零手写
    /// </summary>
    public static class BehaviourNodeGenerator {
        const string behaviourDir = "Assets/LazyPan/Scripts/GamePlay/Behaviour";
        const string generatedPath = "Assets/LazyPan/Scripts/GamePlay/Graph/BehaviourGraphNodes.Generated.cs";
        const string settingFolder = "Assets/LazyPan/Bundles/Configs/Setting";

        [MenuItem("Assets/Create/LazyPan/一键生成行为节点")]
        public static void GenerateAll() {
            if (!Directory.Exists(behaviourDir)) {
                Debug.LogError($"未找到行为目录:{behaviourDir}");
                return;
            }

            //新项目引导只建 Behaviour/Config/Data/Flow，没有 Graph 目录，先补上，不然写文件直接抛异常中断
            string graphDir = Path.GetDirectoryName(generatedPath);
            if (!Directory.Exists(graphDir)) {
                Directory.CreateDirectory(graphDir);
            }

            List<string> behaviourFiles = Directory.GetFiles(behaviourDir, "Behaviour_*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains("Template")).ToList();
            Dictionary<string, string> nameMap = LoadBehaviourNames();
            string oldContent = File.Exists(generatedPath) ? File.ReadAllText(generatedPath, Encoding.UTF8) : "";
            HashSet<string> existingSigns = CollectExistingBehaviourSigns(oldContent);
            // 清理不存在行为的残留：文件删了/手移了，生成的节点类+视图一起摘掉，不留僵尸
            var liveSigns = new HashSet<string>(behaviourFiles.Select(p => Path.GetFileNameWithoutExtension(p)));
            int pruned = PruneStaleNodes(ref oldContent, existingSigns, liveSigns);
            ReportDeadGraphNodes(liveSigns);
            var sb = new StringBuilder();
            int created = 0;
            int skipped = 0;

            foreach (string file in behaviourFiles) {
                string text = File.ReadAllText(file, Encoding.UTF8);
                string behaviourSign = Path.GetFileNameWithoutExtension(file);
                Match settingMatch = Regex.Match(text, @"LoadAsset<(\w+Setting)>");
                Match dataMatch = Regex.Match(text, @"out\s+(\w+SettingData)\b");
                string dataTypeName = dataMatch.Success ? dataMatch.Groups[1].Value : null;
                if (settingMatch.Success && string.IsNullOrEmpty(dataTypeName)) {
                    dataTypeName = ResolveSettingDataType(settingMatch.Groups[1].Value);
                }
                //兼容 out var 写法：按命名约定 Setting名 + Data 反推（如 InputWASDMoveSetting -> InputWASDMoveSettingData）
                if (settingMatch.Success && string.IsNullOrEmpty(dataTypeName)) {
                    string guess = settingMatch.Groups[1].Value + "Data";
                    if (SettingDataTypeExists(guess)) {
                        dataTypeName = guess;
                    }
                }
                //无 Setting 的行为（如透明桌面）不跳过，按无配置节点生成，开箱即用
                bool hasSetting = settingMatch.Success && !string.IsNullOrEmpty(dataTypeName);
                if (!hasSetting) {
                    Debug.Log($"行为 {behaviourSign} 无 Setting，按无配置节点生成!");
                }

                if (existingSigns.Contains(behaviourSign)) {
                    continue;
                }

                if (!nameMap.TryGetValue(behaviourSign, out string cn) || string.IsNullOrEmpty(cn)) {
                    Debug.LogWarning($"跳过 {behaviourSign}: BehaviourConfig.csv 缺少该行为的中文名 请先登记再生成!");
                    skipped++;
                    continue;
                }

                string cnName = cn;
                string nodeName = "BehaviourNode_" + Regex.Replace(behaviourSign, @"^Behaviour_(Auto|Event|Trigger)_", "");

                if (hasSetting) {
                    EnsureSettingAsset(settingMatch.Groups[1].Value);
                }

                sb.AppendLine("    /// <summary>");
                if (hasSetting) {
                    sb.AppendLine($"    /// {cnName}行为节点 对应 {dataTypeName} 一条");
                    sb.AppendLine($"    /// 参数便签见 BehaviourPayloadDoc.Get(nameof({behaviourSign}))，节点身上只读显示");
                } else {
                    sb.AppendLine($"    /// {cnName}行为节点 无配置，开箱即用");
                }
                sb.AppendLine("    /// </summary>");
                sb.AppendLine("    [Serializable]");
                sb.AppendLine($"    [NodeMenuItem(\"LazyPan/行为/{cnName}\")]");
                sb.AppendLine($"    public class {nodeName} : BehaviourGraphNode {{");
                if (hasSetting) {
                    sb.AppendLine($"        public {dataTypeName} Config;");
                }
                sb.AppendLine($"        public override string name => \"{cnName}\";");
                sb.AppendLine($"        public override string BehaviourSign => nameof({behaviourSign});");
                sb.AppendLine("    }");
                sb.AppendLine();
                created++;
                existingSigns.Add(behaviourSign);
                EnsureNodeView(behaviourSign);
            }

            if (created == 0 && pruned == 0) {
                Debug.Log($"行为节点已是最新 无需生成 跳过:{skipped} 个!");
                return;
            }

            string body = oldContent.Contains("namespace LazyPan") ? StripTailBrace(oldContent) : DefaultHeader();
            body += sb.ToString() + "}\n";
            File.WriteAllText(generatedPath, body, new UTF8Encoding(true, false));
            AssetDatabase.ImportAsset(generatedPath);
            AssetDatabase.Refresh();
            Debug.Log($"已生成 {created} 个行为节点 跳过:{skipped} 个 清理僵尸:{pruned} 个!");
        }

        /// <summary>
        /// 摘僵尸节点：生成文件里有、行为文件没了的类整段删，配套视图一起删。
        /// 图资产里的死节点不动（用户数据），只在控制台点名，让人自己去图里删。
        /// </summary>
        static int PruneStaleNodes(ref string content, HashSet<string> existingSigns, HashSet<string> liveSigns) {
            var stale = existingSigns.Where(s => !liveSigns.Contains(s)).ToList();
            if (stale.Count == 0 || string.IsNullOrEmpty(content)) {
                return 0;
            }
            var lines = content.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None).ToList();
            int removed = 0;
            foreach (string sign in stale) {
                string nodeName = "BehaviourNode_" + Regex.Replace(sign, @"^Behaviour_(Auto|Event|Trigger)_", "");
                int classIdx = -1;
                for (int i = 0; i < lines.Count; i++) {
                    if (lines[i].Contains("public class " + nodeName + " ")) {
                        classIdx = i;
                        break;
                    }
                }
                if (classIdx < 0) {
                    continue;
                }
                int start = classIdx;
                for (int i = classIdx - 1; i >= 0 && i >= classIdx - 6; i--) {
                    if (lines[i].TrimStart().StartsWith("/// <summary>")) {
                        start = i;
                        break;
                    }
                }
                int end = -1;
                for (int i = classIdx; i < lines.Count; i++) {
                    if (lines[i] == "    }") {
                        end = i;
                        break;
                    }
                }
                if (end < 0) {
                    continue;
                }
                lines.RemoveRange(start, end - start + 1);
                existingSigns.Remove(sign);
                removed++;
                string core = Regex.Replace(sign, @"^Behaviour_(Auto|Event|Trigger)_", "");
                string viewPath = $"Assets/LazyPan/Editor/{core}NodeView.cs";
                if (File.Exists(viewPath)) {
                    File.Delete(viewPath);
                    string meta = viewPath + ".meta";
                    if (File.Exists(meta)) {
                        File.Delete(meta);
                    }
                    Debug.Log($"已删除僵尸视图:{viewPath}");
                }
            }
            if (removed > 0) {
                content = string.Join("\n", lines.ToArray());
            }
            return removed;
        }

        /// <summary>
        /// 图资产点名：实体图里还挂着已删行为的节点类，只报告不删，删图是用户的事。
        /// </summary>
        static void ReportDeadGraphNodes(HashSet<string> liveSigns) {
            var liveNodes = new HashSet<string>(liveSigns.Select(s => "BehaviourNode_" + Regex.Replace(s, @"^Behaviour_(Auto|Event|Trigger)_", "")));
            string graphDir = "Assets/LazyPan/Bundles/Configs/Graph";
            if (!Directory.Exists(graphDir)) {
                return;
            }
            foreach (string asset in Directory.GetFiles(graphDir, "*.asset", SearchOption.TopDirectoryOnly)) {
                string text;
                try {
                    text = File.ReadAllText(asset, Encoding.UTF8);
                } catch {
                    continue;
                }
                var dead = new HashSet<string>();
                foreach (Match m in Regex.Matches(text, @"BehaviourNode_\w+")) {
                    if (!liveNodes.Contains(m.Value)) {
                        dead.Add(m.Value);
                    }
                }
                if (dead.Count > 0) {
                    Debug.LogWarning($"图 {Path.GetFileName(asset)} 里还有已删行为的节点:{string.Join("、", dead.ToArray())}，去图里手动删掉");
                }
            }
        }

        /// <summary>
        /// 节点和视图一起生：视图引用节点类，分开生必红一次。视图文件已存在就不碰。
        /// </summary>
        static void EnsureNodeView(string behaviourSign) {
            string core = Regex.Replace(behaviourSign, @"^Behaviour_(Auto|Event|Trigger)_", "");
            string viewPath = $"Assets/LazyPan/Editor/{core}NodeView.cs";
            if (File.Exists(viewPath)) {
                return;
            }
            string templatePath = "Assets/LazyPan/Bundles/Configs/Txt/GenerateNodeViewTemplate.txt";
            if (!File.Exists(templatePath)) {
                Debug.LogWarning($"缺少视图模板:{templatePath}，视图没生成，节点照常用默认视图");
                return;
            }
            string text = File.ReadAllText(templatePath, Encoding.UTF8);
            text = Regex.Replace(text, "#行为标识#", core);
            text = Regex.Replace(text, "#中文名#", core);
            File.WriteAllText(viewPath, text, new UTF8Encoding(true, false));
            AssetDatabase.ImportAsset(viewPath);
            Debug.Log($"已生成节点视图:{viewPath}");
        }

        static Dictionary<string, string> LoadBehaviourNames() {            var map = new Dictionary<string, string>();
            string csv = Path.Combine(Application.streamingAssetsPath, "Csv", "BehaviourConfig.csv");
            if (!File.Exists(csv)) {
                return map;
            }

            string[] lines = File.ReadAllLines(csv, Encoding.UTF8);
            for (int i = 3; i < lines.Length; i++) {
                string[] cols = lines[i].Split(',');
                if (cols.Length < 2) {
                    continue;
                }

                string sign = cols[0].Trim();
                string name = cols[1].Trim();
                if (!string.IsNullOrEmpty(sign) && !string.IsNullOrEmpty(name)) {
                    map[sign] = name;
                }
            }

            return map;
        }

        static bool SettingDataTypeExists(string dataTypeName) {
            foreach (string dir in new[] { "Assets/LazyPan/Scripts/GamePlay/Config/Setting", behaviourDir }) {
                if (!Directory.Exists(dir)) {
                    continue;
                }
                foreach (string f in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)) {
                    string t = File.ReadAllText(f, Encoding.UTF8);
                    if (Regex.IsMatch(t, @"(struct|class)\s+" + Regex.Escape(dataTypeName) + @"\b")) {
                        return true;
                    }
                }
            }
            return false;
        }

        static string ResolveSettingDataType(string settingTypeName) {
            if (string.IsNullOrEmpty(settingTypeName)) {
                return null;
            }
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies()) {
                Type[] types;
                try {
                    types = asm.GetTypes();
                } catch {
                    continue;
                }
                foreach (var t in types) {
                    if (t.Name != settingTypeName || !typeof(Setting).IsAssignableFrom(t)) {
                        continue;
                    }
                    var datasField = t.GetField("Datas");
                    if (datasField == null || !datasField.FieldType.IsGenericType) {
                        return null;
                    }
                    var args = datasField.FieldType.GetGenericArguments();
                    return args.Length == 1 ? args[0].Name : null;
                }
            }
            return null;
        }

        static HashSet<string> CollectExistingBehaviourSigns(string content) {
            var set = new HashSet<string>();
            if (string.IsNullOrEmpty(content)) {
                return set;
            }

            foreach (Match m in Regex.Matches(content, @"nameof\((Behaviour_\w+)\)")) {
                set.Add(m.Groups[1].Value);
            }

            return set;
        }

        static void EnsureSettingAsset(string settingTypeName) {
            string path = $"{settingFolder}/{settingTypeName}.asset";
            if (AssetDatabase.LoadAssetAtPath<Setting>(path) != null) {
                return;
            }

            Type type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => {
                    try {
                        return a.GetTypes();
                    } catch {
                        return new Type[0];
                    }
                })
                .FirstOrDefault(t => t.Name == settingTypeName && typeof(Setting).IsAssignableFrom(t));
            if (type == null) {
                Debug.LogWarning($"未找到 Setting 类型:{settingTypeName} 请先创建 Setting 脚本!");
                return;
            }

            var inst = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(inst, path);
            Debug.Log($"已补建 Setting 资产:{path}");
        }

        static string DefaultHeader() {
            return "using System;\nusing GraphProcessor;\n\nnamespace LazyPan {\n";
        }

        static string StripTailBrace(string content) {
            int idx = content.LastIndexOf('}');
            return idx >= 0 ? content.Substring(0, idx) : content;
        }
    }
}
