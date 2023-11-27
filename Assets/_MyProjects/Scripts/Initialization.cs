using UnityEngine;

public class Initialization : MonoBehaviour
{
    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        BoomDaoManager.Instance.Login(OnFinishedLogin);
    }

    private void OnFinishedLogin()
    {
        TestUI.Instance.ManageButtons(true);
    }
}
