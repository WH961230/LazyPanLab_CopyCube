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

            List<string> behaviourFiles = Directory.GetFiles(behaviourDir, "Behaviour_*.cs", SearchOption.TopDirectoryOnly)
                .Where(p => !p.Contains("Template")).ToList();
            Dictionary<string, string> nameMap = LoadBehaviourNames();
            string oldContent = File.Exists(generatedPath) ? File.ReadAllText(generatedPath, Encoding.UTF8) : "";
            HashSet<string> existingSigns = CollectExistingBehaviourSigns(oldContent);
            var sb = new StringBuilder();
            int created = 0;
            int skipped = 0;

            foreach (string file in behaviourFiles) {
                string text = File.ReadAllText(file, Encoding.UTF8);
                string behaviourSign = Path.GetFileNameWithoutExtension(file);
                Match settingMatch = Regex.Match(text, @"LoadAsset<(\w+Setting)>");
                Match dataMatch = Regex.Match(text, @"out\s+(\w+SettingData)\b");
                if (!settingMatch.Success || !dataMatch.Success) {
                    Debug.LogWarning($"跳过 {behaviourSign}: 未识别到 Setting 类型或 SettingData 类型 请检查构造写法是否参考 Death/BeginLogo!");
                    skipped++;
                    continue;
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

                EnsureSettingAsset(settingMatch.Groups[1].Value);

                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// {cnName}行为节点 对应 {dataMatch.Groups[1].Value} 一条");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine("    [Serializable]");
                sb.AppendLine($"    [NodeMenuItem(\"LazyPan/行为/{cnName}\")]");
                sb.AppendLine($"    public class {nodeName} : BehaviourGraphNode {{");
                sb.AppendLine($"        public {dataMatch.Groups[1].Value} Config;");
                sb.AppendLine($"        public override string name => \"{cnName}\";");
                sb.AppendLine($"        public override string BehaviourSign => nameof({behaviourSign});");
                sb.AppendLine("    }");
                sb.AppendLine();
                created++;
                existingSigns.Add(behaviourSign);
            }

            if (created == 0) {
                Debug.Log($"行为节点已是最新 无需生成 跳过:{skipped} 个!");
                return;
            }

            string body = oldContent.Contains("namespace LazyPan") ? StripTailBrace(oldContent) : DefaultHeader();
            body += sb.ToString() + "}\n";
            File.WriteAllText(generatedPath, body, new UTF8Encoding(true, false));
            AssetDatabase.ImportAsset(generatedPath);
            AssetDatabase.Refresh();
            Debug.Log($"已生成 {created} 个行为节点 跳过:{skipped} 个!");
        }

        static Dictionary<string, string> LoadBehaviourNames() {
            var map = new Dictionary<string, string>();
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
