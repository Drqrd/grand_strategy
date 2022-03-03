using UnityEngine;

public class DefaultWorld : MonoBehaviour
{
    void Start()
    {
        GetComponent<WorldSave>().LoadWorld("DefaultWorld");
    }
}
