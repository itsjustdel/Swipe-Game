using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    // Transform of the camera to shake. Grabs the gameObject's transform
    // if null.
    public Transform camTransform;

    // How long the object should shake for.
    public float shakeDuration = 0f;

    // Amplitude of the shake. A larger value shakes the camera harder.
    public float cellShakeAmount = 0.1f;
    public float hitShakeAmount = 0.2f;
    public float decreaseFactor = 1.0f;

    Vector3 originalPos;

    PlayerGlobalInfo pgi;

    //do count for plyer shake requests - may need to add different types of shake?
    public int cellShake;

    public bool shake;



    void Awake()
    {
        if (camTransform == null)
        {
            camTransform = GetComponent(typeof(Transform)) as Transform;
        }


    }

    void OnEnable()
    {
        originalPos = camTransform.localPosition;

        
    }

    void FixedUpdate()
    {
        //happens on round change
        if (GameObject.FindGameObjectWithTag("Code") == null)
            return;

        if(pgi == null)
            pgi = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerGlobalInfo>();

        

        CellHeightAdjust();
        CellShake();

        TimedShake();//timer set from other scripts
    }

    void TimedShake()
    {
        if (shakeDuration > 0)
        {
            camTransform.localPosition = originalPos + Random.insideUnitSphere * cellShakeAmount;

            shakeDuration -= Time.deltaTime * decreaseFactor;
        }
        else
        {
            shakeDuration = 0f;
            //camTransform.localPosition = originalPos;
        }
    }
    void CellShake()
    {

        if (cellShake > 0)
        {
            float thisShake = cellShake * cellShakeAmount;
            camTransform.localPosition = originalPos + Random.insideUnitSphere * thisShake;
        }
    }
    void CellHeightAdjust()
    {
        //detect if anyone is changing a cell height
        cellShake = 0;
        for (int i = 0; i < pgi.playerGlobalList.Count; i++)
        {
            if (pgi.playerGlobalList[i].GetComponent<CellHeights>().loweringCell || pgi.playerGlobalList[i].GetComponent<CellHeights>().raisingCell)
                cellShake++;
        }

    }

    public void ShakeForHit()
    {
        camTransform.localPosition = originalPos + Random.insideUnitSphere * hitShakeAmount;
    }

   
}