using NUnit.Framework;
using Unity.MLAgents.Policies;
using UnityEngine;

public class bullet_script : MonoBehaviour
{
    private TankManager _parent;
    private int _damage = 40;
    private EnvController envController;
    private TargetPracticeController _targetPracticeController;
    private bool destroyed = false;
    [SerializeField] private Transform SparkEmitterPrefab;

    private void Start()
    {
        envController = GetComponentInParent<EnvController>();
    }
    public void SetShooter(TankManager parent)
    {
        _parent = parent;
        _targetPracticeController = _parent != null ? _parent.EnvController.GetComponent<TargetPracticeController>() : null;
    }

    public void Shoot(Vector3 pos, Quaternion rot, Vector3 dir, float force)
    {
        destroyed = false;
        this.gameObject.SetActive(true);
        transform.position = pos;
        transform.rotation = rot;

        this.GetComponent<Rigidbody>().linearVelocity = new Vector3(0, 0, 0);
        this.GetComponent<Rigidbody>().AddForce(dir * force);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (destroyed) return;
        destroyed = true;

        if ((collision.gameObject.CompareTag("YellowAgent") && _parent.Team == Team.Red) ||
            (collision.gameObject.CompareTag("RedAgent") && _parent.Team == Team.Yellow))
        {
            //Transform emitter = GameObject.Instantiate(SparkEmitterPrefab);
            //emitter.position = collision.transform.position;

            if (_parent.Team == Team.Red)
                envController.EnemyDetected(_parent.gameObject, Team.Yellow);
            else
                envController.EnemyDetected(_parent.gameObject, Team.Red);

            GameObject target = collision.collider.gameObject.transform.parent.gameObject;

            bool isTargetHit = target.gameObject == _parent.GetTarget() ? true : false;

            float healthBeforeHit = target.GetComponent<ITargetable>().Health;
            target.GetComponent<ITargetable>().Hit(_damage);

            //Hit rewards
            if(isTargetHit)
            {
                _parent.AddRewardToShooter(0.2f);
            }
            else if(_parent.GetTarget() == null)
            {
                 _parent.AddRewardToShooter(0.08f);
            }
            else
            {
                _parent.AddRewardToShooter(0.02f);
            }

            //Eliminate rewards
            if(healthBeforeHit <= 40.0f)
            {
                if(isTargetHit)
                {
                    _parent.AddReward(1.0f);
                     Debug.Log("Eliminated marked!");
                    _targetPracticeController?.HandleMarkedTargetHit(_parent);
                }
                else if(_parent.GetTarget() == null)
                {
                     _parent.AddRewardToShooter(0.5f);
                    Debug.Log("Eliminated unmarked!");
                }
                else
                {
                    _parent.AddRewardToShooter(0.2f);
                    Debug.Log("Eliminated unmarked!");
                }
                
            }
        }
        else if((collision.gameObject.CompareTag("YellowAgent") && _parent.Team == Team.Yellow) ||
               (collision.gameObject.CompareTag("RedAgent") && _parent.Team == Team.Red))
        {
            _parent.AddRewardToShooter(-0.5f);
            collision.collider.gameObject.transform.parent.GetComponent<ITargetable>().Hit(_damage);
            float healthBeforeHit =  collision.collider.gameObject.transform.parent.GetComponent<ITargetable>().Health;
            if(healthBeforeHit <= 40.0f)
            {
                _parent.AddRewardToShooter(-2.0f);
                Debug.Log("Eliminated friendly!");
            }
        }
        else
        {
           _parent.AddRewardToShooter(-0.005f);
        }

        this.gameObject.SetActive(false);
    }
}
