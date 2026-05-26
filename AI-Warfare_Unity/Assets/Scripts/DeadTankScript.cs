using UnityEngine;

public class DeadTankScript : MonoBehaviour
{
    [SerializeField] private Transform tankTower;
    [SerializeField] private Transform tankCannon;

    private float lifeTime = 0.0f;

    void Start()
    {
        lifeTime = 15.0f;
    }

    public void setTransform(TankController toCopy)
    {
        transform.position = toCopy.transform.position;
        transform.rotation = toCopy.transform.rotation;

        tankTower.localRotation = toCopy.getTowerTransform().localRotation;
        tankCannon.localRotation = toCopy.getCannonTransform().localRotation;
    }

    void Update()
    {
        lifeTime -= Time.deltaTime;

        transform.position = new Vector3(transform.position.x, transform.position.y - (5.0f/10.0f * Time.deltaTime) , transform.position.z);

        if(lifeTime <= 0)
        {
            Destroy(gameObject);
        }
    }
}
