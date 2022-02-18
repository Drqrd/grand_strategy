using UnityEngine;

public class SpaceSkybox : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] [Range(1,100)] int resolution = 1;

    private Vector3[] rotations = new Vector3[]
    {
        new Vector3(0,0,0),
        new Vector3(0,0,-90),
        new Vector3(0,0,90),
        new Vector3(90,0,0),
        new Vector3(-90,0,0),
        new Vector3(180,0,0)
    };

    private void OnAwake()
    {
        // Create the six quads of the skybox
        // Works around weird issue with an inverted cube skybox not working
        for (int a = 0; a < 6; a++)
        {
            GenerateQuad();
        }
    }


    private void GenerateQuad()
    {
        Vector3
    }
}
