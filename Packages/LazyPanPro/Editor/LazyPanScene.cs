using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace LazyPan {
    // 定义一个存储多个字段的类
    [System.Serializable]
    public class MySceneData {
        public string[] Infos;//标识 描述 物体路径 流程 加载时间
        public int FlowIndex;
        public int OperationIndex;

        public MySceneData(string[] infos, int flowIndex) {
            this.Infos = infos;
            this.FlowIndex = flowIndex;
        }
    }

    public class LazyPanScene : EditorWindow {
        private LazyPanTool _tool;

        //数据
        private List<MySceneData> sceneDatas = new List<MySceneData>();
        //流程选项(所有场景标识)
        private List<string> sceneSignOptions = new List<string>();
        //操作选项
        private List<string> operationNameOptions = new List<string>();
        //记录列表
        private ReorderableList reorderableList;

        private bool isFoldoutTool;
        private bool isFoldoutData;

        //模糊搜索
        private string UserInput;
        private bool isFocused;
        private string placeholder = "场景模糊搜索请输入关键字输入提示文本";
        private string tmpPlaceHolder;
        private string textName = "场景模糊搜索控件";

        private int _index;

        //标题表
        private string[] names = new[] {
            "场景预览标题标识",
            "场景预览标题描述",
            "场景预览标题物体路径",
            "场景预览标题流程",
            "场景预览标题加载时间",
            "场景预览标题操作",
        };

        public void OnStart(LazyPanTool tool) {
            _tool = tool;
            _tool.InitScroll();
            isFoldoutTool = true;
            isFoldoutData = true;
            InitReorderableList("");
        }

        public void OnCustomGUI(float areaX) {
            GUILayout.BeginArea(new Rect(areaX + _tool.scrollOffsetX, 60 + _tool.scrollOffsetY, Screen.width, Screen.height * 10));
            Title();
            AutoTool();
            PreviewSceneConfigData();
            GUILayout.EndArea();
        }

        /// <summary>
        /// 初始化无序列表
        /// </summary>
        private void InitReorderableList(string fuzzyContent) {
            ReadSceneData(fuzzyContent);
            RefreshSignOptions();
            reorderableList = new ReorderableList(sceneDatas, typeof(MySceneData), true, true, true, true);
            //头部标题
            reorderableList.drawHeaderCallback = (Rect rect) => {
                _index = 0;
                for (int i = 0; i < names.Length; i++) {
                    EditorGUI.LabelField(
                        new Rect(rect.x + 15f + Screen.width / names.Length * _index++,
                            rect.y,
                            Screen.width / names.Length,
                            EditorGUIUtility.singleLineHeight),
                        LazyPanTool.GetText(names[i]));
                }
            };
            //绘制无序列表
            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                rect.y += 2.5f;
                rect.width -= 2;

                //宽度
                float _width = Screen.width - 10f;

                //遍历标题
                for (int i = 0; i < names.Length; i++) {
                    Rect labelRect = new Rect(rect.x + _width / names.Length * i,
                        rect.y,
                        _width / names.Length,
                        EditorGUIUtility.singleLineHeight);
                    if (i == 3) {
                        //流程列 下拉选择场景标识
                        labelRect.height = 18f;
                        int selectIndex = EditorGUI.Popup(labelRect, sceneDatas[index].FlowIndex, sceneSignOptions.ToArray());
                        if (selectIndex >= 0 && sceneDatas[index].FlowIndex != selectIndex) {
                            sceneDatas[index].FlowIndex = selectIndex;
                            sceneDatas[index].Infos[i] = sceneSignOptions[selectIndex];
                            reorderableList.onChangedCallback.Invoke(reorderableList);
                        }
                    } else if (i == 4) {
                        //加载时间列 数字输入
                        labelRect.height = 18f;
                        float.TryParse(sceneDatas[index].Infos[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float delayTime);
                        float newDelayTime = EditorGUI.FloatField(labelRect, delayTime);
                        if (Math.Abs(newDelayTime - delayTime) > 0.0001f) {
                            sceneDatas[index].Infos[i] = newDelayTime.ToString(CultureInfo.InvariantCulture);
                            reorderableList.onChangedCallback.Invoke(reorderableList);
                        }
                    } else if (i == 5) {
                        //操作列 打开或创建场景 创建物体加载文件夹
                        labelRect.height = 18f;
                        bool sceneExists = TryGetSceneAssetPath(sceneDatas[index].Infos[0], out string sceneAssetPath);
                        RefreshOperationNameOptions(sceneExists);

                        List<string> displayOperationNameOptionsOfLanguage = new List<string>();
                        foreach (var tmpOption in operationNameOptions) {
                            displayOperationNameOptionsOfLanguage.Add(LazyPanTool.GetText(tmpOption));
                        }

                        int selectIndex = EditorGUI.Popup(labelRect, sceneDatas[index].OperationIndex, displayOperationNameOptionsOfLanguage.ToArray());
                        if (sceneDatas[index].OperationIndex != selectIndex) {
                            sceneDatas[index].OperationIndex = 0;
                            string operationName = operationNameOptions[selectIndex];
                            Operation(operationName, sceneDatas[index], sceneAssetPath);
                        }
                    } else {
                        //文本列 标识 描述 物体路径
                        string textField = EditorGUI.TextField(labelRect, sceneDatas[index].Infos[i]);
                        if (sceneDatas[index].Infos[i] != textField) {
                            sceneDatas[index].Infos[i] = textField;
                            reorderableList.onChangedCallback.Invoke(reorderableList);
                            if (i == 0) {
                                RefreshSignOptions();
                            }
                        }
                    }
                }
            };
            reorderableList.onReorderCallback = (ReorderableList list) => {
            };
            reorderableList.onAddCallback = (ReorderableList list) => {
                string[] infos = new string[5];
                infos[4] = "0";
                sceneDatas.Add(new MySceneData(infos, -1));
                WriteSceneData();
            };
            reorderableList.onRemoveCallback = (ReorderableList list) => {
                // 确保索引有效
                if (list.index >= 0 && list.index < list.list.Count) {
                    list.list.RemoveAt(list.index);  // 移除当前选中的元素
                    WriteSceneData();
                }
            };
            reorderableList.onChangedCallback = (ReorderableList list) => {
                //csv数据自动更新
                WriteSceneData();
            };
        }

        /// <summary>
        /// 读取场景数据
        /// </summary>
        private void ReadSceneData(string fuzzyContent) {
            sceneDatas.Clear();
            ReadCSV.Instance.Read("SceneConfig", out string content, out string[] lines);
            if (lines != null && lines.Length > 0) {
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = lines[i].Split(",");
                        if (lineStr.Length > 0) {
                            bool hasFuzzyContent = false;
                            for (int j = 0; j <= 4; j++) {
                                if (lineStr[j].Contains(fuzzyContent)) {
                                    hasFuzzyContent = true;
                                    break;
                                }
                            }

                            if (!hasFuzzyContent) {
                                continue;
                            }

                            //场景数据
                            string[] lineInfo = new string[5];
                            for (int j = 0; j <= 4; j++) {
                                lineInfo[j] = lineStr[j];
                            }

                            //流程数据
                            int selectFlow = -1;
                            for (int j = 0; j < sceneSignOptions.Count; j++) {
                                if (sceneSignOptions[j] == lineInfo[3]) {
                                    selectFlow = j;
                                    break;
                                }
                            }

                            sceneDatas.Add(new MySceneData(lineInfo, selectFlow));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 刷新流程下拉选项(所有场景标识)并重算选中索引
        /// </summary>
        private void RefreshSignOptions() {
            sceneSignOptions.Clear();
            ReadCSV.Instance.Read("SceneConfig", out string content, out string[] lines);
            if (lines != null && lines.Length > 0) {
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = lines[i].Split(",");
                        if (lineStr.Length > 0 && !string.IsNullOrEmpty(lineStr[0]) && !sceneSignOptions.Contains(lineStr[0])) {
                            sceneSignOptions.Add(lineStr[0]);
                        }
                    }
                }
            }

            //编辑中尚未写回的场景标识也纳入选项
            foreach (MySceneData sceneData in sceneDatas) {
                if (!string.IsNullOrEmpty(sceneData.Infos[0]) && !sceneSignOptions.Contains(sceneData.Infos[0])) {
                    sceneSignOptions.Add(sceneData.Infos[0]);
                }
            }

            //重算每行流程选中索引
            foreach (MySceneData sceneData in sceneDatas) {
                sceneData.FlowIndex = -1;
                for (int j = 0; j < sceneSignOptions.Count; j++) {
                    if (sceneSignOptions[j] == sceneData.Infos[3]) {
                        sceneData.FlowIndex = j;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 写入场景数据
        /// </summary>
        private void WriteSceneData() {
            ReadCSV.Instance.Read("SceneConfig", out string content, out string[] lines);
            try {
                Queue<MySceneData> sceneDataQue = new Queue<MySceneData>(sceneDatas);
                int newLength = -1;
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] linesStr = lines[i].Split(',');
                        if (sceneDataQue.Count == 0) {
                            newLength = i;
                            break;
                        }

                        MySceneData data = sceneDataQue.Dequeue();
                        if (data != null) {
                            for (int j = 0; j < data.Infos.Length; j++) {
                                linesStr[j] = data.Infos[j];
                            }

                            lines[i] = string.Join(",", linesStr);
                        }
                    }
                }

                string[] newLines;
                if (newLength > -1) {
                    //需要裁剪
                    newLines = new string[newLength];
                    Array.Copy(lines, newLines, newLength);
                    ReadCSV.Instance.Write("SceneConfig", newLines);
                } else {
                    newLines = new string[sceneDataQue.Count];
                    int index = 0;
                    while (sceneDataQue.Count > 0) {
                        MySceneData data = sceneDataQue.Dequeue();
                        if (data != null) {
                            string[] linesStr = new string[5];
                            for (int j = 0; j < data.Infos.Length; j++) {
                                linesStr[j] = data.Infos[j];
                            }

                            newLines[index] = string.Join(",", linesStr);
                            index++;
                        }
                    }
                    ReadCSV.Instance.Write("SceneConfig", lines.Concat(newLines).ToArray());
                }
            } catch {
                Debug.LogError("录入错误");
            }
        }

        /// <summary>
        /// 尝试获取场景资产路径(优先从 BuildSettings 中查找 其次全局搜索场景资产)
        /// </summary>
        private bool TryGetSceneAssetPath(string sceneSign, out string sceneAssetPath) {
            sceneAssetPath = null;
            if (string.IsNullOrEmpty(sceneSign)) {
                return false;
            }

            foreach (EditorBuildSettingsScene editorScene in EditorBuildSettings.scenes) {
                if (Path.GetFileNameWithoutExtension(editorScene.path) == sceneSign) {
                    sceneAssetPath = editorScene.path;
                    return true;
                }
            }

            string[] guids = AssetDatabase.FindAssets("t:SceneAsset", new[] {"Assets"});
            foreach (string guid in guids) {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(assetPath) == sceneSign) {
                    sceneAssetPath = assetPath;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 打开场景
        /// </summary>
        private void OpenSceneAsset(string sceneAssetPath) {
            EditorSceneManager.OpenScene(sceneAssetPath, OpenSceneMode.Single);
        }

        /// <summary>
        /// 创建场景(创建于框架场景目录下并自动装载至 BuildSettings 同时写回场景标识)
        /// </summary>
        private void CreateSceneAsset(string sceneSign) {
            if (string.IsNullOrEmpty(sceneSign)) {
                Debug.LogError("错误! 场景标识为空 无法创建场景!");
                return;
            }

            if (TryGetSceneAssetPath(sceneSign, out _)) {
                Debug.LogError($"错误! 场景{sceneSign}已存在 请检查配置!");
                return;
            }

            //确保框架场景目录存在
            string sceneDirectory = "Assets/LazyPan/Bundles/Scenes";
            if (!AssetDatabase.IsValidFolder("Assets/LazyPan")) {
                AssetDatabase.CreateFolder("Assets", "LazyPan");
            }
            if (!AssetDatabase.IsValidFolder("Assets/LazyPan/Bundles")) {
                AssetDatabase.CreateFolder("Assets/LazyPan", "Bundles");
            }
            if (!AssetDatabase.IsValidFolder(sceneDirectory)) {
                AssetDatabase.CreateFolder("Assets/LazyPan/Bundles", "Scenes");
            }

            //创建并保存场景(包含相机与灯光)
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            string scenePath = $"{sceneDirectory}/{sceneSign}.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);

            //自动装载至 BuildSettings
            List<EditorBuildSettingsScene> buildScenes = EditorBuildSettings.scenes.ToList();
            buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath));
            Debug.Log($"创建场景成功! 路径:{scenePath}");
        }

        /// <summary>
        /// 创建物体加载文件夹(创建于框架预制体物体目录下并按约定补全物体路径列)
        /// </summary>
        private void CreateObjLoadFolder(MySceneData sceneData) {
            string sceneSign = sceneData.Infos[0];
            if (string.IsNullOrEmpty(sceneSign)) {
                Debug.LogError("错误! 场景标识为空 无法创建物体加载文件夹!");
                return;
            }

            //确保框架预制体物体目录存在
            string objRootDirectory = "Assets/LazyPan/Bundles/Prefabs/Obj";
            if (!AssetDatabase.IsValidFolder("Assets/LazyPan")) {
                AssetDatabase.CreateFolder("Assets", "LazyPan");
            }
            if (!AssetDatabase.IsValidFolder("Assets/LazyPan/Bundles")) {
                AssetDatabase.CreateFolder("Assets/LazyPan", "Bundles");
            }
            if (!AssetDatabase.IsValidFolder("Assets/LazyPan/Bundles/Prefabs")) {
                AssetDatabase.CreateFolder("Assets/LazyPan/Bundles", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(objRootDirectory)) {
                AssetDatabase.CreateFolder("Assets/LazyPan/Bundles/Prefabs", "Obj");
            }

            string objLoadDirectory = $"{objRootDirectory}/{sceneSign}";
            if (!AssetDatabase.IsValidFolder(objLoadDirectory)) {
                AssetDatabase.CreateFolder(objRootDirectory, sceneSign);
            }

            //按框架约定补全物体路径列(运行时 Entity 加载实体使用的目录)
            string dirPath = $"Obj/{sceneSign}/";
            if (string.IsNullOrEmpty(sceneData.Infos[2])) {
                sceneData.Infos[2] = dirPath;
                WriteSceneData();
            }

            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(objLoadDirectory));
            Debug.Log($"创建物体加载文件夹成功! 路径:{objLoadDirectory} 物体路径:{dirPath}");
        }

        /// <summary>
        /// 刷新操作选项(根据场景是否存在动态切换)
        /// </summary>
        private void RefreshOperationNameOptions(bool sceneExists) {
            operationNameOptions.Clear();
            operationNameOptions.Add("场景操作操作文本");
            operationNameOptions.Add(sceneExists ? "场景操作打开场景文本" : "场景操作创建场景文本");
            operationNameOptions.Add("场景操作创建物体加载文件夹文本");
        }

        /// <summary>
        /// 操作分发
        /// </summary>
        private void Operation(string operationName, MySceneData sceneData, string sceneAssetPath) {
            if (operationName == "场景操作打开场景文本") {
                OpenSceneAsset(sceneAssetPath);
            } else if (operationName == "场景操作创建场景文本") {
                CreateSceneAsset(sceneData.Infos[0]);
            } else if (operationName == "场景操作创建物体加载文件夹文本") {
                CreateObjLoadFolder(sceneData);
            }
        }

        private void PreviewSceneConfigData() {
            isFoldoutData = EditorGUILayout.Foldout(isFoldoutData, LazyPanTool.GetText("场景预览场景配置数据展开文本"), true);
            Rect rect = GUILayoutUtility.GetLastRect();
            float height = 0;
            if (isFoldoutData) {
                height += FuzzySearch();
                reorderableList.DoLayoutList();

                height += GUILayoutUtility.GetLastRect().height;
                GUILayout.Space(10);
            } else {
                GUILayout.Space(10);
            }

            LazyPanTool.DrawBorder(new Rect(rect.x + 2f, rect.y - 2f, rect.width - 2f, rect.height + height + 5f), Color.white);

            GUILayout.Space(10);
        }

        private float FuzzySearch() {
            GUILayout.BeginHorizontal();

            float inputHeight = 20f;

            // 设置控件名称用于检测焦点
            GUI.SetNextControlName(textName);

            // 如果提示文本变更直接切换
            if (tmpPlaceHolder != LazyPanTool.GetText(placeholder)) {
                tmpPlaceHolder = LazyPanTool.GetText(placeholder);
                isFocused = true;
                UserInput = "";
            }

            // 判断输入框内容
            string displayText = string.IsNullOrEmpty(UserInput) && !isFocused ? tmpPlaceHolder : UserInput;

            // 显示输入框
            string newInput = EditorGUILayout.TextField(displayText, GUILayout.Height(inputHeight));
            GUILayout.EndHorizontal(); // 结束水平布局

            // 检测焦点状态
            if (GUI.GetNameOfFocusedControl() == textName) {
                if (!isFocused) {
                    isFocused = true; // 聚焦状态
                    if (displayText == tmpPlaceHolder) {
                        newInput = ""; // 清空提示文本
                    }
                }
            } else {
                if (isFocused) {
                    isFocused = false; // 失去焦点
                    if (string.IsNullOrEmpty(newInput)) {
                        newInput = tmpPlaceHolder; // 恢复提示文本
                    }
                }
            }

            //更新
            if (UserInput != newInput) {
                UserInput = newInput;
                string fuzzyContent = UserInput == tmpPlaceHolder ? "" : UserInput;
                InitReorderableList(fuzzyContent);
            }

            return inputHeight;
        }

        private void AutoTool() {
            isFoldoutTool = EditorGUILayout.Foldout(isFoldoutTool, LazyPanTool.GetText("场景自动化工具展开文本"), true);
            Rect rect = GUILayoutUtility.GetLastRect();
            float height = 0;
            if (isFoldoutTool) {
                GUILayout.Label("");
                height += GUILayoutUtility.GetLastRect().height;
                GUILayout.BeginHorizontal();
                GUIStyle style = LazyPanTool.GetGUISkin("AButtonGUISkin").GetStyle("button");
                if (GUILayout.Button(LazyPanTool.GetText("场景自动化工具按钮文本"), style)) {
                    OpenSceneCsv();
                }
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;
            } else {
                GUILayout.Space(10);
            }

            LazyPanTool.DrawBorder(new Rect(rect.x + 2f, rect.y - 2f, rect.width - 2f, rect.height + height + 5f), Color.white);

            GUILayout.Space(10);
        }

        private void Title() {
            GUILayout.BeginHorizontal();
            GUIStyle style = LazyPanTool.GetGUISkin("LogoGUISkin").GetStyle("label");
            GUILayout.Label("SCENE", style);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            style = LazyPanTool.GetGUISkin("AnnotationGUISkin").GetStyle("label");
            GUILayout.Label("@" + LazyPanTool.GetText("场景小标题"), style);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
        }

        private void OpenSceneCsv() {
            string filePath = Application.dataPath + "/StreamingAssets/Csv/SceneConfig.csv";
            Process.Start(filePath);
        }
    }
}