using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Word : MonoBehaviour
{
    public enum wordType
    {
        subject,
        verb,
        obj
    }

    public wordType type;
    public JsonParser.Technos technos;
    public JsonParser.Word word;
}
