using UnityEngine;
using Player.Core;
using Player.States;
using Player.Combat.Melee;
using Player.Combat.Ranged;
using Player.Feel;
using PurrNet;
using PurrNet.Prediction.StateMachine;

namespace Player
{
    [AddComponentMenu("Player/Player Setup")]
    public class PlayerSetup : MonoBehaviour
    {
        [Header("核心能力")]
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private PlayerMovementCore movementCore;
        [SerializeField] private ControlAuthority controlAuthority;

        [Header("状态机（子物体）")]
        [SerializeField] private PredictedStateMachine stateMachine;
        [SerializeField] private MovementStateNode movementState;
        [SerializeField] private DashStateNode dashState;
        [SerializeField] private MeleeCombatSystem meleeCombatSystem;
        [SerializeField] private MeleeStateNode meleeState;
        [SerializeField] private RangedCombatSystem rangedCombatSystem;
        [SerializeField] private RangedStateNode rangedState;

        [Header("Feel")]
        [SerializeField] private MovementFeelController feelController;

        [Header("自动化设置")]
        [SerializeField] private bool autoSetupComponents = true;

        private void Awake()
        {
            if (autoSetupComponents)
                SetupComponents();

            ValidateSetup();
        }

        [ContextMenu("Setup Components")]
        public void SetupComponents()
        {
            var childObjects = CreateChildObjects();

            EnsureComponent<CharacterController>();
            EnsureComponent<PurrNet.Prediction.PredictedTransform>();

            inputHandler = EnsureComponent<PlayerInputHandler>();
            movementCore = EnsureComponent<PlayerMovementCore>();
            controlAuthority = EnsureComponent<ControlAuthority>();

            feelController = EnsureComponent<MovementFeelController>();
            EnsureComponent<CoreMovementFeel>();

            GameObject logicObject = childObjects["Logic"];
            stateMachine = EnsureOrMoveComponentToChild<PredictedStateMachine>(logicObject);
            movementState = EnsureOrMoveComponentToChild<MovementStateNode>(logicObject);
            dashState = EnsureOrMoveComponentToChild<DashStateNode>(logicObject);
            meleeState = EnsureOrMoveComponentToChild<MeleeStateNode>(logicObject);
            rangedState = EnsureOrMoveComponentToChild<RangedStateNode>(logicObject);

            GameObject systemsObject = childObjects["Systems"];
            meleeCombatSystem = EnsureOrMoveComponentToChild<MeleeCombatSystem>(systemsObject);
            rangedCombatSystem = EnsureOrMoveComponentToChild<RangedCombatSystem>(systemsObject);

            var meleeCombo = EnsureOrMoveComponentToChild<MeleeComboStateMachine>(systemsObject);
            meleeCombatSystem.ComboStateMachine = meleeCombo;

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            movementState.Initialize(movementCore, controlAuthority);
            dashState.Initialize(movementCore, controlAuthority);
            meleeState.Initialize(movementCore, meleeCombatSystem, controlAuthority);
            rangedState.Initialize(movementCore, rangedCombatSystem, controlAuthority);

            if (inputHandler != null)
                controlAuthority.SetProvider(inputHandler);
        }

        private System.Collections.Generic.Dictionary<string, GameObject> CreateChildObjects()
        {
            var dict = new System.Collections.Generic.Dictionary<string, GameObject>();
            string[] names = { "Logic", "Systems" };
            foreach (var n in names)
            {
                Transform t = transform.Find(n);
                if (t == null)
                {
                    GameObject go = new GameObject(n);
                    go.transform.SetParent(transform);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;
                    dict[n] = go;
                }
                else dict[n] = t.gameObject;
            }
            return dict;
        }

        private T EnsureOrMoveComponentToChild<T>(GameObject childObject) where T : Component
        {
            var comp = childObject.GetComponent<T>();
            if (comp != null) return comp;

            var rootComp = GetComponent<T>();
            if (rootComp != null)
            {
                comp = childObject.AddComponent<T>();
                // 简单复制
                var fields = typeof(T).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                foreach (var f in fields) { try { f.SetValue(comp, f.GetValue(rootComp)); } catch { } }
                DestroyImmediate(rootComp);
                return comp;
            }
            return childObject.AddComponent<T>();
        }

        private T EnsureComponent<T>() where T : Component
        {
            var comp = GetComponent<T>();
            return comp != null ? comp : gameObject.AddComponent<T>();
        }

        [ContextMenu("Validate Setup")]
        public void ValidateSetup()
        {
            bool allValid = true;
            void CheckHierarchy(Component comp, string name, string expectedObject)
            {
                if (comp == null || comp.gameObject.name != expectedObject || comp.transform.parent != transform)
                {
                    Debug.LogWarning($"{name} is not correctly placed on {expectedObject}");
                    allValid = false;
                }
            }

            CheckHierarchy(stateMachine, "PredictedStateMachine", "Logic");
            CheckHierarchy(movementState, "MovementStateNode", "Logic");
            CheckHierarchy(dashState, "DashStateNode", "Logic");
            CheckHierarchy(meleeState, "MeleeStateNode", "Logic");
            CheckHierarchy(rangedState, "RangedStateNode", "Logic");

            if (allValid) Debug.Log("✅ Player setup validation passed!");
        }

        public PlayerMovementCore MovementCore => movementCore;
        public PredictedStateMachine StateMachine => stateMachine;
    }
}
