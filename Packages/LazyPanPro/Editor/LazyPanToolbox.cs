using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LazyPan {
    public class LazyPanToolbox : EditorWindow {
        private LazyPanTool _tool;
        private bool isFoldoutBehaviour = true;

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
                    Debug.Log("已全量安装/重装，节点已生成");
                }

                string targetFolder = "Packages/evoreek.lazypan/Download/Behaviour/";//遍历目标文件夹下所有的脚本名
                string targetFolderDisk = ResolveToDiskPath(targetFolder);//解析为磁盘路径 虚拟包名与目录名可能不一致
                if (!string.IsNullOrEmpty(targetFolderDisk) && Directory.Exists(targetFolderDisk)) {
                    string[] folders = Directory.GetDirectories(targetFolderDisk);
                    foreach (string folder in folders) {
                        GUILayout.BeginHorizontal();
                        bool installed = BehaviourDownloadInstaller.IsInstalled(folder);
                        string sign = BehaviourDownloadInstaller.FindSign(folder);
                        GUILayout.Label(Path.GetFileName(folder), GUILayout.ExpandWidth(true), GUILayout.Height(18));
                        GUILayout.Label(installed ? "已安装" : "未安装", GUILayout.Width(60), GUILayout.Height(18));
                        if (installed) {
                            if (GUILayout.Button("重装", rowStyle, GUILayout.Width(70), GUILayout.Height(18))) {
                                BehaviourDownloadInstaller.Install(folder, true);
                                Debug.Log("已重装");
                            }
                            if (GUILayout.Button("卸载", rowStyle, GUILayout.Width(70), GUILayout.Height(18))) {
                                if (BehaviourDownloadInstaller.ConfirmUninstall(folder, sign)) {
                                    BehaviourDownloadInstaller.Uninstall(folder, true);
                                    Debug.Log("已卸载");
                                }
                            }
                        } else {
                            if (GUILayout.Button("获取", rowStyle, GUILayout.Width(70), GUILayout.Height(18))) {
                                BehaviourDownloadInstaller.Install(folder, true);
                                Debug.Log("已获取");
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.EndVertical();
                height += GUILayoutUtility.GetLastRect().height;
            } else {
                GUILayout.Space(10);
            }

            LazyPanTool.DrawBorder(new Rect(rect.x + 2f, rect.y - 2f, rect.width - 2f, rect.height + height + 5f), Color.white);

            GUILayout.Space(10);
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