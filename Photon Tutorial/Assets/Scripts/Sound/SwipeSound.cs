using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwipeSound : MonoBehaviour {

    public ProceduralAudioController pAC;
    public Swipe swipe;
    [Range(1, 200)]
    public float freqMultiplier = 1;
    [Range(1, 200)]
    public float diffMultiplier = 3;
    [Range(1, 200)]
    public float volumeMultiplier = 6;
    
    [Range(100, 2000)]
    public float mainFrequencyBase = 500;

    public float lerpSpeed = 0.5f;

    private float prevDiff;
    private float prevRsPos;

    private float prevY;
    private void Awake()
    {
        enabled = false;
    }
    // Use this for initialization
    void Start () {
        pAC = GetComponent<ProceduralAudioController>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        //calculate how far the swipe point has moved
        float difference = Vector3.Distance(swipe.previousSwipePoint, swipe.swipePoint);

        float RsPos = swipe.pA.lookDirRightStick.magnitude;

        float y = swipe.swipePoint.normalized.y;

        //atm, frew goes down on thumbstick move
        //try and get freq to change on distance moved? rspos - prevRsPos

        //  float targetFreq = mainFrequencyBase = (y - prevY)*diffMultiplier;
        float rSPosDifference = Mathf.Abs(prevRsPos - RsPos);
        float targetFreq = mainFrequencyBase - (freqMultiplier * rSPosDifference);// + difference * diffMultiplier;
        pAC.mainFrequency = Mathf.Lerp((float)pAC.mainFrequency, targetFreq, lerpSpeed);

        float targetVolume = swipe.pA.lookDirRightStick.magnitude *volumeMultiplier;
        pAC.masterVolume = Mathf.Lerp((float)pAC.masterVolume, targetVolume, lerpSpeed);
    

        prevRsPos = RsPos;
        prevDiff = difference;
        prevY = y;
	}
}
