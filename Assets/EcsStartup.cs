using System.Collections;
using Leopotam.Ecs;
using UnityEngine;
using System;
using DG.Tweening;

namespace Client {
    sealed class EcsStartup : MonoBehaviour {
        public GameUI gameUI = null;
        public Prefabs prefabs = null;

        EcsWorld _world;
        EcsSystems _systems;

        void Start () {
            if (!PlayerPrefs.HasKey("level")){
                PlayerPrefs.SetInt("level", 1);
                PlayerPrefs.Save();
            }

            _world = new EcsWorld ();
            _systems = new EcsSystems (_world);
#if UNITY_EDITOR
            Leopotam.Ecs.UnityIntegration.EcsWorldObserver.Create (_world);
            Leopotam.Ecs.UnityIntegration.EcsSystemsObserver.Create (_systems);
#endif
            _systems
            .Add (new SetLevelSystem()) 
            .Add (new UpdateTimerSystem())

            .Add (new HandlePauseSystem())
            .Add (new HandleStopSystem())

            .Add (new RequestUnitSystem())
            .Add (new SpawnUnitSystem())
            .OneFrame<SpawnRequest>()

            .Add (new RequestBombSystem())
            .Add (new SpawnBombSystem())
            .OneFrame<SpawnBombRequest>()

            .Add (new DetonateBombRequestSystem())
            .Add (new DetonateBombSystem())
            .OneFrame<DetonateBombRequest>()

            .Add (new AttackUnitSystem())
            .OneFrame<AttackUnitRequest>()

            .Add (new DetectFallenUnitSystem())

            .Add (new DetectLosingSystem())
            .Add (new DetectWinningSystem())

            .Inject (gameUI)
            .Inject (prefabs)
                // .Inject (new NavMeshSupport ())
            .Init ();
        }

        void Update () {
            _systems?.Run ();
        }

        void OnDestroy () {
            if (_systems != null) {
                _systems.Destroy ();
                _systems = null;
                _world.Destroy ();
                _world = null;
            }
        }

        public void restart()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(1);
        }

        public void nextLevel()
        {

            PlayerPrefs.SetInt("level", PlayerPrefs.GetInt("level") + 1);
            PlayerPrefs.Save();
            restart();
        }
    }

    /*
        before starting playing, the level is to be displayed
    */
    internal class SetLevelSystem : IEcsInitSystem
    {
        public GameUI _gameUI = null;
        public void Init()
        {
            _gameUI.levelText.text = "LEVEL " + PlayerPrefs.GetInt("level");
        }
    }

    /*
        there are 25 seconds for each level,
        and the timer has to be updated and stopped when the time os over
    */
    internal class UpdateTimerSystem : IEcsRunSystem
    {
        public GameUI _gameUI = null;
        public void Run()
        {
            if (_gameUI.time > 0)
            {
                _gameUI.time -= Time.deltaTime;
            }
            int time = (int)Math.Round(_gameUI.time);

            _gameUI.timerText.text = time + "";
        }
    }

    /*
        the player might want to press "pause" button
        then everything has to freeze
    */
    internal class HandlePauseSystem : IEcsRunSystem
    {
        public GameUI _gameUI = null;

        public void Run()
        {
            if (_gameUI.paused)
            {
                Time.timeScale = 0;
                _gameUI.pauseCanvas.SetActive(true);
            } 
            else 
            {
                Time.timeScale = 1;
                _gameUI.pauseCanvas.SetActive(false);
            }
        }
    }

    /*
        as the game is over (win or lose),
        eveyrhting has to freeze as well
    */
    internal class HandleStopSystem : IEcsRunSystem
    {
        public GameUI _gameUI = null;

        public void Run()
        {
            if (_gameUI.stopped)
            {
                Time.timeScale = 0;
            } 
        }
    }

    /*
        every second a request to spawn a unit is requested
    */
    internal class RequestUnitSystem : IEcsRunSystem
    {
        private readonly EcsWorld _world = null;
        private GameUI _gameUI = null;

        private float _time = 1f;

        public void Run()
        {
            _time += Time.deltaTime;
            if (_time >= 1f && _gameUI.time > 0)
            {
                var entity = _world.NewEntity();
                ref var SpawnRequest = ref entity.Get<SpawnRequest>();
                _time = 0.0f;
            }
        }
    }

    /*
        the dollowing system handles all the requests to spawn a unit
    */
    internal class SpawnUnitSystem : IEcsRunSystem
    {
        private Prefabs _prefabs = null;

        private readonly EcsWorld _world = null;
        private EcsFilter<SpawnRequest> _filter = null;

        public void Run()
        {
            foreach(var it in _filter)
            {
                for (int i = 0; i < PlayerPrefs.GetInt("level"); i++){
                    var entity = _world.NewEntity();
                    ref var unit = ref entity.Get<UnitComponent>();

                    float leftEdgeX = _prefabs.Beginning.transform.position.x; //leftmost point for a unit
                    float rightEdgeX = _prefabs.Ending.transform.position.x; //rightmost point for a unit

                    float nextX = UnityEngine.Random.Range(leftEdgeX, rightEdgeX); //random X position
                    float nextY = _prefabs.Beginning.transform.position.y + UnityEngine.Random.Range(0, 1);
                    float nextZ = _prefabs.Beginning.transform.position.z + UnityEngine.Random.Range(-0.2f, 0.2f);

                    Vector3 position = new Vector3(nextX, nextY, nextZ);

                    unit.gameObject = UnityEngine.Object.Instantiate(_prefabs.unitPrefab, position, Quaternion.identity);
                    unit.setHealth(1f); //setting fll health

                    Rigidbody rb = unit.gameObject.GetComponent<Rigidbody>();
                    rb.AddForce(0f, 0f, -75f, ForceMode.Force); //adding force so that the units will move 
                
                }
            }
        }
    }

    /*
        every time, the player clicks or taps, 
        a request for a bomb creation should appear
    */
    internal class RequestBombSystem : IEcsRunSystem
    {
        private readonly EcsWorld _world = null;
        private GameUI _gameUI = null;

        public void Run()
        {
            if (Input.GetMouseButtonDown(0) && !_gameUI.paused && _gameUI.canThrow)
            {
                var entity = _world.NewEntity();
                ref var SpawnBombRequest = ref entity.Get<SpawnBombRequest>();

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); //throwing a ray
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit)) //getting position where the ray hits
                {
                    SpawnBombRequest.position = hit.point; //srequesting for a bomb there
                }
            }
            _gameUI.canThrow = true;
        }
    }

    /*
        the dollowing system handles all the requests to spawn a bomb
    */
    internal class SpawnBombSystem : IEcsRunSystem
    {
        private Prefabs _prefabs = null;
        private EcsFilter<SpawnBombRequest> _filter = null;

        public void Run()
        {
            foreach(var it in _filter)
            {
                ref var entity = ref _filter.GetEntity(it);
                ref var bomb = ref entity.Get<BombComponent>();

                Vector3 position = _filter.Get1(it).position + new Vector3(0f, 4f, 0f); //a position above the requested position

                bomb.gameObject = UnityEngine.Object.Instantiate(_prefabs.bombPrefab, position, Quaternion.identity);

                Rigidbody rb = bomb.gameObject.GetComponent<Rigidbody>();
                rb.AddForce(0f, -500f, 0f, ForceMode.Acceleration); //adding some force so that it would fall faster
            }
        }

    }

    /*
        whenever a bomb is too close to a unit or the plane,
        a corresponding request should be made
    */
    internal class DetonateBombRequestSystem : IEcsRunSystem
    {
        private EcsFilter<BombComponent> _bombs_filter = null;
        private EcsFilter<UnitComponent> _units_filter = null;

        public Prefabs _prefabs = null;

        public void Run()
        {
            foreach(var bomb_it in _bombs_filter)
            {
                ref var entity = ref _bombs_filter.GetEntity(bomb_it);

                if (_bombs_filter.Get1(bomb_it).gameObject.transform.position.y <= 0.35f) //if it is too low, detonate
                {
                    ref var DetonateBombRequest = ref entity.Get<DetonateBombRequest>(); 
                    DetonateBombRequest.bombComponent = _bombs_filter.Get1(bomb_it);
                } 
                else 
                {
                    foreach (var unit_it in _units_filter)
                    {
                        ref var unit_entity = ref _units_filter.GetEntity(unit_it);

                        Vector3 unit_position = _units_filter.Get1(unit_it).gameObject.transform.position;
                        Vector3 bomb_position = _bombs_filter.Get1(bomb_it).gameObject.transform.position;

                        if (Vector3.Distance(unit_position, bomb_position) <= 0.35f) //if it is too close to some of the units
                        {
                            ref var DetonateBombRequest = ref entity.Get<DetonateBombRequest>();
                            DetonateBombRequest.bombComponent = _bombs_filter.Get1(bomb_it);
                        }
                    }
                }
            }
        }
    }

    /*
        the dollowing system handles all the requests to detonate a bomb
    */
    internal class DetonateBombSystem : IEcsRunSystem
    {
        private EcsFilter<DetonateBombRequest> _bombs_filter = null;
        private EcsFilter<UnitComponent> _units_filter = null;

        public Prefabs _prefabs = null;

        public void Run()
        {
            foreach(var bomb_it in _bombs_filter){
                ref var entity = ref _bombs_filter.GetEntity(bomb_it);
                ref var bomb = ref _bombs_filter.Get1(bomb_it).bombComponent;

                UnityEngine.Object.Destroy(bomb.gameObject); //destroying a bomb

                Vector3 exp_position = new Vector3( //explosion position
                    bomb.gameObject.transform.position.x,
                    0, 
                    bomb.gameObject.transform.position.z
                );
                
                GameObject _exp = UnityEngine.Object.Instantiate( //explosion
                    _prefabs.expPrefab,
                    exp_position, 
                    bomb.gameObject.transform.rotation
                );

                UnityEngine.Object.Destroy(_exp, 5); //destroying explosion in 5 seconds

                foreach(var unit_it in _units_filter) 
                {
                    float distance = Vector3.Distance( //distance to the units
                        _units_filter.Get1(unit_it).gameObject.transform.position,
                        bomb.gameObject.transform.position
                    );

                    if (distance <= 1f) //if it is too close we reduce its health value
                    {
                        ref var unit_entity = ref _units_filter.GetEntity(unit_it);
                        ref var AttackUnitRequest = ref unit_entity.Get<AttackUnitRequest>(); //requesting to attack the unit

                        AttackUnitRequest.unitComponent = _units_filter.Get1(unit_it);
                        AttackUnitRequest.intencity = 1f - distance; //reducing depending on the distance
                    }
                }

                entity.Destroy(); //destoring the entity
            }
        }
    }

    /*
        the dollowing system handles all the requests to attack a unit
    */
    internal class AttackUnitSystem : IEcsRunSystem
    {
        private EcsFilter<AttackUnitRequest> _filter = null;
        private Prefabs _prefabs = null;

        public void Run()
        {
            foreach(var it in _filter)
            {

                ref var entity = ref _filter.GetEntity(it);
                ref var unit = ref _filter.Get1(it).unitComponent;

                unit.deacreaseHealth(_filter.Get1(it).intencity); //decreasing the health

                unit.gameObject.transform.DOShakePosition(0.1f, 0.1f, 20, 0, false, true); //shaking it

                if (unit.getHealth() <= 0) //if the health is less than zero
                {
                    DOTween.Kill(unit.gameObject.transform); //we kill the tween
                    UnityEngine.Object.Destroy(unit.gameObject); 

                    GameObject _unit_exp = UnityEngine.Object.Instantiate( //adding explosion
                        _prefabs.unitExpPrefab, 
                        unit.gameObject.transform.position, 
                        unit.gameObject.transform.rotation
                    );

                    UnityEngine.Object.Destroy(_unit_exp, 5); //destroing explosion in 5 seconds
                    entity.Destroy(); //destroing the unit entity
                }
            }
        }
    }

    /*
        the follwoing system detects when a unit is on the red zone 
    */
    internal class DetectLosingSystem : IEcsRunSystem
    {
        private EcsFilter<UnitComponent> _filter = null;
        private GameUI _gameUI = null;

        public void Run()
        {
            foreach(var it in _filter)
            {
                ref var unit = ref _filter.Get1(it);

                if (unit.gameObject.transform.position.z <= -1.97f)
                {
                    unit.gameObject.GetComponent<MeshRenderer> ().material = _gameUI.redMaterial; //changing its color
                    _gameUI.loseCanvas.SetActive(true); //displaying the menu
                    _gameUI.canThrow = false; //we cannot throw a bomb from this moment
                    _gameUI.stopped = true; //the game should be stopped
                }
            }
        }
    }

    /*
        the follwoing system detects when we have destroyed
        all the units
    */
    internal class DetectWinningSystem : IEcsRunSystem
    {
        private EcsFilter<UnitComponent> _filter = null;
        private GameUI _gameUI = null;

        public void Run()
        {
            int count = 0;
            foreach(var it in _filter){
                count++;
                if (count == 1)
                {
                    break;
                }
            }

            if (_gameUI.time <= 0 && count == 0)
            {
                _gameUI.winCanvas.SetActive(true); //displaying the menu
                _gameUI.canThrow = false; //we cannot throw a bomb from this moment
                _gameUI.stopped = true; //the game should be stoppped
            }
        }
    }

    /*
        detects when we lost a unit
        we should simply stop caring about it 
    */
    internal class DetectFallenUnitSystem : IEcsRunSystem
    {
        private EcsFilter<UnitComponent> _filter = null;

        public void Run()
        {
            foreach(var it in _filter)
            {
                ref var entity = ref _filter.GetEntity(it);
                ref var unit = ref _filter.Get1(it);

                if (unit.gameObject.transform.position.y <= -5f)
                {
                    UnityEngine.Object.Destroy(unit.gameObject); //destroying a unit that has fallen away
                    entity.Destroy();
                }
            }
        }
    }
}