using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelBase : MonoBehaviour {


    int voxelAmount = 1000;
    public List<GameObject> voxelsActive = new List<GameObject>();
    public List<GameObject> voxelsDisabled = new List<GameObject>();
    public GameObject parent;
	// Use this for initialization
	void Start ()
    {
        for (int i = 0; i < voxelAmount; i++)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(c.GetComponent<BoxCollider>());
            c.AddComponent<MeshCollider>().enabled = false;
            c.GetComponent<MeshCollider>().convex = true;
            c.transform.position = -Vector3.up * 10;
            c.transform.parent = parent.transform;
            c.layer = LayerMask.NameToLayer("Voxel");
            voxelsDisabled.Add(c);
            
            
        }
	}
}
