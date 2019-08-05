using UnityEngine;

public class NoiseMaker : MonoBehaviour
{
    [Range(-1f, 1f)]
    public float offset;

    public bool whiteNoise = true;
    public bool perlinNoise = false;
    public float perlinMultiplier = 1f;
    
   

    float perlinX = 1f;
    float perlinY = 1f;

    System.Random rand = new System.Random();

    private void Update()
    {
        perlinX +=  perlinMultiplier*Time.deltaTime;
        perlinY +=  perlinMultiplier * Time.deltaTime;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
      

        if (whiteNoise)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (float)(rand.NextDouble() * 2.0 - 1.0 + offset);
            }
        }

        

        if (perlinNoise)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (float)(Mathf.PerlinNoise(i*perlinX, i * perlinY));
            }
        }
    }
}

