using UnityEngine;

public class SparkEmitterScript : MonoBehaviour
{
    private float destroyTimer = 0.0f;
    void Start()
    {
        destroyTimer = 1.0f;
    }

    void Update()
    {
        destroyTimer -= Time.deltaTime;

        if (destroyTimer < 0.0f)
        {
            Destroy(gameObject);
        }
    }
}
