using UnityEngine;

public class TargetScript : MonoBehaviour
{
    private TargetPracticeController targetController;

    public void setController(TargetPracticeController controller)
    {
        this.targetController = controller;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (targetController && collision.gameObject.tag == "Bullet")
        {
            targetController.HandleTargetHit();
        }

    }
}
