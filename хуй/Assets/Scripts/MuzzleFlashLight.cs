using UnityEngine;

public class MuzzleFlashLight : MonoBehaviour
{
    public Light flashLight;
    public float flashIntensity = 4f;
    public float flashTime = 0.03f;

    private void Awake()
    {
        if (flashLight == null)
            flashLight = GetComponent<Light>();

        if (flashLight != null)
            flashLight.intensity = 0f;
    }

    public void Flash()
    {
        if (flashLight == null) return;

        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        flashLight.intensity = flashIntensity;
        yield return new WaitForSeconds(flashTime);
        flashLight.intensity = 0f;
    }
}