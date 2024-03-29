﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guide : MonoBehaviour {

    public float guideSize = 4.5f;
    public bool useParticleSystem = false;
    //  public float trailTime = 5f;

    public PlayerAttacks playerAttacks;
    public Swipe swipe;
    public Inputs inputs;

    float previousMagnitude = 0f;
    float swipeMagnitude = 0f;

    public float scaleSizeOnStationary = 8f;

    public static GameObject GenerateGuide(Swipe swipe)
    {
        
        
       // float length = 11f;

        //pivot
       // GameObject swordPivot = new GameObject();
       // swordPivot.name = "Sword Pivot";
      //  swordPivot.transform.parent = swipe.transform.transform;
      //  swordPivot.transform.localPosition = Vector3.up * swipe.playerClassValues.armLength;
        //meat //using trail renderer // use a prefab, can't be bothered figuring how to programitcally adjust curve values-character customisation would be a reason to change this
        GameObject swordPrefab =Instantiate( Resources.Load("SwordPrefab")) as GameObject;
        swordPrefab.transform.parent = swipe.transform;
        swordPrefab.transform.localPosition = Vector3.zero;
        /*
        GameObject swordMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        swordMesh.transform.localScale = new Vector3(1f, 1f, length);
        swordMesh.transform.parent = swordPivot.transform;
        swordMesh.transform.localPosition = Vector3.zero;
        swordPivot.transform.LookAt(swipe.head.transform);
        swordMesh.transform.position = swordPivot.transform.position + Vector3.up* length * 0.5f;
        */

        //w.i.p
        //attaching this script to player. Means we can customise from here rather than calling on geneeric statics all the time
        Guide guide = swordPrefab.AddComponent<Guide>();
        guide.swipe = swipe;
        guide.inputs = swipe.inputs;

        
        return swordPrefab;
    }
    

    public void Update()
    {
        previousMagnitude = swipeMagnitude;

        if (inputs == null)
            inputs = GetComponent<Inputs>();

        //animate sword guide
        //disable if within deadzone/not moving
        
        swipeMagnitude = swipe.pA.lookDirRightStick.magnitude;
        
        transform.GetComponent<TrailRenderer>().widthMultiplier = swipeMagnitude* guideSize;

        //if planning phase, make guide visible , nicely
        if (swipe.planningPhaseOverheadSwipe)
            swipeMagnitude += previousMagnitude + (scaleSizeOnStationary*Time.deltaTime);

        //clamp --elastic easing?
        if (swipeMagnitude >= 1)
            swipeMagnitude = 1f;

        transform.localScale = Vector3.one * swipeMagnitude * guideSize;
     
        

        if(useParticleSystem)//enable child in hierarchy
        {
           //can also set scaling mode to "hierarchy" 
            ParticleSystem.MainModule main =transform.GetChild(0).GetComponent<ParticleSystem>().main;
            main.startSize = swipeMagnitude * guideSize;
        }

        transform.position = swipe.head.transform.position + swipe.swipePoint.normalized*(swipe.playerClassValues.armLength + swipe.playerClassValues.swordLength);
        

        //ChangeColourOnAngle(); //old

        if (inputs.blocking0 || swipe.GetComponent<CellHeights>().loweringCell || swipe.GetComponent<CellHeights>().raisingCell )
        {
            GetComponent<MeshRenderer>().enabled = false;
            GetComponent<TrailRenderer>().enabled = false;
        }
        else
        {
            GetComponent<MeshRenderer>().enabled = true;
            GetComponent<TrailRenderer>().enabled = true;
        }
        //colour is different on pull back or planning, disabled on swipe
        ChangeColourOnState();
    }

    void ChangeColourOnAngle()
    {
        if (inputs.state.Buttons.RightShoulder == XInputDotNetPure.ButtonState.Pressed)
        {
          
              GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("FlatMaterials/BlueFlat1") as Material;
        }
        else if(swipe.overheadSwiping)
        {
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("FlatMaterials/OrangeFlat") as Material;
        }
        else if (swipe.waitingOnResetOverhead)// || swipe.waitingOnResetLunge)
        {
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("FlatMaterials/PinkFlat") as Material;
        }
        else if (swipe.pA.lookDirRightStick.magnitude > 0.95f)
        {
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("FlatMaterials/YellowFlat") as Material;
        }
        else
        {
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("FlatMaterials/PinkFlat") as Material;
        }

    }

    void ChangeColourOnState()
    {
        if(inputs.attack0)//?
        {
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("FlatMaterials/YellowFlat") as Material;
        }
        else if (swipe.planningPhaseOverheadSwipe)
        {
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("FlatMaterials/OrangeFlat") as Material;
        }
        else if(swipe.overheadSwiping)
        {
           GetComponent<MeshRenderer>().enabled = false;
           GetComponent<TrailRenderer>().enabled = false;

        }
        else
        {
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("FlatMaterials/PinkFlat") as Material;
        }
    }
}
