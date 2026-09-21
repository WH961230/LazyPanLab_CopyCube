using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LazyPan {
    // 定义一个存储多个字段的类
    [System.Serializable]
    public class MyEntityData {
        public string[] Infos;
        public int SceneIndexs;
        public List<string> BindBehaviour;
        public bool[] BehaviourIndexs;
        public int OperationIndex;

        public MyEntityData(string[] infos, int SceneIndexs, List<string> bindBehaviour, bool[] behaviourIndexs) {
            this.Infos = infos;
            this.SceneIndexs = SceneIndexs;
            this.BindBehaviour = bindBehaviour;
            this.BehaviourIndexs = behaviourIndexs;
        }
    }

    public class LazyPanEntity : EditorWindow {
        private string _instanceFlowName;
        private string _instanceTypeName;
        private string _instanceObjName;
        private string _instanceObjChineseName;
        
        private string[][] _entityConfig;//实体配置
        private string[] behaviourNames;//行为名称数组

        private bool[] selectedOptions;

        private int _index;

        private Dictionary<string, string> linkedSceneDictionary = new Dictionary<string, string>();
        private List<string> sceneNameOptions = new List<string>();

        private Dictionary<string, string> linkedBehaviourDictionary = new Dictionary<string, string>();
        private List<string> behaviourNameOptions = new List<string>();

        private List<string> operationNameOptions = new List<string>();

        //标题表
        private string[] names = new[] {
            "实体预览标题标识",
            "实体预览标题流程",
            "实体预览标题类型",
            "实体预览标题名称",
            "实体预览标题创建绑定的位置信息",
            "实体预览标题创建绑定行为名字",
            "实体预览标题操作",
        };

        //数据 全量(与 csv 一一对应 改写 csv 只认它)
        private List<MyEntityData> allEntityDatas = new List<MyEntityData>();
        //数据 当前显示(可能是模糊搜索过滤后的子集 存的是同一批对象引用 改它等于改全量)
        private List<MyEntityData> entityDatas = new List<MyEntityData>();
        //模糊搜索数据
        private List<MyEntityData> fuzzyEntityDatas = new List<MyEntityData>();
        //记录列表
        private ReorderableList reorderableList;

        private bool isFoldoutTool;
        private bool isFoldoutData;
        private bool isFoldoutGenerate;
        
        //模糊搜索
        private string UserInput;
        private bool isFocused;
        private string placeholder = "实体模糊搜索请输入关键字输入提示文本";
        private string tmpPlaceHolder;
        private string textName = "模糊搜索控件";

        private LazyPanTool _tool;

        public void OnStart(LazyPanTool tool) {
            _tool = tool;
            _tool.InitScroll();

            #region 实体配置

            //实体配置数组
            ReadCSV.Instance.Read("ObjConfig", out string content, out string[] lines);
            if (lines != null && lines.Length > 0) {
                _entityConfig = new string[lines.Length - 3][];
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = lines[i].Split(",");
                        _entityConfig[i - 3] = new string[lineStr.Length];
                        if (lineStr.Length > 0) {
                            for (int j = 0; j < lineStr.Length; j++) {
                                _entityConfig[i - 3][j] = lineStr[j];
                            }
                        }
                    }
                }
            }

            #endregion

            #region 行为配置

            //读取行为配置
            ReadCSV.Instance.Read("BehaviourConfig", out content, out lines);
            string[][] _behaviourConfig = new string[lines.Length - 3][];
            if (lines != null && lines.Length > 0) {
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = lines[i].Split(",");
                        _behaviourConfig[i - 3] = new string[lineStr.Length];
                        if (lineStr.Length > 0) {
                            for (int j = 0; j < lineStr.Length; j++) {
                                _behaviourConfig[i - 3][j] = lineStr[j];
                            }
                        }
                    }
                }

                //获取行为名称
                behaviourNames = new string[_behaviourConfig.Length];
                for (int i = 0; i < _behaviourConfig.Length; i++) {
                    string[] tmpStr = _behaviourConfig[i];
                    for (int j = 0; j < tmpStr.Length; j++) {
                        if (j == 1) {
                            behaviourNames[i] = tmpStr[j];
                        }
                    }
                }
            }

            #endregion

            isFoldoutTool = true;
            isFoldoutData = true;
            isFoldoutGenerate = true;
            
            InitReorderableList("");
        }

        public void OnCustomGUI(float areaX) {
            GUILayout.BeginArea(new Rect(areaX + _tool.scrollOffsetX, 60 + _tool.scrollOffsetY, Screen.width, Screen.height * 10));
            Title();
            AutoTool();
            PreviewEntityConfigData();
            ManualGeneratePrefabTool();
            GUILayout.EndArea();
        }

        /// <summary>
        /// 初始化无序列表
        /// </summary>
        private void InitReorderableList(string fuzzyContent) {
            InitLinkDic();
            InitOptionDic();
            ReadEntityData(fuzzyContent);
            reorderableList = new ReorderableList(entityDatas, typeof(MyEntityData), true, true, true, true);
            //头部标题
            reorderableList.drawHeaderCallback = (Rect rect) => {
                _index = 0;
                for (int i = 0; i < names.Length; i++) {
                    EditorGUI.LabelField(
                        new Rect(rect.x + 15f + Screen.width / 7 * _index++,
                            rect.y,
                            Screen.width / 7,
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
                string tmpFlowSign = "";
                
                //遍历标题
                for (int i = 0; i < names.Length; i++) {
                    Rect labelRect = new Rect(rect.x + _width / 7 * i,
                        rect.y,
                        _width / 7,
                        EditorGUIUtility.singleLineHeight);
                    if (i == 1) {
                        labelRect.height = 18f;
                        int selectIndex = EditorGUI.Popup(labelRect, entityDatas[index].SceneIndexs, sceneNameOptions.ToArray());
                        if (entityDatas[index].SceneIndexs != selectIndex) {
                            entityDatas[index].SceneIndexs = selectIndex;
                            string sceneName = sceneNameOptions[selectIndex];
                            entityDatas[index].Infos[i] = sceneName;
                            reorderableList.onChangedCallback.Invoke(reorderableList);
                        }
                    } else if (i < 5) {
                        // 设置Label的矩形区域
                        string textField = EditorGUI.TextField(labelRect, entityDatas[index].Infos[i], EditorStyles.wordWrappedLabel);
                        if (entityDatas[index].Infos[i] != textField) {
                            entityDatas[index].Infos[i] = textField;
                            reorderableList.onChangedCallback.Invoke(reorderableList);
                        }
                    } else if (i == 5) {
                        //行为按钮的文本
                        string behaviourBindNames = "";
                        List<string> bindBehaviour = entityDatas[index].BindBehaviour;
                        for (int k = 0 ; k < bindBehaviour.Count ; k++) {
                            behaviourBindNames += bindBehaviour[k];
                            if (k != bindBehaviour.Count - 1) {
                                behaviourBindNames += ",";
                            }
                        }

                        //实体设置按钮：按实体标识打开对应 Graph 并传入该行配置的行为名
                        labelRect.height = 18f;
                        if (GUI.Button(labelRect, "实体设置")) {
                            string[] behaviourNames = entityDatas[index].Infos[5].Split('|');
                            EntityGraphHook.OpenEntityGraph?.Invoke(entityDatas[index].Infos[0], behaviourNames);
                        }
                        /*
                        if (GUI.Button(labelRect, behaviourBindNames)) {
                            selectedOptions = entityDatas[index].BehaviourIndexs;
                            GenericMenu menu = new GenericMenu();
                            //遍历所有配置的行为名
                            for (int j = 0; j < behaviourNames.Length; j++) {
                                int tmpIndex = j;
                                string tmpBehaviourName = behaviourNames[j];
                                bool isSelected = entityDatas[index].BindBehaviour.Contains(tmpBehaviourName);
                                selectedOptions[j] = isSelected;
                                menu.AddItem(new GUIContent(tmpBehaviourName), isSelected, () => ToggleLayerSelection(entityDatas[index], tmpIndex, entityDatas[index].Infos[1], entityDatas[index].Infos[0], tmpBehaviourName));
                            }
                            Rect buttonRect = labelRect; // 使用原始的labelRect
                            Vector2 menuPosition = new Vector2(buttonRect.xMin, buttonRect.yMax);
                            menu.DropDown(new Rect(menuPosition, Vector2.zero));
                        }
                        */
                    } else if (i == 6) {
                        RefreshOperationNameOptions(IsExitPrefab(entityDatas[index]));

                        List<string> displayOprationNameOptionsOfLanguage = new List<string>();
                        foreach (var tmpOption in operationNameOptions) {
                            displayOprationNameOptionsOfLanguage.Add(LazyPanTool.GetText(tmpOption));
                        }

                        labelRect.height = 18f;
                        int selectIndex = EditorGUI.Popup(labelRect, entityDatas[index].OperationIndex, displayOprationNameOptionsOfLanguage.ToArray());
                        if (entityDatas[index].OperationIndex != selectIndex) {
                            entityDatas[index].OperationIndex = 0;
                            string operationName = operationNameOptions[selectIndex];
                            Operation(operationName, entityDatas[index].Infos);
                        }
                    }
                }
            };
            reorderableList.onReorderCallback = (ReorderableList list) => {
            };
            reorderableList.onAddCallback = (ReorderableList list) => {
                string[] infos = new string[]{"", "", "", "", "", ""};
                int sceneIndex = -1;
                List<string> bindBehaviour = new List<string> { behaviourNames.Length > 0 ? behaviourNames[0] : string.Empty };
                bool[] behaviourName = new bool[behaviourNames.Length];
                MyEntityData fresh = new MyEntityData(infos, sceneIndex, bindBehaviour, behaviourName);
                entityDatas.Add(fresh);
                //新行必须进全量 否则它永远写不进 csv(搜过滤时尤其如此)
                if (!allEntityDatas.Contains(fresh)) {
                    allEntityDatas.Add(fresh);
                }
            };
            reorderableList.onRemoveCallback = (ReorderableList list) => {
                // 确保索引有效 显示与全量一起删 否则删完重读又回来
                if (list.index >= 0 && list.index < list.list.Count) {
                    MyEntityData removed = entityDatas[list.index];
                    list.list.RemoveAt(list.index);  // 移除当前选中的元素
                    allEntityDatas.Remove(removed);
                }
            };
            reorderableList.onChangedCallback = (ReorderableList list) => {
                //csv数据自动更新
                WriteEntityData();
            };
        }

        private void Operation(string operationName, string[] infos) {
            GameObject prefab = GetPrefab(infos[1], infos[0], out string flowCode);
            if (operationName == "实体操作跳转到预制体") {
                //点击的实体如果在实体配置存在直接跳转 如果没有游戏物体创建
                if (prefab != null) {
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
            } else if (operationName == "实体操作创建预制体") {
                _instanceFlowName = flowCode;
                _instanceTypeName = infos[2];
                _instanceObjName = SignNamePart(infos[0]);
                _instanceObjChineseName = infos[3];
                GUI.FocusControl("objChineseName");
            } else if (operationName == "实体操作打印绑定数据到日志") {
                DisplayEntityData(prefab, infos);
            }
        }

        private void DisplayEntityData(GameObject prefab, string[] infos) {
            Data data = prefab.GetComponent<Data>();
            // 设置每列的固定宽度
            int signWidth = 30; // Sign列的宽度
            int descriptionWidth = 30; // Description列的宽度
            int valueWidth = 30; // 数据列的宽度（Bool, Float, Int, String, Vector3）
            int titleWidth = 40;

            // 计算分隔符的统一长度
            int totalWidth = signWidth + descriptionWidth + valueWidth + 6; // 6 是额外的空白和间隔

            string log = "";

            // 打印标题
            log += new string('=', titleWidth) + "\n"; // 使用等号生成统一长度的分隔线
            log += $"              {infos[1]} {infos[0]} 数据展示\n";
            log += new string('=', titleWidth) + "\n";
            log += "\n"; // 增加空行

            // 打印 Bool 数据
            log += "\n[ Bools ]\n"; // 在类别前加空行
            log += new string('-', totalWidth / 2) + "\n"; // 使用短横线作为每个数据类别的分隔符
            log += string.Format("{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                "Sign", "Description", "Value");
            log += new string('-', totalWidth / 2) + "\n";
            foreach (var tmp in data.Bools) {
                log += string.Format(
                    "{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                    tmp.Sign, tmp.Description, tmp.Bool);
            }

            log += "\n"; // 在数据后增加空行

            // 打印 Float 数据
            log += "\n[ Floats ]\n"; // 在类别前加空行
            log += new string('-', totalWidth / 2) + "\n";
            log += string.Format("{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                "Sign", "Description", "Value");
            log += new string('-', totalWidth / 2) + "\n";
            foreach (var tmp in data.Floats) {
                log += string.Format(
                    "{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                    tmp.Sign, tmp.Description, tmp.Float);
            }

            log += "\n"; // 在数据后增加空行

            // 打印 Int 数据
            log += "\n[ Integers ]\n"; // 在类别前加空行
            log += new string('-', totalWidth / 2) + "\n";
            log += string.Format("{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                "Sign", "Description", "Value");
            log += new string('-', totalWidth / 2) + "\n";
            foreach (var tmp in data.Ints) {
                log += string.Format(
                    "{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                    tmp.Sign, tmp.Description, tmp.Int);
            }

            log += "\n"; // 在数据后增加空行

            // 打印 String 数据
            log += "\n[ Strings ]\n"; // 在类别前加空行
            log += new string('-', totalWidth / 2) + "\n";
            log += string.Format("{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                "Sign", "Description", "Value");
            log += new string('-', totalWidth / 2) + "\n";
            foreach (var tmp in data.Strings) {
                log += string.Format(
                    "{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                    tmp.Sign, tmp.Description, tmp.String);
            }

            log += "\n"; // 在数据后增加空行

            // 打印 Vector3 数据
            log += "\n[ Vector3s ]\n"; // 在类别前加空行
            log += new string('-', totalWidth / 2) + "\n";
            log += string.Format("{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                "Sign", "Description", "Value");
            log += new string('-', totalWidth / 2) + "\n";
            foreach (var tmp in data.Vector3s) {
                log += string.Format(
                    "{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                    tmp.Sign, tmp.Description, tmp.Vector3);
            }

            // 打印结束标记
            Debug.Log(log);
        }

        private GameObject GetPrefab(string flowName, string entitySign, out string flowCode) {
            GameObject prefab = null;
            flowCode = null;
            if (linkedSceneDictionary.TryGetValue(flowName, out flowCode)) {
                string prefabPath = $"Assets/LazyPan/Bundles/Prefabs/Obj/{flowCode}/{entitySign}.prefab";
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
            // 新 sign 已含场景段(Obj_类型_场景_名称) 旧路径找不到时按 sign 第3段定位目录
            if (prefab == null) {
                string[] parts = (entitySign ?? "").Split("_");
                if (parts.Length >= 4) {
                    string sceneCode = parts[2];
                    string prefabPath = $"Assets/LazyPan/Bundles/Prefabs/Obj/{sceneCode}/{entitySign}.prefab";
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    flowCode = sceneCode;
                }
            }

            return prefab;
        }

        private void InitLinkDic() {
            //行为 - 读取 BehaviourConfig
            linkedBehaviourDictionary.Clear();
            ReadCSV.Instance.Read("BehaviourConfig", out string objContent, out string[] objContents);
            if (objContents != null && objContents.Length > 0) {
                for (int i = 0; i < objContents.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = objContents[i].Split(",");
                        linkedBehaviourDictionary.TryAdd(lineStr[1], lineStr[0]);
                    }
                }
            }
            
            linkedSceneDictionary.Clear();
            ReadCSV.Instance.Read("SceneConfig", out string sceneContent, out string[] sceneContents);
            if (sceneContents != null && sceneContents.Length > 0) {
                for (int i = 0; i < sceneContents.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = sceneContents[i].Split(",");
                        linkedSceneDictionary.TryAdd(lineStr[1], lineStr[0]);
                    }
                }
            }
        }

        private void InitOptionDic() {
            behaviourNameOptions = new List<string>();
            if (linkedBehaviourDictionary != null) {
                foreach (var tmp in linkedBehaviourDictionary) {
                    behaviourNameOptions.Add(tmp.Key);
                }
            }
            
            sceneNameOptions = new List<string>();
            if (linkedSceneDictionary != null) {
                foreach (var tmp in linkedSceneDictionary) {
                    sceneNameOptions.Add(tmp.Key);
                }
            }
        }

        private void RefreshOperationNameOptions(bool exist) {
            operationNameOptions.Clear();
            operationNameOptions.Add("实体操作操作文本");
            operationNameOptions.Add(exist ? "实体操作跳转到预制体" : "实体操作创建预制体");
            if (exist) {
                operationNameOptions.Add("实体操作打印绑定数据到日志");
            }
        }

        private void ReadEntityData(string fuzzyContent) {
            allEntityDatas.Clear();
            entityDatas.Clear();
            ReadCSV.Instance.Read("ObjConfig", out string content, out string[] lines);
            if (lines != null && lines.Length > 0) {
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = lines[i].Split(",");
                        if (lineStr.Length > 0) {
                            bool hasFuzzyContent = string.IsNullOrEmpty(fuzzyContent);
                            for (int j = 0; j <= 5 && !hasFuzzyContent; j++) {
                                if (lineStr[j].Contains(fuzzyContent)) {
                                    hasFuzzyContent = true;
                                    break;
                                }
                            }

                            //实体数据 空行为允许 原样保留 不回填默认行为
                            string[] lineInfo = new string[6];
                            for (int j = 0; j <= 5; j++) {
                                lineInfo[j] = lineStr[j];
                            }
                            //场景数据
                            string[] sceneIndex = sceneNameOptions.ToArray();
                            int selectScene = -1;
                            for (int j = 0; j < sceneIndex.Length; j++) {
                                if (sceneIndex[j] == lineInfo[1]) {
                                    selectScene = j;
                                    break;
                                }
                            }
                            //行为数据
                            string behaviourBind = lineStr[5];
                            string[] behaviourBindName = behaviourBind.Split('|');
                            //遍历玩家绑定的行为
                            bool[] selectBehaviour = new bool[behaviourNames.Length];
                            foreach (var tmpBehaviourConfig in behaviourNames) {
                                for (int j = 0; j < behaviourBindName.Length; j++) {
                                    string behaviourName = behaviourBindName[j];
                                    if (tmpBehaviourConfig == behaviourName) {
                                        selectBehaviour[j] = true;
                                    }
                                }
                            }

                            if (string.IsNullOrWhiteSpace(lineInfo[5])) {
                                behaviourBindName = new string[0];
                            }

                            MyEntityData instanceEntityData = new MyEntityData(lineInfo, selectScene,
                                behaviourBindName.ToList(), selectBehaviour);
                            allEntityDatas.Add(instanceEntityData);
                            if (string.IsNullOrEmpty(fuzzyContent) || hasFuzzyContent) {
                                entityDatas.Add(instanceEntityData);
                            }
                        }
                    }
                }
            }
        }

        private bool IsExitPrefab(MyEntityData entityData) {
            return GetPrefab(entityData.Infos[1], entityData.Infos[0], out string flowCode) != null;
        }

        private void WriteEntityData() {
            ReadCSV.Instance.Read("ObjConfig", out string content, out string[] lines);
            //空行为允许 只有 Sign 为空的行才跳过 不拦别人
            //按 Sign 键名回写 不按位置 模糊搜索过滤后改某行不会串到别的行
            List<MyEntityData> validDatas = new List<MyEntityData>();
            foreach (MyEntityData tmp in allEntityDatas) {
                if (tmp?.Infos == null || tmp.Infos.Length <= 0 || string.IsNullOrWhiteSpace(tmp.Infos[0])) {
                    continue;
                }

                validDatas.Add(tmp);
            }

            try {
                HashSet<MyEntityData> written = new HashSet<MyEntityData>();
                for (int i = 0; i < lines.Length; i++) {
                    if (i <= 2) {
                        continue;
                    }

                    string[] linesStr = lines[i].Split(',');
                    if (linesStr.Length == 0 || string.IsNullOrWhiteSpace(linesStr[0])) {
                        continue;
                    }

                    MyEntityData match = null;
                    foreach (MyEntityData tmp in validDatas) {
                        if (!written.Contains(tmp) && tmp.Infos[0].Trim() == linesStr[0].Trim()) {
                            match = tmp;
                            break;
                        }
                    }

                    if (match == null) {
                        //csv 里有但列表里没了=用户删了这行 跟着删掉
                        lines[i] = null;
                        continue;
                    }

                    for (int j = 0; j < match.Infos.Length && j < linesStr.Length; j++) {
                        linesStr[j] = match.Infos[j];
                    }

                    lines[i] = string.Join(",", linesStr);
                    written.Add(match);
                }

                List<string> outLines = new List<string>();
                foreach (string line in lines) {
                    if (line != null) {
                        outLines.Add(line);
                    }
                }

                //列表里有但 csv 里没有=新增 追加到末尾
                foreach (MyEntityData tmp in validDatas) {
                    if (written.Contains(tmp)) {
                        continue;
                    }

                    string[] linesStr = new string[6];
                    for (int j = 0; j < tmp.Infos.Length && j < 6; j++) {
                        linesStr[j] = tmp.Infos[j];
                    }

                    outLines.Add(string.Join(",", linesStr));
                }

                ReadCSV.Instance.Write("ObjConfig", outLines.ToArray());
            } catch {
                Debug.LogError("录入错误");
            }
        }

        private void PreviewEntityConfigData() {
            isFoldoutData = EditorGUILayout.Foldout(isFoldoutData, LazyPanTool.GetText("实体预览实体配置数据展开文本"), true);
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

        private void ManualGeneratePrefabTool() {
            isFoldoutGenerate = EditorGUILayout.Foldout(isFoldoutGenerate, LazyPanTool.GetText("实体手动创建预制体工具展开文本"), true);
            Rect rect = GUILayoutUtility.GetLastRect();
            float height = 0;
            if (isFoldoutGenerate) {
                GUILayout.BeginVertical();
                GUILayout.Label("");
                GUILayout.BeginHorizontal();
                GUILayout.Label(LazyPanTool.GetText("实体手动创建预制体工具流程流程标题"), GUILayout.Width(300));  // 设置提示文本的宽度
                _instanceFlowName = EditorGUILayout.TextField(_instanceFlowName, GUILayout.Height(20));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(LazyPanTool.GetText("实体手动创建预制体工具流程类型标题"), GUILayout.Width(300));  // 设置提示文本的宽度
                _instanceTypeName = EditorGUILayout.TextField(_instanceTypeName, GUILayout.Height(20));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(LazyPanTool.GetText("实体手动创建预制体工具流程实体标题"), GUILayout.Width(300));  // 设置提示文本的宽度
                _instanceObjName = EditorGUILayout.TextField(_instanceObjName, GUILayout.Height(20));
                GUILayout.EndHorizontal();
                GUI.SetNextControlName("objChineseName");
                GUILayout.BeginHorizontal();
                GUILayout.Label(LazyPanTool.GetText("实体手动创建预制体工具流程实体中文名字标题"), GUILayout.Width(300));  // 设置提示文本的宽度
                _instanceObjChineseName = EditorGUILayout.TextField(_instanceObjChineseName, GUILayout.Height(20));
                GUILayout.EndHorizontal();
                GUIStyle style = LazyPanTool.GetGUISkin("AButtonGUISkin").GetStyle("button");
                if(GUILayout.Button(LazyPanTool.GetText("实体手动创建预制体工具流程创建实体物体按钮文本"), style)) {
                    InstanceCustomObj();
                }
                if(GUILayout.Button(LazyPanTool.GetText("实体手动创建预制体工具流程创建实体物体点位配置"), style)) {
                    InstanceCustomLocationSetting();
                }
                GUILayout.EndVertical();
                height += GUILayoutUtility.GetLastRect().height;
            } else {
                GUILayout.Space(10);
            }
            
            LazyPanTool.DrawBorder(new Rect(rect.x + 2f, rect.y - 2f, rect.width - 2f, rect.height + height + 5f), Color.white);

            GUILayout.Space(10);
        }

        private void AutoTool() {
            GUIStyle style = LazyPanTool.GetGUISkin("AButtonGUISkin").GetStyle("button");
            isFoldoutTool = EditorGUILayout.Foldout(isFoldoutTool, LazyPanTool.GetText("实体自动化工具展开文本"), true);
            Rect rect = GUILayoutUtility.GetLastRect();
            float height = 0;
            if (isFoldoutTool) {
                GUILayout.Label("");
                height += GUILayoutUtility.GetLastRect().height;
                if(GUILayout.Button(LazyPanTool.GetText("实体自动化工具按钮文本"), style)) {
                    GUILayout.BeginHorizontal();
                    OpenEntityCsv();
                    GUILayout.EndHorizontal();
                }
                height += GUILayoutUtility.GetLastRect().height;
                if(GUILayout.Button(LazyPanTool.GetText("实体批量输出实体绑定数据日志按钮文本"), style)) {
                    GUILayout.BeginHorizontal();
                    LogAllEntityData();
                    GUILayout.EndHorizontal();
                }
                height += GUILayoutUtility.GetLastRect().height;
            } else {
                GUILayout.Space(10);
            }
            
            LazyPanTool.DrawBorder(new Rect(rect.x + 2f, rect.y - 2f, rect.width - 2f, rect.height + height + 10f), Color.white);

            GUILayout.Space(10);
        }

        private void LogAllEntityData() {
            foreach (var tmpEntity in entityDatas) {
                if (IsExitPrefab(tmpEntity)) {
                    GameObject prefab = GetPrefab(tmpEntity.Infos[1], tmpEntity.Infos[0], out string flowCode);
                    DisplayEntityData(prefab, tmpEntity.Infos);
                }
            }
        }

        private void Title() {
            GUILayout.BeginHorizontal();
            GUIStyle style = LazyPanTool.GetGUISkin("LogoGUISkin").GetStyle("label");
            GUILayout.Label("ENTITY", style);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            style = LazyPanTool.GetGUISkin("AnnotationGUISkin").GetStyle("label");
            GUILayout.Label("@" + LazyPanTool.GetText("实体小标题"), style);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
        }

        //选中改变状态
        private void ToggleLayerSelection(MyEntityData entityData, int index, string flowSign, string entitySign, string behaviourSign) {
            entityData.BehaviourIndexs[index] = !entityData.BehaviourIndexs[index];
            //如果点击后是增加 则增加行为数据 反之减少 操作是对行为重新录入
            UpdateEntityBehaviourData(entityData.BehaviourIndexs[index], flowSign, entitySign, behaviourSign);
            OnStart(_tool);
        }

        private void UpdateEntityBehaviourData(bool add, string flowSign, string entitySign, string behaviourName) {
            ReadCSV.Instance.Read("ObjConfig", out string content, out string[] lines);
            for (int i = 0; i < lines.Length; i++) {
                if (i > 2) {
                    string[] linesStr = lines[i].Split(',');
                    if (linesStr[0] == entitySign && linesStr[1] == flowSign) {
                        string[] bindBehaviourName = linesStr[5].Split('|');
                        string newBind = "";
                        foreach (var tmpBindStr in bindBehaviourName) {
                            if (add) {
                                newBind += tmpBindStr + "|";
                            } else {
                                if (tmpBindStr != behaviourName) {
                                    newBind += tmpBindStr + "|";
                                }
                            }
                        }

                        if (add) {
                            newBind += behaviourName;
                        }

                        newBind = newBind.TrimEnd('|');
                        newBind = newBind.TrimStart('|');

                        linesStr[5] = newBind;
                
                        // 将更新后的内容重新拼接回 lines 数组
                        lines[i] = string.Join(",", linesStr);
                        break;
                    }
                }
            }

            ReadCSV.Instance.Write("ObjConfig", lines);
        }

        private void ExpandEntityData() {
            bool hasContent = false;
            if (_entityConfig != null && _entityConfig.Length > 0) {
                GUILayout.BeginVertical();
                string entitySign = "";
                int displayCount = 0;
                foreach (var str in _entityConfig) {
                    if (str != null) {
                        if (entitySign != str[1]) {
                            entitySign = str[1];
                            GUILayout.Label("");
                        }

                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        string strContent = "";
                        displayCount = str.Length;
                        for (int i = 0; i < displayCount; i++) {
                            bool isLast = false;
                            strContent = str[i];

                            bool hasPrefab = HasPrefabTips(str);
                            Color fontColor;
                            if (i == 1) {
                                fontColor = Color.cyan;
                            } else if (i == 0) {
                                fontColor = hasPrefab ? Color.green : Color.red;
                            } else {
                                fontColor = Color.green;
                            }

                            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                            labelStyle.normal.textColor = fontColor; // 设置字体颜色

                            //预制体相关判断
                            if (GUILayout.Button(strContent, isLast ? null : labelStyle,
                                GUILayout.Width(Screen.width / (displayCount + 1) - 10))) {
                                switch (i) {
                                    case 0:
                                        PrefabJudge(hasPrefab, str);
                                        break;
                                    case 5:
                                        BehaviourJudge(str);
                                        break;
                                }
                            }

                            string tooltip = "";
                            // 检测鼠标悬停
                            Rect buttonRect = GUILayoutUtility.GetLastRect();
                            if (buttonRect.Contains(Event.current.mousePosition)) {
                                if (!hasPrefab) {
                                    tooltip = "找不到预制体，请添加: " + str[0];
                                }
                            }

                            // 显示悬浮信息
                            if (!string.IsNullOrEmpty(tooltip)) {
                                Vector2 tooltipPosition =
                                    Event.current.mousePosition + new Vector2(10, 10); // 设置悬浮提示位置
                                GUI.Label(new Rect(tooltipPosition.x, tooltipPosition.y, 250, 20), tooltip);
                            }

                            hasContent = true;
                        }

                        strContent = "行为绑定";
                        SelectBindBehaviour(strContent, displayCount, str[1], str[0], str[5]);

                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();                
                    }
                }
                GUILayout.EndVertical();
            }

            if(!hasContent || _entityConfig == null || _entityConfig.Length == 0) {
                GUILayout.Label("找不到 EntityConfig.csv 的配置数据！\n请检查是否存在路径或者配置内数据是否为空！");
            }
        }

        private void SelectBindBehaviour(string buttonName, int displayCount, string flowSign, string entitySign, string behaviourSigns) {
            // if (GUILayout.Button(buttonName, GUILayout.Width(Screen.width / (displayCount + 1) - 10))) {
            //     GenericMenu menu = new GenericMenu();
            //     List<string> behaviourClips = behaviourSigns.Split('|').ToList();
            //     selectedOptions = new bool[behaviourNames.Length];
            //     for (int i = 0; i < behaviourNames.Length; i++) {
            //         int index = i;
            //         string tmpBehaviourName = behaviourNames[i];
            //         bool isSelected = behaviourClips.Contains(tmpBehaviourName);
            //         selectedOptions[i] = isSelected;
            //         menu.AddItem(new GUIContent(tmpBehaviourName), isSelected, () => ToggleLayerSelection(index, flowSign, entitySign, tmpBehaviourName));
            //     }
            //     menu.ShowAsContext();
            // }
        }

        //绑定行为相关
        private void BehaviourJudge(string[] str) {
            //Color green
            //Color red
            //去 BehaviourConfig 判断是否配置行为 没有的话 点击 CSV 创建？ 有的话跳转到行为预览？
            Debug.Log("行为点击：" + str[5]);
        }

        //预制体相关
        private void PrefabJudge(bool hasPrefab, string[] str) {
            //点击的实体如果在实体配置存在直接跳转 如果没有游戏物体创建
            if (hasPrefab) {
                string path = $"Assets/LazyPan/Bundles/Prefabs/Obj/{str[1]}/{str[0]}.prefab"; // 修改为你的路径
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) {
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
            } else {
                _instanceFlowName = str[1];
                _instanceTypeName = str[2];
                _instanceObjName = SignNamePart(str[0]);
                GUI.FocusControl("objChineseName");
            }
        }

        //是否存在预制体
        private bool HasPrefabTips(string[] str) {
            string prefabPath = $"Assets/LazyPan/Bundles/Prefabs/Obj/{str[1]}/{str[0]}.prefab";
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
        }

        private void OpenEntityCsv() {
            string filePath = Application.dataPath + "/StreamingAssets/Csv/ObjConfig.csv";
            Process.Start(filePath);
        }

        private void InstanceCustomObj() {
            if (!CheckInstanceInput(out string validError)) {
                Debug.LogError(validError);
                return;
            }
            string newSign = string.Concat("Obj_", _instanceTypeName, "_", _instanceFlowName, "_", _instanceObjName);
            string sourcePath = ResolveSamplePrefabPath();
            if (string.IsNullOrEmpty(sourcePath)) {
                Debug.LogError("创建预制体失败: 找不到样板预制体 Obj_Sample_Sample.prefab 请检查包内 Runtime/Bundles/Prefabs/Obj/ 下是否存在");
                return;
            }
            string targetFolderPath = $"Assets/LazyPan/Bundles/Prefabs/Obj/{_instanceFlowName}";
            // 目标已存在直接提示 不覆盖
            string existPath = $"{targetFolderPath}/{newSign}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(existPath) != null) {
                Debug.LogError($"预制体已存在: {existPath} 不再重复创建");
                return;
            }
            // 获取选中的游戏对象
            GameObject selectedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (selectedPrefab != null && PrefabUtility.IsPartOfPrefabAsset(selectedPrefab)) {
                // 确保场景子目录存在(与 SceneConfig.DirPath=Obj/场景/ 保持一致)
                if (!AssetDatabase.IsValidFolder(targetFolderPath)) {
                    Directory.CreateDirectory(targetFolderPath);
                    AssetDatabase.Refresh();
                }

                // 获取预制体路径
                string prefabPath = AssetDatabase.GetAssetPath(selectedPrefab);

                // 拷贝预制体到场景子目录
                string targetPath = $"{targetFolderPath}/{Path.GetFileName(prefabPath)}";
                AssetDatabase.CopyAsset(prefabPath, targetPath);

                // 刷新AssetDatabase
                AssetDatabase.Refresh();

                //修改资源的名字为自定义 新格式: Obj_类型_场景_名称
                AssetDatabase.RenameAsset(targetPath, newSign);
                AssetDatabase.Refresh();
                Debug.Log($"预制体已创建: {targetFolderPath}/{newSign}.prefab");
            } else {
                Debug.LogError($"创建预制体失败: 样板预制体加载失败 路径:{sourcePath}");
            }
        }

        /// <summary>
        /// 样板预制体按文件名全局搜 不写死包名 本地 LazyPanPro 与拉取后 evoreek.lazypan 都能命中
        /// </summary>
        private string ResolveSamplePrefabPath() {
            string[] guids = AssetDatabase.FindAssets("Obj_Sample_Sample t:Prefab");
            if (guids == null || guids.Length == 0) {
                return null;
            }

            //优先用本编辑器脚本所在的包 避免机器上同时装两个包时串包
            string selfScriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            string selfPackage = null;
            if (!string.IsNullOrEmpty(selfScriptPath) && selfScriptPath.StartsWith("Packages/")) {
                string[] segments = selfScriptPath.Split('/');
                if (segments.Length >= 2) {
                    selfPackage = string.Concat(segments[0], "/", segments[1], "/");
                }
            }

            string fallback = null;
            foreach (string guid in guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) {
                    continue;
                }

                if (fallback == null) {
                    fallback = path;
                }

                if (!string.IsNullOrEmpty(selfPackage) && path.StartsWith(selfPackage)) {
                    return path;
                }
            }

            return fallback;
        }

        /// <summary>
        /// 校验手动创建四段输入: 非空/英文数字/段内无下划线/场景必须在 SceneConfig 存在
        /// </summary>
        private bool CheckInstanceInput(out string error) {
            error = null;
            if (string.IsNullOrWhiteSpace(_instanceObjName) || string.IsNullOrWhiteSpace(_instanceTypeName)
                || string.IsNullOrWhiteSpace(_instanceFlowName) || string.IsNullOrWhiteSpace(_instanceObjChineseName)) {
                error = "创建预制体失败: 场景/类型/名称/中文名均不能为空";
                return false;
            }
            if (!IsSingleWord(_instanceTypeName) || !IsSingleWord(_instanceFlowName) || !IsSingleWord(_instanceObjName)) {
                error = $"创建预制体失败: 场景/类型/名称只能是英文数字 且不能含下划线与空格 当前({_instanceTypeName}/{_instanceFlowName}/{_instanceObjName})";
                return false;
            }
            // 场景段必须在 SceneConfig 登记 否则运行时 DirPath 拼不出路径
            ReadCSV.Instance.Read("SceneConfig", out string content, out string[] lines);
            bool sceneExist = false;
            if (lines != null) {
                for (int i = 3; i < lines.Length; i++) {
                    if (lines[i].Split(",")[0].Trim() == _instanceFlowName.Trim()) {
                        sceneExist = true;
                        break;
                    }
                }
            }
            if (!sceneExist) {
                error = $"创建预制体失败: 场景 {_instanceFlowName} 不在 SceneConfig.csv 中 请先登记场景";
                return false;
            }
            return true;
        }

        private static bool IsSingleWord(string s) {
            if (string.IsNullOrEmpty(s))
                return false;
            foreach (char c in s.Trim()) {
                bool ok = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
                if (!ok)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 从新格式 Sign(Obj_类型_场景_名称)取第4段名称 兼容旧格式回退第3段
        /// </summary>
        private static string SignNamePart(string sign) {
            if (string.IsNullOrEmpty(sign))
                return "";
            string[] parts = sign.Split("_");
            if (parts.Length >= 4)
                return parts[3];
            if (parts.Length >= 3)
                return parts[2];
            return sign;
        }

        private void InstanceCustomLocationSetting() {
            if (_instanceObjName == "" || _instanceTypeName == "" || _instanceFlowName == "" || _instanceObjChineseName == "") {
                return;
            }

            // 创建实例并赋值
            LocationInformationSetting testAsset = CreateInstance<LocationInformationSetting>();
            testAsset.SettingName = _instanceObjChineseName;
            testAsset.locationInformationDatas = new List<LocationInformationData>();
            testAsset.locationInformationDatas.Add(new LocationInformationData());

            // 替换为你希望保存的目录路径，例如 "Assets/MyFolder/"
            string savePath = "Assets/LazyPan/Bundles/Configs/Setting/LocationInformationSetting/";

            // 确保目标文件夹存在，如果不存在则创建
            if (!AssetDatabase.IsValidFolder(savePath)) {
                AssetDatabase.CreateFolder("Assets", "LazyPan/Bundles/Configs/Setting/LocationInformationSetting");
            }

            // 生成一个唯一的文件名
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(savePath + string.Concat("LocationInf_", _instanceFlowName, "_", _instanceObjName, ".asset"));

            // 将实例保存为.asset文件
            AssetDatabase.CreateAsset(testAsset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}