using System;
using UnityEngine;


public class BulletTrail : MonoBehaviour
{
    
    private TrailRenderer _bulletTrail;
    private float _destroyTimer = 0.0f;

    public delegate void OnDisableCallback(BulletTrail bulletTrail);
    public OnDisableCallback Disable;

    private bool _canDeactivate = false;

    private void Awake()
    {
        _bulletTrail = GetComponent<TrailRenderer>();
        _destroyTimer = _bulletTrail.time;
    }
    public void Reset()
    {
        _destroyTimer = _bulletTrail.time;
        _canDeactivate = false;
    }
    void Update()
    {
        _destroyTimer -= Time.deltaTime;
        if (_destroyTimer < 0.0f)
        {
            //Destroy(this);
            _canDeactivate = true;
            Disable.Invoke(this);

        }
        
    }
}
