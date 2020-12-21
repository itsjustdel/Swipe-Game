using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour {

  //  public float maxHeight = 3f;


    public float buildSpeed = 0.1f;

    public bool uniqueVertices = true;

    float wallThickness = 2f;

    GameObject wall;
    List<GameObject> adjacentCells;

    List<int> sharedEdges = new List<int>();
    public List<GameObject> walls = new List<GameObject>();
    public List<List<int>> edges = new List<List<int>>();
    public List<List<Vector3>> miters = new List<List<Vector3>>();
    public List<List<Vector3>> mitersSorted = new List<List<Vector3>>();

    public List<int[]> edgesAdjacents = new List<int[]>();

    public List<GameObject> adjacentEdgeCells = new List<GameObject>();
    public List<float> wallHeights = new List<float>();
    public List<float> wallHeightsCurrent = new List<float>();

    PlayerClassValues playerClassValues;

    public int adjacentEdgesCount;

    float tolerance = 5f;
    OverlayDrawer overlayDrawer;
    private void Awake()
    {
        tolerance = GameObject.FindGameObjectWithTag("Code").GetComponent<MeshGenerator>().minEdgeSize;//testing

        playerClassValues = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerClassValues>();
        overlayDrawer= GameObject.FindGameObjectWithTag("Code").GetComponent<OverlayDrawer>();

        enabled = false;
        //calculate early as other scripte use this straight away - FindSharedEdges in Start() will look for other instances of this (Wall.cs)
       // Edges();//calling from mesh generator but also here as we have changed the mesh since
    }
    private void Start()
    {

        
       //   FindSharedEdges();// calling from mG

        //figure out where to call from or when to enable?
       // BuildWalls();
    }


    private void FixedUpdate()
    {
        //Mesh mesh = CellWall();
        //wall.GetComponent<MeshFilter>().mesh = mesh;


        CalculateHeights();
        MoveWalls();
    }
    public void Edges()
    {
        edges.Clear();
        miters.Clear();
        
        //makes list of edges

        adjacentCells = gameObject.GetComponent<AdjacentCells>().adjacentCells;

        //build game objects for each wall/edge
        Mesh mesh =  GetComponent<ExtrudeCell>().originalMesh;
        
        Vector3[] verts = mesh.vertices;
        for (int i = 1; i < verts.Length; i++)
        {
            
            int prevInt = i - 1;
            int thisInt = i;
            int nextInt = i + 1;
            int nextNextInt = i + 2;

            if(prevInt <= 0)
            {
                //if central point, move to last
                prevInt = verts.Length - 1;
            }
            if (nextInt > verts.Length - 1)
            {
                //if next is over vertices length, put to start
                nextInt -= verts.Length - 1; //-1 less so we skip 0
            }
            if(nextNextInt > verts.Length -1)
            {
                nextNextInt -= verts.Length - 1;
            }
             



            Vector3 p0 = verts[prevInt];
            Vector3 p1 = verts[thisInt];
            Vector3 p2 = verts[nextInt];
            Vector3 p3 = verts[nextNextInt];

            //so order around cell is previous,p0,p1,next
            Vector3 miterDirection0 = MiterDirection(p0, p1, p2);
            Vector3 miterDirection1 = MiterDirection(p1, p2, p3);

            List<int> edge = new List<int>() { thisInt, nextInt };

            bool showCubes = false; //helps show how points are running around cell
            if (showCubes)
            {
               // Debug.Log("pff");

                GameObject c0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c0.transform.position = verts[thisInt];
                c0.name = "this" + thisInt;
                c0.transform.parent = transform;

                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = verts[prevInt];
                c.name = "prev" + prevInt;
                c.transform.parent = c0.transform;

              

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = verts[nextInt];
                c.name = "next" + nextInt;
                c.transform.parent = c0.transform;


                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.name = "next next" + nextNextInt;
                c.transform.position = verts[nextNextInt];

                c.transform.parent = c0.transform;
            }


            edges.Add(edge);

            //save
            miters.Add(new List<Vector3>() { -miterDirection0, -miterDirection1 });

            /*
            wall = new GameObject();
            wall.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load("White") as Material;
            wall.AddComponent<MeshFilter>();
            //wall.transform.SetParent(transform, true);
            wall.transform.position = transform.position;// - Vector3.up * maxHeight;
            Mesh originalMesh = GetComponent<ExtrudeCell>().originalMesh;
            wall.GetComponent<MeshFilter>().mesh = IndividualWall(originalMesh, p1, p0, playerClassValues.maxClimbHeight, -miterDirection0, -miterDirection1);
            */
            
        }
    }

   Vector3 MiterDirection(Vector3 p0, Vector3 p1,Vector3 p2)
    {
        Vector3 miterDirection = new Vector3();

        //directions facing away from center point p1
        Vector3 dir0 = (p2 - p1).normalized;
       // Vector3 dir1 = (p2 - p1).normalized;

        //find the normal vector
        Vector3 normal0 = new Vector3(-dir0.z, 0f, dir0.x);

        //find the tangent vector at both end
        Vector3 tan0 = ((p1 - p0).normalized + dir0).normalized;

        //find the miter line, which is the normal of tangent
        miterDirection = new Vector3(-tan0.z, 0f, tan0.x);

        //find the lnegth of the miter by projecting the miter on to the normal
        float length = wallThickness / Vector3.Dot(normal0, miterDirection);
        miterDirection *= length;

      //  Debug.DrawLine(p0, p1, Color.blue);
      //  Debug.DrawLine(p2, p1, Color.blue);
     // if(draw)
     //   Debug.DrawLine(p1, p1 + miterDirection * -length , Color.red);
        /*
        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = p0;
        c.name = "p0";
        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = p1;
        c.name = "p1";
        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = p1 + dir0;
        c.name = "m";
        */

       
//
        return miterDirection;
    }

   public void FindSharedEdges()
    {
        edgesAdjacents.Clear();
        adjacentEdgeCells.Clear();
        mitersSorted.Clear();

        //go through edge list and find what edge it is sharing with
        MeshGenerator mg = GameObject.FindGameObjectWithTag("Code").GetComponent<MeshGenerator>();
        Vector3[] thisOriginalVertices =  GetComponent<ExtrudeCell>().originalMesh.vertices;
       //s Mesh originalMesh = GetComponent<ExtrudeCell>().originalMesh;
        //for each each adjacent cell

        //for each edge in this cell
        for (int a = 0; a < edges.Count; a++)
        {
            //check eah edge in each adjacent cell
            for (int i = 0; i < adjacentCells.Count; i++)
            {
                Vector3[] adjacentOriginalVertices = adjacentCells[i].GetComponent<ExtrudeCell>().originalMesh.vertices;
                List<List<int>> otherEdges = adjacentCells[i].GetComponent<Wall>().edges;

                /*
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = thisOriginalVertices[edges[a][0]];
                c.name = "0";
                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = thisOriginalVertices[edges[a][1]];
                c.name = "1";
                */
                //is cell adjacent low enough to warrant a wall?
            //    Debug.Log("other edges = " + otherEdges.Count);
                for (int b = 0; b < otherEdges.Count; b++)
                {
                    /*
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = adjacentOriginalVertices[otherEdges[b][0]];
                    c.name = "other 0";
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = adjacentOriginalVertices[otherEdges[b][1]];
                    c.name = "other 1";                    
                    */
                    Vector3 a0 = thisOriginalVertices[edges[a][0]];
                    Vector3 a1 = thisOriginalVertices[edges[a][1]];
                    Vector3 b0 = adjacentOriginalVertices[otherEdges[b][0]];
                    Vector3 b1 = adjacentOriginalVertices[otherEdges[b][1]];

                   // if (a0 == b0 && a1 == b1 || a0 == b1 && b0 == a1)
                        if(Vector3.Distance(a0,b0) <tolerance && Vector3.Distance(a1, b1)< tolerance
                        || Vector3.Distance(a0, b1) < tolerance && Vector3.Distance(b0, a1) < tolerance)
                    {
                        int[] adEdge = new int[] { otherEdges[b][0], otherEdges[b][1] };
                        //save edge
                        edgesAdjacents.Add(adEdge);/// do we need to re run loop with new edgesAdjacents list? 
                        //and which cell it belongs to
                        adjacentEdgeCells.Add(adjacentCells[i]); 
                        //save miters too
                        mitersSorted.Add(miters[a]);
                    }
                }
            }
        }

        adjacentEdgesCount = adjacentEdgeCells.Count;
    }

   public void BuildWalls(GameObject parent,float wallHeight)
    {
        //build walls off shared edges found - cells on edge wont have walls on outside
        Mesh mesh = GetComponent<ExtrudeCell>().originalMesh;

        GameObject wallParent = new GameObject();
        wallParent.name = "Walls";
        wallParent.transform.parent = parent.transform;
        
        //use new list of adjacents, (ordered with walls)
        for (int i = 0; i < adjacentEdgeCells.Count; i++)
        {
            Vector3[] verts = adjacentEdgeCells[i].GetComponent<ExtrudeCell>().originalMesh.vertices;

            wall = new GameObject();
            wall.name = "Wall" + i.ToString();
            
            wall.AddComponent<MeshRenderer>().enabled = false;//easier// sharedMaterial = Resources.Load("Materials/Transparent") as Material;
            wall.AddComponent<MeshFilter>();
            
            wall.layer = LayerMask.NameToLayer("Wall");//could add wall layer i guess
          // wall.transform.SetParent(transform, true);//cant do unless we dont use scale for making cells taller(would need to alter mesh data)
          //unless we do some maths on the walls scale method and factor in parent scale
            wall.transform.position = transform.position;// - Vector3.up * maxHeight;
            wall.transform.parent = wallParent.transform;
            walls.Add(wall);
            wallHeights.Add(0f);
            wallHeightsCurrent.Add(0f);

            //use edges adjacent matching/parallel list
            Vector3 p0 = verts[edgesAdjacents[i][0]];
            Vector3 p1 = verts[edgesAdjacents[i][1]];

            //to create miter points (even corners) we need next edge and previous edge

            Mesh originalMesh = GetComponent<ExtrudeCell>().originalMesh;
            Vector3 miterDir0 = mitersSorted[i][1];
            Vector3 miterDir1 = mitersSorted[i][0];

            Debug.DrawLine(p0, p0 + miterDir0,Color.red);
            wall.GetComponent<MeshFilter>().mesh = IndividualWall(originalMesh, p0, p1, wallHeight, miterDir0,miterDir1);
            wall.AddComponent<MeshCollider>().sharedMesh = wall.GetComponent<MeshFilter>().mesh;
        }
    }

    void CalculateHeights()
    {
        
        for (int i = 0; i < adjacentEdgeCells.Count; i++)
        {
            //for each edge, check if the cell it shares an edge with is lower than it
            bool setHigh = false;
            if (GetComponent<AdjacentCells>().controlledBy < 0)
            {
                setHigh = false;
            }
            else if (transform.localScale.y - adjacentEdgeCells[i].transform.localScale.y > playerClassValues.maxClimbHeight * overlayDrawer.heightMultiplier)
            {
                //if this isnt owned by the same player (dont build walls with territory)
                int thisOwnedBy = GetComponent<AdjacentCells>().controlledBy;
                int adjacentOwnedBy =adjacentEdgeCells[i].GetComponent<AdjacentCells>().controlledBy;


               // if (thisOwnedBy != adjacentOwnedBy)
                    setHigh = true;

                //build wall on to frontline cell
                if (adjacentEdgeCells[i].GetComponent<AdjacentCells>().controlledBy > -1)
                    setHigh = true;
            }

            
            
            if(setHigh)
            {
                //set walls high in parallel list
                wallHeights[i] = 1f;
            }
            else
            {
                wallHeights[i] = 0f;
            }
        }
    }

    void MoveWallsForAutoHeights()
    {
        //for each wall
        for (int i = 0; i < walls.Count; i++)
        {
            //instead of building mesh every frame, just move wall object
            
            //place wall at top of cell height
            walls[i].transform.position = new Vector3(walls[i].transform.position.x, transform.localScale.y, walls[i].transform.position.z);

            //if cell has finished adjusting height, build wall      -only want to when building up,otherwise, take wall down instantly
            AdjacentCells aC = GetComponent<AdjacentCells>();
            if (aC.targetY == transform.localScale.y)
            {
                if (wallHeights[i] == 1f)
                {
                    wallHeightsCurrent[i] += buildSpeed;
                    if (wallHeightsCurrent[i] > 1f)
                    {
                        wallHeightsCurrent[i] = 1f;
                    }
                }
                if(wallHeights[i] < 1f)
                {
                    wallHeightsCurrent[i] -= buildSpeed;
                    if (wallHeightsCurrent[i] < 0f)
                        wallHeightsCurrent[i] = 0f;
                }              
            }

            //we need this check too
            if(aC.targetY < transform.localScale.y)
            {  
                wallHeightsCurrent[i] -= buildSpeed;

                if (wallHeightsCurrent[i] < 0f)
                    wallHeightsCurrent[i] = 0f;
            }

            walls[i].transform.localScale = new Vector3(1f, wallHeightsCurrent[i], 1f);

            if (wallHeightsCurrent[i] <= buildSpeed)///stops the last z fight
            {
                walls[i].SetActive(false);
            }
            else
            {
                walls[i].SetActive(true);
            }

            walls[i].GetComponent<MeshRenderer>().enabled = true;
            if (GetComponent<AdjacentCells>().controlledBy == 0)
                walls[i].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Team0a") as Material;
            else if (GetComponent<AdjacentCells>().controlledBy == 1)
                walls[i].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Team1a") as Material;
            else if (GetComponent<AdjacentCells>().controlledBy == 2)
                walls[i].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Team2a") as Material;
            else if (GetComponent<AdjacentCells>().controlledBy == 3)
                walls[i].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Team3a") as Material;
        }
    }
    void MoveWalls()
    {
        //for each wall
        for (int i = 0; i < walls.Count; i++)
        {
            //instead of building mesh every frame, just move wall object

            //place wall at top of cell height
            walls[i].transform.position = new Vector3(walls[i].transform.position.x, transform.localScale.y, walls[i].transform.position.z);

            //if cell has finished adjusting height, build wall      -only want to when building up,otherwise, take wall down instantly
            AdjacentCells aC = GetComponent<AdjacentCells>();
          //  if (aC.targetY == transform.localScale.y)
            {
                if (wallHeights[i] == 1f)
                {
                    wallHeightsCurrent[i] += buildSpeed;
                    if (wallHeightsCurrent[i] > 1f)
                    {
                        wallHeightsCurrent[i] = 1f;
                    }
                }
                if (wallHeights[i] < 1f)
                {
                    wallHeightsCurrent[i] -= buildSpeed;
                    if (wallHeightsCurrent[i] < 0f)
                        wallHeightsCurrent[i] = 0f;
                }
            }

            //we need this check too
            if (aC.targetY < transform.localScale.y)
            {
                wallHeightsCurrent[i] -= buildSpeed;

                if (wallHeightsCurrent[i] < 0f)
                    wallHeightsCurrent[i] = 0f;
            }

            walls[i].transform.localScale = new Vector3(1f, wallHeightsCurrent[i], 1f);

            if (wallHeightsCurrent[i] <= buildSpeed)///stops the last z fight
            {
                walls[i].SetActive(false);
            }
            else
            {
                walls[i].SetActive(true);
            }

            walls[i].GetComponent<MeshRenderer>().enabled = true;
            if (GetComponent<AdjacentCells>().controlledBy == 0)
                walls[i].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Team0a") as Material;
            else if (GetComponent<AdjacentCells>().controlledBy == 1)
                walls[i].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Team1a") as Material;
            else if (GetComponent<AdjacentCells>().controlledBy == 2)
                walls[i].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Team2a") as Material;
            else if (GetComponent<AdjacentCells>().controlledBy == 3)
                walls[i].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Team3a") as Material;
        }
    }


    public Mesh IndividualWall( Mesh mesh, Vector3 p0,Vector3 p1,float target,Vector3 dirToCentre0,Vector3 dirToCentre1)
    {

        //first of all we need to move p0, and p1 towards the centre of the cell (we did this in extrude cell already for the main cell)
        //orig mesh in world, and new extruded mesh in local..(add transform position)

        Vector3[] verts = mesh.vertices;
        p0 = Vector3.Lerp(transform.position,p0, GetComponent<ExtrudeCell>().scale);//should be parameter
        p1 = Vector3.Lerp(transform.position,p1, GetComponent<ExtrudeCell>().scale);

        /*
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = p0;
         cube.transform.localScale *= 1f;
        cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = p1;
        cube.transform.localScale *= 1f;
        */
        int vertCountStart = verts.Length;
        List<Vector3> vertsList = new List<Vector3>();
        List<int> trisList = new List<int>();

        float tempBuildSpeed = buildSpeed;
        if (target == transform.localScale.y)
            tempBuildSpeed = -buildSpeed;

        float yHeight = target;




        //front wall
        vertsList.Add(p0);// + (Vector3.up * (transform.localScale.y)));//can add a little gap here
        vertsList.Add(p1);// + (Vector3.up * (transform.localScale.y)));
        vertsList.Add(p1 + (Vector3.up * yHeight));
        vertsList.Add(p0 + (Vector3.up * yHeight));

        //top
        //Vector3 dirToCentre0 = (transform.position - p0).normalized;
       // Vector3 dirToCentre1 = (transform.position - p1).normalized;

        vertsList.Add(p0 + (Vector3.up * yHeight));
        vertsList.Add(p1 + (Vector3.up * (yHeight)));
        vertsList.Add(p1 + (Vector3.up * yHeight + dirToCentre1));
        vertsList.Add(p0 + (Vector3.up * yHeight) + dirToCentre0 );

        //inside
        //alter order so triangle loop makes it face the other way 
        vertsList.Add(p0 + dirToCentre0 );
        vertsList.Add(p0 + ((Vector3.up * (yHeight))) + dirToCentre0 );
        vertsList.Add(p1 + ((Vector3.up * (yHeight))) + dirToCentre1 );
        vertsList.Add(p1  + dirToCentre1 );

        //side panel 1
        
        vertsList.Add(p0 + dirToCentre0  + Vector3.up*yHeight);
        vertsList.Add(p0 + dirToCentre0 );                
        vertsList.Add(p0);
        vertsList.Add(p0 + Vector3.up * yHeight);

        //side panel 2
        
        vertsList.Add(p1 + dirToCentre1 );
        vertsList.Add(p1 + dirToCentre1  + Vector3.up * yHeight);
        vertsList.Add(p1 + Vector3.up * yHeight);
        vertsList.Add(p1);
        


        for (int i = 0; i < vertsList.Count; i++)
        {

           vertsList[i] -= transform.position;


//            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
  //          cube.transform.position = vertsList[i];
    //             cube.transform.localScale *= 1f;

            
        }


        //tris


        for (int i = 0; i < vertsList.Count - 2; i += 4)
        {
            trisList.Add(i + 0);            
            trisList.Add(i + 2);
            trisList.Add(i + 1);


            
            trisList.Add(i + 0);
            trisList.Add(i + 3);
            trisList.Add(i + 2);

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

   

  
}
