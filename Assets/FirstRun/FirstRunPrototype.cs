using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeadBoat.FirstRun
{
    // Isolated greybox: no normal SaveManager, Photon, ads, purchases or real rewards.
    public sealed class FirstRunPrototype : MonoBehaviour
    {
        public const string CheckpointKey = "DeadBoat.FirstRun.Prototype.v1";
        public FirstRunProgress Progress { get; private set; }
        private CharacterController player;
        private Camera view;
        private Transform boat, target, fuel, held;
        private Vector3 walkOrigin;
        private float pitch, gravity, attackAt, damageAt;
        private string lastCheckpoint;
        private float nextSave;
        private FirstRunStage lastSavedStage = (FirstRunStage)(-1);
        private static bool returnToLobby;
        public static bool ConsumeLobbyReturn() { bool value=returnToLobby;returnToLobby=false;return value; }
        private void ExitToLobby() { Save();PlayerPrefs.Save();returnToLobby=true;SceneManager.LoadScene("LoadingScene"); }
        private bool english;
        private readonly Vector3 fuelPoint = new Vector3(0, .6f, 7), enginePoint = new Vector3(0, 1, 12), wheelPoint = new Vector3(0, 1, 15);
        private readonly System.Collections.Generic.List<Material> materials = new();
        public static FirstRunProgress ReadCheckpoint(string json)
        {
            try { var p = JsonUtility.FromJson<FirstRunProgress>(json); if (p != null && p.Version == FirstRunProgress.ContentVersion &&
                System.Enum.IsDefined(typeof(FirstRunStage), p.Stage) && p.Hits >= 0 && p.Hits <= 2 && p.Health > 0 && p.Health <= 100 &&
                !float.IsNaN(p.Distance) && !float.IsInfinity(p.Distance) && p.Distance >= 0 && p.Distance <= 180) return p; } catch { }
            return new FirstRunProgress();
        }
        private void Start()
        {
            Time.timeScale=1; // The standalone scene owns its gameplay clock.
            Progress = ReadCheckpoint(PlayerPrefs.GetString(CheckpointKey, ""));
            Cube("Shore", new Vector3(0,-.5f,5), new Vector3(20,1,25), new Color(.35f,.55f,.3f));
            Cube("River",new Vector3(0,-1,105),new Vector3(30,.2f,210),new Color(.1f,.5f,.7f));
            Cube("Arrival dock",new Vector3(0,0,195),new Vector3(15,1,12),Color.gray);
            boat = Cube("Boat",new Vector3(0,.4f,14),new Vector3(4,.8f,7),new Color(.45f,.24f,.1f)).transform;
            fuel = Cube("Fuel can",fuelPoint,new Vector3(.5f,.8f,.5f),Color.cyan).transform;
            Cube("Engine",enginePoint,new Vector3(1,1,1),Color.blue);
            Cube("Wheel",wheelPoint,new Vector3(.6f,1,.3f),Color.yellow);
            var random = new DeadBoat.Online.RunRandom(FirstRunProgress.Seed,"first-run-decor-v1",0);
            for(int i=0;i<18;i++) Cube("River marker",new Vector3((i%2==0?-1:1)*random.Range(6,11),1,20+i*9),new Vector3(1,2,1),Color.gray);
            var light = new GameObject("Tutorial sunlight").AddComponent<Light>(); light.type=LightType.Directional; light.transform.rotation=Quaternion.Euler(55,-30,0);
            var go=new GameObject("Prototype player"); player=go.AddComponent<CharacterController>(); player.height=1.8f;player.center=Vector3.up*.9f;
            var eye=new GameObject("View");eye.transform.SetParent(go.transform,false);eye.transform.localPosition=Vector3.up*1.6f;
            view=eye.AddComponent<Camera>();view.tag="MainCamera";view.farClipPlane=300;eye.AddComponent<AudioListener>();
            held=Cube("Held can",Vector3.zero,new Vector3(.2f,.3f,.2f),Color.cyan).transform;Destroy(held.GetComponent<Collider>());held.SetParent(eye.transform,false);held.localPosition=new Vector3(.3f,-.3f,.65f);
            target=Cube("Practice threat",new Vector3(0,1.2f,67),new Vector3(.8f,1.8f,.8f),Color.red).transform;
            boat.position=new Vector3(0,.4f,14+Progress.Distance);
            SetPlayer(Progress.Stage >= FirstRunStage.Sail ? boat.position+new Vector3(0,.9f,1) : Progress.Stage >= FirstRunStage.Board ? wheelPoint + Vector3.back*2 : new Vector3(0,1,1));
            walkOrigin=player.transform.position; Save();
        }
        private GameObject Cube(string name,Vector3 position,Vector3 scale,Color color)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(transform);go.transform.position=position;go.transform.localScale=scale;
            var material=materials.Find(m=>m.color==color);if(material==null){material=new Material(Shader.Find("Standard"));material.color=color;materials.Add(material);}go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }
        private void SetPlayer(Vector3 position) { player.enabled=false;player.transform.position=position;player.enabled=true;gravity=0; }
        private bool Near(Vector3 p) => Vector3.Distance(player.transform.position,p)<3;
        private void Update()
        {
            if(Progress==null)return;
            bool sailing=Progress.Stage>=FirstRunStage.Sail && !Progress.Closed;
            if(Input.GetMouseButton(1)) { player.transform.Rotate(0,Input.GetAxis("Mouse X")*2,0);pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*2,-65,65);view.transform.localRotation=Quaternion.Euler(pitch,0,0); }
            if(!sailing && !Progress.Closed)
            {
                gravity=player.isGrounded?-2:gravity+Physics.gravity.y*Time.deltaTime;
                player.Move((player.transform.forward*Input.GetAxis("Vertical")*4+player.transform.right*Input.GetAxis("Horizontal")*4+Vector3.up*gravity)*Time.deltaTime);
                if(player.transform.position.y < -4) SetPlayer(new Vector3(0,1,1));
            }
            if(Progress.Stage==FirstRunStage.Walk && Vector3.Distance(walkOrigin,player.transform.position)>3) Progress.Advance(FirstRunStage.Walk);
            if(Input.GetKeyDown(KeyCode.E)) Interact();
            if(Input.GetKeyDown(KeyCode.Tab)) Bag();
            if(Progress.Stage==FirstRunStage.Sail || Progress.Stage==FirstRunStage.Return) Progress.Sail(Mathf.Max(0,Input.GetAxis("Vertical"))*10*Time.deltaTime);
            if(sailing) { boat.position=new Vector3(0,.4f,14+Progress.Distance);SetPlayer(boat.position+new Vector3(0,.9f,1)); }
            if(Progress.Stage==FirstRunStage.Threat)
            {
                target.position=boat.position+new Vector3(0,1.1f,3);
                if(Input.GetMouseButtonDown(0) && Time.unscaledTime>=attackAt && Vector3.Dot(view.transform.forward,(target.position-view.transform.position).normalized)>.35f) {attackAt=Time.unscaledTime+.5f;Progress.Hit();}
                if(Time.unscaledTime>=damageAt) {damageAt=Time.unscaledTime+2;Progress.Damage(5);}
            }
            target.gameObject.SetActive(Progress.Stage==FirstRunStage.Threat);
            fuel.gameObject.SetActive(Progress.Stage<=FirstRunStage.PickUp);
            held.gameObject.SetActive(Progress.Stage==FirstRunStage.Stash || Progress.Stage==FirstRunStage.Fuel);
            Save();
        }
        public void Interact()
        {
            if(Progress.Stage==FirstRunStage.PickUp && Near(fuelPoint)) Progress.Advance(FirstRunStage.PickUp);
            else if(Progress.Stage==FirstRunStage.Fuel && Near(enginePoint)) Progress.Advance(FirstRunStage.Fuel);
            else if(Progress.Stage==FirstRunStage.Board && Near(wheelPoint)) Progress.Advance(FirstRunStage.Board);
        }
        public void Bag() { if(Progress.Stage==FirstRunStage.Stash)Progress.Advance(FirstRunStage.Stash);else if(Progress.Stage==FirstRunStage.Retrieve)Progress.Advance(FirstRunStage.Retrieve); }
        private void Save() { if(Time.unscaledTime<nextSave && lastSavedStage==Progress.Stage)return;nextSave=Time.unscaledTime+.5f;lastSavedStage=Progress.Stage;string value=JsonUtility.ToJson(Progress);if(value==lastCheckpoint)return;lastCheckpoint=value;PlayerPrefs.SetString(CheckpointKey,value); }
        private void OnApplicationPause(bool pause) { if(pause) PlayerPrefs.Save(); }
        private void OnApplicationQuit() { PlayerPrefs.Save(); }
        private void OnDestroy() { foreach(var m in materials) if(m!=null)Destroy(m); }
        private static readonly string[] ru={"Подойди к голубой канистре — WASD","Подбери канистру — E","Убери канистру в сумку — Tab","Достань канистру — Tab","Подойди к синему двигателю, заправь — E","Подойди к жёлтому рулю — E","Вперёд по реке — W","Посмотри на угрозу и ударь дважды — ЛКМ","Доплыви до причала — W","Первый заплыв завершён!","Обучение пропущено"};
        private static readonly string[] en={"Walk to the blue fuel can — WASD","Pick up the can — E","Put the can in your bag — Tab","Take the can out — Tab","Go to the blue engine and refuel — E","Go to the yellow wheel — E","Sail downriver — W","Look at the threat and hit twice — LMB","Sail to the dock — W","First voyage completed!","Tutorial skipped"};
        private void OnGUI()
        {
            if(Progress==null)return;
            GUI.Box(new Rect(12,12,Mathf.Min(Screen.width-24,680),112),(english?en:ru)[(int)Progress.Stage]+"\n"+(english?"Hold RMB to look":"Удерживай ПКМ, чтобы осмотреться")+" | HP "+Progress.Health);
            if(GUI.Button(new Rect(20,85,85,26),english?"Русский":"English"))english=!english;
            if(!Progress.Closed && GUI.Button(new Rect(115,85,190,26),english?"Skip to lobby":"Пропустить, в лобби")) {Progress.Skip();ExitToLobby();}
            if(Progress.Closed && GUI.Button(new Rect(20,135,260,40),english?"Go to lobby":"Перейти в лобби")) {ExitToLobby();}
        }
    }
}
