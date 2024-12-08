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

        this.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        this.GetComponent<Rigidbody>().AddForce(dir * force);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (destroyed) return;
        destroyed = true;

        if ((collision.gameObject.CompareTag("YellowAgent") && _parent.team == Team.Red) ||
          (collision.gameObject.CompareTag("RedAgent") && _parent.team == Team.Yellow))
       {
            Transform emitter = GameObject.Instantiate(SparkEmitterPrefab);
            emitter.position = collision.transform.position;
            if (_parent.team == Team.Red)
                envController.EnemyDetected(_parent.gameObject, Team.Yellow);
            else
                envController.EnemyDetected(_parent.gameObject, Team.Red);

            _parent.AddReward(0.1f);
            collision.gameObject.GetComponent<TankAgent>().Hit(_damage);
            
       }
       else if((collision.gameObject.CompareTag("YellowAgent") && _parent.team == Team.Yellow) ||
               (collision.gameObject.CompareTag("RedAgent") && _parent.team == Team.Red))
       {
            Transform emitter = GameObject.Instantiate(SparkEmitterPrefab);
            emitter.position = collision.transform.position;
            _parent.AddReward(-0.5f);
            collision.gameObject.GetComponent<TankAgent>().Hit(_damage);
        }
       else
       {
            _parent.AddReward(-0.01f);
       }

        this.gameObject.SetActive(false);
    }
}
