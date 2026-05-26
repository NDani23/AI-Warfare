using UnityEngine;
using UnityEngine.Pool;

public class BulletTrailPool : MonoBehaviour
{
    [SerializeField] private BulletTrail _bulletTrail;
    [SerializeField] private uint _base_size = 5;
    [SerializeField] private uint _max_size = 10;

    private ObjectPool<BulletTrail> _bulletTrailPool;

    private void Awake()
    {
        _bulletTrailPool = new ObjectPool<BulletTrail>(CreatePooledObject, OnTakeFromPool, OnReturnToPool, OnDestroyObject, false, 5, 10);
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(_bulletTrailPool.CountActive);
    }

    private void ReturnObjectToPool(BulletTrail instance)
    {
        _bulletTrailPool.Release(instance);
    }

    private BulletTrail CreatePooledObject()
    {
        BulletTrail instance = Instantiate(_bulletTrail, Vector3.zero, Quaternion.identity);
        instance.Disable += ReturnObjectToPool;
        //instance.gameObject.SetActive(false);
        instance.GetComponent<TrailRenderer>().AddPosition(Vector3.zero);
        return instance;
    }

    private void OnTakeFromPool(BulletTrail instance)
    {
        //instance.gameObject.SetActive(true);
        //instance.Reset();
    }

    private void OnReturnToPool(BulletTrail instance)
    {
        //instance.gameObject.SetActive(false);
        instance.GetComponent<TrailRenderer>().AddPosition(Vector3.zero);
    }

    private void OnDestroyObject(BulletTrail instance)
    {
        //Destroy(instance.gameObject);
    }

    public BulletTrail GetBullet()
    {
        return _bulletTrailPool.Get();
    }
}
