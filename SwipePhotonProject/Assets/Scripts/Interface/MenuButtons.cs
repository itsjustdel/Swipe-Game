using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuButtons : MonoBehaviour
{
    //object which starts connecting and building etc
    public GameObject launcher;
    public GameObject title;
    public GameObject start;
    public GameObject options;
    public GameObject exit;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayClick()
    {
        //Starting scripts
        launcher.SetActive(true);

        //turn off title and buttons
        title.SetActive(false);
        start.SetActive(false);
        options.SetActive(false);
        exit.SetActive(false);
    }
}
