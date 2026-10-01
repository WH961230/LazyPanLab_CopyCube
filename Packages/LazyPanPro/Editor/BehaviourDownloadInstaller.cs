using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 下载行为一键安装/卸载：把包内 Download/Behaviour 下每个行为的五件套拷进 Assets，
    /// 并在 StreamingAssets 的 BehaviourConfig.csv 里登记中文名，装完顺手调一键生成节点。
    /// 已安装的行再点就是卸载：删五件套 + 取消登记 + 摘掉生成的节点类，有实体在用先弹窗确认。
    /// 链条：下载安装 → 一键生成节点 → 实体行填中文名 → 开图见节点。
    /// </summary>
    public static class BehaviourDownloadInstaller {
        private static bool _foldout = true;
        private static Vector2 _scroll;

        private const string BehaviourDir = "Assets/LazyPan/Scripts/GamePlay/Behaviour";
        private const string SettingDir = "Assets/LazyPan/Scripts/GamePlay/Config/Setting";
        private const string DataDir = "Assets/LazyPan/Scripts/GamePlay/Data";
        private const string SettingAssetDir = "Assets/LazyPan/Bundles/Configs/Setting";
        private const string EditorDir = "Assets/Editor";

        public static void DrawTool() {
            _foldout = EditorGUILayout.Foldout(_foldout, "下载行为一键安装（装完自动生成节点）", true);
            if (!_foldout) {
                GUILayout.Space(10);
                return;
            }
            string downloadRoot = Path.Combine(LazyPanTool.GetPackageRoot(), "Download", "Behaviour").Replace('\\', '/');
            if (!Directory.Exists(downloadRoot)) {
                EditorGUILayout.HelpBox("包里没找到 Download/Behaviour，请确认包已正确拉取。", MessageType.Warning);
                return;
            }
            string[] dirs = Directory.GetDirectories(downloadRoot);
            if (GUILayout.Button("全部安装", GUILayout.Height(24))) {
                InstallAll();
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(220));
            foreach (string dir in dirs) {
                DrawRow(dir);
            }
            EditorGUILayout.EndScrollView();
            GUILayout.Space(10);
        }

        public static void InstallAll() {
            string downloadRoot = Path.Combine(LazyPanTool.GetPackageRoot(), "Download", "Behaviour").Replace('\\', '/');
            if (!Directory.Exists(downloadRoot)) {
                return;
            }
            foreach (string dir in Directory.GetDirectories(downloadRoot)) {
                Install(dir, false);
            }
            AssetDatabase.Refresh();
            BehaviourNodeGenerator.GenerateAll();
        }

        public static bool IsInstalled(string dir) {
            return FindInstalledCopies(dir).Count > 0;
        }

        private static void DrawRow(string dir) {
            string sign = FindSign(dir);
            if (string.IsNullOrEmpty(sign)) {
                return;
            }
            bool installed = IsInstalled(dir);
            bool registered = IsRegistered(sign);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{Path.GetFileName(dir)}", GUILayout.Width(200));
            GUILayout.Label(installed ? "已安装" : "未安装", GUILayout.Width(60));
            GUILayout.Label(registered ? "已登记" : "未登记", GUILayout.Width(60));
            if (GUILayout.Button(installed ? "重装" : "安装", GUILayout.Width(60))) {
                Install(dir, true);
                AssetDatabase.Refresh();
                BehaviourNodeGenerator.GenerateAll();
            }
            if (installed && GUILayout.Button("卸载", GUILayout.Width(60))) {
                if (ConfirmUninstall(dir, sign)) {
                    Uninstall(dir, true);
                    AssetDatabase.Refresh();
                }
            }
            GUILayout.EndHorizontal();
        }

        public static string FindSign(string dir) {
            string[] files = Directory.GetFiles(dir, "Behaviour_*.cs", SearchOption.TopDirectoryOnly);
            foreach (string file in files) {
                if (file.EndsWith(".meta")) {
                    continue;
                }
                return Path.GetFileNameWithoutExtension(file);
            }
            return null;
        }

        private static bool IsRegistered(string sign) {
            string csv = Path.Combine(Application.streamingAssetsPath, "Csv", "BehaviourConfig.csv");
            if (!File.Exists(csv)) {
                return false;
            }
            foreach (string line in CsvEncoding.ReadAllLines(csv).Skip(3)) {
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                if (line.Split(',')[0].Trim() == sign) {
                    return true;
                }
            }
            return false;
        }

        public static void Install(string dir, bool log) {
            string sign = FindSign(dir);
            if (string.IsNullOrEmpty(sign)) {
                if (log) {
                    Debug.LogWarning($"跳过 {Path.GetFileName(dir)}：里面没有 Behaviour_*.cs");
                }
                return;
            }
            EnsureDir(BehaviourDir);
            EnsureDir(SettingDir);
            EnsureDir(DataDir);
            EnsureDir(SettingAssetDir);
            EnsureDir(EditorDir);
            int copied = 0;
            copied += CopyFiles(Path.Combine(dir, "SettingScript"), SettingDir, "*.cs");
            copied += CopyFiles(Path.Combine(dir, "Data"), DataDir, "*.cs");
            copied += CopyFiles(dir, BehaviourDir, "Behaviour_*.cs");
            copied += CopyFiles(Path.Combine(dir, "NodeView"), EditorDir, "*NodeView.cs");
            copied += CopyFiles(Path.Combine(dir, "Setting"), SettingAssetDir, "*.asset");
            RegisterCsv(sign, Path.GetFileName(dir));
            BehaviourNodeGenerator.GenerateAll();
            if (log) {
                Debug.Log($"行为已安装：{sign}，拷了 {copied} 个文件，中文名已登记，节点已生成。");
            }
        }

        /// <summary>
        /// 卸载：删掉本包当初拷进 Assets 的全部同名文件（平铺+老项目双层目录都认），
        /// 再取消 BehaviourConfig.csv 登记、摘掉生成的节点类。实体图上残留节点会变 Missing，重装即恢复。
        /// </summary>
        public static void Uninstall(string dir, bool log) {
            string sign = FindSign(dir);
            if (string.IsNullOrEmpty(sign)) {
                return;
            }
            List<string> copies = FindInstalledCopies(dir);
            foreach (string f in copies) {
                try {
                    File.Delete(f);
                } catch {
                }
                string meta = f + ".meta";
                if (File.Exists(meta)) {
                    try {
                        File.Delete(meta);
                    } catch {
                    }
                }
            }
            UnregisterCsv(sign);
            RemoveGeneratedNode(sign);
            if (log) {
                Debug.Log($"行为已卸载：{sign}，删了 {copies.Count} 个文件，登记与节点类已摘除。");
            }
        }

        /// <summary>本包已拷进 Assets 的文件（平铺旧错位和双层现行目录都认，NodeView/资产只认顶层）</summary>
        private static List<string> FindInstalledCopies(string dir) {
            var found = new List<string>();
            CollectCopies(Path.Combine(dir, "SettingScript"), "Assets/LazyPan/Scripts/GamePlay/Config", "*.cs", SearchOption.AllDirectories, found);
            CollectCopies(Path.Combine(dir, "Data"), "Assets/LazyPan/Scripts/GamePlay/Data", "*.cs", SearchOption.AllDirectories, found);
            CollectCopies(dir, "Assets/LazyPan/Scripts/GamePlay/Behaviour", "Behaviour_*.cs", SearchOption.AllDirectories, found);
            CollectCopies(Path.Combine(dir, "NodeView"), EditorDir, "*NodeView.cs", SearchOption.TopDirectoryOnly, found);
            CollectCopies(Path.Combine(dir, "Setting"), SettingAssetDir, "*.asset", SearchOption.TopDirectoryOnly, found);
            return found;
        }

        private static void CollectCopies(string srcDir, string dstRoot, string pattern, SearchOption opt, List<string> found) {
            if (!Directory.Exists(srcDir) || !Directory.Exists(dstRoot)) {
                return;
            }
            var names = new HashSet<string>();
            foreach (string f in Directory.GetFiles(srcDir, pattern, SearchOption.TopDirectoryOnly)) {
                if (f.EndsWith(".meta")) {
                    continue;
                }
                names.Add(Path.GetFileName(f));
            }
            foreach (string n in names) {
                foreach (string f in Directory.GetFiles(dstRoot, n, opt)) {
                    if (!f.EndsWith(".meta") && !found.Contains(f)) {
                        found.Add(f);
                    }
                }
            }
        }

        /// <summary>有实体还在用就弹窗确认，没人用直接过</summary>
        public static bool ConfirmUninstall(string dir, string sign) {
            List<string> users = FindReferencingEntities(sign);
            if (users.Count == 0) {
                return true;
            }
            return EditorUtility.DisplayDialog("卸载行为",
                $"{sign} 还被这些实体用着：{string.Join("、", users)}\n\n卸载后它们图上该节点会变 Missing（重装即恢复）。\n确定卸载吗？",
                "确定卸载", "取消");
        }

        /// <summary>ObjConfig 行为列里还写着该行为中文名的实体</summary>
        private static List<string> FindReferencingEntities(string sign) {
            var users = new List<string>();
            string csv = Path.Combine(Application.streamingAssetsPath, "Csv", "BehaviourConfig.csv");
            string objCsv = Path.Combine(Application.streamingAssetsPath, "Csv", "ObjConfig.csv");
            if (!File.Exists(csv) || !File.Exists(objCsv)) {
                return users;
            }
            string chinese = null;
            foreach (string line in CsvEncoding.ReadAllLines(csv).Skip(3)) {
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                string[] cols = line.Split(',');
                if (cols.Length > 0 && cols[0].Trim() == sign) {
                    if (cols.Length > 1) {
                        chinese = cols[1].Trim();
                    }
                    break;
                }
            }
            if (string.IsNullOrEmpty(chinese)) {
                return users;
            }
            foreach (string line in CsvEncoding.ReadAllLines(objCsv).Skip(3)) {
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                string[] cols = line.Split(',');
                if (cols.Length < 6) {
                    continue;
                }
                foreach (string b in cols[5].Split('|')) {
                    if (b.Trim() == chinese) {
                        users.Add(cols[0].Trim());
                        break;
                    }
                }
            }
            return users;
        }

        private static void UnregisterCsv(string sign) {
            string csv = Path.Combine(Application.streamingAssetsPath, "Csv", "BehaviourConfig.csv");
            if (!File.Exists(csv)) {
                return;
            }
            List<string> lines = CsvEncoding.ReadAllLines(csv).ToList();
            bool changed = false;
            for (int i = lines.Count - 1; i >= 3; i--) {
                if (string.IsNullOrWhiteSpace(lines[i])) {
                    continue;
                }
                if (lines[i].Split(',')[0].Trim() == sign) {
                    lines.RemoveAt(i);
                    changed = true;
                }
            }
            if (changed) {
                CsvEncoding.WriteAllLines(csv, lines.ToArray());
            }
        }

        /// <summary>
        /// 摘掉生成的节点类：不摘的话 Data/Setting 脚本删掉后，Generated 里引用野类型直接全工程编译报错。
        /// 块格式固定（3 行说明 + 2 行特性 + 类），往上找到 /// &lt;summary&gt;、往下找到顶格 }，整段删。
        /// </summary>
        private static void RemoveGeneratedNode(string sign) {
            string path = "Assets/LazyPan/Scripts/GamePlay/Graph/BehaviourGraphNodes.Generated.cs";
            if (!File.Exists(path)) {
                return;
            }
            string nodeName = "BehaviourNode_" + Regex.Replace(sign, @"^Behaviour_(Auto|Event|Trigger)_", "");
            List<string> lines = File.ReadAllLines(path, Encoding.UTF8).ToList();
            int classIdx = -1;
            for (int i = 0; i < lines.Count; i++) {
                if (lines[i].Contains("public class " + nodeName + " ")) {
                    classIdx = i;
                    break;
                }
            }
            if (classIdx < 0) {
                return;
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
                return;
            }
            lines.RemoveRange(start, end - start + 1);
            File.WriteAllLines(path, lines.ToArray(), new UTF8Encoding(true));
            Debug.Log($"已摘除生成节点类：{nodeName}");
        }

        private static void EnsureDir(string dir) {
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
        }

        private static int CopyFiles(string srcDir, string dstDir, string pattern) {
            if (!Directory.Exists(srcDir)) {
                return 0;
            }
            int count = 0;
            foreach (string file in Directory.GetFiles(srcDir, pattern, SearchOption.TopDirectoryOnly)) {
                if (file.EndsWith(".meta")) {
                    continue;
                }
                File.Copy(file, Path.Combine(dstDir, Path.GetFileName(file)), true);
                count++;
            }
            return count;
        }

        private static void RegisterCsv(string sign, string folderName) {
            string csv = Path.Combine(Application.streamingAssetsPath, "Csv", "BehaviourConfig.csv");
            if (!File.Exists(csv)) {
                Debug.LogError($"登记失败：{csv} 不存在，请先跑引导第二步拷贝 Csv！");
                return;
            }
            List<string> lines = CsvEncoding.ReadAllLines(csv).ToList();
            for (int i = 3; i < lines.Count; i++) {
                if (string.IsNullOrWhiteSpace(lines[i])) {
                    continue;
                }
                if (lines[i].Split(',')[0].Trim() == sign) {
                    return;
                }
            }
            string chinese = GuessChineseName(folderName, sign);
            lines.Add($"{sign},{chinese},{chinese}");
            CsvEncoding.WriteAllLines(csv, lines.ToArray());
        }

        private static string GuessChineseName(string folderName, string sign) {
            int idx = folderName.IndexOf('_');
            if (idx > 0) {
                string head = folderName.Substring(0, idx);
                foreach (char c in head) {
                    if (c >= 0x4E00 && c <= 0x9FFF) {
                        return head;
                    }
                }
            }
            Debug.LogWarning($"{sign} 的文件夹名里没中文，中文名先用 Sign 顶着，记得去 BehaviourConfig.csv 里改！");
            return sign;
        }
    }
}
