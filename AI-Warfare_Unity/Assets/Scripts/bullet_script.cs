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

            collision.collider.gameObject.transform.parent.GetComponent<ITargetable>().Hit(_damage);

        }
        else if((collision.gameObject.CompareTag("YellowAgent") && _parent.Team == Team.Yellow) ||
               (collision.gameObject.CompareTag("RedAgent") && _parent.Team == Team.Red))
        {
            //if (_parent.GetComponent<BehaviorParameters>().BehaviorType != BehaviorType.Default)
            //{
            //    Transform emitter = GameObject.Instantiate(SparkEmitterPrefab);
            //    emitter.position = collision.transform.position;
            //}
            _parent.AddRewardToShooter(-0.5f);
            collision.collider.gameObject.transform.parent.GetComponent<ITargetable>().Hit(_damage);
        }
        else
        {
           _parent.AddRewardToShooter(-0.005f);
        }

        this.gameObject.SetActive(false);
    }
}
