using System.Collections;
using UnityEngine;

public class VFXAutoDestroyRealtime : MonoBehaviour
{
    public float life = 2f;

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(life);
        Destroy(gameObject);
    }
}