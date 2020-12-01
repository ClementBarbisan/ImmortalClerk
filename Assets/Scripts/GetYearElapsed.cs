using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GetYearElapsed : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        int years = PlayerPrefs.GetInt("year");
        GetComponent<TextMeshProUGUI>().text = "Congratulations ! You made it to the moon in " + years.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
