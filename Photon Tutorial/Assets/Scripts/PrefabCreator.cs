﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabCreator : MonoBehaviour
{

    public int teamNumber = 0;
    public float baseSize = 1f;
    public float headSize = 3f;
    public float headHeight = 3f;
    public float shieldX = 1f;
    public float shieldY = 1f;
    public float shieldZ = 1f;
    // Start is called before the first frame update
    void Start()
    {
        Meshes();//and sound   
    }

    void Meshes()
    {
        GameObject player = gameObject;
        player.name = "Player " + teamNumber.ToString();
        //add child for mesh
        GameObject playerMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        playerMesh.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/White") as Material;
        playerMesh.transform.position = player.transform.position;
        playerMesh.transform.localScale *= baseSize;
        playerMesh.name = "BaseMesh";

        playerMesh.transform.parent = player.transform;

        //add info script
        PlayerInfo pI = player.AddComponent<PlayerInfo>();
        pI.playerNumber = teamNumber;
        // pI.homeCell = homeCell;



        //add movementy script
        player.AddComponent<PlayerMovement>();//.enabled = false;//////*********disabling atm;
        //add attack script
        player.AddComponent<PlayerAttacks>().enabled = false;//////*********disabling atm
        //add swipe attack script
        player.AddComponent<Swipe>().enabled = false;//////*********disabling atm;
        //add cell height controller
        player.AddComponent<CellHeights>().enabled = false;//////*********disabling atm;
        //controller input
        player.AddComponent<Inputs>().enabled = false;//////*********disabling atm;
        //vibration info
        player.AddComponent<PlayerVibration>().enabled = false;//////*********disabling atm;
        //sound
        player.AddComponent<PlayerSounds>().enabled = false;//////*********disabling atm;
        //tree for diufferent sound types in player
        GameObject soundParent = new GameObject();
        soundParent.name = "Sounds";
        soundParent.transform.parent = player.transform;
        //walk noise
        GameObject walkObject = new GameObject();
        walkObject.name = "Walk";
        walkObject.transform.parent = soundParent.transform;
        //noise
        walkObject.AddComponent<NoiseMaker>().enabled = false;//////*********disabling atm;
        // walkObject.AddComponent<ProceduralAudioController>();
        walkObject.AddComponent<AudioSource>().enabled = false;//////*********disabling atm;


        //head
        GameObject child = new GameObject();
        child.transform.position = Vector3.up * headHeight;
        child.name = "Head";
        //add mesh object
        GameObject headMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headMesh.name = "HeadMesh";
        headMesh.transform.parent = child.transform;
        headMesh.transform.localScale *= headSize;
        headMesh.transform.position = child.transform.position;
        headMesh.layer = LayerMask.NameToLayer("PlayerBody");
        //add collider stuff to do bumps from player
        headMesh.AddComponent<Rigidbody>().isKinematic = true;
        //
        headMesh.AddComponent<PlayerCollision>().enabled = false;//////*********disabling atm
        headMesh.GetComponent<BoxCollider>().isTrigger = true;
        //disable mesh renderer
        headMesh.GetComponent<MeshRenderer>().enabled = false;
        //add children which will visualise player health

        for (int i = 0; i < 2; i++)
        {
            //anchor so scales easy
            GameObject headMesh0 = new GameObject();
            headMesh0.transform.parent = headMesh.transform;
            if (i == 0)
                headMesh0.transform.position = headMesh.transform.position + Vector3.down * headSize * 0.5f;
            else
                headMesh0.transform.position = headMesh.transform.position + Vector3.up * headSize * 0.5f;


            //actual cube we will see
            GameObject headMesh0a = GameObject.CreatePrimitive(PrimitiveType.Cube);


            if (i == 0)
                headMesh0a.transform.position = headMesh0.transform.position + (headSize * 0.5f) * Vector3.up;
            else
                headMesh0a.transform.position = headMesh0.transform.position - (headSize * 0.5f) * Vector3.up;

            headMesh0a.transform.parent = headMesh0.transform;

            headMesh0a.transform.localScale = Vector3.one * headSize;
            //first cube is blood cube - no blood to start
            if (i == 0)
                headMesh0.transform.localScale = new Vector3(headMesh0.transform.localScale.x, 0f, headMesh0.transform.localScale.z);

            if (i == 0)
            {
                headMesh0a.name = "Blood";
                headMesh0a.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Red0") as Material;
            }
            else
                headMesh0a.name = "Health";

            headMesh0a.GetComponent<BoxCollider>().enabled = false;
        }

        GameObject noseMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        noseMesh.name = "NoseMesh";
        noseMesh.transform.parent = child.transform;
        noseMesh.transform.localScale *= headSize * .5f;
        noseMesh.transform.position = child.transform.position + Vector3.forward * headSize * .5f;
        noseMesh.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/White") as Material;
        Destroy(noseMesh.GetComponent<BoxCollider>());

        //stabber/sword
        /*
        GameObject stabber = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stabber.transform.rotation *= Quaternion.Euler(90, 0, 0);
        stabber.transform.position = child.transform.position;
        stabber.transform.position += Vector3.forward * ((headSize * .5f));// +(stabberStartLength));        
                                                                           //now alter vertices
        Vector3[] vertices = stabber.GetComponent<MeshFilter>().mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            //vertices[i] += Vector3.up * 1f;// ( stabberStartLength + headSize*.5f);
            //scale mesh
            vertices[i].x *= stabberStartWidth;
            vertices[i].y *= stabberStartLength;
            vertices[i].z *= stabberStartWidth;

            //put bottom vertices at zero, flush agaisnt cube
            if (vertices[i].y < 0f)
                vertices[i].y = 0f;

        }

        stabber.GetComponent<MeshFilter>().mesh.vertices = vertices;

        //stabber.transform.localScale = new Vector3(stabberStartWidth, stabberStartLength, stabberStartWidth);
        stabber.name = "Stabber";
        */
        //shield

        GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Cube);

        shield.transform.position = child.transform.position;
        shield.transform.position += Vector3.forward * ((headSize * .5f) + (shieldZ * 2));
        shield.name = "Shield";
        shield.layer = LayerMask.NameToLayer("Shield");
        shield.transform.localScale = new Vector3(shieldX, shieldY, shieldZ);
        shield.AddComponent<ShieldCollision>();
        shield.GetComponent<BoxCollider>().isTrigger = true;
        shield.AddComponent<Rigidbody>().isKinematic = true;
        //we will use a shield pivot
        GameObject shieldPivot = new GameObject();
        shieldPivot.transform.position = child.transform.position;
        shieldPivot.name = "ShieldPivot";



        /*
        GameObject shieldMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shieldMesh.name = "SheildMesh";
        shieldMesh.transform.parent = shield.transform;
        
        */

        /*
        //swiper 
        GameObject swiper = new GameObject();
        swiper.AddComponent<MeshRenderer>();
        swiper.AddComponent<MeshFilter>();
        swiper.transform.position = child.transform.position;
        swiper.name = "Swiper";
        */

        child.transform.parent = player.transform;
        //stabber.transform.parent = child.transform;
        shield.transform.parent = shieldPivot.transform;
        shieldPivot.transform.parent = child.transform;
        //swiper.transform.parent = player.transform;

        //start shield pointing to the ground now we ahve parent collider to it
        shieldPivot.transform.localRotation = Quaternion.Euler(90, 0, 0);





        //homeCell.transform.localScale = new Vector3(1f, 50f, 1f);/droppping atm, cool idea tho/////*********

        if (teamNumber == 0)
        {
            // playerMesh.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Cyan0") as Material;
            headMesh.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Team0a") as Material;
            headMesh.transform.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Team0a") as Material;
            // stabber.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Blue") as Material;
            shield.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Team0a") as Material;
            // swiper.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { Resources.Load("Red1") as Material };//,Resources.Load("Blue1") as Material};
        }
        else if (teamNumber == 1)
        {
            //  playerMesh.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Orange0") as Material;
            headMesh.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Orange0") as Material;
            //  stabber.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Red2") as Material;
            shield.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Orange1") as Material;
            headMesh.transform.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Orange0") as Material;
            //   swiper.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { Resources.Load("Blue1") as Material };//, Resources.Load("Red1") as Material };
        }
        else if (teamNumber == 2)
        {
            //   playerMesh.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Green0") as Material;
            headMesh.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Green0") as Material;
            headMesh.transform.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Green0") as Material;
            //  stabber.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Red2") as Material;
            shield.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Green1") as Material;

            //   swiper.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { Resources.Load("Blue1") as Material };//, Resources.Load("Red1") as Material };
        }
        else if (teamNumber == 3)
        {

            headMesh.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/DeepBlue0") as Material;
            headMesh.transform.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/DeepBlue0") as Material;
            shield.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/DeepBlue2") as Material;

        }




        //audio
        //  ProceduralAudioController pAC = player.AddComponent<ProceduralAudioController>(); //////*********disabling atm
        // pAC.guide = true;
        //pAC.useSinusAudioWave = true;

        //player.AddComponent<SwipeSound>();
        //player.AddComponent<SineWaveExample>();

    }
}
