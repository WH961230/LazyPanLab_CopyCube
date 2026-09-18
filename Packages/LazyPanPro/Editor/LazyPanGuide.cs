using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LazyPan {
    public class LazyPanGuide : EditorWindow {
        private int oldSelectedLanguageIndex = 0;
        private bool isFoldoutLanguage = true;
        private bool oldOpenTabSliderAnim;
        private bool isOpenTabSliderAnim = true;
        public static string[] languages = { "English", "中文", "日本語"};
        public static int selectedLanguageIndex = 0;
        public static bool openTabSliderAnim;

        private bool isFoldout = true;
        private LazyPanTool _tool;

        private void OnEnable() {
            int savedIndex = Mathf.Clamp(EditorPrefs.GetInt("SelectedLanguageIndex", 0), 0, languages.Length - 1);
            LazyPanTool.InitLanguage();

            if (EditorPrefs.HasKey(LazyPanTool.LanguagePreferenceKey)) {
                selectedLanguageIndex = GetLanguageIndex(LazyPanTool._currentLanguage);
            } else {
                selectedLanguageIndex = savedIndex;
            }

            oldSelectedLanguageIndex = selectedLanguageIndex;
            LazyPanTool.CheckLanguage(languages[selectedLanguageIndex]);

            openTabSliderAnim = EditorPrefs.GetBool("OpenSliderAnim");//是否开启滑动动画
            LazyPanTool._currentTabSliderAnim = openTabSliderAnim;
        }

        private static int GetLanguageIndex(string language) {
            for (int i = 0; i < languages.Length; i++) {
                if (languages[i] == language) {
                    return i;
                }
            }

            return 0;
        }
        
        public void OnStart(LazyPanTool tool) {
            _tool = tool;
        }

        public void OnCustomGUI(float areaX) {
            GUILayout.BeginArea(new Rect(areaX, 60, Screen.width, Screen.height));

            GUIStyle style = LazyPanTool.GetGUISkin("LogoGUISkin").GetStyle("label");
            GUIStyle redStyle = new GUIStyle(style); // 复制现有的样式
            redStyle.normal.textColor = Color.red; // 设置文本颜色为红色

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            GUILayout.Label("LAZYPAN", style);
            GUILayout.Label("PRO", redStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            style = LazyPanTool.GetGUISkin("AnnotationGUISkin").GetStyle("label");
            GUILayout.Label("@" + LazyPanTool.GetText("引导小标题"), style);
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);

            CloseSliderAnim();
            ChangeLanguage();

            EditorStyles.foldout.fontSize = 20;
            EditorStyles.foldout.fontStyle = FontStyle.Bold;
            isFoldout = EditorGUILayout.Foldout(isFoldout, LazyPanTool.GetText("环境配置"), true);
            Rect rect = GUILayoutUtility.GetLastRect();
            float height = 0;
            if (isFoldout) {
                GUILayout.Space(10);
                height += GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                style = LazyPanTool.GetGUISkin("TitleGUISkin").GetStyle("label");
                GUILayout.Label(LazyPanTool.GetText("配置提示"), style);
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                style = LazyPanTool.GetGUISkin("AButtonGUISkin").GetStyle("button");
                if (GUILayout.Button(LazyPanTool.GetText("清除目录"), style)) {
                    DeleteExitDirectory();
                }
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                style = LazyPanTool.GetGUISkin("TitleGUISkin").GetStyle("label");
                GUILayout.Label(LazyPanTool.GetText("第一步标题"), style);
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                style = LazyPanTool.GetGUISkin("AButtonGUISkin").GetStyle("button");
                if (GUILayout.Button(LazyPanTool.GetText("第一步按钮"), style)) {
                    CreateBaseFilePath();
                    Repaint();
                }
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                style = LazyPanTool.GetGUISkin("TitleGUISkin").GetStyle("label");
                GUILayout.Label(LazyPanTool.GetText("第二步标题"), style);
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                style = LazyPanTool.GetGUISkin("AButtonGUISkin").GetStyle("button");
                if (GUILayout.Button(LazyPanTool.GetText("第二步按钮"), style)) {
                    CopyFilesToDirectory("Bundles/Configs/Input", "LazyPan/Bundles/Configs/Input");
                    CopyFilesToDirectory("Bundles/Configs/Setting", "LazyPan/Bundles/Configs/Setting");
                    CopyFilesToDirectory("Bundles/Configs/Txt", "LazyPan/Bundles/Configs/Txt");
                    CopyFilesToDirectory("Bundles/Prefabs/Global", "LazyPan/Bundles/Prefabs/Global");
                    CopyFilesToDirectory("Bundles/Prefabs/Obj", "LazyPan/Bundles/Prefabs/Obj");
                    CopyFilesToDirectory("Bundles/Prefabs/Tool", "LazyPan/Bundles/Prefabs/Tool");
                    CopyFilesToDirectory("Bundles/Prefabs/UI", "LazyPan/Bundles/Prefabs/UI");
                    CopyFilesToDirectory("Bundles/Scenes", "LazyPan/Bundles/Scenes");
                    CopyFilesToDirectory("Bundles/TextMeshPro", "");
                    CopyFilesToDirectory("Bundles/Csv/StreamingAssets/Csv", "StreamingAssets/Csv");
                    AssetDatabase.Refresh();
                    Repaint();
                }

                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                style = LazyPanTool.GetGUISkin("TitleGUISkin").GetStyle("label");
                GUILayout.Label(LazyPanTool.GetText("第三步标题"), style);
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                style = LazyPanTool.GetGUISkin("AButtonGUISkin").GetStyle("button");
                if (GUILayout.Button(LazyPanTool.GetText("第三步按钮"), style)) {
                    CreateAddressableAsset();
                    AutoInstallAddressableData();
                    Repaint();
                }
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                style = LazyPanTool.GetGUISkin("TitleGUISkin").GetStyle("label");
                GUILayout.Label(LazyPanTool.GetText("第四步标题"), style);
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                style = LazyPanTool.GetGUISkin("AButtonGUISkin").GetStyle("button");
                if (GUILayout.Button(LazyPanTool.GetText("第四步按钮"), style)) {
                    MoveSceneToBuildSettings();
                    Repaint();
                }
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                style = LazyPanTool.GetGUISkin("TitleGUISkin").GetStyle("label");
                GUILayout.Label(LazyPanTool.GetText("第五步标题"), style);
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                style = LazyPanTool.GetGUISkin("AButtonGUISkin").GetStyle("button");
                if (GUILayout.Button(LazyPanTool.GetText("第五步按钮"), style)) {
                    AutoGenerateFlow();
                    Repaint();
                }
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                style = LazyPanTool.GetGUISkin("TitleGUISkin").GetStyle("label");
                GUILayout.Label(LazyPanTool.GetText("第六步标题"), style);
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                style = LazyPanTool.GetGUISkin("AButtonGUISkin").GetStyle("button");
                if (GUILayout.Button(LazyPanTool.GetText("第六步按钮"), style)) {
                    AutoGenerateBehaviour();
                    Repaint();
                }
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                style = LazyPanTool.GetGUISkin("TitleGUISkin").GetStyle("label");
                GUILayout.Label(LazyPanTool.GetText("第七步标题"), style);
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                style = LazyPanTool.GetGUISkin("AButtonGUISkin").GetStyle("button");
                if (GUILayout.Button(LazyPanTool.GetText("第七步按钮"), style)) {
                    EditorSceneManager.OpenScene("Assets/LazyPan/Bundles/Scenes/Launch.unity");
                }
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;
                height += 80f;
            } else {
                GUILayout.Space(10);
            }
            
            LazyPanTool.DrawBorder(new Rect(rect.x + 2f, rect.y - 2f, rect.width - 2f, rect.height + height + 5f), Color.white);

            GUILayout.EndArea();
        }

        private void CloseSliderAnim() {
            EditorStyles.foldout.fontSize = 20;
            EditorStyles.foldout.fontStyle = FontStyle.Bold;
            isOpenTabSliderAnim = EditorGUILayout.Foldout(isOpenTabSliderAnim, LazyPanTool.GetText("标签页面滑动动画"), true);
            Rect lastRect = GUILayoutUtility.GetLastRect();
            float height = 0;
            if (isOpenTabSliderAnim) {
                GUILayout.Space(10);
                height += GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                openTabSliderAnim = EditorGUILayout.Toggle(LazyPanTool.GetText("是否开启标签页面滑动动画"), openTabSliderAnim);
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;
                GUILayout.Space(5);
                //语言的选项变更
                if (oldOpenTabSliderAnim != openTabSliderAnim) {
                    LazyPanTool.CheckTabSliderAnim(openTabSliderAnim);
                    EditorPrefs.SetBool("OpenSliderAnim", openTabSliderAnim);
                    oldOpenTabSliderAnim = openTabSliderAnim;
                }
            } else {
                GUILayout.Space(10);
            }

            LazyPanTool.DrawBorder(
                new Rect(lastRect.x + 2f, lastRect.y - 2f, lastRect.width - 2f,
                    lastRect.height + height + 5f), Color.white);

            GUILayout.Space(10);
        }

        private void ChangeLanguage() {
            EditorStyles.foldout.fontSize = 20;
            EditorStyles.foldout.fontStyle = FontStyle.Bold;
            isFoldoutLanguage = EditorGUILayout.Foldout(isFoldoutLanguage, LazyPanTool.GetText("选择语言"), true);
            Rect rectLanguage = GUILayoutUtility.GetLastRect();
            float heightLanguage = 0;
            if (isFoldoutLanguage) {
                GUILayout.Space(10);
                heightLanguage += GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                selectedLanguageIndex = EditorGUILayout.Popup(LazyPanTool.GetText("选择语言"), selectedLanguageIndex, languages);
                GUILayout.EndHorizontal();
                heightLanguage += GUILayoutUtility.GetLastRect().height;
                GUILayout.Space(5);
                //语言的选项变更
                if (oldSelectedLanguageIndex != selectedLanguageIndex) {
                    LazyPanTool.CheckLanguage(languages[selectedLanguageIndex]);
                    EditorPrefs.SetInt("SelectedLanguageIndex", selectedLanguageIndex);
                    oldSelectedLanguageIndex = selectedLanguageIndex;
                    GUI.changed = true;
                    _tool?.Repaint();
                    Repaint();
                }
            } else {
                GUILayout.Space(10);
            }

            LazyPanTool.DrawBorder(
                new Rect(rectLanguage.x + 2f, rectLanguage.y - 2f, rectLanguage.width - 2f,
                    rectLanguage.height + heightLanguage + 5f), Color.white);

            GUILayout.Space(10);
        }

        private void DeleteExitDirectory() {
            string pathA = "Assets/AddressableAssetsData";
            string pathB = "Assets/LazyPan";
            string pathC = "Assets/StreamingAssets";
            string pathD = "Assets/TextMesh Pro";
            if (AssetDatabase.IsValidFolder(pathA)) {
                AssetDatabase.DeleteAsset(pathA);
            }
            if (AssetDatabase.IsValidFolder(pathB)) {
                AssetDatabase.DeleteAsset(pathB);
            }
            if (AssetDatabase.IsValidFolder(pathC)) {
                AssetDatabase.DeleteAsset(pathC);
            }
            if (AssetDatabase.IsValidFolder(pathD)) {
                AssetDatabase.DeleteAsset(pathD);
            }
            AssetDatabase.Refresh();
        }

        private void CreateBaseFilePath() {
            string targetBundlesPath = "Assets/LazyPan/Bundles";

            string targetBundlesConfigsPath = "Assets/LazyPan/Bundles/Configs";
            string targetBundlesConfigsInputPath = "Assets/LazyPan/Bundles/Configs/Input";
            string targetBundlesConfigsTxtPath = "Assets/LazyPan/Bundles/Configs/Txt";
            string targetBundlesConfigsSettingPath = "Assets/LazyPan/Bundles/Configs/Setting";
            string targetBundlesConfigsSettingLocationInformationSettingPath =
                "Assets/LazyPan/Bundles/Configs/Setting/LocationInformationSetting";

            string targetBundlesImagesPath = "Assets/LazyPan/Bundles/Images";

            string targetBundlesMaterialsPath = "Assets/LazyPan/Bundles/Materials";

            string targetBundlesPrefabsPath = "Assets/LazyPan/Bundles/Prefabs";
            string targetBundlesPrefabsGlobalPath = "Assets/LazyPan/Bundles/Prefabs/Global";
            string targetBundlesPrefabsObjPath = "Assets/LazyPan/Bundles/Prefabs/Obj";
            string targetBundlesPrefabsToolPath = "Assets/LazyPan/Bundles/Prefabs/Tool";
            string targetBundlesPrefabsUIPath = "Assets/LazyPan/Bundles/Prefabs/UI";
            
            string targetBundlesSoundsPath = "Assets/LazyPan/Bundles/Sounds";

            string targetScriptsPath = "Assets/LazyPan/Scripts";

            string targetScriptsGamePlayPath = "Assets/LazyPan/Scripts/GamePlay";
            string targetScriptsGamePlayBehaviourPath = "Assets/LazyPan/Scripts/GamePlay/Behaviour";
            string targetScriptsGamePlayBehaviourTemplatePath = "Assets/LazyPan/Scripts/GamePlay/Behaviour/Template";
            string targetScriptsGamePlayConfigPath = "Assets/LazyPan/Scripts/GamePlay/Config";
            string targetScriptsGamePlayDataPath = "Assets/LazyPan/Scripts/GamePlay/Data";
            string targetScriptsGamePlayFlowPath = "Assets/LazyPan/Scripts/GamePlay/Flow";

            
            CreateDirectory(targetBundlesPath);
            CreateDirectory(targetBundlesConfigsPath);
            CreateDirectory(targetBundlesConfigsInputPath);
            CreateDirectory(targetBundlesConfigsTxtPath);
            CreateDirectory(targetBundlesConfigsSettingPath);
            CreateDirectory(targetBundlesConfigsSettingLocationInformationSettingPath);
            CreateDirectory(targetBundlesImagesPath);
            CreateDirectory(targetBundlesMaterialsPath);
            CreateDirectory(targetBundlesPrefabsPath);
            CreateDirectory(targetBundlesPrefabsGlobalPath);
            CreateDirectory(targetBundlesPrefabsObjPath);
            CreateDirectory(targetBundlesPrefabsToolPath);
            CreateDirectory(targetBundlesPrefabsUIPath);
            CreateDirectory(targetBundlesSoundsPath);
            CreateDirectory(targetScriptsPath);
            CreateDirectory(targetScriptsGamePlayPath);
            CreateDirectory(targetScriptsGamePlayBehaviourPath);
            CreateDirectory(targetScriptsGamePlayBehaviourTemplatePath);
            CreateDirectory(targetScriptsGamePlayConfigPath);
            CreateDirectory(targetScriptsGamePlayDataPath);
            CreateDirectory(targetScriptsGamePlayFlowPath);

            AssetDatabase.Refresh();
        }

        private void CreateDirectory(string path) {
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
        }

        private void CreateAddressableAsset() {
            AddressableAssetSettingsDefaultObject.Settings = AddressableAssetSettings.Create(
                AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
        }

        private void AutoInstallAddressableData() {
            /*游戏总配置*/
            string targetGameSettingPath = $"Packages/evoreek.lazypan/Runtime/Bundles/GameSetting/GameSetting.asset";
            AddAssetToAddressableEntries(targetGameSettingPath);

            /*游戏配置*/
            string targetBundlesConfigsPath = "Assets/LazyPan/Bundles/Configs";
            if (Directory.Exists(targetBundlesConfigsPath)) {
                AddAssetToAddressableEntries(targetBundlesConfigsPath);
            }

            /*游戏不同类型资源加载目录*/
            string targetBundlesPrefabsGlobalPath = "Assets/LazyPan/Bundles/Prefabs/Global";
            string targetBundlesPrefabsObjPath = "Assets/LazyPan/Bundles/Prefabs/Obj";
            string targetBundlesPrefabsToolPath = "Assets/LazyPan/Bundles/Prefabs/Tool";
            string targetBundlesPrefabsUIPath = "Assets/LazyPan/Bundles/Prefabs/UI";
            string targetBundlesPrefabsImagePath = "Assets/LazyPan/Bundles/Images";
            string targetBundlesSoundsPath = "Assets/LazyPan/Bundles/Sounds";

            if (Directory.Exists(targetBundlesPrefabsGlobalPath)) {
                AddAssetToAddressableEntries(targetBundlesPrefabsGlobalPath);
            }

            if (Directory.Exists(targetBundlesPrefabsObjPath)) {
                AddAssetToAddressableEntries(targetBundlesPrefabsObjPath);
            }

            if (Directory.Exists(targetBundlesPrefabsToolPath)) {
                AddAssetToAddressableEntries(targetBundlesPrefabsToolPath);
            }

            if (Directory.Exists(targetBundlesPrefabsUIPath)) {
                AddAssetToAddressableEntries(targetBundlesPrefabsUIPath);
            }

            if (Directory.Exists(targetBundlesPrefabsImagePath)) {
                AddAssetToAddressableEntries(targetBundlesPrefabsImagePath);
            }
            
            if (Directory.Exists(targetBundlesSoundsPath)) {
                AddAssetToAddressableEntries(targetBundlesSoundsPath);
            }

            /*输入控制器*/
            string targetInputControlPath = "Assets/LazyPan/Bundles/Configs/Input/LazyPanInputControl.inputactions";
            AddAssetToAddressableEntries(targetInputControlPath);
        }

        private void AddAssetToAddressableEntries(string path) {
            Object dir = AssetDatabase.LoadAssetAtPath<Object>(path);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (dir != null) {
                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings != null) {
                    var group = settings.DefaultGroup;
                    if (group != null) {
                        AddressableAssetEntry entry = group.entries.FirstOrDefault(e => e.guid == guid);
                        if (entry == null) {
                            entry = settings.CreateOrMoveEntry(guid, group, false, false);
                        }

                        entry.address = path;
                    }
                }
            }
        }

        public void CopyFilesToDirectory(string sourceDirectory, string destinationDirectory) {
            string sourcePath = $"Packages/evoreek.lazypan/Runtime/{sourceDirectory}"; // 源文件夹路径
            string targetPath = $"Assets/{destinationDirectory}"; // 目标文件夹路径
            // 检查源目录是否存在
            if (!Directory.Exists(sourcePath)) {
                Debug.LogError($"Source directory does not exist: {sourcePath}");
                return;
            }

            // 创建目标目录（如果不存在）
            if (!Directory.Exists(targetPath)) {
                Directory.CreateDirectory(targetPath);
            }

            // 复制源目录及其子目录中的所有文件
            CopyDirectory(sourcePath, targetPath);
            
            Debug.Log($"Files copied from {sourcePath} to {targetPath}");
        }

        private void CopyDirectory(string sourceDir, string destDir) {
            // 创建目标目录
            Directory.CreateDirectory(destDir);

            // 获取源目录中的所有文件
            string[] files = Directory.GetFiles(sourceDir);
            foreach (string file in files) {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destDir, fileName);
                File.Copy(file, destFile, true); // 设置true以覆盖目标目录中的同名文件
            }

            // 获取源目录中的所有子目录
            string[] directories = Directory.GetDirectories(sourceDir);
            foreach (string directory in directories) {
                string directoryName = Path.GetFileName(directory);
                string destDirectory = Path.Combine(destDir, directoryName);
                CopyDirectory(directory, destDirectory); // 递归复制子目录
            }
        }

        public void MoveSceneToBuildSettings() {
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[0]; // 清空 Build Settings 中的场景
            // 获取指定文件夹中的所有场景文件
            string[] sceneFiles =
                Directory.GetFiles("Assets/LazyPan/Bundles/Scenes", "*.unity", SearchOption.AllDirectories);
            List<EditorBuildSettingsScene> newScenes = new List<EditorBuildSettingsScene>();
            // 将所有场景文件添加到 Build Settings 中
            foreach (string sceneFile in sceneFiles) {
                newScenes.Add(new EditorBuildSettingsScene(sceneFile, true));
            }

            // 更新 Build Settings 场景列表
            EditorBuildSettings.scenes = newScenes.ToArray();
        }

        private void AutoGenerateFlow() {
            Generate.GenerateFlow();
        }

        private void AutoGenerateBehaviour() {
            Generate.GenerateBehaviour(false);
        }
    }
}