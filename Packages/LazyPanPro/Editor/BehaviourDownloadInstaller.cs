using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 下载行为一键安装：把包内 Download/Behaviour 下每个行为的四件套拷进 Assets，
    /// 并在 StreamingAssets 的 BehaviourConfig.csv 里登记中文名，装完顺手调一键生成节点。
    /// 链条：下载安装 → 一键生成节点 → 实体行填中文名 → 开图见节点。
    /// </summary>
    public static class BehaviourDownloadInstaller {
        private static bool _foldout = true;
        private static Vector2 _scroll;

        private const string BehaviourDir = "Assets/LazyPan/Scripts/GamePlay/Behaviour";
        private const string SettingDir = "Assets/LazyPan/Scripts/GamePlay/Config/Setting";
        private const string DataDir = "Assets/LazyPan/Scripts/GamePlay/Data";
        private const string SettingAssetDir = "Assets/LazyPan/Bundles/Configs/Setting";

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
                foreach (string dir in dirs) {
                    Install(dir, false);
                }
                AssetDatabase.Refresh();
                BehaviourNodeGenerator.GenerateAll();
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(220));
            foreach (string dir in dirs) {
                DrawRow(dir);
            }
            EditorGUILayout.EndScrollView();
            GUILayout.Space(10);
        }

        private static void DrawRow(string dir) {
            string sign = FindSign(dir);
            if (string.IsNullOrEmpty(sign)) {
                return;
            }
            bool installed = File.Exists($"{BehaviourDir}/{sign}.cs");
            bool registered = IsRegistered(sign);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{Path.GetFileName(dir)}", GUILayout.Width(260));
            GUILayout.Label(installed ? "已安装" : "未安装", GUILayout.Width(60));
            GUILayout.Label(registered ? "已登记" : "未登记", GUILayout.Width(60));
            if (GUILayout.Button(installed ? "重装" : "安装", GUILayout.Width(60))) {
                Install(dir, true);
                AssetDatabase.Refresh();
                BehaviourNodeGenerator.GenerateAll();
            }
            GUILayout.EndHorizontal();
        }

        private static string FindSign(string dir) {
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
            int copied = 0;
            copied += CopyFiles(Path.Combine(dir, "SettingScript"), SettingDir, "*.cs");
            copied += CopyFiles(Path.Combine(dir, "Data"), DataDir, "*.cs");
            copied += CopyFiles(dir, BehaviourDir, "Behaviour_*.cs");
            copied += CopyFiles(Path.Combine(dir, "Setting"), SettingAssetDir, "*.asset");
            RegisterCsv(sign, Path.GetFileName(dir));
            if (log) {
                Debug.Log($"行为已安装：{sign}，拷了 {copied} 个文件，中文名已登记。接下来去点一键生成节点（已自动触发一次）。");
            }
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
