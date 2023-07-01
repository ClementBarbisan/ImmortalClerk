using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(VectorLabelsAttribute))]
public class VectorLabelsAttributeDrawer : PropertyDrawer
{
    private static readonly GUIContent[] defaultLabels = new GUIContent[] { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z"), new GUIContent("W") };

    int factor = 1;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        factor = 4;
        if (property.propertyType == SerializedPropertyType.Vector2Int || property.propertyType == SerializedPropertyType.Vector2)
        {
            factor = 3;
        }
        else if (property.propertyType == SerializedPropertyType.Vector4)
        {
            factor = 5;
        }
        return (factor * base.GetPropertyHeight(property, label));
    }

    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        VectorLabelsAttribute vectorLabels = (VectorLabelsAttribute)attribute;

        if (property.propertyType == SerializedPropertyType.Vector2Int)
        {
            int[] array = new int[] { property.vector2IntValue.x, property.vector2IntValue.y };
            array = DrawFields(position, array, ObjectNames.NicifyVariableName(property.name), EditorGUI.IntField, vectorLabels);
            property.vector2IntValue = new Vector2Int(array[0], array[1]);
        }
        else if (property.propertyType == SerializedPropertyType.Vector3Int)
        {
            int[] array = new int[] { property.vector3IntValue.x, property.vector3IntValue.y, property.vector3IntValue.z };
            array = DrawFields(position, array, ObjectNames.NicifyVariableName(property.name), EditorGUI.IntField, vectorLabels);
            property.vector3IntValue = new Vector3Int(array[0], array[1], array[2]);
        }
        else if (property.propertyType == SerializedPropertyType.Vector2)
        {
            float[] array = new float[] { property.vector2Value.x, property.vector2Value.y };
            array = DrawFields(position, array, ObjectNames.NicifyVariableName(property.name), EditorGUI.FloatField, vectorLabels);
            property.vector2Value = new Vector2(array[0], array[1]);
        }
        else if (property.propertyType == SerializedPropertyType.Vector3)
        {
            float[] array = new float[] { property.vector3Value.x, property.vector3Value.y, property.vector3Value.z };
            array = DrawFields(position, array, ObjectNames.NicifyVariableName(property.name), EditorGUI.FloatField, vectorLabels);
            property.vector3Value = new Vector3(array[0], array[1], array[2]);
        }
        else if (property.propertyType == SerializedPropertyType.Vector4)
        {
            float[] array = new float[] { property.vector4Value.x, property.vector4Value.y, property.vector4Value.z, property.vector4Value.w };
            array = DrawFields(position, array, ObjectNames.NicifyVariableName(property.name), EditorGUI.FloatField, vectorLabels);
            property.vector4Value = new Vector4(array[0], array[1], array[2], array[3]);
        }
    }

    private T[] DrawFields<T>(Rect rect, T[] vector, string mainLabel, System.Func<Rect, GUIContent, T, T> fieldDrawer, VectorLabelsAttribute vectorLabels)
    {
        T[] result = vector;

        // Get the rect of the main label
        Rect mainLabelRect = rect;
        mainLabelRect.width = EditorGUIUtility.labelWidth;
        mainLabelRect.height /= factor;

        // Get the size of each field rect
        Rect fieldRect = rect;
        fieldRect.height /= factor;
        fieldRect.width = rect.width;
        EditorGUI.LabelField(mainLabelRect, mainLabel);

        for (int i = 0; i < vector.Length; i++)
        {
            fieldRect.y += fieldRect.height;
            GUIContent label = vectorLabels.Labels.Length > i ? new GUIContent(vectorLabels.Labels[i]) : defaultLabels[i];
            Vector2 labelSize = EditorStyles.label.CalcSize(label);
            EditorGUIUtility.labelWidth = fieldRect.width / 2f;
            result[i] = fieldDrawer(fieldRect, label, vector[i]);
        }
        fieldRect.y += fieldRect.height;
        EditorGUIUtility.labelWidth = 0;

        return result;
    }
}