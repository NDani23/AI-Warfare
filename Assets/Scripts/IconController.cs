using UnityEngine;
using UnityEngine.UIElements;

public class IconController : MonoBehaviour
{

    [SerializeField] private float fixedHeight = 600.0f;


    void Update()
    {
        this.transform.position = new Vector3(transform.parent.transform.position.x, fixedHeight, transform.parent.transform.position.z);
        this.transform.rotation = Quaternion.Euler(0.0f, transform.parent.transform.rotation.eulerAngles.y, 0.0f);
    }
}
