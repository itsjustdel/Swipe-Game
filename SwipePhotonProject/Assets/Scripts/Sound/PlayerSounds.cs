using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class PlayerSounds : MonoBehaviour
{
    public float cellBassFreq = 1f;
    public float cellFreqMod = 0.015f;
    public float cellRampSpeedOn = 5f;
    public float cellRampSpeedOff = 1.25f;
    public bool cellAtLimit = false;

    public bool startWalk = false;
    public float walkRampOff = 5f;
    public float walkTimer = 0f;
    public float footStartFreq=3f;    
    public float footStartVariance = 0.2f;
    public float footFreqRampOff = 1f;
    public float cellSizeMod = .01f;
    OverlayDrawer overlayDrawer;
    PlayerMovement playerMovement;
    
    AudioSource walkSource;
    NoiseMaker walkNoise;
    //ProceduralAudioController walkPAC;
    // Start is called before the first frame update

    private void Awake()
    {
    }
    void Start()
    {
        overlayDrawer = GameObject.FindGameObjectWithTag("Code").GetComponent<OverlayDrawer>();
        playerMovement = GetComponent<PlayerMovement>();


        SetMixers();

        //work ourt volumes on start, other wise we get a pop
        CellHeights();
        Walk();
    }

    void SetMixers()
    {
        walkSource = transform.Find("Sounds").transform.Find("Walk").GetComponent<AudioSource>();
        walkNoise = transform.Find("Sounds").transform.Find("Walk").GetComponent<NoiseMaker>();
        //walkPAC= transform.Find("Sounds").transform.Find("Walk").GetComponent<ProceduralAudioController>();
        //walkPAC.useSawAudioWave = true;

        //stil to set mixers in unity
        AudioMixer mixer = Resources.Load("Sound/Walks") as AudioMixer;
        
        walkSource.outputAudioMixerGroup = mixer.FindMatchingGroups("0")[0];
     



    }

    // Update is called once per frame
    void Update()
    {
        CellHeights();
        Walk();
    }

    void Walk()
    {
        //if (playerMovement.fracComplete >= 1f)//  && playerMovement.walking)
        if(startWalk)
        {
            GameObject currentCell = GetComponent<PlayerInfo>().currentCell;
            if (currentCell == null)
                return;

            float cellSize = currentCell.GetComponent<MeshRenderer>().bounds.size.magnitude;

            walkSource.pitch = footStartFreq + Random.value * footStartVariance - cellSize* cellSizeMod;//= 
            walkSource.volume = 1f;
            startWalk = false;
             
            ///walkPAC.mainFrequency = footStartFreq +(Random.value*10f);
        }
     
     
        
        walkSource.volume -= Time.deltaTime * walkRampOff;

        //walkSourceainFrequency -= Time.deltaTime * footFreqRampOff;  
        if (walkSource.volume < 0f)
            walkSource.volume = 0f;

    }

    void CellHeights()
    {
        GameObject currentCell = GetComponent<PlayerInfo>().currentCell;
        if (currentCell == null)
            return;

        NoiseMaker noiseMaker = currentCell.GetComponent<NoiseMaker>();
        AudioSource audioSource = currentCell.GetComponent<AudioSource>();
        AdjacentCells adjacentCells = currentCell.GetComponent<AdjacentCells>();

        float yScale = currentCell.transform.localScale.y;

        if (yScale == adjacentCells.targetY || yScale == overlayDrawer.minHeight)
        {
            //take noise away - we are at the top or the bottom of the cell's height targets
            if (audioSource.volume > 0f)
            {
                audioSource.volume -= cellRampSpeedOff * Time.deltaTime;
            }
            if (audioSource.volume < 0f)
                audioSource.volume = 0f;

            cellAtLimit = true;

            return;
        }
        else
            cellAtLimit = false;

        if (GetComponent<CellHeights>().loweringCell || GetComponent<CellHeights>().raisingCell)
        {
            if (audioSource.volume < 1f)
            {                 
                audioSource.volume += cellRampSpeedOn * Time.deltaTime;
            }

            audioSource.pitch = cellBassFreq + yScale * cellFreqMod;
        }
        else
        {            
            if (audioSource.volume > 0f)
            {                
                audioSource.volume -= cellRampSpeedOff * Time.deltaTime;
            }
            if (audioSource.volume < 0f)
                audioSource.volume = 0f;

            audioSource.pitch = cellBassFreq + yScale * cellFreqMod;
        }

        



    }
}

