﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DualGraph2d;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine.Audio;

[RequireComponent (typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider))]
public class MeshGenerator : MonoBehaviour {

	//public BuildController buildController;
	public Material[] materials;
	public Transform cubePrefab;
	
	//public BezierSpline roadCurveL;
	//public BezierSpline roadCurveR;
	//public float roadCurveFrequency;
	//public float roadSpread = 3f;
	//public GridPlayer gridPlayer;
    private List<Vector3> roadList = new List<Vector3>();
    public Vector3 volume;//= new Vector3(20.0f,0.0f,20.0f);
	public float rootTriMultiplier=1.0f;
	public int cellNumber= 20;
    public int lloydIterations = 3;
    public float minEdgeSize = 3f;
    public bool useSortedGeneration;//=true;
	public bool drawCells=false;
	public bool drawDeluany=false;
	public bool drawRoots=false;
	public bool drawVoronoi=false;
	public bool drawGhostEdges=false;
	public bool drawPartialGhostEdge=false;
	public bool drawCircumspheres=false;
	public Color sphereColor= Color.cyan;
	

	//public DualGraph dualGraph;
//	private float totalTime;
	private float computeTime;
	private Mesh graphMesh;

	
	public bool fillWithRandom;
	public bool fillWithPoints;
    public bool fillWithMasterPoints;

    public bool extrudeCells;
    public bool walls = true;
    
    public bool weldCells = true;
    public float weldThreshold = 10f;//how wide should minimum  ledge size be?
    public bool makeSkyscraper = true;
    public float threshold = 2f;
    
    public bool startOverlay = true;
	public List<Vector3> masterPoints = new List<Vector3>();
 //   public List<GameObject> cellsList = new List<GameObject>();
   // public List<Mesh> meshList = new List<Mesh>();
    public List<Vector3[]> meshVerts = new List<Vector3[]>();
    public List<int[]> meshTris = new List<int[]>();

    public int density = 5;

    public List<GameObject> cells = new List<GameObject>();
    //public List<List<GameObject>> adjacentCells = new List<List<GameObject>>();//moved t0 saving on each cell with AdjacentCells class
    public int adjacentCellsCount = 0;

    public int counter = 0;
    List<Vector3> centroids = new List<Vector3>();
    public List<Vector3> previousCentroids = new List<Vector3>();
    float tolerance =5f;

    public void Start ()
    {

        tolerance = minEdgeSize;//testing


        //StartCoroutine("Lloyds");


        Lloyds();

        //disable all cells for tests
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].SetActive(false);
        }
    }

    void GeneratePoints(Vector3[] points,DualGraph dualGraph)
    {
       
        //set up cells
        dualGraph.DefineCells(points, rootTriMultiplier);
    //    computeTime = Time.realtimeSinceStartup;

        //compute
        if (useSortedGeneration)
            dualGraph.ComputeForAllSortedCells();
        else
            dualGraph.ComputeForAllCells();




        //  yield return new WaitForEndOfFrame();

    
        

       // Debug.Log("coroutine");

        
    }

    public void Lloyds()
    {
        
    
       
        //for (int x = 0; x < lloydIterations; x++)
        
        //we will keep relaxing until all our edges are at least 
        bool edgeShortEnough = false;
        //int maxCounts = 50;
        int count = 0;
        List<Vector3> borderPoints = new List<Vector3>();
        while(!edgeShortEnough && count < lloydIterations)
        {
          //  Debug.Log("LastIteration =" + count);


            //Go get points from Road Curve       
            DualGraph dualGraph = new DualGraph(volume);
            cells.Clear();

            cellNumber = (int)volume.x / density;

            Vector3[] points = new Vector3[cellNumber];
            //Debug.Log(points.Length);
            if (count > 0)
                points = new Vector3[centroids.Count];

            if(count > 0)
            {
                fillWithPoints = true;
                fillWithRandom = false;
            }

            GenSortedRandCells(ref points);


           // if (count == 1)
            {
                for (int i = 0; i < points.Length; i++)
                {
                  //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  //  c.transform.position = points[i];
                  //   c.name = count.ToString() + " " + i.ToString();
                }
            }


            //save to list before we clear and work out new centroids
            previousCentroids = new List<Vector3>( centroids );

            centroids.Clear();

            dualGraph.DefineCells(points, rootTriMultiplier);
            dualGraph.ComputeForAllSortedCells();
           // dualGraph.ComputeForAllCells();

            dualGraph.PrepareCellsForMesh();
            
            //work out centroids for next iteration 
            for (int i = 0; i < dualGraph.cells.Count; i++)
            {
                if (!dualGraph.cells[i].root && !dualGraph.cells[i].IsOpenEdge())
                {                       //use only interior until faux edges are added
                    if (dualGraph.cells[i].IsOpenEdge())
                    {
                        Debug.Log("open edge");
                    }
                    Vector3 avg = Vector3.zero;
                    for (int a = 0; a < dualGraph.cells[i].mesh.verts.Length; a++)
                    {
                        if (a != 0)
                            avg += dualGraph.cells[i].mesh.verts[a];//make temp []

                    }
                    avg /= dualGraph.cells[i].mesh.verts.Length - 1;
                    centroids.Add(avg);
                }
                else
                {
                    //saving outside border points, if we dont the graph will get smaller and smaller with each lloyd iteration
                    if (( dualGraph.cells[i].IsOpenEdge() && !dualGraph.cells[i].root))
                    {
                        centroids.Add(dualGraph.cells[i].point);
                       // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        //c.transform.position = dualGraph.cells[i].point;
                    }
                }
            }

           

            if (count == 0)
            {

                for (int i = 0; i < centroids.Count; i++)
                {
                   // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                   // c.transform.position = centroids[i];
                  //  c.name = "c";// x.ToString() + " " + i.ToString();
                }

            }

            bool doShortestEdge = false;
            if (doShortestEdge)
            {
                //work out if we have relaxed enough
                float shortestEdgeDistance = FindShortestEdgeDistance(dualGraph);
                if (shortestEdgeDistance >= minEdgeSize)
                {
                    Debug.Log("min edge distance reached ");
                    edgeShortEnough = true;
                }
                else
                    Debug.Log("shortest distance = " + shortestEdgeDistance);
            }

            count++;



            for (int i = 0; i < transform.childCount; i++)
            {
                //transform.GetChild(i).gameObject.SetActive(false);
                Destroy(transform.GetChild(i).gameObject);
            }

            cells = new List<GameObject>();
            GenerateMesh(dualGraph);

            CellMeshes();

         //   yield return new WaitForSeconds(.1f);

           
        }

        AddExtrudes();
        AddAdjacents();

        CalculateAdjacents();
        Edges();
        RemoveSmallEdges();
        
        



        ReMesh();

        //refind adjacents
        CalculateAdjacents();
        //redo edges now we have removed some
        Edges();//and shared eges

        //red do the meshes using the new edges
      //  yield return new WaitForEndOfFrame();
         AddToCells();

        for (int i = 0; i < previousCentroids.Count; i++)
        {
         // GameObject c =  GameObject.CreatePrimitive(PrimitiveType.Cube);
           // c.transform.position = previousCentroids[i];
        }
    }

    void RemoveSmallEdges()
    {

        //now we need to look for small edges
        for (int a = 0; a < cells.Count; a++)
        {
            List<List<int>> edges = cells[a].GetComponent<Wall>().edges;
            Vector3[] vertices = cells[a].GetComponent<MeshFilter>().mesh.vertices;
            for (int i = 0; i < edges.Count; i++)
            {
                Vector3 p0 = vertices[edges[i][0]];
                Vector3 p1 = vertices[edges[i][1]];

                float distance = Vector3.Distance(p0, p1);
                if (distance < minEdgeSize)
                {

                    /*
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                     c.transform.position = p0;
                     c.name = a.ToString() + " " + i.ToString() + " 0 first";
                     c.transform.parent = cells[a].transform;
                     c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                     c.transform.position = p1;
                     c.name = a.ToString() + " " + i.ToString() + " 1 ";
                     */
                    //find all other vertices which equal this

                    //add this
                    List<List<int>> toMove = new List<List<int>>();
                    toMove.Add(new List<int>() { a, i });
                    for (int b = 0; b < cells.Count; b++)
                    {
                        if (a == b)
                            continue;

                        Vector3[] otherVertices = cells[b].GetComponent<MeshFilter>().mesh.vertices;
                        List<List<int>> otherEdges = cells[b].GetComponent<Wall>().edges;
                        for (int j = 0; j < otherEdges.Count; j++)
                        {
                            Vector3 q0 = otherVertices[otherEdges[j][0]];
                            Vector3 q1 = otherVertices[otherEdges[j][1]];

                           // if (p0 == q0 && p1 == q1 || p0 == q1 && p1 == q0) ////////////working (p0 == q0 && p1 == q1)
                            if(Vector3.Distance(p0,q0)<tolerance && Vector3.Distance(p1,q1)<tolerance
                                || Vector3.Distance(p0, q1) < tolerance && Vector3.Distance(p1, q0) < tolerance)
                            {
                                //we have a match

                                // c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                // c.transform.position = q0;
                                //  c.name = b.ToString() + " " + j.ToString() + " 0 second";
                                //  c.transform.parent = cells[b].transform;

                                toMove.Add(new List<int>() { b, j });
                                //c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                //c.transform.position = q1;
                                //c.name = b.ToString() + " " + j.ToString() + " 1 ";
                                //  Vector3 movedPos = q0 + Vector3.up * 10f;

                                //  otherVertices[otherEdges[j][0]] = movedPos;
                                //  cells[b].GetComponent<MeshFilter>().mesh.vertices = otherVertices;
                            }
                        }

                        for (int x = 0; x < toMove.Count; x++)
                        {
                            Vector3[] verticesToMove = cells[toMove[x][0]].GetComponent<MeshFilter>().mesh.vertices;
                            List<List<int>> edgesToMove = cells[toMove[x][0]].GetComponent<Wall>().edges;

                            p0 = verticesToMove[edgesToMove[toMove[x][1]][0]];
                            p1 = verticesToMove[edgesToMove[toMove[x][1]][1]];

                            Vector3 centre = Vector3.Lerp(p0, p1, 0.5f);
                            //parallel lists - other fixes we need to make
                            List<GameObject> cellsToFix = new List<GameObject>();
                            List<int> vertsToFix = new List<int>();
                            List<Vector3> targetsForFix = new List<Vector3>();

                            //there may be solo vertices still not moved, search for them now

                            for (int y = 0; y < cells.Count; y++)
                            {
                                //skip our own cell
                                if (cells[toMove[x][0]] == cells[y])
                                    continue;

                                //List<List<int>> edges = cells[x].GetComponent<Wall>().edges;
                                Vector3[] verticesOther = cells[y].GetComponent<MeshFilter>().mesh.vertices;

                                for (int z = 0; z < verticesOther.Length; z++)
                                {
                                    if (verticesOther[z] == p0 || verticesOther[z] == p1)
                                    {
                                        //            vertices[i] = centre;//remember and mvoe later
                                        cellsToFix.Add(cells[y]);
                                        vertsToFix.Add(z);
                                        targetsForFix.Add(centre);

                                    }
                                }

                            }


                            //using parallel list tomove points
                            for (int y = 0; y < cellsToFix.Count; y++)
                            {

                                Vector3[] verticesToFix = cellsToFix[y].GetComponent<MeshFilter>().mesh.vertices;
                                verticesToFix[vertsToFix[y]] = targetsForFix[y];
                                cellsToFix[y].GetComponent<MeshFilter>().mesh.vertices = verticesToFix;

                                /*GameObject c0 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                c0.transform.position = targetsForFix[y];
                                c0.transform.parent = cellsToFix[y].transform;
                                c0.name = "solo";
                                */
                            }


                            /*
                            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = p0;
                            c.transform.parent = cells[toMove[x][0]].transform;
                            c.name = "0";

                            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = p1;
                            c.name = "1";
                            c.transform.parent = cells[toMove[x][0]].transform;

                            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = centre;
                            c.name = "c";
                            c.transform.parent = cells[toMove[x][0]].transform;

                        */

                            verticesToMove[edgesToMove[toMove[x][1]][0]] = centre;
                            verticesToMove[edgesToMove[toMove[x][1]][1]] = centre;

                            // now we use these edges to make a new mesh

                            cells[toMove[x][0]].GetComponent<MeshFilter>().mesh.vertices = verticesToMove;

                        }
                        //move first pos
                        //   Vector3 movedPosA = p0 + Vector3.up * 10;
                        //  vertices[edges[i][0]] = movedPosA;
                        //  cells[a].GetComponent<MeshFilter>().mesh.vertices = vertices;
                    }


                }
            }
        }
    }

    void AddExtrudes()
    {
        //adding script to hold mesh info, will be enabled later
        for (int i = 0; i < cells.Count; i++)
        {

                ExtrudeCell ex = cells[i].AddComponent<ExtrudeCell>();
                ex.SaveOriginalMesh();
         
        }
    }

    void AddAdjacents()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            AdjacentCells aj = cells[i].AddComponent<AdjacentCells>();
        }
    }
    
    void CalculateAdjacents()
    {
        //work out which cells are adjacent to each cell, save in a list
        for (int i = 0; i < cells.Count; i++)
        {
            //set layer here
            cells[i].layer = LayerMask.NameToLayer("Cells");

            List<GameObject> adjacents = new List<GameObject>();

            Vector3[] thisVertices = cells[i].GetComponent<MeshFilter>().mesh.vertices;
            for (int j = 0; j < cells.Count; j++)
            {
                //don't check own cell
                if (i == j)
                    continue;

                Vector3[] otherVertices = cells[j].GetComponent<MeshFilter>().mesh.vertices;
                int matches = 0;

                for (int a = 0; a < thisVertices.Length; a++)
                { 
                    for (int b = 0; b < otherVertices.Length; b++)
                    {
                        //if we have a match, add "other" cell to a list of adjacents for this cell
                        if (Vector3.Distance( thisVertices[a], otherVertices[b]) < tolerance) //opt0- think this is ok as ==
                        {
                            //adjacents.Add(cells[j]); //making so we need two points for an adjacent cell

                            //force out of the loops
                            //a = thisVertices.Length;


                            matches++;
                        }
                    }
                }

                if (matches > 1)//means if cell mathces one ponton a corner, we ignore. it has to be a solid edge
                    adjacents.Add(cells[j]);
            }

            //adjacentCells.Add(adjacents); //removing
            //add to list and save it on game object. Doing it this way allows us to hot reload, if we save it all in a list here, it won't serialize

            AdjacentCells aj = cells[i].GetComponent<AdjacentCells>();
            aj.adjacentCells = adjacents;
       //     aj.targetY = GetComponent<OverlayDrawer>().minHeight; // taken out for network test //set from extrude cell now anyway

        }
    }

    void ReMesh()
    {
        

        for (int i = 0; i < cells.Count; i++)
        {
            Vector3[] vertices = cells[i].GetComponent<MeshFilter>().mesh.vertices;


            List<Vector3> newVertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            for (int j = 0; j < vertices.Length; j++)
            {
                //we are looking for welded vertices
                if (j < 2)
                {
                    //always add [0] (centre) and 1(we will compare this in the next iteration)
                    newVertices.Add(vertices[j]);
                }
                else if (Vector3.Distance( vertices[j], vertices[j - 1]) <= tolerance)//was ==
                {
                  //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  //  c.transform.position = vertices[j];
                  //  c.name = "match";
                  //  c.transform.parent = cells[i].transform;

                    //don't add this duplicate
                    continue;
                    
                }
                else
                {
                    if(Vector3.Distance( vertices[j],newVertices[1]) > tolerance)//can loop, so catch if it tries to weld last to first (instead !=. doing distance)
                        newVertices.Add(vertices[j]);
                }
            }

            //create new mesh


            for (int j = 0; j < newVertices.Count; j++)
            {
                if (j < 2) continue;

                triangles.Add(j);
                triangles.Add(0);
                triangles.Add(j-1);                
            }
            //add last
            triangles.Add(1);
            triangles.Add(0);
            triangles.Add(newVertices.Count-1);

            Mesh mesh = new Mesh();
            mesh.vertices = newVertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();

            bool test = false;
            if (test)
            {
                //create test body
                GameObject cell = new GameObject();
                cell.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load("White") as Material;

                cell.AddComponent<MeshFilter>().sharedMesh = mesh;
            }
            else
            {
                //replace cell's mesh with new welded one
                cells[i].GetComponent<MeshFilter>().mesh = mesh;
                //also replace extrude cell's "original" mesh with this one

               // Debug.Log("mesh count before update");
                cells[i].GetComponent<ExtrudeCell>().SaveOriginalMesh();
            }
        }

    }
    
    void Edges()
    {
        //makes list of edges for each cell and removes any that are too small
        
        for (int a = 0; a < cells.Count; a++)
        {
            //hold edge info in wall script in cell 
            if(cells[a].GetComponent<Wall>() == null)
                cells[a].AddComponent<Wall>();

            Wall wall = cells[a].GetComponent<Wall>();

            //this will figure out edges and save them on the script
            wall.Edges();
        }

        //now we have worked out all edges, find adjacent edges
        for (int a = 0; a < cells.Count; a++)
        {   
            cells[a].GetComponent<Wall>().FindSharedEdges();
        }
      
    }

    float FindShortestEdgeDistance(DualGraph dualGraph)
    {
        float shortestDistance = Mathf.Infinity;

        Vector3 p0;
        Vector3 p1;

        int shortestIndexI = 0;
        int shortestIndexJa = 0;
        int shortestIndexJb = 0;
        for (int i = 0; i < dualGraph.cells.Count; i++)
        {
            if (!dualGraph.cells[i].root && !dualGraph.cells[i].IsOpenEdge())
            {
                for (int j = 0; j < dualGraph.cells[i].mesh.verts.Length; j++)
                {
                    if (j == 0)
                        continue;

                    int nextIndex = j + 1;
                    //looping but skipping 0 central point
                    if (nextIndex > dualGraph.cells[i].mesh.verts.Length - 1)
                        nextIndex = 1;

                    p0 = (dualGraph.cells[i].mesh.verts[j]);
                    p1 = (dualGraph.cells[i].mesh.verts[nextIndex]);

                    /*
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = p0;// dualGraph.cells[shortestIndexI].mesh.verts[shortestIndexJa];
                    c.name = i.ToString() +" "+ j.ToString();// "shortest A";

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = p1;//dualGraph.cells[shortestIndexI].mesh.verts[shortestIndexJb];
                    c.name = c.name = i.ToString()+" " + nextIndex.ToString();
                    */

                    float d = Vector2.Distance(p0, p1);

                    if (d < shortestDistance)
                    {
                        shortestIndexI = i;
                        shortestIndexJa = j;
                        shortestIndexJb = nextIndex;
                        shortestDistance = d;
                    }
                }
            }
        }
        /*
        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = dualGraph.cells[shortestIndexI].mesh.verts[shortestIndexJa];
        c.name = shortestIndexI.ToString() + " " + shortestIndexJa.ToString();// "shortest A";

        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = dualGraph.cells[shortestIndexI].mesh.verts[shortestIndexJb];
        c.name = shortestIndexI.ToString() + " " + shortestIndexJb.ToString();// 
        */
        return shortestDistance;
    }


    List<GameObject> RemoveEdgeCells (List<GameObject> cells)
    {
        List<GameObject> toRemove = new List<GameObject>();
        //each cell
        for (int i = 0; i < cells.Count; i++)
        {

            Vector3[] thisVertices = cells[i].GetComponent<MeshFilter>().mesh.vertices;

            //each vertice in this cell (i)
            for (int a = 1; a < thisVertices.Length; a++)//start at 1, central point is 0
            {
                int matches = 0;

                //check against every other cell
                for (int j = 0; j < cells.Count; j++)
                {
                    //skip this cell
                    if (i == j)
                        continue;

                    Vector3[] otherVertices = cells[j].GetComponent<MeshFilter>().mesh.vertices;

                    //and every other vertices within other cell
                    for (int b = 1; b < otherVertices.Length; b++)
                    {
                        if (thisVertices[a] == otherVertices[b])
                        {
                            matches++;
                        }
                    }
                }

                if (matches < 2)
                {
                    // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    // c.transform.position = thisVertices[a];

                    cells[i].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Red") as Material;
                  

                    toRemove.Add(cells[i]);
                }
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
           cells.Remove(toRemove[i]);
        }


        return cells;
    }


    List<GameObject> ColourEdgeCells(List<GameObject> cells)
    {
       
        for (int i = 0; i < cells.Count; i++)
        {

            Vector3[] thisVertices = cells[i].GetComponent<MeshFilter>().mesh.vertices;

            //each vertice in this cell (i)
            for (int a = 1; a < thisVertices.Length; a++)//start at 1, central point is 0
            {
                int matches = 0;

                //check against every other cell
                for (int j = 0; j < cells.Count; j++)
                {
                    //skip this cell
                    if (i == j)
                        continue;

                    Vector3[] otherVertices = cells[j].GetComponent<MeshFilter>().mesh.vertices;

                    //and every other vertices within other cell
                    for (int b = 1; b < otherVertices.Length; b++)
                    {
                       // if (thisVertices[a] == otherVertices[b])
                        if(Vector3.Distance(thisVertices[a],otherVertices[b])<  tolerance)//tolerance ok here?testing
                        {
                            matches++;

                            
                            
                          //  c.transform.parent = cells[a].transform;
                        }
                    }
                }

                if (matches < 2)
                {
                   // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  //  c.transform.position = thisVertices[a];

                    cells[i].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Ground0") as Material;
                    cells[i].GetComponent<AdjacentCells>().edgeCell = true;

                    
                }
            }
        }

        return cells;
    }

    private void AddCentroids(ref Vector3[] p,List<Vector3> centroids)
    {

        List<Vector3> tempList = new List<Vector3>();

        /*	for (int i = 0; i <gridPlayer.Path.Count;i++)
            {
                Vector3 position = gridPlayer.Path[i] - transform.position;
                position.y = 0f;
                tempList.Add(position);

            }

            for(int i=0; i<p.Length - gridPlayer.Path.Count; i++){

                Vector3 position = new Vector3(UnityEngine.Random.Range(-volume.x,volume.x),0.0f,UnityEngine.Random.Range(-volume.z,volume.z));
                tempList.Add(position);
                //p[i]= new Vector3(Random.Range(-volume.x,volume.x),0.0f,Random.Range(-volume.z,volume.z));
            }

            p = tempList.ToArray();

    */
            //cellNumber = yardPoints.Count;

            for (int i = 0; i < centroids.Count; i++)
            {
                tempList.Add(centroids[i]);
            }

            p = tempList.ToArray();
        
    }

    /// <summary>
    /// Generates random cells, sorted by x value.
    /// </summary>
    /// <param name="points">Points.</param>
    //Note about sorting: using a sorted list requires the x values to always be different
    private void GenSortedRandCells(ref Vector3[] points){
		SortedList<float, Vector3> p= new SortedList<float,Vector3>();


        if(fillWithMasterPoints)
        {
            cellNumber = masterPoints.Count;

            for (int i = 0; i < masterPoints.Count; i++)
            {
                try
                {
                    p.Add(masterPoints[i].x, masterPoints[i]);
                }
                catch (System.ArgumentException)
                {

                    Array.Resize(ref points, points.Length - 1);
                    cellNumber -= 1;
                }
            }
            p.Values.CopyTo(points, 0);
        }
		//adds random values for the rest
		else if(fillWithRandom)
			{
			for(int i=0; i<cellNumber; i++){
				Vector3 v = new Vector3(UnityEngine.Random.Range(-volume.x,volume.x),0.0f,UnityEngine.Random.Range(-volume.z,volume.z));
				try{
					p.Add(v.x, v);

				
				}
				catch(System.ArgumentException){
					i--;
					//Debug.Log("sort conflict");
				}
			}
			p.Values.CopyTo(points,0);
		}

		else if(fillWithPoints)
		{
			//cellNumber = yardPoints.Count;

			for(int i = 0; i < centroids.Count; i++)
			{
				try{
					p.Add(centroids[i].x, centroids[i]);
				}
				catch(System.ArgumentException)
				{

					Array.Resize(ref points,points.Length-1);
					cellNumber-=1;
				}
			}
			p.Values.CopyTo(points,0);
		}

        
	}
	/// <summary>
	/// Generates the mesh.
	/// </summary>
	void GenerateMesh(DualGraph dualGraph)
    {
    //    Debug.Log("prepare cells for mesh start");
        dualGraph.PrepareCellsForMesh();
        //yield return new WaitForEndOfFrame();
     //   Debug.Log("prepare cells for mesh end");
		if (graphMesh==null){
			graphMesh= new Mesh();
			graphMesh.name= "Graph Mesh";
		}
		else{
			//For the love of god, why are you calling this twice?!?!
			graphMesh.Clear();
		}

        meshVerts.Clear();
        meshTris.Clear();

	//	List<Vector3> vert= new List<Vector3>();
	//	List<Vector2> uvs= new List<Vector2>();
	//	List<int> tris= new List<int>();
	//	int vertCount=0;

	//	foreach(Cell c in dualGraph.cells)
     //   {
        for(int i = 0; i < dualGraph.cells.Count; i++)
        {
            //bottleneck protection
           // if(i!=0 && i % 100 == 0)
            //    yield return new WaitForEndOfFrame();




            List<Vector3> vert= new List<Vector3>();
			List<Vector2> uvs= new List<Vector2>();
			List<int> tris= new List<int>();
			int vertCount=0;
			if(!dualGraph.cells[i].root && !dualGraph.cells[i].IsOpenEdge()){						//use only interior until faux edges are added
				if(dualGraph.cells[i].IsOpenEdge()){
					Debug.Log("open edge");
				}

                for (int a = 0; a < dualGraph.cells[i].mesh.verts.Length; a++)
                {

                    //Debug.Log("in verts");
                    vert.Add(dualGraph.cells[i].mesh.verts[a]);

                   // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                   // c.transform.position= dualGraph.cells[i].mesh.verts[a];
                   // c.name = a.ToString();

                }
                foreach (Vector2 v in dualGraph.cells[i].mesh.uv){
					uvs.Add(v);
                   // Debug.Log("in uv");
                }

				for(int j = 2; j < dualGraph.cells[i].mesh.verts.Length; j++){
					tris.Add(vertCount);
					tris.Add(vertCount + j - 1);
					tris.Add(vertCount + j);
				}

				//finishing the loop
				tris.Add(vertCount);
				tris.Add(vertCount+ dualGraph.cells[i].mesh.verts.Length-1);
				tris.Add(vertCount+1);

				vertCount=vert.Count;
			}
			//Check for empty meshes and skip
			if (vert.Count == 0) continue;

			///Export to individual GameObject
		//	GameObject cell = new GameObject();

            
            //add mesh info to lists to crate mesh in a coroutine and drip feed in to unity
            //Mesh mesh = new Mesh();
		//	mesh.vertices = vert.ToArray();
        //    mesh.triangles = tris.ToArray();
          
            meshVerts.Add(vert.ToArray());
            meshTris.Add(tris.ToArray());       
    
            
		}

        //StartCoroutine("AddToCells");

       
    }


    void CellMeshes()
    {
        for (int i = 0; i < meshVerts.Count; i++)
        {
            //create a game object for each cell in the mesh list
            GameObject cell = new GameObject();
            cell.transform.parent = this.gameObject.transform;
            cell.name = "Cell";
            // cell.tag = "Cell";


            //create a mesh from the already populated lists
            Mesh mesh = new Mesh();
            mesh.vertices = meshVerts[i];
            mesh.triangles = meshTris[i];
            mesh.RecalculateNormals();


            MeshFilter meshFilter = cell.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;


            MeshRenderer meshRenderer = cell.AddComponent<MeshRenderer>();


            meshRenderer.sharedMaterial = Resources.Load("Materials/Ground1") as Material;



            //bottleneck protection, build a 100 at a time
            //  if (i != 0 && i % 100 == 0)
            //       yield return new WaitForEndOfFrame();


            //master list of cells
            cells.Add(cell);


        }
    }

    void AddToCells()
    {
        //look for and cells with clustered points and weld them together - cheap hack that works only due to the fact that the central points will never weld
        //make sure [0] never welds!
        if (weldCells)
        {

            for (int i = 0; i < cells.Count; i++)
            {
               // Debug.Log("before " + cells[i].GetComponent<MeshFilter>().mesh.vertexCount);
                Mesh mesh = AutoWeld.AutoWeldFunction(cells[i].GetComponent<MeshFilter>().mesh, tolerance, 100);
                cells[i].GetComponent<MeshFilter>().mesh = mesh;
              //  Debug.Log("after " + cells[i].GetComponent<MeshFilter>().mesh.vertexCount);
            }
        }

        //colour outside and save in adjacent cells script
        ColourEdgeCells(cells);

        //now we have found adjacents etc, we can scale cells
        for (int i = 0; i < cells.Count; i++)
        {
            if (extrudeCells)
            {
                ExtrudeCell ex = cells[i].GetComponent<ExtrudeCell>();
                ex.uniqueVertices = true;
                //call start straight away
                ex.Start();
            }
        }

        GameObject wallsParent = new GameObject();
        wallsParent.name = "Walls";
        wallsParent.transform.parent = transform;

        for (int i = 0; i < cells.Count; i++)
        {
            if (walls)
            {
                Wall wall = cells[i].GetComponent<Wall>();
                // wall.FindSharedEdges();//?
                float wallHeight = GetComponent<OverlayDrawer>().wallHeight;
                wall.BuildWalls(wallsParent,wallHeight);
                wall.enabled = true;
            }
        }

        bool addAudio = true;

        if (addAudio)
        {

            //audio for cells
            AudioMixer mixer = Resources.Load("Sound/CellHeights") as AudioMixer;
            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].AddComponent<NoiseMaker>();//.enabled = false;//laggy to start?
                AudioSource aS = cells[i].AddComponent<AudioSource>();
                aS.volume = 0f;
                aS.outputAudioMixerGroup = mixer.FindMatchingGroups("Master/0")[0];
            }

        }

        if (startOverlay)
            GetComponent<OverlayDrawer>().enabled = true;

        

       // bool addCanvas = false;
      //  if(addCanvas)
      //   StartCanvas();//doing after network spawna nd connect

        // yield break;
    }

    void StartCanvas()
    {
        GameObject canvas = GameObject.Find("Canvas(Clone)");
        canvas.GetComponent<Canvas>().enabled = false;
        GetComponent<CellMeter>().EnableNextFrame();
        GetComponent<CellMeter>().currentRoundTime = 0;
    }

    /*
	void OnDrawGizmos(){
		if(dualGraph!=null){
			if(drawCells){
				foreach(Cell c in dualGraph.cells){
					if(c.root){
						Gizmos.color=Color.red;
					}
					else{
						Gizmos.color= Color.blue;
					}
					Gizmos.DrawCube(c.point,Vector3.one);
				}
			}
			
			if (drawDeluany){
				foreach(Cell c in dualGraph.cells){
					foreach(VoronoiEdge e in c.edges){
						if (e.cellPair.root || c.root){
							if (drawRoots){
								Gizmos.color= Color.gray;
								Gizmos.DrawLine(c.point, e.cellPair.point);
							}
						}
						else{
							Gizmos.color= Color.green;
							Gizmos.DrawLine(c.point, e.cellPair.point);
						}
					}
				}
			}
			if (drawVoronoi){
				foreach(Cell c in dualGraph.cells){
					foreach (VoronoiEdge e in c.edges){
						if(e.isConnected){
							Gizmos.color=Color.black;
							if(e.ghostStatus==Ghosting.none){
								Gizmos.DrawLine(e.Sphere.Circumcenter, e.SpherePair.Circumcenter);
							}
							else if(drawGhostEdges){
								Gizmos.DrawLine(e.Sphere.Circumcenter, e.SpherePair.Circumcenter);
							}
							else if(drawPartialGhostEdge&& e.ghostStatus== Ghosting.partial){
								Gizmos.DrawLine(e.Sphere.Circumcenter, e.SpherePair.Circumcenter);
							}
						}
					}
				}
			}
			if (drawCircumspheres){
				Gizmos.color= sphereColor;
				foreach(Circumcircle c in dualGraph.spheres){
					Gizmos.DrawSphere(c.Circumcenter, c.circumradius);
				}
			}
			
		}
	}

    */
}
