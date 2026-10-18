using UnityEditor;
using UnityEngine;


[CustomPropertyDrawer(typeof(ExpandableAttribute), true)]
public class ExpandableDrawer : PropertyDrawer
{
    private const float Spacing = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (!IsOpen(property)) return height;

        var inner = new SerializedObject(property.objectReferenceValue);
        var it = inner.GetIterator();
        it.NextVisible(true);
        while (it.NextVisible(false))
            height += EditorGUI.GetPropertyHeight(it, true) + Spacing;
        return height + Spacing;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        if (property.objectReferenceValue != null)
        {
            var arrow = new Rect(line.x, line.y, EditorGUIUtility.labelWidth, line.height);
            property.isExpanded = EditorGUI.Foldout(arrow, property.isExpanded, GUIContent.none, true);
        }
        EditorGUI.PropertyField(line, property, label);

        if (!IsOpen(property)) return;

        var inner = new SerializedObject(property.objectReferenceValue);
        inner.Update();
        EditorGUI.indentLevel++;

        float y = line.yMax + Spacing;
        var it = inner.GetIterator();
        it.NextVisible(true); 
        while (it.NextVisible(false))
        {
            float h = EditorGUI.GetPropertyHeight(it, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), it, true);
            y += h + Spacing;
        }

        EditorGUI.indentLevel--;
        inner.ApplyModifiedProperties(); 
    }

    private static bool IsOpen(SerializedProperty property) =>
        property.isExpanded && property.objectReferenceValue != null;
}
