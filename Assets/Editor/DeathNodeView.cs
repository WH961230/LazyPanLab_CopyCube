using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using GraphProcessor;
using LazyPan;

/// <summary>
/// 死亡节点便签视图: 便签直接平铺在节点最顶上显示全部内容，不折叠、不收起。
/// 文本来源 Behaviour_Event_Death 头上的 MemoDoc，改说明只改那一处即可。
/// </summary>
[NodeCustomEditor(typeof(BehaviourNode_Death))]
public class DeathNodeView : BaseNodeView {
    UnityEngine.UIElements.Label memoLabel;
    Toggle bloodToggle;

    public override void Enable() {
        base.Enable();

        //节点本身强制展开，不然折叠后便签跟着被藏起来
        if (nodeTarget != null) {
            nodeTarget.expanded = true;
        }
        expanded = true;

        //藏起节点自带的“参数便签”输入框，只留下面平铺的 Label，两份不打架、不压 Config
        var builtinMemo = controlsContainer.Q("参数便签");
        if (builtinMemo != null) {
            builtinMemo.style.display = DisplayStyle.None;
        }

        memoLabel = new UnityEngine.UIElements.Label();
        memoLabel.style.whiteSpace = WhiteSpace.Normal;
        memoLabel.style.marginTop = 4f;
        memoLabel.style.marginBottom = 4f;

        // 说明书包一层折叠，标题叫“行为说明书”，默认展开，想藏点一下就行
        var memoFoldout = new Foldout { text = "行为说明书", value = true };
        memoFoldout.Add(memoLabel);
        controlsContainer.Insert(0, memoFoldout);

        // 有血条开关：开=展开 Health/MaxHealth 填写，关=收起并自动填 0（无血条模式）
        bloodToggle = new Toggle("有血条");
        bloodToggle.RegisterValueChangedCallback(OnBloodToggle);
        controlsContainer.Add(bloodToggle);

        var refreshButton = new Button(RefreshMemo) { text = "刷新便签" };
        controlsContainer.Add(refreshButton);

        RefreshMemo();
        schedule.Execute(RefreshMemo).Every(2000);
    }

    void RefreshMemo() {
        if (memoLabel == null) {
            return;
        }

        string text = BehaviourPayloadDoc.Get(nameof(Behaviour_Event_Death));
        if (string.IsNullOrEmpty(text)) {
            text = "便签为空：先等脚本编译完，再点“刷新便签”。";
        }

        //内容没变就不碰，避免每秒重排把 Config 顶来顶去
        if (memoLabel.text != text) {
            memoLabel.text = text;
        }

        //顺手把节点身上的快照字段也刷成最新，存盘后下次打开直接就是新的
        var node = nodeTarget as BehaviourGraphNode;
        if (node != null && node.参数便签 != text) {
            node.参数便签 = text;
        }

        RefreshBloodRows();
    }

    void OnBloodToggle(ChangeEvent<bool> e) {
        var death = nodeTarget as BehaviourNode_Death;
        if (death == null) {
            return;
        }

        // 开=有血条模式，无正数血量时给个默认值，开关才立得住；关=无血条模式，血量填 0
        if (!e.newValue) {
            var c = death.Config;
            c.Health = 0f;
            c.MaxHealth = 0f;
            death.Config = c;
        } else if (death.Config.MaxHealth <= 0f) {
            var c = death.Config;
            c.MaxHealth = 100f;
            c.Health = 100f;
            death.Config = c;
        }

        RefreshBloodRows();
    }

    /// <summary>
    /// 有血条开关同步 Config 里 Health/MaxHealth 两行的显隐。
    /// 开关状态从 MaxHealth>0 反推，不另存字段，生成器重跑也不丢。
    /// </summary>
    void RefreshBloodRows() {
        var death = nodeTarget as BehaviourNode_Death;
        if (death == null) {
            return;
        }

        bool hasBar = death.Config.MaxHealth > 0f;
        if (bloodToggle != null && bloodToggle.value != hasBar) {
            bloodToggle.SetValueWithoutNotify(hasBar);
        }

        var configField = controlsContainer
            .Query<PropertyField>()
            .ToList()
            .FirstOrDefault(p =>
                p.name == "Config" ||
                (!string.IsNullOrEmpty(p.bindingPath) && p.bindingPath.EndsWith(".Config")));
        if (configField == null) {
            return;
        }

        foreach (var child in configField.Query<PropertyField>().ToList()) {
            string path = child.bindingPath ?? "";
            // 注意 MaxHealth 也以 Health 结尾，必须先判 MaxHealth
            bool isBloodRow = path.EndsWith(".MaxHealth") || path.EndsWith(".Health");
            if (isBloodRow) {
                child.style.display = hasBar ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
