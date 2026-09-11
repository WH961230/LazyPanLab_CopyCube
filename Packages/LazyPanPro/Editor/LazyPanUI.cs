using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace LazyPan {
    // 定义一个存储多个字段的类
    [System.Serializable]
    public class MyUIData {
        public string[] Infos;//标识 描述 类型 渲染层级
        public int TypeIndex;
        public int OperationIndex;

        public MyUIData(string[] infos, int typeIndex) {
            this.Infos = infos;
            this.TypeIndex = typeIndex;
        }
    }

    public class LazyPanUI : EditorWindow {
        private LazyPanTool _tool;

        //数据
        private List<MyUIData> uiDatas = new List<MyUIData>();
        //类型选项值
        private readonly string[] typeValueOptions = {"0", "1"};
        //操作选项
        private List<string> operationNameOptions = new List<string>();
        //记录列表
        private ReorderableList reorderableList;

        private bool isFoldoutTool;
        private bool isFoldoutData;

        //模糊搜索
        private string UserInput;
        private bool isFocused;
        private string placeholder = "UI模糊搜索请输入关键字输入提示文本";
        private string tmpPlaceHolder;
        private string textName = "UI模糊搜索控件";

        private int _index;

        //标题表
        private string[] names = new[] {
            "UI预览标题标识",
            "UI预览标题描述",
            "UI预览标题类型",
            "UI预览标题渲染层级",
            "UI预览标题操作",
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
            PreviewUIConfigData();
            GUILayout.EndArea();
        }

        /// <summary>
        /// 初始化无序列表
        /// </summary>
        private void InitReorderableList(string fuzzyContent) {
            ReadUIData(fuzzyContent);
            reorderableList = new ReorderableList(uiDatas, typeof(MyUIData), true, true, true, true);
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
                    if (i == 2) {
                        //类型列 下拉选择 互斥/常驻
                        labelRect.height = 18f;
                        List<string> displayTypeOptions = new List<string>();
                        foreach (string tmpValue in typeValueOptions) {
                            displayTypeOptions.Add(tmpValue == "0"
                                ? LazyPanTool.GetText("UI类型互斥选项")
                                : LazyPanTool.GetText("UI类型常驻选项"));
                        }

                        int selectIndex = EditorGUI.Popup(labelRect, uiDatas[index].TypeIndex, displayTypeOptions.ToArray());
                        if (selectIndex >= 0 && uiDatas[index].TypeIndex != selectIndex) {
                            uiDatas[index].TypeIndex = selectIndex;
                            uiDatas[index].Infos[i] = typeValueOptions[selectIndex];
                            reorderableList.onChangedCallback.Invoke(reorderableList);
                        }
                    } else if (i == 3) {
                        //渲染层级列 数字输入
                        labelRect.height = 18f;
                        int.TryParse(uiDatas[index].Infos[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int renderQueue);
                        int newRenderQueue = EditorGUI.IntField(labelRect, renderQueue);
                        if (newRenderQueue != renderQueue) {
                            uiDatas[index].Infos[i] = newRenderQueue.ToString(CultureInfo.InvariantCulture);
                            reorderableList.onChangedCallback.Invoke(reorderableList);
                        }
                    } else if (i == 4) {
                        //操作列 跳转到预制体 创建预制体
                        labelRect.height = 18f;
                        bool prefabExists = TryGetUIPrefabPath(uiDatas[index].Infos[0], out string prefabPath);
                        RefreshOperationNameOptions(prefabExists);

                        List<string> displayOperationNameOptionsOfLanguage = new List<string>();
                        foreach (var tmpOption in operationNameOptions) {
                            displayOperationNameOptionsOfLanguage.Add(LazyPanTool.GetText(tmpOption));
                        }

                        int selectIndex = EditorGUI.Popup(labelRect, uiDatas[index].OperationIndex, displayOperationNameOptionsOfLanguage.ToArray());
                        if (uiDatas[index].OperationIndex != selectIndex) {
                            uiDatas[index].OperationIndex = 0;
                            string operationName = operationNameOptions[selectIndex];
                            Operation(operationName, uiDatas[index], prefabPath);
                        }
                    } else {
                        //文本列 标识 描述
                        string textField = EditorGUI.TextField(labelRect, uiDatas[index].Infos[i]);
                        if (uiDatas[index].Infos[i] != textField) {
                            uiDatas[index].Infos[i] = textField;
                            reorderableList.onChangedCallback.Invoke(reorderableList);
                        }
                    }
                }
            };
            reorderableList.onReorderCallback = (ReorderableList list) => {
            };
            reorderableList.onAddCallback = (ReorderableList list) => {
                string[] infos = new string[4];
                infos[2] = "0";
                infos[3] = "0";
                uiDatas.Add(new MyUIData(infos, 0));
                WriteUIData();
            };
            reorderableList.onRemoveCallback = (ReorderableList list) => {
                // 确保索引有效
                if (list.index >= 0 && list.index < list.list.Count) {
                    list.list.RemoveAt(list.index);  // 移除当前选中的元素
                    WriteUIData();
                }
            };
            reorderableList.onChangedCallback = (ReorderableList list) => {
                //csv数据自动更新
                WriteUIData();
            };
        }

        /// <summary>
        /// 读取界面数据
        /// </summary>
        private void ReadUIData(string fuzzyContent) {
            uiDatas.Clear();
            ReadCSV.Instance.Read("UIConfig", out string content, out string[] lines);
            if (lines != null && lines.Length > 0) {
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = lines[i].Split(",");
                        if (lineStr.Length > 0) {
                            bool hasFuzzyContent = false;
                            for (int j = 0; j <= 3; j++) {
                                string cell = j < lineStr.Length ? lineStr[j] : "";
                                if (cell.Contains(fuzzyContent)) {
                                    hasFuzzyContent = true;
                                    break;
                                }
                            }

                            if (!hasFuzzyContent) {
                                continue;
                            }

                            //界面数据
                            string[] lineInfo = new string[4];
                            for (int j = 0; j < lineInfo.Length; j++) {
                                lineInfo[j] = j < lineStr.Length ? lineStr[j] : "";
                            }

                            //类型数据
                            int selectType = -1;
                            for (int j = 0; j < typeValueOptions.Length; j++) {
                                if (typeValueOptions[j] == lineInfo[2]) {
                                    selectType = j;
                                    break;
                                }
                            }

                            uiDatas.Add(new MyUIData(lineInfo, selectType));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 写入界面数据
        /// </summary>
        private void WriteUIData() {
            ReadCSV.Instance.Read("UIConfig", out string content, out string[] lines);
            try {
                Queue<MyUIData> uiDataQue = new Queue<MyUIData>(uiDatas);
                int newLength = -1;
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] linesStr = lines[i].Split(',');
                        if (uiDataQue.Count == 0) {
                            newLength = i;
                            break;
                        }

                        MyUIData data = uiDataQue.Dequeue();
                        if (data != null) {
                            for (int j = 0; j < data.Infos.Length && j < linesStr.Length; j++) {
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
                    ReadCSV.Instance.Write("UIConfig", newLines);
                } else {
                    newLines = new string[uiDataQue.Count];
                    int index = 0;
                    while (uiDataQue.Count > 0) {
                        MyUIData data = uiDataQue.Dequeue();
                        if (data != null) {
                            string[] linesStr = new string[4];
                            for (int j = 0; j < data.Infos.Length; j++) {
                                linesStr[j] = data.Infos[j];
                            }

                            newLines[index] = string.Join(",", linesStr);
                            index++;
                        }
                    }
                    ReadCSV.Instance.Write("UIConfig", lines.Concat(newLines).ToArray());
                }
            } catch {
                Debug.LogError("录入错误");
            }
        }

        /// <summary>
        /// 刷新操作选项
        /// </summary>
        private void RefreshOperationNameOptions(bool exist) {
            operationNameOptions.Clear();
            operationNameOptions.Add("UI操作操作文本");
            operationNameOptions.Add(exist ? "UI操作跳转到预制体" : "UI操作创建预制体");
            if (exist) {
                operationNameOptions.Add("UI操作重命名预制体");
            }
        }

        /// <summary>
        /// 操作
        /// </summary>
        private void Operation(string operationName, MyUIData uiData, string prefabPath) {
            if (operationName == "UI操作跳转到预制体") {
                Object prefab = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);
                if (prefab != null) {
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
            } else if (operationName == "UI操作创建预制体") {
                CreateDefaultUIPrefab(uiData);
            }
        }

        /// <summary>
        /// 尝试获取界面预制体路径
        /// </summary>
        private bool TryGetUIPrefabPath(string uiSign, out string prefabPath) {
            prefabPath = null;
            if (string.IsNullOrEmpty(uiSign)) {
                return false;
            }

            prefabPath = $"Assets/LazyPan/Bundles/Prefabs/UI/{uiSign}.prefab";
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
        }

        /// <summary>
        /// 创建默认界面预制体(创建于框架界面预制体目录下 根节点 RectTransform+Canvas+Comp 并预置标签)
        /// </summary>
        private void CreateDefaultUIPrefab(MyUIData uiData) {
            string uiSign = uiData.Infos[0];
            if (string.IsNullOrEmpty(uiSign)) {
                Debug.LogError("错误! 界面标识为空 无法创建界面预制体!");
                return;
            }

            if (TryGetUIPrefabPath(uiSign, out _)) {
                Debug.LogError($"错误! 界面预制体{uiSign}已存在 请检查配置!");
                return;
            }

            //确保框架界面预制体目录存在
            string uiDirectory = "Assets/LazyPan/Bundles/Prefabs/UI";
            if (!AssetDatabase.IsValidFolder("Assets/LazyPan")) {
                AssetDatabase.CreateFolder("Assets", "LazyPan");
            }
            if (!AssetDatabase.IsValidFolder("Assets/LazyPan/Bundles")) {
                AssetDatabase.CreateFolder("Assets/LazyPan", "Bundles");
            }
            if (!AssetDatabase.IsValidFolder("Assets/LazyPan/Bundles/Prefabs")) {
                AssetDatabase.CreateFolder("Assets/LazyPan/Bundles", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(uiDirectory)) {
                AssetDatabase.CreateFolder("Assets/LazyPan/Bundles/Prefabs", "UI");
            }

            //创建界面根节点(带 Canvas 层级取渲染层级配置)
            int.TryParse(uiData.Infos[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int renderQueue);
            GameObject rootGo = new GameObject(uiSign, typeof(RectTransform));
            rootGo.layer = LayerMask.NameToLayer("UI") == -1 ? 5 : LayerMask.NameToLayer("UI");
            RectTransform rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            Canvas canvas = rootGo.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = renderQueue;
            Comp comp = rootGo.AddComponent<Comp>();

            //创建背景
            GameObject bgGo = new GameObject("BG", typeof(RectTransform));
            bgGo.layer = rootGo.layer;
            bgGo.transform.SetParent(rootGo.transform, false);
            RectTransform bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.35f);

            //预置标签
            comp.Transforms.Add(new Comp.TransformData() { Sign = Label.ROOT, Tran = rootGo.transform });
            comp.Images.Add(new Comp.ImageData() { Sign = Label.FRAME, Image = bgImage });

            //保存预制体
            rootGo.name = string.IsNullOrEmpty(uiData.Infos[1]) ? uiSign : uiData.Infos[1];
            string prefabPath = $"{uiDirectory}/{uiSign}.prefab";
            PrefabUtility.SaveAsPrefabAsset(rootGo, prefabPath);
            Object.DestroyImmediate(rootGo);

            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(prefabPath));
            Debug.Log($"创建界面预制体成功! 路径:{prefabPath} 渲染层级:{renderQueue}");
        }

        private void PreviewUIConfigData() {
            isFoldoutData = EditorGUILayout.Foldout(isFoldoutData, LazyPanTool.GetText("UI预览界面配置数据展开文本"), true);
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
            isFoldoutTool = EditorGUILayout.Foldout(isFoldoutTool, LazyPanTool.GetText("UI自动化工具展开文本"), true);
            Rect rect = GUILayoutUtility.GetLastRect();
            float height = 0;
            if (isFoldoutTool) {
                GUILayout.Label("");
                height += GUILayoutUtility.GetLastRect().height;
                GUILayout.BeginHorizontal();
                GUIStyle style = LazyPanTool.GetGUISkin("AButtonGUISkin").GetStyle("button");
                if (GUILayout.Button(LazyPanTool.GetText("UI自动化工具按钮文本"), style)) {
                    OpenUICsv();
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
            GUILayout.Label("UI", style);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            style = LazyPanTool.GetGUISkin("AnnotationGUISkin").GetStyle("label");
            GUILayout.Label("@" + LazyPanTool.GetText("界面小标题"), style);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
        }

        private void OpenUICsv() {
            string filePath = Application.dataPath + "/StreamingAssets/Csv/UIConfig.csv";
            Process.Start(filePath);
        }
    }
}