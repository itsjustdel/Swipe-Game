using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementHelper : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	

    public class CellAndAngle
    {
        public GameObject cell;
        public float angle;
        public float sliceStart;
        public float sliceEnd;
    }

    public static float SignedAngle(Vector3 from, Vector3 to, Vector3 normal)
    {
        // angle in [0,180]
        float angle = Vector3.Angle(from, to);
        float sign = Mathf.Sign(Vector3.Dot(normal, Vector3.Cross(from, to)));
        return angle * sign;
    }


    public static int SortByAngle(CellAndAngle c1, CellAndAngle c2)
    {
        return c1.angle.CompareTo(c2.angle);
    }
}
