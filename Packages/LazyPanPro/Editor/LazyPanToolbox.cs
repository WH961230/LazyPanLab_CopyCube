using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LazyPan {
    public class LazyPanToolbox : EditorWindow {
        private LazyPanTool _tool;
        private bool isFoldoutBehaviour = true;
        private ReorderableList behaviourTable;
        private bool behaviourTableDirty = true;
        private int behaviourFolderCount = -1;

        /// <summary>一行行为两套状态：装没装 × 哪来的，表格只认这个。</summary>
        private class BehaviourRow {
            public string sign;
            public bool installed;
            public bool inPackage;
        }

        /// <summary>
        /// 项目根目录磁盘路径
        /// </summary>
        private static string ProjectRoot {
            get {
                DirectoryInfo info = Directory.GetParent(Application.dataPath);
                return info != null ? info.FullName : null;
            }
        }

        /// <summary>
        /// 将 Unity 虚拟资源路径转换为磁盘绝对路径
        /// 兼容包目录名与 package.json name 不一致(如 Packages/LazyPanPro 注册为 evoreek.lazypan)的情况
        /// </summary>
        private static string ResolveToDiskPath(string virtualPath) {
            if (string.IsNullOrEmpty(virtualPath) || string.IsNullOrEmpty(ProjectRoot)) {
                return null;
            }

            virtualPath = virtualPath.Replace("\\", "/");
            if (virtualPath.StartsWith("Assets/")) {
                return Path.Combine(Application.dataPath, virtualPath.Substring("Assets/".Length));
            }

            if (virtualPath.StartsWith("Packages/")) {
                string[] segments = virtualPath.Split('/');
                if (segments.Length < 2) {
                    return null;
                }

                string packagesDisk = Path.Combine(ProjectRoot, "Packages");
                //优先匹配目录名
                string direct = Path.Combine(packagesDisk, segments[1]);
                if (Directory.Exists(direct) && File.Exists(Path.Combine(direct, "package.json"))) {
                    return Path.Combine(direct, string.Join("/", segments, 2, segments.Length - 2));
                }

                //遍历 Packages 下所有含 package.json 的目录 按 name 字段匹配
                foreach (string dir in Directory.GetDirectories(packagesDisk)) {
                    string packageJson = Path.Combine(dir, "package.json");
                    if (File.Exists(packageJson)) {
                        string content = File.ReadAllText(packageJson);
                        if (Regex.IsMatch(content, "\"name\"\\s*:\\s*\"" + Regex.Escape(segments[1]) + "\"")) {
                            return Path.Combine(dir, string.Join("/", segments, 2, segments.Length - 2));
                        }
                    }
                }

                return null;
            }

            return virtualPath;
        }

        /// <summary>
        /// 读取 meta 文件的 GUID
        /// </summary>
        private static string ReadMetaGuid(string metaPath) {
            if (!File.Exists(metaPath)) {
                return null;
            }

            Match match = Regex.Match(File.ReadAllText(metaPath), "guid:\\s*([0-9a-fA-F]{32})");
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// 递归复制文件夹下所有文件(含 meta)到目标磁盘目录 保持相对结构
        /// </summary>
        private static void CopyDirectoryFiles(string sourceDisk, string targetDisk) {
            if (!Directory.Exists(sourceDisk)) {
                return;
            }

            if (!Directory.Exists(targetDisk)) {
                Directory.CreateDirectory(targetDisk);
            }

            foreach (string file in Directory.GetFiles(sourceDisk, "*", SearchOption.AllDirectories)) {
                string relative = Path.GetRelativePath(sourceDisk, file);
                string targetFile = Path.Combine(targetDisk, relative);
                string targetDir = Path.GetDirectoryName(targetFile);
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir)) {
                    Directory.CreateDirectory(targetDir);
                }

                File.Copy(file, targetFile, true);
            }
        }

        public void OnStart(LazyPanTool lazyPanTool) {
            _tool = lazyPanTool;
            _tool.InitScroll();
        }

        public void OnCustomGUI(float areaX) {
            GUILayout.BeginArea(new Rect(areaX + _tool.scrollOffsetX, 60 + _tool.scrollOffsetY, Screen.width, Screen.height * 10));
            Title();//标题
            BehaviourDownload();
            GUILayout.EndArea();
        }

        private void BehaviourDownload() {
            //根据文件夹分类生成获取按钮
            isFoldoutBehaviour = EditorGUILayout.Foldout(isFoldoutBehaviour, LazyPanTool.GetText("工具箱行为获取展开文本"), true);
            Rect rect = GUILayoutUtility.GetLastRect();
            float height = 0;
            if (isFoldoutBehaviour) {
                GUILayout.Label("");
                height += GUILayoutUtility.GetLastRect().height;
                GUILayout.BeginVertical();

                GUIStyle style = LazyPanTool.GetGUISkin("AButtonGUISkin").GetStyle("button");
                GUIStyle rowStyle = new GUIStyle(style);
                rowStyle.fontSize = 12;
                rowStyle.padding = new RectOffset(4, 4, 0, 0);
                rowStyle.fixedHeight = 0;
                rowStyle.stretchHeight = false;
                rowStyle.alignment = TextAnchor.MiddleCenter;

                if (GUILayout.Button("全部安装或重装", style, GUILayout.ExpandWidth(true))) {
                    BehaviourDownloadInstaller.InstallAll();
                    behaviourTableDirty = true;
                    Debug.Log("已全量安装/重装，节点已生成");
                }

                if (GUILayout.Button("一键卸载所有行为", style, GUILayout.ExpandWidth(true))) {
                    if (EditorUtility.DisplayDialog("一键卸载所有行为", "确定要卸载全部已安装行为吗？\n实体图上节点会变 Missing，重装即恢复。", "确定卸载", "取消")) {
                        BehaviourDownloadInstaller.UninstallAll();
                        behaviourTableDirty = true;
                        Debug.Log("已卸载全部行为");
                    }
                }

                if (GUILayout.Button("检测行为状态", style, GUILayout.ExpandWidth(true))) {
                    behaviourTableDirty = true;
                    int total, pkg, local, inst;
                    CountBehaviours(out total, out pkg, out local, out inst);
                    Debug.Log($"行为状态：共{total}个，插件{pkg}个，纯本地{local}个，已安装{inst}个");
                }
                if (behaviourTableDirty || behaviourTable == null) {
                    RefreshBehaviourTable();
                } else {
                    int total, pkg, local, inst;
                    CountBehaviours(out total, out pkg, out local, out inst);
                    if (total != behaviourFolderCount) {
                        RefreshBehaviourTable();
                    }
                }
                if (behaviourTable != null) {
                    behaviourTable.DoLayoutList();
                    height += GUILayoutUtility.GetLastRect().height;
                }

                GUILayout.EndVertical();
                height += GUILayoutUtility.GetLastRect().height;
            } else {
                GUILayout.Space(10);
            }

            LazyPanTool.DrawBorder(new Rect(rect.x + 2f, rect.y - 2f, rect.width - 2f, rect.height + height + 5f), Color.white);

            GUILayout.Space(10);
        }

        private GUIStyle centerLabelStyle;

        /// <summary>表格字居中：表头和四列共用一个样式，不每帧new。</summary>
        private GUIStyle CenterLabel() {
            if (centerLabelStyle == null) {
                centerLabelStyle = new GUIStyle(EditorStyles.label);
                centerLabelStyle.alignment = TextAnchor.MiddleCenter;
            }
            return centerLabelStyle;
        }

        /// <summary>文件夹名取中文：下划线前面那截就是中文，是不是中文都认。没下划线就认不出。</summary>
        private string FolderChineseName(string dir, string sign) {
            string folderName = Path.GetFileName(dir);
            if (string.IsNullOrEmpty(folderName)) {
                return null;
            }
            int idx = folderName.IndexOf('_');
            if (idx > 0) {
                return folderName.Substring(0, idx);
            }
            return null;
        }

        /// <summary>
        /// 操作区按钮均分居中：N个60宽按钮在区域里左右留白相等，行宽变了自动重算。
        /// </summary>
        private float[] EvenSlots(float x, float width, int count, float btnWidth) {
            var xs = new float[count];
            float gap = count > 0 ? (width - btnWidth * count) / (count + 1) : 0f;
            if (gap < 2f) {
                gap = 2f;
            }
            for (int i = 0; i < count; i++) {
                xs[i] = x + gap * (i + 1) + btnWidth * i;
            }
            return xs;
        }

        /// <summary>行为两套状态计数：总数/插件数/纯本地数/已安装数，检测按钮和哨兵共用。</summary>
        private void CountBehaviours(out int total, out int pkg, out int local, out int inst) {
            pkg = 0;
            inst = 0;
            var packageSigns = new HashSet<string>();
            string disk = ResolveToDiskPath("Packages/evoreek.lazypan/Download/Behaviour/");
            if (!string.IsNullOrEmpty(disk) && Directory.Exists(disk)) {
                foreach (string dir in Directory.GetDirectories(disk)) {
                    string s = BehaviourDownloadInstaller.FindSign(dir);
                    if (string.IsNullOrEmpty(s)) {
                        continue;
                    }
                    pkg++;
                    packageSigns.Add(s);
                    if (BehaviourDownloadInstaller.IsInstalled(dir)) {
                        inst++;
                    }
                }
            }
            local = BehaviourDownloadInstaller.CanUpload() ? BehaviourDownloadInstaller.FindNewBehaviours().Count : 0;
            inst += local;
            total = pkg + local;
        }

        /// <summary>
        /// 行为安装表：跟实体/流程页签一样的预览表格（表头+着色行），行里直接点按钮。
        /// 文件夹变了（上传新建/外部删除）下次打开自动重建，动作做完标脏下一帧刷。
        /// </summary>
        private void RefreshBehaviourTable() {
            behaviourTableDirty = false;
            string targetFolderDisk = ResolveToDiskPath("Packages/evoreek.lazypan/Download/Behaviour/");
            var packageSigns = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(targetFolderDisk) && Directory.Exists(targetFolderDisk)) {
                foreach (string dir in Directory.GetDirectories(targetFolderDisk)) {
                    string s = BehaviourDownloadInstaller.FindSign(dir);
                    if (!string.IsNullOrEmpty(s)) {
                        packageSigns[s] = dir;
                    }
                }
            }
            behaviourFolderCount = packageSigns.Count;
            var rows = new List<BehaviourRow>();
            foreach (var kv in packageSigns) {
                rows.Add(new BehaviourRow {
                    sign = kv.Key,
                    inPackage = true,
                    installed = BehaviourDownloadInstaller.IsInstalled(kv.Value),
                });
            }
            foreach (string sign in BehaviourDownloadInstaller.FindNewBehaviours()) {
                if (!BehaviourDownloadInstaller.CanUpload()) {
                    continue;
                }
                rows.Add(new BehaviourRow { sign = sign, inPackage = false, installed = true });
            }
            rows.Sort((a, b) => string.Compare(a.sign, b.sign));
            behaviourTable = new ReorderableList(rows, typeof(BehaviourRow), false, true, false, false);
            behaviourTable.elementHeight = EditorGUIUtility.singleLineHeight + 6f;
            behaviourTable.headerHeight = EditorGUIUtility.singleLineHeight + 4f;
            behaviourTable.drawHeaderCallback = (Rect rect) => {
                EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.16f));
                float h = EditorGUIUtility.singleLineHeight;
                float colW = rect.width / 5f;
                EditorGUI.LabelField(new Rect(rect.x, rect.y + 2f, colW, h), "名字", CenterLabel());
                EditorGUI.LabelField(new Rect(rect.x + colW, rect.y + 2f, colW, h), "中文", CenterLabel());
                EditorGUI.LabelField(new Rect(rect.x + colW * 2f, rect.y + 2f, colW, h), "安装", CenterLabel());
                EditorGUI.LabelField(new Rect(rect.x + colW * 3f, rect.y + 2f, colW, h), "来源", CenterLabel());
                EditorGUI.LabelField(new Rect(rect.x + colW * 4f, rect.y + 2f, colW, h), "操作", CenterLabel());
            };
            behaviourTable.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                if (index < 0 || index >= rows.Count) {
                    return;
                }
                BehaviourRow row = rows[index];
                string chinese = BehaviourDownloadInstaller.GetChineseName(row.sign);
                if (string.IsNullOrEmpty(chinese) && row.inPackage) {
                    chinese = FolderChineseName(packageSigns[row.sign], row.sign);
                }
                if (string.IsNullOrEmpty(chinese)) {
                    chinese = "未登记";
                }
                Color bg = row.installed
                    ? new Color(0.30f, 0.50f, 0.30f, 0.25f)
                    : new Color(0.50f, 0.50f, 0.50f, 0.15f);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + 1f, rect.width, EditorGUIUtility.singleLineHeight + 4f), bg);
                float h = EditorGUIUtility.singleLineHeight;
                float y = rect.y + 3f;
                float colW = rect.width / 5f;
                EditorGUI.LabelField(new Rect(rect.x, y, colW, h), new GUIContent(row.sign, row.sign), CenterLabel());
                EditorGUI.LabelField(new Rect(rect.x + colW, y, colW, h), new GUIContent(chinese, chinese), CenterLabel());
                EditorGUI.LabelField(new Rect(rect.x + colW * 2f, y, colW, h), row.installed ? "已安装" : "未安装", CenterLabel());
                EditorGUI.LabelField(new Rect(rect.x + colW * 3f, y, colW, h),
                    !row.inPackage ? "本地" : (row.installed ? "本地+插件" : "插件"), CenterLabel());
                float zoneX = rect.x + colW * 4f;
                if (!row.installed && row.inPackage) {
                    string dir = packageSigns[row.sign];
                    float[] xs = EvenSlots(zoneX, colW, 1, 60f);
                    if (GUI.Button(new Rect(xs[0], y, 60f, h), "获取")) {
                        BehaviourDownloadInstaller.Install(dir, true);
                        Debug.Log("已获取");
                        behaviourTableDirty = true;
                    }
                } else if (row.installed && row.inPackage) {
                    string dir = packageSigns[row.sign];
                    bool canUpload = BehaviourDownloadInstaller.CanUpload();
                    float[] xs = EvenSlots(zoneX, colW, canUpload ? 3 : 2, 60f);
                    if (GUI.Button(new Rect(xs[0], y, 60f, h), "重装")) {
                        BehaviourDownloadInstaller.Install(dir, true);
                        Debug.Log("已重装");
                        behaviourTableDirty = true;
                    }
                    if (GUI.Button(new Rect(xs[1], y, 60f, h), "卸载")) {
                        if (BehaviourDownloadInstaller.ConfirmUninstall(dir, row.sign)) {
                            BehaviourDownloadInstaller.Uninstall(dir, true);
                            Debug.Log("已卸载");
                            behaviourTableDirty = true;
                        }
                    }
                    if (canUpload && GUI.Button(new Rect(xs[2], y, 60f, h), "上传")) {
                        BehaviourDownloadInstaller.Upload(row.sign);
                        behaviourTableDirty = true;
                    }
                } else if (row.installed && !row.inPackage) {
                    float[] xs = EvenSlots(zoneX, colW, 2, 60f);
                    if (GUI.Button(new Rect(xs[0], y, 60f, h), "上传")) {
                        BehaviourDownloadInstaller.Upload(row.sign);
                        behaviourTableDirty = true;
                    }
                    if (GUI.Button(new Rect(xs[1], y, 60f, h), "卸载")) {
                        BehaviourDownloadInstaller.UninstallLocal(row.sign);
                        behaviourTableDirty = true;
                    }
                }
            };
        }

        private void AutoCopyBehaviours(string targetFolder) {
            string targetFolderDisk = ResolveToDiskPath(targetFolder);
            if (string.IsNullOrEmpty(targetFolderDisk) || !Directory.Exists(targetFolderDisk)) {
                Debug.LogError($"错误! 未找到行为来源目录:{targetFolder}");
                return;
            }

            //复制脚本(仅当前目录 不含子目录)到行为目录
            Directory.CreateDirectory("Assets/LazyPan/Scripts/GamePlay/Behaviour/");
            foreach (string scriptFile in Directory.GetFiles(targetFolderDisk, "*.cs", SearchOption.TopDirectoryOnly)) {
                string scriptName = Path.GetFileName(scriptFile);
                string targetScriptDisk = ResolveToDiskPath($"Assets/LazyPan/Scripts/GamePlay/Behaviour/{scriptName}");
                File.Copy(scriptFile, targetScriptDisk, true);
            }

            //复制 Setting 资源(含 meta 保留 guid 引用)到配置目录
            string settingSourceDisk = Path.Combine(targetFolderDisk, "Setting");
            string settingTargetDisk = ResolveToDiskPath("Assets/LazyPan/Bundles/Configs/Setting/");
            if (Directory.Exists(settingSourceDisk)) {
                CopyDirectoryFiles(settingSourceDisk, settingTargetDisk);
            }

            //复制 SettingScript 脚本(含 meta)到运行时配置脚本目录
            string settingScriptSourceDisk = Path.Combine(targetFolderDisk, "SettingScript");
            string settingScriptTargetDisk = ResolveToDiskPath("Assets/LazyPan/Scripts/GamePlay/Config/Setting/");
            if (Directory.Exists(settingScriptSourceDisk)) {
                CopyDirectoryFiles(settingScriptSourceDisk, settingScriptTargetDisk);
            }

            //复制 Data 数据脚本(含 meta)到运行时数据脚本目录
            string dataScriptSourceDisk = Path.Combine(targetFolderDisk, "Data");
            string dataScriptTargetDisk = ResolveToDiskPath("Assets/LazyPan/Scripts/GamePlay/Data/");
            if (Directory.Exists(dataScriptSourceDisk)) {
                CopyDirectoryFiles(dataScriptSourceDisk, dataScriptTargetDisk);
            }

            AssetDatabase.Refresh();
            RepairSettingAssets(settingScriptSourceDisk, settingScriptTargetDisk, settingSourceDisk, settingTargetDisk);
        }

        /// <summary>
        /// 修复 Setting 资产:以包内资产内容为准重新落盘 并把 m_Script 指向项目内实际脚本 guid
        /// 同时强制主对象名与文件名一致 消除 Addressables 的命名警告
        /// </summary>
        private void RepairSettingAssets(string settingScriptSourceDisk, string settingScriptTargetDisk, string settingSourceDisk, string settingTargetDisk) {
            if (!Directory.Exists(settingScriptSourceDisk) || !Directory.Exists(settingScriptTargetDisk) ||
                !Directory.Exists(settingSourceDisk) || !Directory.Exists(settingTargetDisk)) {
                return;
            }

            //收集目标目录脚本的实际 guid(脚本名 → guid) 项目内 meta 可能已被 Unity 重新生成
            Dictionary<string, string> scriptGuids = new Dictionary<string, string>();
            foreach (string csFile in Directory.GetFiles(settingScriptTargetDisk, "*.cs", SearchOption.AllDirectories)) {
                string scriptName = Path.GetFileNameWithoutExtension(csFile);
                string csGuid = ReadMetaGuid($"{csFile}.meta");
                if (!string.IsNullOrEmpty(csGuid)) {
                    scriptGuids[scriptName] = csGuid;
                }
            }

            if (scriptGuids.Count == 0) {
                return;
            }

            bool changed = false;
            foreach (string sourceAsset in Directory.GetFiles(settingSourceDisk, "*.asset", SearchOption.AllDirectories)) {
                string relative = Path.GetRelativePath(settingSourceDisk, sourceAsset);
                string targetAsset = Path.Combine(settingTargetDisk, relative);
                string assetName = Path.GetFileNameWithoutExtension(targetAsset);
                if (!scriptGuids.TryGetValue(assetName, out string scriptGuid)) {
                    continue;
                }

                //以包内源资产内容为准(保证 m_Name 与数据完整)
                string content = File.ReadAllText(sourceAsset);
                //修正 m_Script 引用为项目内实际脚本 guid
                content = Regex.Replace(content,
                    @"m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*[0-9a-fA-F]{32},\s*type:\s*3\}",
                    $"m_Script: {{fileID: 11500000, guid: {scriptGuid}, type: 3}}");
                //主对象名与文件名一致(Addressables 要求 避免空名警告)
                content = Regex.Replace(content, @"(?m)^(\s*)m_Name:.*$",
                    match => $"{match.Groups[1].Value}m_Name: {assetName}");

                string oldContent = File.Exists(targetAsset) ? File.ReadAllText(targetAsset) : null;
                if (content != oldContent) {
                    File.WriteAllText(targetAsset, content);
                    changed = true;
                }
            }

            if (changed) {
                AssetDatabase.Refresh();
            }
        }

        private void Title() {
            GUILayout.BeginHorizontal();
            GUIStyle style = LazyPanTool.GetGUISkin("LogoGUISkin").GetStyle("label");
            GUILayout.Label("TOOLBOX", style);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            style = LazyPanTool.GetGUISkin("AnnotationGUISkin").GetStyle("label");
            GUILayout.Label("@" + LazyPanTool.GetText("工具箱小标题"), style);
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
        }
    }
}