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
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
