using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuideSound : MonoBehaviour
{
    NoiseMaker noiseMaker;
    Swipe swipe;
    AudioSource audioSource;
    // Start is called before the first frame update
    void Start()
    {
        noiseMaker = GetComponent<NoiseMaker>();
        swipe = transform.parent.GetComponent<Swipe>();
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        audioSource.volume = swipe.pA.lookDirRightStick.magnitude;
    }
}
