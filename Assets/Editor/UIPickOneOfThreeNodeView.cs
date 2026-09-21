using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using TMPro;
using GraphProcessor;
using LazyPan;

/// <summary>
/// UI三选一节点编辑器扩展: 把面板预制体契约直接显示在 Graph Node 里
/// 左边配 参数 右边就能看到预制体缺什么 不用等到运行时看报错
/// </summary>
[NodeCustomEditor(typeof(BehaviourNode_UIPickOneOfThree))]
public class UIPickOneOfThreeNodeView : BaseNodeView {
    static readonly string[] requiredButtons = { "Card0", "Card1", "Card2" };
    static readonly string[] requiredTexts = {
        "Card0_Title", "Card0_Desc",
        "Card1_Title", "Card1_Desc",
        "Card2_Title", "Card2_Desc",
    };

    UnityEngine.UIElements.Label contractLabel;

    public override void Enable() {
        base.Enable();
        contractLabel = new UnityEngine.UIElements.Label();
        contractLabel.style.whiteSpace = WhiteSpace.Normal;
        var checkButton = new UnityEngine.UIElements.Button(RefreshContract) { text = "检查面板预制体契约" };
        controlsContainer.Add(contractLabel);
        controlsContainer.Add(checkButton);
        RefreshContract();
    }

    /// <summary>
    /// 按 PanelPrefabSign 找到预制体资产 核对 Comp 标签与子物体名 缺的标出来
    /// </summary>
    void RefreshContract() {
        var node = nodeTarget as BehaviourNode_UIPickOneOfThree;
        if (node == null) {
            contractLabel.text = "节点数据异常";
            return;
        }

        string prefabSign = node.Config != null ? node.Config.PanelPrefabSign : "";
        if (string.IsNullOrEmpty(prefabSign)) {
            contractLabel.text = "面板预制体标识为空\n先在 Config 里填 PanelPrefabSign\n如 UI/UI_PickOneOfThree";
            return;
        }

        GameObject prefab = FindPrefab(prefabSign);
        if (prefab == null) {
            contractLabel.text = $"找不到预制体:{prefabSign}\n请检查 Bundles/Prefabs 下有没有这个文件\n且已进 Addressable 分组";
            return;
        }

        var comp = prefab.GetComponent<Comp>();
        var buttonSigns = new HashSet<string>();
        var textSigns = new HashSet<string>();
        var childNames = new HashSet<string>();
        if (comp != null) {
            foreach (var b in comp.Buttons) {
                if (b != null && !string.IsNullOrEmpty(b.Sign))
                    buttonSigns.Add(b.Sign);
            }
            foreach (var t in comp.TextMeshProUGUIs) {
                if (t != null && !string.IsNullOrEmpty(t.Sign))
                    textSigns.Add(t.Sign);
            }
        }
        foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true)) {
            if (child != null && child.gameObject != prefab)
                childNames.Add(child.gameObject.name);
        }

        var lines = new List<string>();
        if (comp == null)
            lines.Add("✗ 根上没挂 Comp 脚本(只能靠子物体名兜底)");
        else
            lines.Add("✓ 根上有 Comp");

        int okCount = 0;
        foreach (string sign in requiredButtons) {
            bool ok = buttonSigns.Contains(sign) || childNames.Contains(sign);
            if (ok)
                okCount++;
            lines.Add((ok ? "✓ " : "✗ 缺") + sign + "(按钮)");
        }
        foreach (string sign in requiredTexts) {
            bool ok = textSigns.Contains(sign) || childNames.Contains(sign);
            if (ok)
                okCount++;
            lines.Add((ok ? "✓ " : "✗ 缺") + sign + "(文本)");
        }

        int total = requiredButtons.Length + requiredTexts.Length;
        string head = okCount == total ? $"面板契约全齐({total}/{total})" : $"面板缺 {total - okCount} 个组件 先补再运行";
        lines.Insert(0, head);
        contractLabel.text = string.Join("\n", lines);
    }

    /// <summary>
    /// 按标识尾段文件名全局搜预制体 不写死包与地址前缀 本地与拉取后都能命中
    /// </summary>
    static GameObject FindPrefab(string prefabSign) {
        string fileName = prefabSign;
        int slash = prefabSign.LastIndexOf('/');
        if (slash >= 0 && slash < prefabSign.Length - 1)
            fileName = prefabSign.Substring(slash + 1);
        string[] guids = AssetDatabase.FindAssets(fileName + " t:Prefab");
        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                continue;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                return prefab;
        }

        return null;
    }
}
