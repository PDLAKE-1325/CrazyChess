using UnityEngine;

public class Check_Notify : MonoBehaviour
{
    void Start()
    {
        Invoke("Destroy", 1.5f);
    }
    void Destroy()
    {
        Destroy(this.gameObject);
    }
}
