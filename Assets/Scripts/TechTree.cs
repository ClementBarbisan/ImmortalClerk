using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "TechTree", order = 0)]
public class TechTree : ScriptableObject
{
    public JsonParser.Data Data;
}
