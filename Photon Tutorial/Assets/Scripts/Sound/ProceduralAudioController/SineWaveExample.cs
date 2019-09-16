using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SineWaveExample : MonoBehaviour
{
    [Range(1, 20000)]  //Creates a slider in the inspector
    public float frequency1;

    [Range(1, 20000)]  //Creates a slider in the inspector
    public float frequency2;

    public float sampleRate = 44100;
    public float waveLengthInSeconds = 2.0f;

    public AudioSource audioSource;
    int timeIndex = 0;

    public Swipe swipe;
    public float multiplier = 1f;
    public float lerpSpeed = 0.1f;
    public float volume = 1f;
    
    public Vector3 prevRsPos;

    public float frequencyPlayed = 0;

    public float time;
    public float lastTime;

   // int buffLength = 0;
   // int buffSize = 0;
    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0; //force 2D sound
        
        
    }

    void Update()
    {





        audioSource.volume = volume;



    }

    void OnAudioFilterRead(float[] data, int channels)
    {

        float rSPosDifference = Mathf.Abs(prevRsPos.magnitude - swipe.pA.lookDirRightStick.magnitude);
        float targetFreq = frequency1 - (multiplier * rSPosDifference);// + difference * diffMultiplier;

        for (int i = 0; i < data.Length; i += channels)
        {
            data[i] = CreateSine(timeIndex, targetFreq, sampleRate);

            /// if (channels == 2)
            //    data[i + 1] = CreateSine(timeIndex, frequency2, sampleRate);

            timeIndex++;

            // time += sampleRate/buff;
            float aa = swipe.arcDetail;
            
            //if timeIndex gets too big, reset it to 0
            if (timeIndex >= (sampleRate * waveLengthInSeconds))
            {
                timeIndex = 0;
            }
        }

        prevRsPos = swipe.pA.lookDirRightStick;
    }

    //Creates a sinewave
    public float CreateSine(float timeIndex, float frequency, float sampleRate)
    {
        return Mathf.Sin(2 * Mathf.PI * (timeIndex * frequency / sampleRate));
    }
}

