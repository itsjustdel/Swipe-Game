/*	Author: Kostas Sfikas
	Date: April 2017
	Language: c#
	Platform: Unity 5.5.0 f3 (personal edition) */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using System;


public class ProceduralAudioController : MonoBehaviour
{
    /* This class is the main audio engine, 
	- It uses the OnAudioFilterRead() function to create sound by applying mathematical functions
	on each separate audio sample.
	- It uses the SawWave, SinusWave and SquareWave classes to produce the audio waves, 
	as well as the Frequency and Amplitude Modulations. 
	- This class (as well as the related classes) has not been optimized for performance. Therefore 
	it is not recommended to use multiple instance of this class, because you may have performance issues.*/

    SawWave sawAudioWave;
    SquareWave squareAudioWave;
    SinusWave sinusAudioWave;

    SinusWave amplitudeModulationOscillator;
    SinusWave frequencyModulationOscillator;

    public bool autoPlay;

    [Header("Volume / Frequency")]
    [Range(0.0f, 1.0f)]
    public float masterVolume = 0.5f;
    [Range(100, 2000)]
    public double mainFrequency = 500;

    [Space(10)]

    [Header("Tone Adjustment")]
    public bool useSinusAudioWave;
    [Range(0.0f, 1.0f)]
    public float sinusAudioWaveIntensity = 0.25f;
    [Space(5)]
    public bool useSquareAudioWave;
    [Range(0.0f, 1.0f)]
    public float squareAudioWaveIntensity = 0.25f;
    [Space(5)]
    public bool useSawAudioWave;
    [Range(0.0f, 1.0f)]
    public float sawAudioWaveIntensity = 0.25f;

    [Space(10)]

    [Header("Amplitude Modulation")]
    public bool useAmplitudeModulation;
    [Range(0.2f, 30.0f)]
    public float amplitudeModulationOscillatorFrequency = 1.0f;
    [Header("Frequency Modulation")]
    public bool useFrequencyModulation;
    [Range(0.2f, 30.0f)]
    public float frequencyModulationOscillatorFrequency = 1.0f;
    [Range(1.0f, 100.0f)]
    public float frequencyModulationOscillatorIntensity = 10.0f;

    [Header("Out Values")]
    [Range(0.0f, 1.0f)]
    public float amplitudeModulationRangeOut;
    [Range(0.0f, 1.0f)]
    public float frequencyModulationRangeOut;


    float mainFrequencyPreviousValue;
    private System.Random RandomNumber = new System.Random();

    private double sampleRate;  // samples per second
                                //private double myDspTime;	// dsp time
    private double dataLen;     // the data length of each channel
    double chunkTime;
    double dspTimeStep;
    double currentDspTime;

    public ProceduralAudioController pAC;
    public Swipe swipe;
    [Range(-2000, 2000)]
    public float freqMultiplier = -50;
    [Range(-2000, 2000)]
    public float diffMultiplier = -10;
    [Range(1, 200)]
    public float volumeMultiplier = 80;
    public float volumeMin = -80f;

    [Range(100, 2000)]
    public float mainFrequencyBase = 400;

    public float mainFrequencyCeiling = 500;

    public float lerpSpeed = 0.5f;
    public float guideRampUp = 150f;
    public float guideRampDown = 10f;

    private float prevDiff;
    private float prevRsPos;

    private float prevY;

   public float targetVolume;

    public AudioMixer swipeMixer;

    public bool running = true;


    string guideVolumeString;

    public bool guide;
    public bool swipeObject;
    public bool walk;
    //sent from swipe script as it is being created, tells us how large the swipe is
    public float swipeObjectDistance;
    void Start()
    {

        

        sawAudioWave = new SawWave();
        squareAudioWave = new SquareWave();
        sinusAudioWave = new SinusWave();

        amplitudeModulationOscillator = new SinusWave();
        frequencyModulationOscillator = new SinusWave();

        sampleRate = AudioSettings.outputSampleRate;
        swipeMixer = Resources.Load("Sound/SwipeMixer") as AudioMixer;
      

        if (guide)
        {
            swipe = GetComponent<Swipe>();

            //setting output to mixer
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = swipeMixer.FindMatchingGroups("Guides/0")[0];
            if (GetComponent<PlayerInfo>().playerNumber == 0)
                guideVolumeString = "GuideVolume0";
            else if (GetComponent<PlayerInfo>().playerNumber == 1)
                guideVolumeString = "GuideVolume1";
            else if (GetComponent<PlayerInfo>().playerNumber == 2)
                guideVolumeString = "GuideVolume2";
            else if (GetComponent<PlayerInfo>().playerNumber == 3)
                guideVolumeString = "GuideVolume3";

            mainFrequency = 500;
        }
        else if(swipeObject)
        {
          //  AudioSource audioSource = gameObject.AddComponent<AudioSource>();
         //   audioSource.outputAudioMixerGroup = swipeMixer.FindMatchingGroups("SwipeObjects")[0];
            
        }
        else if(walk)
        {
            /*
            swipeMixer = Resources.Load("Sound/WalkMix") as AudioMixer;

            audioSource.outputAudioMixerGroup = swipeMixer.FindMatchingGroups("0")[0];
           // if (GetComponent<PlayerInfo>().playerNumber == 0)
                guideVolumeString = "0";
                */
            
        }

    }


    void Update()
    {
        

        if (autoPlay)
        {
            if (!useSinusAudioWave)
            {
                useSinusAudioWave = true;
            }
            if (!useSquareAudioWave)
            {
                useSquareAudioWave = true;
            }
            if (!useSawAudioWave)
            {
                useSawAudioWave = true;
            }
            if (!useAmplitudeModulation)
            {
                useAmplitudeModulation = true;
            }
            if (!useFrequencyModulation)
            {
                useFrequencyModulation = true;
            }

            mainFrequency =  Mathf.PingPong(Time.time * 200.0f, 1900.0f) + 100.0f;
            
            sinusAudioWaveIntensity = Mathf.PingPong(Time.time * 0.5f, 1.0f);
            squareAudioWaveIntensity = Mathf.PingPong(Time.time * 0.6f, 1.0f);
            sawAudioWaveIntensity = Mathf.PingPong(Time.time * 0.7f, 1.0f);
            amplitudeModulationOscillatorFrequency = targetVolume;// Mathf.PingPong(Time.time * 3.0f, 30.0f);
            frequencyModulationOscillatorFrequency = (float)mainFrequency - 2000;// Mathf.PingPong(Time.time * 4.0f, 30.0f);
            frequencyModulationOscillatorIntensity = Mathf.PingPong(Time.time * 10.0f, 100.0f);
            
        }
       
        if(guide)
        {

            GuideValuesGlass();
        }
        else if (swipeObject)
        {
            SwipeObjectValues();
        }
        
    }

    void SwipeObjectValues()
    {
        //baseFrequency =baseFrequency/2 + ((baseFrequency / 8) * 12);
        //proceduralAudioControllerForNewObject.mainFrequency = baseFrequency;
        float start = mainFrequencyBase*.5f * 1.6f;
        //float targetFreq = (float)((mainFrequencyBase / 8) * 12);
        float targetFreq = mainFrequencyBase*.5f;
        //how much to speed up audio ahead of swipe generation
        float accelerator = .5f;
        mainFrequency =  Mathf.Lerp( targetFreq,start,swipeObjectDistance*accelerator);

        
    }

    void GuideValuesDistorted()
    {
        //calculate how far the swipe point has moved
        float difference = Vector3.Distance(swipe.previousSwipePoint, swipe.swipePoint);

        float RsPos = swipe.pA.lookDirRightStick.magnitude;

        float y = swipe.swipePoint.normalized.y;

        //atm, frew goes down on thumbstick move
        //try and get freq to change on distance moved? rspos - prevRsPos

        //  float targetFreq = mainFrequencyBase = (y - prevY)*diffMultiplier;
        float rSPosDifference = (prevRsPos - RsPos);
        //float targetFreq = mainFrequencyBase - (freqMultiplier * rSPosDifference);// + difference * diffMultiplier;
        float targetFreq = mainFrequencyBase;
        targetFreq += -(freqMultiplier * y);// + difference * diffMultiplier;
        targetFreq += difference * diffMultiplier;

     
         mainFrequency = Mathf.Lerp((float)mainFrequency, targetFreq, lerpSpeed);
        

        targetVolume = -80f + swipe.pA.lookDirRightStick.magnitude * volumeMultiplier;
        targetVolume = Mathf.Clamp(targetVolume, volumeMin, targetVolume);


        swipeMixer.SetFloat(guideVolumeString, targetVolume);
        //pAC.masterVolume = Mathf.Lerp((float)pAC.masterVolume, targetVolume, lerpSpeed);


        prevRsPos = RsPos;
        prevDiff = difference;
        prevY = y;
    }

    void GuideValuesGlass()
    {
        //calculate how far the swipe point has moved
        float difference = Vector3.Distance(swipe.previousSwipePoint, swipe.swipePoint);

        float RsPos = swipe.pA.lookDirRightStick.magnitude;

        float y = swipe.swipePoint.y;

        //atm, frew goes down on thumbstick move
        //try and get freq to change on distance moved? rspos - prevRsPos

        //  float targetFreq = mainFrequencyBase = (y - prevY)*diffMultiplier;
       // float rSPosDifference = (prevRsPos - RsPos);
        //float targetFreq = mainFrequencyBase - (freqMultiplier * rSPosDifference);// + difference * diffMultiplier;
        float targetFreq = mainFrequencyBase;
        targetFreq += -(freqMultiplier * RsPos);// + difference * diffMultiplier;
        targetFreq += difference * diffMultiplier;

        mainFrequency = Mathf.Lerp((float)mainFrequency, targetFreq, lerpSpeed);

        float prevVol = targetVolume;
        float still = 0f;
       // if (swipe.rightStickStill)
       //     still += guideRampUp;

        targetVolume = -80f + swipe.pA.lookDirRightStick.magnitude * volumeMultiplier - still;
        //targetVolume = -80 + difference * volumeMultiplier;


        targetVolume = Mathf.Clamp(targetVolume, volumeMin, 0f);
        targetVolume = Mathf.Lerp(prevVol, targetVolume, lerpSpeed);


        swipeMixer.SetFloat(guideVolumeString, targetVolume);
        //pAC.masterVolume = Mathf.Lerp((float)pAC.masterVolume, targetVolume, lerpSpeed);


        prevRsPos = RsPos;
        prevDiff = difference;
        prevY = y;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {


        if (!running)
            return;
        /* This is called by the system
		suppose: sampleRate = 48000
		suppose: data.Length = 2048
		suppose: channels = 2
		then:
		dataLen = 2048/2 = 1024
		chunkTime = 1024 / 48000 = 0.0213333... so the chunk time is around 21.3 milliseconds.
		dspTimeStep = 0.0213333 / 1024 = 2.083333.. * 10^(-5) = 0.00002083333..sec = 0.02083 milliseconds
			keep note that 1 / dspTimeStep = 48000 ok!		
		*/

        currentDspTime = AudioSettings.dspTime;
        dataLen = data.Length / channels;   // the actual data length for each channel
        chunkTime = dataLen / sampleRate;   // the time that each chunk of data lasts
        dspTimeStep = chunkTime / dataLen;  // the time of each dsp step. (the time that each individual audio sample (actually a float value) lasts)

        double preciseDspTime;
        for (int i = 0; i < dataLen; i++)
        { // go through data chunk
            preciseDspTime = currentDspTime + i * dspTimeStep;
            double signalValue = 0.0;
            double currentFreq = mainFrequency;
            if (useFrequencyModulation)
            {
                double freqOffset = (frequencyModulationOscillatorIntensity * mainFrequency * 0.75) / 100.0;
                currentFreq += mapValueD(frequencyModulationOscillator.calculateSignalValue(preciseDspTime, frequencyModulationOscillatorFrequency), -1.0, 1.0, -freqOffset, freqOffset);
                frequencyModulationRangeOut = (float)frequencyModulationOscillator.calculateSignalValue(preciseDspTime, frequencyModulationOscillatorFrequency) * 0.5f + 0.5f;
            }
            else
            {
                frequencyModulationRangeOut = 0.0f;
            }

            if (useSinusAudioWave)
            {
                signalValue += sinusAudioWaveIntensity * sinusAudioWave.calculateSignalValue(preciseDspTime, currentFreq);
            }
            if (useSawAudioWave)
            {
                signalValue += sawAudioWaveIntensity * sawAudioWave.calculateSignalValue(preciseDspTime, currentFreq);
            }
            if (useSquareAudioWave)
            {
                signalValue += squareAudioWaveIntensity * squareAudioWave.calculateSignalValue(preciseDspTime, currentFreq);
            }

            if (useAmplitudeModulation)
            {
                signalValue *= mapValueD(amplitudeModulationOscillator.calculateSignalValue(preciseDspTime, amplitudeModulationOscillatorFrequency), -1.0, 1.0, 0.0, 1.0);
                amplitudeModulationRangeOut = (float)amplitudeModulationOscillator.calculateSignalValue(preciseDspTime, amplitudeModulationOscillatorFrequency) * 0.5f + 0.5f;

                
            }
            else
            {
                //amplitudeModulationRangeOut = 0.0f;
            }

            float x = masterVolume * 0.5f * (float)signalValue;

            for (int j = 0; j < channels; j++)
            {
                data[i * channels + j] = x;
            }
        }

    }

    float mapValue(float referenceValue, float fromMin, float fromMax, float toMin, float toMax)
    {
        /* This function maps (converts) a Float value from one range to another */
        return toMin + (referenceValue - fromMin) * (toMax - toMin) / (fromMax - fromMin);
    }

    double mapValueD(double referenceValue, double fromMin, double fromMax, double toMin, double toMax)
    {
        /* This function maps (converts) a Double value from one range to another */
        return toMin + (referenceValue - fromMin) * (toMax - toMin) / (fromMax - fromMin);
    }
}