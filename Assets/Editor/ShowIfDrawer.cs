using UnityEditor;
using UnityEngine;

/// <summary>
/// ShowIf 字段绘制器 条件不满足直接藏起整行 高度归零
/// 条件字段只找同对象上的兄弟字段 支持 enum/int/bool/string/float 五种比对
/// </summary>
[CustomPropertyDrawer(typeof(LazyPan.ShowIfAttribute))]
public class ShowIfDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        var showIf = (LazyPan.ShowIfAttribute) attribute;
        if (!IsSatisfied(property, showIf)) {
            return;
        }

        //标题由 ShowIf 自己画 Unity 自带的 Header 藏字段时藏不住 必须接管
        if (!string.IsNullOrEmpty(showIf.Header)) {
            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(headerRect, showIf.Header, EditorStyles.boldLabel);
            Rect fieldRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2f,
                position.width, position.height - EditorGUIUtility.singleLineHeight - 2f);
            EditorGUI.PropertyField(fieldRect, property, label, true);
            return;
        }

        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        var showIf = (LazyPan.ShowIfAttribute) attribute;
        if (!IsSatisfied(property, showIf)) {
            return 0f;
        }

        float height = EditorGUI.GetPropertyHeight(property, label, true);
        if (!string.IsNullOrEmpty(showIf.Header)) {
            height += EditorGUIUtility.singleLineHeight + 2f;
        }

        return height;
    }

    static bool IsSatisfied(SerializedProperty property, LazyPan.ShowIfAttribute attr) {
        if (attr == null || string.IsNullOrEmpty(attr.ConditionField)) {
            return true;
        }

        SerializedProperty condition = FindSibling(property, attr.ConditionField);
        if (condition == null) {
            return true;
        }

        object expect = attr.ExpectValue;
        switch (condition.propertyType) {
            case SerializedPropertyType.Enum:
                //枚举按名字比 不按序号 枚举加项换序不影响
                string[] names = condition.enumNames;
                int index = condition.enumValueIndex;
                string current = index >= 0 && index < names.Length ? names[index] : "";
                return current == (expect != null ? expect.ToString() : "");
            case SerializedPropertyType.Integer:
                return condition.intValue == System.Convert.ToInt32(expect);
            case SerializedPropertyType.Boolean:
                return condition.boolValue == System.Convert.ToBoolean(expect);
            case SerializedPropertyType.String:
                return condition.stringValue == (expect != null ? expect.ToString() : "");
            case SerializedPropertyType.Float:
                return Mathf.Approximately(condition.floatValue, System.Convert.ToSingle(expect));
            default:
                return true;
        }
    }

    /// <summary>
    /// 同对象兄弟字段 兼容嵌套在 List 里的元素(list.Array.data[i].xxx 这种路径)
    /// </summary>
    static SerializedProperty FindSibling(SerializedProperty property, string fieldName) {
        string path = property.propertyPath;
        int dot = path.LastIndexOf('.');
        string siblingPath = dot >= 0 ? path.Substring(0, dot + 1) + fieldName : fieldName;
        return property.serializedObject.FindProperty(siblingPath);
    }
}
