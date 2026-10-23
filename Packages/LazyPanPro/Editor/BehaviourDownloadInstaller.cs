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
        private const string EditorDir = "Assets/LazyPan/Editor";

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
            if (GUILayout.Button("一键卸载所有行为", GUILayout.Height(24))) {
                if (EditorUtility.DisplayDialog("一键卸载所有行为", "确定要卸载全部已安装行为吗？", "确定卸载", "取消")) {
                    UninstallAll();
                }
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

        public static void UninstallAll() {
            string downloadRoot = Path.Combine(LazyPanTool.GetPackageRoot(), "Download", "Behaviour").Replace('\\', '/');
            if (!Directory.Exists(downloadRoot)) {
                return;
            }
            foreach (string dir in Directory.GetDirectories(downloadRoot)) {
                if (!IsInstalled(dir)) {
                    continue;
                }
                Uninstall(dir, false);
            }
            AssetDatabase.Refresh();
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
            var names = Directory.GetFiles(dir, "Behaviour_*.cs", SearchOption.TopDirectoryOnly)
                .Where(ff => !ff.EndsWith(".meta"))
                .Select(ff => Path.GetFileNameWithoutExtension(ff))
                .ToList();
            if (names.Count == 0) {
                return null;
            }
            if (names.Count == 1) {
                return names[0];
            }
            string folder = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            foreach (string name in names) {
                string core = Regex.Replace(name, @"^Behaviour_(Auto|Event|Trigger)_", "");
                if (core == folder || folder.EndsWith("_" + core)) {
                    return name;
                }
            }
            foreach (string name in names) {
                if (!name.Contains("Template")) {
                    return name;
                }
            }
            return names[0];
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
            string problem = VerifyNodeGenerated(sign);
            if (!string.IsNullOrEmpty(problem)) {
                Debug.LogError($"行为已安装：{sign}，但节点没生成：{problem}");
            } else if (log) {
                Debug.Log($"行为已安装：{sign}，拷了 {copied} 个文件，中文名已登记，节点已生成。");
            }
        }

        /// <summary>
        /// 验节点：生成文件里有没有这个行为的节点类，没有就定位断在哪一环，免得右键菜单空了还不知道
        /// </summary>
        public static string VerifyNodeGenerated(string sign) {
            string path = "Assets/LazyPan/Scripts/GamePlay/Graph/BehaviourGraphNodes.Generated.cs";
            if (!File.Exists(path)) {
                return "节点文件不存在，请点菜单“Assets/Create/LazyPan/一键生成行为节点”跑一次";
            }
            if (File.ReadAllText(path, Encoding.UTF8).Contains("nameof(" + sign + ")")) {
                return null;
            }
            if (!IsRegistered(sign)) {
                return "登记表缺行：BehaviourConfig.csv 里没有 " + sign + "，去工具箱点一次重装（自动登记）";
            }
            return "源码未识别：行为源码里没找到 Setting 写法，节点生成器跳过了（看控制台黄色警告）";
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
            // 删完立即刷资源+请求编译，不然图里还挂着旧节点类，目录消不掉
            AssetDatabase.Refresh();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            if (log) {
                Debug.Log($"行为已卸载：{sign}，删了 {copies.Count} 个文件，登记与节点类已摘除。");
            }
        }

        /// <summary>
        /// 卸载本地行为：包里没有的，Assets里那套（逻辑/参数/数据/视图/资产）+登记+节点类一起摘。
        /// 有实体在用先弹窗确认，删完刷资源+请求编译，图里目录跟着消。
        /// </summary>
        public static void UninstallLocal(string sign) {
            if (string.IsNullOrEmpty(sign)) {
                return;
            }
            List<string> users = FindReferencingEntities(sign);
            if (users.Count > 0 && !EditorUtility.DisplayDialog("卸载本地行为",
                    $"{sign} 还被这些实体用着：{string.Join("、", users)}\n\n卸载后它们图上该节点会变 Missing。\n确定卸载吗？",
                    "确定卸载", "取消")) {
                return;
            }
            string core = CoreOf(sign);
            int deleted = 0;
            foreach (string f in new[] {
                FirstFile(BehaviourDir, sign + ".cs", SearchOption.AllDirectories),
                FirstFile(SettingDir, core + "Setting.cs", SearchOption.AllDirectories),
                FirstFile(DataDir, core + "Data.cs", SearchOption.AllDirectories),
                FirstFile(EditorDir, core + "NodeView.cs", SearchOption.TopDirectoryOnly),
                FirstFile(SettingAssetDir, core + "Setting.asset", SearchOption.TopDirectoryOnly),
            }) {
                if (string.IsNullOrEmpty(f)) {
                    continue;
                }
                try {
                    File.Delete(f);
                    deleted++;
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
            AssetDatabase.Refresh();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            Debug.Log($"本地行为已卸载：{sign}，删了 {deleted} 个文件，登记与节点类已摘除。");
        }

        /// <summary>
        /// 能不能上传：只有插件自己工程（包文件夹名就是 LazyPanPro，可写）才开上传。
        /// 外面下载插件的用户包在只读缓存里，没有可写的源文件夹，上传按钮直接藏掉。
        /// </summary>
        public static bool CanUpload() {
            string root = LazyPanTool.GetPackageRoot();
            if (string.IsNullOrEmpty(root)) {
                return false;
            }
            if (Path.GetFileName(root.TrimEnd('/', '\\')) != "LazyPanPro") {
                return false;
            }
            string downloadRoot = Path.Combine(root, "Download", "Behaviour").Replace('\\', '/');
            return Directory.Exists(downloadRoot);
        }

        /// <summary>Sign去前缀取核心名：Behaviour_Auto_Test → Test，文件夹名/文件名派生都靠它</summary>
        public static string CoreOf(string sign) {
            return Regex.Replace(sign ?? "", @"^Behaviour_(Auto|Event|Trigger)_", "");
        }

        /// <summary>按Sign找包里文件夹：有就返回，没有返回空（新建时再按 中文名_核心名 建）</summary>
        public static string FindDownloadDir(string sign) {
            string downloadRoot = Path.Combine(LazyPanTool.GetPackageRoot(), "Download", "Behaviour").Replace('\\', '/');
            if (!Directory.Exists(downloadRoot)) {
                return null;
            }
            foreach (string dir in Directory.GetDirectories(downloadRoot)) {
                if (FindSign(dir) == sign) {
                    return dir;
                }
            }
            return null;
        }

        /// <summary>按Sign从BehaviourConfig.csv取中文名，没有返回空</summary>
        public static string GetChineseName(string sign) {
            string csv = Path.Combine(Application.streamingAssetsPath, "Csv", "BehaviourConfig.csv");
            if (!File.Exists(csv)) {
                return null;
            }
            foreach (string line in CsvEncoding.ReadAllLines(csv).Skip(3)) {
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                string[] cols = line.Split(',');
                if (cols.Length > 1 && cols[0].Trim() == sign) {
                    return cols[1].Trim();
                }
            }
            return null;
        }

        /// <summary>新行为清单：Assets里有、包里没有的Sign。包里老行不管，只管第一次入库。</summary>
        public static List<string> FindNewBehaviours() {
            var result = new List<string>();
            if (!Directory.Exists(BehaviourDir)) {
                return result;
            }
            var downloadSigns = new HashSet<string>();
            string downloadRoot = Path.Combine(LazyPanTool.GetPackageRoot(), "Download", "Behaviour").Replace('\\', '/');
            if (Directory.Exists(downloadRoot)) {
                foreach (string dir in Directory.GetDirectories(downloadRoot)) {
                    string s = FindSign(dir);
                    if (!string.IsNullOrEmpty(s)) {
                        downloadSigns.Add(s);
                    }
                }
            }
            foreach (string file in Directory.GetFiles(BehaviourDir, "Behaviour_*.cs", SearchOption.TopDirectoryOnly)) {
                if (file.EndsWith(".meta")) {
                    continue;
                }
                string sign = Path.GetFileNameWithoutExtension(file);
                if (sign.Contains("Template") || downloadSigns.Contains(sign)) {
                    continue;
                }
                result.Add(sign);
            }
            result.Sort();
            return result;
        }

        /// <summary>
        /// 上传：安装的反向。Assets里跑通的收进包 Download/Behaviour/中文名_核心名/ 六格归位。
        /// 文件夹有就覆盖更新，没有就新建；Doc缺就套空架子并黄字提醒去填。
        /// </summary>
        public static void Upload(string sign) {
            if (string.IsNullOrEmpty(sign)) {
                return;
            }
            if (!CanUpload()) {
                Debug.LogError($"上传失败:{sign} 当前工程不是插件开发工程（包文件夹下没有可写的 LazyPanPro），上传按钮不该出现！");
                return;
            }
            string chinese = GetChineseName(sign);
            if (string.IsNullOrEmpty(chinese)) {
                Debug.LogError($"上传失败:{sign} BehaviourConfig.csv 里没登记中文名，先登记再传！");
                return;
            }
            string downloadRoot = Path.Combine(LazyPanTool.GetPackageRoot(), "Download", "Behaviour").Replace('\\', '/');
            if (!Directory.Exists(downloadRoot)) {
                Debug.LogError($"上传失败:包里没找到 Download/Behaviour");
                return;
            }
            string dir = FindDownloadDir(sign);
            if (string.IsNullOrEmpty(dir)) {
                dir = Path.Combine(downloadRoot, string.Concat(chinese, "_", CoreOf(sign)));
            }
            if (Directory.Exists(dir) && !EditorUtility.DisplayDialog("上传行为",
                    $"{sign} 的包文件夹已存在，覆盖更新吗？\n{dir}", "覆盖上传", "取消")) {
                return;
            }
            string core = CoreOf(sign);
            int copied = 0;
            copied += CopyOne(FirstFile(BehaviourDir, sign + ".cs", SearchOption.AllDirectories), dir);
            copied += CopyOne(FirstFile(SettingDir, core + "Setting.cs", SearchOption.AllDirectories), Path.Combine(dir, "SettingScript"));
            copied += CopyOne(FirstFile(DataDir, core + "Data.cs", SearchOption.AllDirectories), Path.Combine(dir, "Data"));
            copied += CopyOne(FirstFile(EditorDir, core + "NodeView.cs", SearchOption.TopDirectoryOnly), Path.Combine(dir, "NodeView"));
            copied += CopyOne(FirstFile(SettingAssetDir, core + "Setting.asset", SearchOption.TopDirectoryOnly), Path.Combine(dir, "Setting"));
            if (!Directory.Exists(Path.Combine(dir, "Doc")) || Directory.GetFiles(Path.Combine(dir, "Doc"), "*.md").Length == 0) {
                WriteDocStub(dir, sign, chinese);
            }
            AssetDatabase.Refresh();
            Debug.Log($"行为已上传:{sign} → {dir}，收了 {copied} 个文件。");
        }

        static string FirstFile(string root, string name, SearchOption opt) {
            if (!Directory.Exists(root)) {
                return null;
            }
            foreach (string f in Directory.GetFiles(root, name, opt)) {
                if (!f.EndsWith(".meta")) {
                    return f;
                }
            }
            return null;
        }

        static int CopyOne(string src, string dstDir) {
            if (string.IsNullOrEmpty(src) || !File.Exists(src)) {
                Debug.LogWarning($"上传缺件跳过:{src ?? "?"}");
                return 0;
            }
            if (!Directory.Exists(dstDir)) {
                Directory.CreateDirectory(dstDir);
            }
            File.Copy(src, Path.Combine(dstDir, Path.GetFileName(src)), true);
            return 1;
        }

        static void WriteDocStub(string dir, string sign, string chinese) {
            string docDir = Path.Combine(dir, "Doc");
            if (!Directory.Exists(docDir)) {
                Directory.CreateDirectory(docDir);
            }
            string templatePath = "Assets/LazyPan/Bundles/Configs/Txt/GenerateDocTemplate.txt";
            string text;
            if (File.Exists(templatePath)) {
                text = File.ReadAllText(templatePath);
                text = Regex.Replace(text, "#中文名#", chinese ?? sign);
                text = Regex.Replace(text, "#行为标识#", CoreOf(sign));
            } else {
                text = $"# 行为设计文档：{chinese}_{CoreOf(sign)}\n\nTODO：按九节格式补全。\n";
            }
            File.WriteAllText(Path.Combine(docDir, string.Concat("Behaviour_", sign, ".md")), text);
            Debug.LogWarning($"{sign} 的 Doc 是空架子，记得去包里补全！");
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
