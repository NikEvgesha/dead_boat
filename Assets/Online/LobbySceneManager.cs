using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadBoat.Online
{
    // The lobby already runs in Unity's active scene and has no network scene objects.
    // Avoid initializing Fusion's Addressables-aware scene manager for this room.
    public sealed class LobbySceneManager : MonoBehaviour, INetworkSceneManager
    {
        public bool IsBusy => false;
        public Scene MainRunnerScene => SceneManager.GetActiveScene();

        public void Initialize(NetworkRunner runner) { }
        public void Shutdown() { }
        public bool IsRunnerScene(Scene scene) => scene == MainRunnerScene;

        public bool TryGetPhysicsScene2D(out PhysicsScene2D physicsScene)
        {
            physicsScene = MainRunnerScene.GetPhysicsScene2D();
            return physicsScene.IsValid();
        }

        public bool TryGetPhysicsScene3D(out PhysicsScene physicsScene)
        {
            physicsScene = MainRunnerScene.GetPhysicsScene();
            return physicsScene.IsValid();
        }

        public void MakeDontDestroyOnLoad(GameObject gameObject) => DontDestroyOnLoad(gameObject);
        public bool MoveGameObjectToScene(GameObject gameObject, SceneRef sceneRef) => false;
        public NetworkSceneAsyncOp LoadScene(SceneRef sceneRef, NetworkLoadSceneParameters parameters) => default;
        public NetworkSceneAsyncOp UnloadScene(SceneRef sceneRef) => default;
        public SceneRef GetSceneRef(GameObject gameObject) => GetSceneRef(gameObject.scene.path);

        public SceneRef GetSceneRef(string path)
        {
            int index = SceneUtility.GetBuildIndexByScenePath(path);
            return index >= 0 ? SceneRef.FromIndex(index) : default;
        }

        public bool OnSceneInfoChanged(NetworkSceneInfo sceneInfo, NetworkSceneInfoChangeSource source) => true;
    }
}
