using UnityEngine;

public class GoToTrainerController : MonoBehaviour
{
    private EnvController _envController;

    private float _rearrangeTimer = 60.0f;

    void Awake()
    {
        _envController = GetComponentInParent<EnvController>();
    }
    void Start()
    {
        Rearrange();
    }

    void Rearrange()
    {
        this.transform.localPosition = new Vector3(Random.Range(-300, 300), 3.0f, Random.Range(-300, 300));
        foreach (VehicleAgent agent in _envController.AgentsList)
        {
            if(agent.AgentType == AgentType.Tank)
            {
                ((TankAgent) agent).SetGoToPoint(this.transform.localPosition);
            }
        }

        _rearrangeTimer = 60.0f;
    }

    void Update()
    {
        _rearrangeTimer -= Time.deltaTime;
        if(_rearrangeTimer < 0.0f)
            Rearrange();
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "RedAgent" || other.gameObject.tag == "YellowAgent")
        {
            Rearrange();
        }
    }

}
