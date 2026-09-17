using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace LazyPan {
    public class LazyPanTool : EditorWindow {
        public Dictionary<Type, EditorWindow> editorWindows = new Dictionary<Type, EditorWindow>();

        private float areaX = 0;
        private float screenWidth = 1000;
        private float screenHeight = 800;
        private float animToolBarSpeed = 0.001f;
        private bool isAnimToolBar = false;
        private static int currentToolBar = 0;
        private int lastToolBar = 0;
        private int previousToolBar = 0;
        private static string language;

        private string[] values = {
            "引导首页 GuideHomePage ガイドホームページ", "流程 Flow プロセス", "场景 Scene シーン", "界面 UI インターフェース", "实体 Entity 実在物", "行为 Behaviour 行動", "工具箱 ToolBox ツールボックス", "支持 Support サポート"
        };
        
        public float scrollOffsetY;
        public float scrollOffsetX;
        private float maxScrollY = 0f;
        private float minScrollY = -99999f;
        private float maxScrollX = 0f;
        private float minScrollX = -99999f;

        [MenuItem("Tools/LazyPan/Open guide panel 打开引导面板 _F1", priority = 0)]
        public static void OpenGuide() {
            LazyPanTool window = (LazyPanTool)GetWindow(typeof(LazyPanTool), true, "LAZYPANPRO 框架", true);
            window.Show();
            ReadToolBar();
        }

        private void OnGUI() {
            ToolBar();
            Jump();
        }

        //标签
        private void ToolBar() {
            Scroll();
            currentToolBar = GUILayout.Toolbar(currentToolBar, values, GUILayout.Height(30));
            WriteToolBar(currentToolBar);
            try {
                switch (currentToolBar) {
                    case 0:
                        if (editorWindows.TryGetValue(typeof(LazyPanGuide), out EditorWindow ret)) {
                            if (ret is LazyPanGuide ep) {
                                if (lastToolBar != currentToolBar) {
                                    ep.OnStart(this);
                                    lastToolBar = currentToolBar;
                                }

                                ep.OnCustomGUI(areaX);
                            }
                        } else {
                            editorWindows.Add(typeof(LazyPanGuide), CreateInstance<LazyPanGuide>());
                        }

                        break;
                    case 1:
                        if (editorWindows.TryGetValue(typeof(LazyPanFlow), out ret)) {
                            if (ret is LazyPanFlow ep) {
                                if (lastToolBar != currentToolBar) {
                                    ep.OnStart(this);
                                    lastToolBar = currentToolBar;
                                }

                                ep.OnCustomGUI(areaX);
                            }
                        } else {
                            editorWindows.Add(typeof(LazyPanFlow), CreateInstance<LazyPanFlow>());
                        }

                        break;
                    case 2:
                        if (editorWindows.TryGetValue(typeof(LazyPanScene), out ret)) {
                            if (ret is LazyPanScene ep) {
                                if (lastToolBar != currentToolBar) {
                                    ep.OnStart(this);
                                    lastToolBar = currentToolBar;
                                }

                                ep.OnCustomGUI(areaX);
                            }
                        } else {
                            editorWindows.Add(typeof(LazyPanScene), CreateInstance<LazyPanScene>());
                        }

                        break;
                    case 3:
                        if (editorWindows.TryGetValue(typeof(LazyPanUI), out ret)) {
                            if (ret is LazyPanUI ep) {
                                if (lastToolBar != currentToolBar) {
                                    ep.OnStart(this);
                                    lastToolBar = currentToolBar;
                                }

                                ep.OnCustomGUI(areaX);
                            }
                        } else {
                            editorWindows.Add(typeof(LazyPanUI), CreateInstance<LazyPanUI>());
                        }

                        break;
                    case 4:
                        if (editorWindows.TryGetValue(typeof(LazyPanEntity), out ret)) {
                            if (ret is LazyPanEntity ep) {
                                if (lastToolBar != currentToolBar) {
                                    ep.OnStart(this);
                                    lastToolBar = currentToolBar;
                                }

                                ep.OnCustomGUI(areaX);
                            }
                        } else {
                            editorWindows.Add(typeof(LazyPanEntity), CreateInstance<LazyPanEntity>());
                        }

                        break;
                    case 5:
                        if (editorWindows.TryGetValue(typeof(LazyPanBehaviour), out ret)) {
                            if (ret is LazyPanBehaviour ep) {
                                if (lastToolBar != currentToolBar) {
                                    ep.OnStart(this);
                                    lastToolBar = currentToolBar;
                                }

                                ep.OnCustomGUI(areaX);
                            }
                        } else {
                            editorWindows.Add(typeof(LazyPanBehaviour), CreateInstance<LazyPanBehaviour>());
                        }

                        break;
                    case 6:
                        if (editorWindows.TryGetValue(typeof(LazyPanToolbox), out ret)) {
                            if (ret is LazyPanToolbox ep) {
                                if (lastToolBar != currentToolBar) {
                                    ep.OnStart(this);
                                    lastToolBar = currentToolBar;
                                }

                                ep.OnCustomGUI(areaX);
                            }
                        } else {
                            editorWindows.Add(typeof(LazyPanToolbox), CreateInstance<LazyPanToolbox>());
                        }
                        break;
                    case 7:
                        if (editorWindows.TryGetValue(typeof(LazyPanSupport), out ret)) {
                            if (ret is LazyPanSupport ep) {
                                if (lastToolBar != currentToolBar) {
                                    ep.OnStart(this);
                                    lastToolBar = currentToolBar;
                                }

                                ep.OnCustomGUI(areaX);
                            }
                        } else {
                            editorWindows.Add(typeof(LazyPanSupport), CreateInstance<LazyPanSupport>());
                        }
                        break;
                }
            } catch {
                
            }
        }

        private float progress;
        //跳转动画
        private void Jump() {
            //如果关闭滑动默认直接跳转
            if (!_currentTabSliderAnim) {
                areaX = 0; // 确保动画最终位置准确
                return;
            }

            //点击新的标签页
            if (previousToolBar != currentToolBar) {
                areaX = previousToolBar < currentToolBar ? Screen.width : -Screen.width;
                progress = 0;
                previousToolBar = currentToolBar;
            }

            // 动画更新
            if (progress < 1f) {
                progress += Time.deltaTime * animToolBarSpeed; // 更新动画进度
                progress = Mathf.Clamp01(progress); // 确保 progress 在 [0, 1] 范围内

                areaX = Mathf.Lerp(areaX, 0, progress); // 基于起点和终点的平滑插值

                Repaint(); // 强制重绘，刷新动画
            } else {
                areaX = 0; // 确保动画最终位置准确
            }
        }

        public void InitScroll() {
            scrollOffsetX = 0;
            scrollOffsetY = 0;
        }
        
        private void Scroll() {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 2) {
                scrollOffsetX = 0;
                scrollOffsetY = 0;
                return;
            }

            if (e.type == EventType.ScrollWheel) {
                if (e.shift) {
                    scrollOffsetX -= e.delta.y * 10f; // delta.y 为滚动的增量，这里乘以 10f 来控制滚动速度
                    scrollOffsetX = Mathf.Clamp(scrollOffsetX, minScrollX, maxScrollX);
                } else {
                    scrollOffsetY -= e.delta.y * 10f; // delta.y 为滚动的增量，这里乘以 10f 来控制滚动速度
                    scrollOffsetY = Mathf.Clamp(scrollOffsetY, minScrollY, maxScrollY);
                }
                e.Use();
            }
        }

        public static GUISkin GetGUISkin(string guiskinname) {
            return AssetDatabase.LoadAssetAtPath<GUISkin>(
                $"Packages/evoreek.lazypan/Runtime/Bundles/GUISkin/{guiskinname}.guiskin");
        }
        
        //绘制边框
        public static void DrawBorder(Rect rect, Color color) {
            Color borderColor = color;

            // 绘制四条边
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, 1), borderColor); // Top
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - 1, rect.width, 1), borderColor); // Bottom
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, 1, rect.height), borderColor); // Left
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.yMin, 1, rect.height), borderColor); // Right
        }

        //压缩文件
        public static void CompressFile(string sourceFolder, string destinationZipFile) {
            try {
                ZipFile.CreateFromDirectory(sourceFolder, destinationZipFile, CompressionLevel.Optimal, false);
                Debug.Log("Folder compressed successfully.");
            } catch (Exception ex) {
                Debug.LogError($"Failed to compress folder: {ex.Message}");
            }
        }

        //解压文件
        public static void DecompressFile(string sourceZipFile, string destinationFolder) {
            try {
                ZipFile.ExtractToDirectory(sourceZipFile, destinationFolder);
                Debug.Log("Folder decompressed successfully.");
            } catch (Exception ex) {
                Debug.LogError($"Failed to decompress folder: {ex.Message}");
            }
        }

        #region 动画

        public static bool _currentTabSliderAnim;

        /// <summary>
        /// 切换动画开关
        /// </summary>
        public static void CheckTabSliderAnim(bool openAnim) {
            int sliderAnim = PlayerPrefs.GetInt("OpenSliderAnim");
            if (sliderAnim != (openAnim ? 1 : 0)) {
                PlayerPrefs.SetInt("OpenSliderAnim", openAnim ? 1 : 0);
                PlayerPrefs.Save();
                _currentTabSliderAnim = openAnim;
            }
        }

        #endregion
        
        #region 多语言

        public static string _currentLanguage; 
        private static Dictionary<string, LanguageEntry> languageDictionary = new Dictionary<string, LanguageEntry>();

        /// <summary>
        /// 多语言初始化
        /// </summary>
        public static void InitLanguage() {
            if (languageDictionary.Count > 0) {
                return;
            }
            ReadLanguageCSV("Language", out string content, out string[] lines);

            if (lines != null && lines.Length > 0) {
                languageDictionary.Clear();
                string[][] LanguageConfigStr = new string[lines.Length - 1][];
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 0) {
                        string[] lineStr = lines[i].Split(",");
                        LanguageConfigStr[i - 1] = new string[lineStr.Length];
                        if (lineStr.Length > 0) {
                            LanguageEntry entry = new LanguageEntry {
                                Key = lineStr[0],
                                English = lineStr[1],
                                Chinese = lineStr[2],
                                Japanese = lineStr[3]
                            };
                            languageDictionary[entry.Key] = entry;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 切换语言
        /// </summary>
        /// <param name="currentLanguage"></param>
        public static void CheckLanguage(string currentLanguage) {
            string language = PlayerPrefs.GetString("LazyPanLanguage");
            if (string.IsNullOrEmpty(language) || currentLanguage != language) {
                PlayerPrefs.SetString("LazyPanLanguage", currentLanguage);
                PlayerPrefs.Save();
                _currentLanguage = currentLanguage;
            }
        }

        /// <summary>
        /// 初始化索引
        /// </summary>
        private static void ReadToolBar() {
            string toolBar = PlayerPrefs.GetString("LazyPanToolBar");
            if (string.IsNullOrEmpty(toolBar)) {
                WriteToolBar(currentToolBar);
                currentToolBar = 0;
            } else {
                currentToolBar = int.Parse(toolBar);
            }
        }

        /// <summary>
        /// 设置索引
        /// </summary>
        private static void WriteToolBar(int value) {
            string toolBarConfig = PlayerPrefs.GetString("LazyPanToolBar");
            if (string.IsNullOrEmpty(toolBarConfig) || toolBarConfig != value.ToString()) {
                PlayerPrefs.SetString("LazyPanToolBar", value.ToString());
                PlayerPrefs.Save();
                currentToolBar = value;
            }
        }

        /// <summary>
        /// 获取预言
        /// </summary>
        /// <param name="key"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public static string GetText(string key) {
            InitLanguage();

            if (languageDictionary.TryGetValue(key, out LanguageEntry entry)) {
                switch (_currentLanguage) {
                    case "English":
                        return entry.English;
                    case "中文":
                        return entry.Chinese;
                    case "日本語" :
                        return entry.Japanese;
                    default:
                        return entry.English; // 默认返回英文
                }
            }
            return $"<{key} not found>";
        }

        /// <summary>
        /// 读取语言配置
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="content"></param>
        /// <param name="lines"></param>
        private static void ReadLanguageCSV(string fileName, out string content, out string[] lines) {
            string filePath = Path.Combine(Application.dataPath, "../Packages/evoreek.lazypan/Editor/Language/Language.csv");
            filePath = Path.GetFullPath(filePath); // 将路径转换为绝对路径

            // 检查文件是否存在
            if (!File.Exists(filePath)) {
                content = string.Empty;
                lines = default;
                return; // 文件不存在，直接返回
            }

            try {
                using (StreamReader sr = new StreamReader(filePath)) {
                    string str = null;
                    string line;
                    while ((line = sr.ReadLine()) != null) {
                        str += line + '\n';
                    }

                    content = str.TrimEnd('\n');
                    lines = content.Split('\n');
                }
            } catch {
                content = string.Empty;
                lines = default;
                Debug.LogError($"错误 {filePath} 配置读取错误，需要将外部 Excel 软件关闭，请检查!");

#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#endif
            }
        }
        
        #endregion
    }
    
    [Serializable]
    public class LanguageEntry {
        public string Key;
        public string English;
        public string Chinese;
        public string Japanese;
    }
}