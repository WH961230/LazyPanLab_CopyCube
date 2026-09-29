using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LazyPan {
    public class BehaviourDocWindow : EditorWindow {
        private string _sign = string.Empty;
        private string _displayName = string.Empty;
        private string _docPath = string.Empty;
        private string _docText = string.Empty;
        private string _videoPath = string.Empty;
        private string _videoUrl = string.Empty;
        private string _coverPath = string.Empty;
        private Texture2D _cover;
        private Vector2 _scroll;

        public static void Show(string sign, string displayName) {
            BehaviourDocWindow window = GetWindow<BehaviourDocWindow>(true, string.IsNullOrEmpty(sign) ? "行为说明" : sign + " 说明", true);
            window.minSize = new Vector2(520, 480);
            window.Load(sign, displayName);
            window.Show();
        }

        private void Load(string sign, string displayName) {
            _sign = sign != null ? sign.Trim() : string.Empty;
            _displayName = displayName != null ? displayName.Trim() : string.Empty;
            _docPath = string.Empty;
            _docText = string.Empty;
            _videoPath = string.Empty;
            _videoUrl = string.Empty;
            _coverPath = string.Empty;
            _cover = null;
            _scroll = Vector2.zero;
            if (string.IsNullOrEmpty(_sign)) {
                return;
            }
            ResolveDocFiles();
        }

        private void ResolveDocFiles() {
            string[] scriptGuids = AssetDatabase.FindAssets(_sign + " t:script");
            string scriptDir = string.Empty;
            foreach (string guid in scriptGuids) {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(assetPath) == _sign) {
                    scriptDir = Path.GetDirectoryName(assetPath);
                    break;
                }
            }
            if (string.IsNullOrEmpty(scriptDir) && scriptGuids.Length > 0) {
                scriptDir = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(scriptGuids[0]));
            }

            string docDir = string.Empty;
            if (!string.IsNullOrEmpty(scriptDir)) {
                string siblingDoc = Path.Combine(scriptDir, "Doc").Replace("\\", "/");
                if (AssetDatabase.IsValidFolder(siblingDoc)) {
                    docDir = siblingDoc;
                }
            }
            if (string.IsNullOrEmpty(docDir)) {
                string[] docFolders = AssetDatabase.FindAssets("Doc t:folder");
                foreach (string guid in docFolders) {
                    string folder = AssetDatabase.GUIDToAssetPath(guid);
                    if (folder.Contains("/Download/Behaviour/")) {
                        string mdInFolder = Path.Combine(folder, _sign + ".md").Replace("\\", "/");
                        if (File.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "..", mdInFolder)))) {
                            docDir = folder;
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(docDir)) {
                return;
            }

            string mdAsset = Path.Combine(docDir, _sign + ".md").Replace("\\", "/");
            string mdFull = Path.GetFullPath(Path.Combine(Application.dataPath, "..", mdAsset));
            if (File.Exists(mdFull)) {
                _docPath = mdAsset;
                _docText = File.ReadAllText(mdFull, System.Text.Encoding.UTF8);
            }

            string[] videoExts = new[] { ".mp4", ".mov", ".webm" };
            foreach (string ext in videoExts) {
                string videoAsset = Path.Combine(docDir, _sign + ext).Replace("\\", "/");
                if (File.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "..", videoAsset)))) {
                    _videoPath = videoAsset;
                    break;
                }
            }

            string urlAsset = Path.Combine(docDir, _sign + ".url.txt").Replace("\\", "/");
            string urlFull = Path.GetFullPath(Path.Combine(Application.dataPath, "..", urlAsset));
            if (File.Exists(urlFull)) {
                _videoUrl = File.ReadAllText(urlFull, System.Text.Encoding.UTF8).Trim();
            }

            string coverAsset = Path.Combine(docDir, _sign + ".png").Replace("\\", "/");
            if (File.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "..", coverAsset)))) {
                _coverPath = coverAsset;
                _cover = AssetDatabase.LoadAssetAtPath<Texture2D>(coverAsset);
            }
        }

        private void OnGUI() {
            GUILayout.Space(8);
            EditorGUILayout.LabelField(string.IsNullOrEmpty(_displayName) ? _sign : _displayName + "  (" + _sign + ")", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(string.IsNullOrEmpty(_docPath) ? "Doc/ 目录约定：<行为Sign>.md 文字，<行为Sign>.mp4 视频，<行为Sign>.url.txt 在线视频链接" : _docPath, EditorStyles.miniLabel);
            GUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (!string.IsNullOrEmpty(_docText)) {
                EditorGUILayout.LabelField("文字说明", EditorStyles.boldLabel);
                GUIStyle docStyle = new GUIStyle(EditorStyles.textArea);
                docStyle.wordWrap = true;
                float height = Mathf.Clamp(GUI.skin.textArea.CalcHeight(new GUIContent(_docText), position.width - 30f), 120f, 1200f);
                EditorGUILayout.SelectableLabel(_docText, docStyle, GUILayout.Height(height));
                GUILayout.Space(8);
            } else {
                EditorGUILayout.HelpBox("暂无文字说明。在行为脚本同级 Doc/ 下新建 " + _sign + ".md 即可显示。", MessageType.Info);
                GUILayout.Space(8);
            }

            if (_cover != null) {
                EditorGUILayout.LabelField("封面 / 插图", EditorStyles.boldLabel);
                float w = Mathf.Min(position.width - 30f, 480f);
                float h = w * _cover.height / Mathf.Max(1, _cover.width);
                GUILayout.Label(_cover, GUILayout.Width(w), GUILayout.Height(h));
                GUILayout.Space(8);
            }

            EditorGUILayout.LabelField("视频说明", EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(_videoPath)) {
                EditorGUILayout.LabelField(_videoPath, EditorStyles.miniLabel);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("选中视频资源", GUILayout.Height(28))) {
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(_videoPath);
                    EditorGUIUtility.PingObject(Selection.activeObject);
                }
                if (GUILayout.Button("用系统播放器打开", GUILayout.Height(28))) {
                    EditorUtility.RevealInFinder(Path.GetFullPath(Path.Combine(Application.dataPath, "..", _videoPath)));
                }
                GUILayout.EndHorizontal();
            } else if (!string.IsNullOrEmpty(_videoUrl)) {
                EditorGUILayout.LabelField(_videoUrl, EditorStyles.miniLabel);
                if (GUILayout.Button("浏览器打开在线视频", GUILayout.Height(28))) {
                    Application.OpenURL(_videoUrl);
                }
            } else {
                EditorGUILayout.HelpBox("暂无视频。在 Doc/ 下放 " + _sign + ".mp4（或 .mov/.webm），或写 " + _sign + ".url.txt 放 B站/在线链接。", MessageType.Info);
            }

            GUILayout.Space(8);
            if (!string.IsNullOrEmpty(_docPath)) {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("定位说明文件", GUILayout.Height(26))) {
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(_docPath);
                    EditorGUIUtility.PingObject(Selection.activeObject);
                }
                if (GUILayout.Button("打开 Doc 文件夹", GUILayout.Height(26))) {
                    string dir = Path.GetDirectoryName(Path.GetFullPath(Path.Combine(Application.dataPath, "..", _docPath)));
                    EditorUtility.RevealInFinder(dir);
                }
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
