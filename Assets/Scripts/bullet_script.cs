using Unity.MLAgents.Policies;
using UnityEngine;

public class bullet_script : MonoBehaviour
{
    private TankAgent _parent;
    private int _damage = 40;
    private EnvController envController;
    private bool destroyed = false;
    [SerializeField] private Transform SparkEmitterPrefab;

    private void Start()
    {
        envController = GetComponentInParent<EnvController>();
    }
    public void SetShooter(TankAgent parent)
    {
        _parent = parent;
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

            float reward = collision.collider.gameObject.transform.parent.gameObject == _parent.Target ? 1.0f : 0.1f;
            _parent.AddReward(reward);

            collision.collider.gameObject.transform.parent.GetComponent<ITargetable>().Hit(_damage);


            if(reward == 1.0f)
            {
                Debug.Log("Hit marked!");
                _parent.SetNewTarget();
            }
            else
            {
                Debug.Log("Hit unmarked!");
            }
        }
       else if((collision.gameObject.CompareTag("YellowAgent") && _parent.Team == Team.Yellow) ||
               (collision.gameObject.CompareTag("RedAgent") && _parent.Team == Team.Red))
       {
            //if (_parent.GetComponent<BehaviorParameters>().BehaviorType != BehaviorType.Default)
            //{
            //    Transform emitter = GameObject.Instantiate(SparkEmitterPrefab);
            //    emitter.position = collision.transform.position;
            //}
            _parent.AddReward(-2.0f);
            Debug.Log("Friendly fire!");
            //Debug.Log("Reward: " + -2.0f);
            collision.collider.gameObject.transform.parent.GetComponent<ITargetable>().Hit(_damage);
        }
       else
       {
           _parent.AddReward(-0.05f);
       }

        this.gameObject.SetActive(false);
    }
}
