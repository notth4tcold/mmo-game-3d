using UnityEngine.SceneManagement;
using UnityEngine;

public class InstanciateSceneController : MonoBehaviour {
    public static InstanciateSceneController Instance;

    public GameObject client;
    public GameObject server;
    public PhysicsScene clientPhysicsScene;
    public PhysicsScene serverPhysicsScene;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
    }

    void Start() {
        //if (SystemInfo.graphicsDeviceName != null) {}

        var serverScene = SceneManager.LoadScene("PhysicsTest", new LoadSceneParameters() { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D });
        serverPhysicsScene = serverScene.GetPhysicsScene();
        Instantiate(server, serverScene);

        var clientScene = SceneManager.LoadScene("PhysicsTest", new LoadSceneParameters() { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D });
        clientPhysicsScene = clientScene.GetPhysicsScene();
        Instantiate(client, clientScene);
    }
}
