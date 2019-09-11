using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class ExtrudeCell : MonoBehaviour {

    public float depth = 1f;
    public float scale = .9f;
    public bool uniqueVertices = false;

    public Vector3 centroid;

    public Mesh originalMesh;
    private void Awake()
    {
        depth = transform.parent.GetComponent<OverlayDrawer>().minHeight;
        enabled = false;

    }
    // Use this for initialization
    public void Start()
    {

       

        
        //work out centroid, needed for a few things, we can use this script to hold info about the cell
        centroid = Spawner.GetCentroid(gameObject, transform);

        Realign();

        Scale();

        
      //  originalMesh.triangles = GetComponent<Mesh>().triangles;

        Height();
        centroid += Vector3.up * depth;
        
        GetComponent<MeshFilter>().mesh =  Extrude(GetComponent<MeshFilter>().mesh,depth,scale,uniqueVertices);

       // Rotate();

          FixMeshCollider();

        //   Combine();

        
        
    }

    public void SaveOriginalMesh()
    {  //save mesh after scaling, using to build walls(easier to work from flat mesh)
        originalMesh = new Mesh();
        originalMesh.vertices = new List<Vector3>(GetComponent<MeshFilter>().mesh.vertices).ToArray();//probs an array way of doing this
        originalMesh.triangles = new List<int>(GetComponent<MeshFilter>().mesh.triangles).ToArray();
    }

    void Height()
    {
        //work out height depending on closeness to centre
       // float distance = transform.position.magnitude;

       // float xSizeOfCity = GameObject.Find("Code").GetComponent<MeshGenerator>().volume.x;

       // depth = 3f;///just for test, remove comment block below
        /*
//        depth = Random.Range(16, 1000);

        depth =xSizeOfCity - distance;
        depth = inExp(depth/ xSizeOfCity);
        depth *= xSizeOfCity;
        //depth += 200;

        //make less linear
       /depth *= Random.Range(.5f, .7f);

        //1f is 7000, 0 is 0
        */
    }

    void Scale()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] verts = mesh.vertices;
        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 p = Vector3.Lerp(verts[0], verts[i], scale);
            verts[i] = p;
        }

        mesh.vertices = verts;
    }

    void Realign()
    {
        
        //makes the transform position the centre of the mesh and moves the mesh vertices so the stay the same in world space
        Mesh mesh = GetComponent<MeshFilter>().mesh;

       
        transform.position = mesh.vertices[0];

        Vector3[] verts = mesh.vertices;
        List<Vector3> vertsList = new List<Vector3>();

        for(int i = 0; i < verts.Length; i++)
        {
            Vector3 point = verts[i] - transform.position;
        //    point.y = 0;
            vertsList.Add(point);
        }

        

        mesh.vertices = vertsList.ToArray();

       

    }

    public static Mesh Extrude(Mesh mesh,float depth,float scale,bool uniqueVertices)
    { 

        Vector3[] verts = mesh.vertices;
        int vertCountStart = verts.Length;
        List<Vector3> vertsList = new List<Vector3>();
        List<int> trisList = new List<int>();

        for (int i = 0; i < verts.Length-1 ; i++)
        {
            if (i == 0)
                continue;

            vertsList.Add(verts[i]);
            vertsList.Add(verts[i + 1]);
            vertsList.Add(verts[i + 1] + (Vector3.up*depth));
            vertsList.Add(verts[i] + (Vector3.up * depth));
        }

        //add joining link/ last one
        vertsList.Add(verts[verts.Length-1]);
        vertsList.Add(verts[1]);
        vertsList.Add(verts[1] + (Vector3.up * depth));
        vertsList.Add(verts[verts.Length - 1] + (Vector3.up * depth));


        for (int i = 0; i < vertsList.Count - 2; i+=4)
        {
            
            trisList.Add(i + 0);
            trisList.Add(i + 1);
            trisList.Add(i + 2);


            trisList.Add(i + 3);
            trisList.Add(i + 0);
            trisList.Add(i + 2);

        }

        //now add a top
        vertsList.Add(verts[0] + Vector3.up * depth);
        //join the last ring all to this central point
        for (int i = 0; i < vertsList.Count-2; i+=4)
        {
            trisList.Add(i + 2);
            trisList.Add(vertsList.Count-1);
            trisList.Add(i + 3);


        }
        foreach (Vector3 v3 in vertsList)
        {
         //   GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
         //   cube.transform.position = v3;
         //   cube.transform.localScale *= 0.1f;
        }

        Mesh newMesh = new Mesh();
        newMesh.vertices = vertsList.ToArray();
        newMesh.triangles = trisList.ToArray();

        if (uniqueVertices)
        {
            newMesh = MeshTools.UniqueVertices(newMesh);
        }
        else
        {
            newMesh.RecalculateNormals();
            newMesh.RecalculateBounds();
        }

        
        return newMesh;
	}
	
	void Rotate()
    {
        transform.rotation *= Quaternion.Euler(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
    }
    void FixMeshCollider()
    {
        gameObject.AddComponent<MeshCollider>();
    }
    void Combine()
    {
        //if not already added combine, do it now
      //  if (transform.parent.GetComponent<CombineChildren>() == null)
      //      transform.parent.gameObject.AddComponent<CombineChildren>();

    }

    float inExp(float x)
    {
        if (x < 0.0f) return 0.0f;
        if (x > 1.0f) return 1.0f;
        return Mathf.Pow(2.0f, 10.0f * (x - 1.0f));
    }
}
