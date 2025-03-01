using UnityEngine.SceneManagement;
using UnityEngine;

public class InstanciateSceneController : MonoBehaviour {
    public static InstanciateSceneController Instance;

    public GameObject client;
    public GameObject server;
    public PhysicsScene clientPhysicsScene;
    public PhysicsScene serverPhysicsScene;

    private void Awake() {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;

        if (Instance != null && Instance != this) {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
    }

    public void OnServerInstance() {
        //SystemInfo.graphicsDeviceName == null
        var serverScene = SceneManager.LoadScene("PhysicsTest", new LoadSceneParameters() { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D });
        serverPhysicsScene = serverScene.GetPhysicsScene();
        Instantiate(server, serverScene);
    }

    public void OnClientInstance() {
        var clientScene = SceneManager.LoadScene("PhysicsTest", new LoadSceneParameters() { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D });
        clientPhysicsScene = clientScene.GetPhysicsScene();
        Instantiate(client, clientScene);
    }
}
