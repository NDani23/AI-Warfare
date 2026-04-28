using UnityEngine;

public class GoToTrainerController : MonoBehaviour
{
    private EnvController _envController;

    private float _rearrangeTimer = 120.0f;
    private float _stayCountDown = 5.0f;

    private bool _isAgentInTarget = false;

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
        Vector3 localCandidatePos = new Vector3(Random.Range(-300, 300), 3.0f, Random.Range(-300, 300));

        // for(int i = 0; i < 100; i++)
        // {
        //     if(!Physics.CheckSphere(this.transform.parent.TransformPoint(localCandidatePos), 10.0f, ~LayerMask.GetMask("Ground")))
        //     {
        //         break;
        //     }
        //     localCandidatePos = new Vector3(Random.Range(-300, 300), 3.0f, Random.Range(-300, 300));
        // }

        this.transform.localPosition = localCandidatePos;

        foreach (VehicleAgent agent in _envController.AgentsList)
        {
            if(agent.AgentType == AgentType.Tank)
            {
                ((TankAgent) agent).SetGoToPoint(this.transform.localPosition);
            }
        }

        // _stayCountDown = 5.0f;
        // _isAgentInTarget = false;

        _rearrangeTimer = 120.0f;
    }

    void Update()
    {
        _rearrangeTimer -= Time.deltaTime;
        if(_rearrangeTimer < 0.0f)
            Rearrange();

        // if(_isAgentInTarget)
        // {
        //     _stayCountDown -= Time.deltaTime;
        //     if(_stayCountDown <= 0.0f)
        //     {
        //         // Debug.Log("Agent stayed in target for 5 seconds, rearranging...");
        //         foreach (VehicleAgent agent in _envController.AgentsList)
        //         {
        //             if(agent.AgentType == AgentType.Tank)
        //             {
        //                 ((TankAgent) agent).AddReward(5.0f);
        //             }
        //         }
        //         Debug.Log("Target reached!");
        //         Rearrange();
        //     }
        // }
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "RedAgent" || other.gameObject.tag == "YellowAgent")
        {
            if(other.transform.parent.GetComponent<VehicleAgent>() == null)
                return;
            
            //_isAgentInTarget = true;

            other.transform.parent.GetComponent<VehicleAgent>()?.AddReward(5.0f);
            Debug.Log("Target reached!");
            Rearrange();
        }
    }

    // void OnTriggerExit(Collider other)
    // {
    //     if(other.gameObject.tag == "RedAgent" || other.gameObject.tag == "YellowAgent")
    //     {
    //         if(other.transform.parent.GetComponent<VehicleAgent>() == null)
    //             return;
            
    //         _isAgentInTarget = false;
    //         _stayCountDown = 5.0f;
    //     }
    // }

}
