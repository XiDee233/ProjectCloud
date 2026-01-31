using UnityEngine;
using PurrNet.Prediction;
using Player.Combat.Melee;

namespace Player.Core
{
    public enum InputEventType { Tap, Release }

    public class PredictedPlayerInputCollector : PredictedIdentity<PredictedPlayerInputCollector.CollectedInput, PredictedPlayerInputCollector.CollectedInput>
    {
        private ControlAuthority controlAuthority;

        [Header("Buffering"),ReadOnly]
        [SerializeField] private uint bufferMaxAgeTicks = 10;

        public ref readonly CollectedInput InputState => ref currentState;

        public ComboInputBuffer ComboBuffer { get; private set; }

        public float NowSeconds => predictionManager != null ? predictionManager.localTick * predictionManager.tickDelta : 0f;

        [Header("Combo Buffering")]
        [SerializeField] private ComboTree comboTree;


        private void Awake()
        {
            if (controlAuthority == null) controlAuthority = GetComponentInParent<ControlAuthority>();

            ComboBuffer = new ComboInputBuffer();
       
                ComboBuffer.MaxCount = comboTree.bufferMaxCount;
                ComboBuffer.MaxAgeSeconds = comboTree.bufferMaxAgeSeconds;
            
        }

        protected override void GetFinalInput(ref CollectedInput input)
        {
            var p = controlAuthority?.CurrentProvider;
            if (p == null)
            {
                input.ResetPhysicalState();
                return;
            }

            input.movement = p.Movement;
            input.aimWorldDirection = p.AimWorldDirection;
            input.primaryAttackIsPressed = p.PrimaryAttack.isPressed;
            input.secondaryAttackIsPressed = p.SecondaryAttack.isPressed;
            input.dashIsPressed = p.Dash.isPressed;
            input.grappleIsPressed = p.Grapple.isPressed;
        }

        protected override void Simulate(CollectedInput input, ref CollectedInput state, float delta)
        {
            ulong currentTick = predictionManager.localTick;

            // 1. 物理状态采样
            state.movement = input.movement;
            state.aimWorldDirection = input.aimWorldDirection;
            state.primaryAttackIsPressed = input.primaryAttackIsPressed;
            state.secondaryAttackIsPressed = input.secondaryAttackIsPressed;
            state.dashIsPressed = input.dashIsPressed;
            state.grappleIsPressed = input.grappleIsPressed;

            // 2. 边沿检测 & 优先级处理
            bool dashTapped = !state.prevDashIsPressed && state.dashIsPressed;
            bool primaryAttackTapped = !state.prevPrimaryAttackIsPressed && state.primaryAttackIsPressed;
            bool secondaryAttackTapped = !state.prevSecondaryAttackIsPressed && state.secondaryAttackIsPressed;

            // Dash 优先：如果本 tick 有 dash，则攻击输入不被推入 ComboBuffer
            bool canProcessAttackInputs = !dashTapped;

            // 3. 原始事件缓冲（用于 Dash 等非战斗系统）
            DetectAndBufferEdge(ref state, InputActionType.Dash, state.prevDashIsPressed, state.dashIsPressed, currentTick);
            // ...可以为 Grapple 等添加更多

            // 4. 战斗输入路由：将攻击事件翻译并推入 ComboBuffer
            if (canProcessAttackInputs && ComboBuffer != null)
            {
                float currentTime = currentTick * predictionManager.tickDelta;
                if (primaryAttackTapped)
                {
                    ComboBuffer.Push(new ComboInputEvent(ComboInputEventType.Tap, ComboInputSource.PrimaryAttack, currentTime, state.movement));
                }
                if (secondaryAttackTapped)
                {
                    ComboBuffer.Push(new ComboInputEvent(ComboInputEventType.Tap, ComboInputSource.SecondaryAttack, currentTime, state.movement));
                }
            }

            // 5. 更新上一帧状态（用于下一次边沿检测）
            state.prevPrimaryAttackIsPressed = state.primaryAttackIsPressed;
            state.prevSecondaryAttackIsPressed = state.secondaryAttackIsPressed;
            state.prevDashIsPressed = state.dashIsPressed;
            state.prevGrappleIsPressed = state.grappleIsPressed;

            // 6. 清理过期的缓冲
            state.CleanupEvents(currentTick, bufferMaxAgeTicks);
            if (ComboBuffer != null)
            {
                float currentTime = currentTick * predictionManager.tickDelta;
                ComboBuffer.Cleanup(currentTime);
            }
        }

        private static void DetectAndBufferEdge(ref CollectedInput state, InputActionType action, bool wasPressed, bool isPressed, ulong currentTick)
        {
            if (!wasPressed && isPressed)
                state.AddEvent(action, InputEventType.Tap, currentTick);
            else if (wasPressed && !isPressed)
                state.AddEvent(action, InputEventType.Release, currentTick);
        }

        public struct CollectedInput : IPredictedData<CollectedInput>
        {
            private const int EVENT_BUFFER_CAPACITY = 8;

            public Vector2 movement;
            public Vector3 aimWorldDirection;

            public bool primaryAttackIsPressed;
            public bool secondaryAttackIsPressed;
            public bool dashIsPressed;
            public bool grappleIsPressed;

            // 用于确定性边沿检测（存入预测状态，回滚可还原）
            public bool prevPrimaryAttackIsPressed;
            public bool prevSecondaryAttackIsPressed;
            public bool prevDashIsPressed;
            public bool prevGrappleIsPressed;

            // 固定容量事件缓冲（不使用 unsafe/fixed），用 3 组并行数组表示
            public int eventHead;
            public int eventCount;

            public InputActionType eventAction0;
            public InputActionType eventAction1;
            public InputActionType eventAction2;
            public InputActionType eventAction3;
            public InputActionType eventAction4;
            public InputActionType eventAction5;
            public InputActionType eventAction6;
            public InputActionType eventAction7;

            public InputEventType eventType0;
            public InputEventType eventType1;
            public InputEventType eventType2;
            public InputEventType eventType3;
            public InputEventType eventType4;
            public InputEventType eventType5;
            public InputEventType eventType6;
            public InputEventType eventType7;

            public ulong eventTick0;
            public ulong eventTick1;
            public ulong eventTick2;
            public ulong eventTick3;
            public ulong eventTick4;
            public ulong eventTick5;
            public ulong eventTick6;
            public ulong eventTick7;

            public bool eventConsumed0;
            public bool eventConsumed1;
            public bool eventConsumed2;
            public bool eventConsumed3;
            public bool eventConsumed4;
            public bool eventConsumed5;
            public bool eventConsumed6;
            public bool eventConsumed7;

            private void SetEvent(int index, InputActionType action, InputEventType type, ulong tick, bool consumed)
            {
                switch (index)
                {
                    case 0: eventAction0 = action; eventType0 = type; eventTick0 = tick; eventConsumed0 = consumed; break;
                    case 1: eventAction1 = action; eventType1 = type; eventTick1 = tick; eventConsumed1 = consumed; break;
                    case 2: eventAction2 = action; eventType2 = type; eventTick2 = tick; eventConsumed2 = consumed; break;
                    case 3: eventAction3 = action; eventType3 = type; eventTick3 = tick; eventConsumed3 = consumed; break;
                    case 4: eventAction4 = action; eventType4 = type; eventTick4 = tick; eventConsumed4 = consumed; break;
                    case 5: eventAction5 = action; eventType5 = type; eventTick5 = tick; eventConsumed5 = consumed; break;
                    case 6: eventAction6 = action; eventType6 = type; eventTick6 = tick; eventConsumed6 = consumed; break;
                    case 7: eventAction7 = action; eventType7 = type; eventTick7 = tick; eventConsumed7 = consumed; break;
                }
            }

            private void GetEvent(int index, out InputActionType action, out InputEventType type, out ulong tick, out bool consumed)
            {
                switch (index)
                {
                    case 0: action = eventAction0; type = eventType0; tick = eventTick0; consumed = eventConsumed0; return;
                    case 1: action = eventAction1; type = eventType1; tick = eventTick1; consumed = eventConsumed1; return;
                    case 2: action = eventAction2; type = eventType2; tick = eventTick2; consumed = eventConsumed2; return;
                    case 3: action = eventAction3; type = eventType3; tick = eventTick3; consumed = eventConsumed3; return;
                    case 4: action = eventAction4; type = eventType4; tick = eventTick4; consumed = eventConsumed4; return;
                    case 5: action = eventAction5; type = eventType5; tick = eventTick5; consumed = eventConsumed5; return;
                    case 6: action = eventAction6; type = eventType6; tick = eventTick6; consumed = eventConsumed6; return;
                    case 7: action = eventAction7; type = eventType7; tick = eventTick7; consumed = eventConsumed7; return;
                    default: action = default; type = default; tick = 0; consumed = true; return;
                }
            }

            public void AddEvent(InputActionType action, InputEventType type, ulong tick)
            {
                int index = (eventHead + eventCount) % EVENT_BUFFER_CAPACITY;
                SetEvent(index, action, type, tick, false);

                if (eventCount == EVENT_BUFFER_CAPACITY)
                {
                    eventHead = (eventHead + 1) % EVENT_BUFFER_CAPACITY;
                }
                else
                {
                    eventCount++;
                }
            }

            public bool TryConsumeTap(InputActionType actionType)
            {
                for (int i = 0; i < eventCount; i++)
                {
                    int index = (eventHead + i) % EVENT_BUFFER_CAPACITY;
                    GetEvent(index, out var action, out var type, out _, out var consumed);
                    if (!consumed && action == actionType && type == InputEventType.Tap)
                    {
                        GetEvent(index, out action, out type, out var tick, out consumed);
                        SetEvent(index, action, type, tick, true);
                        return true;
                    }
                }
                return false;
            }

            public void CleanupEvents(ulong currentTick, uint maxAgeTicks)
            {
                while (eventCount > 0)
                {
                    GetEvent(eventHead, out _, out _, out var tick, out var consumed);
                    ulong age = currentTick > tick ? (currentTick - tick) : 0;
                    if (consumed || age > maxAgeTicks)
                    {
                        eventHead = (eventHead + 1) % EVENT_BUFFER_CAPACITY;
                        eventCount--;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            public void ResetPhysicalState()
            {
                movement = Vector2.zero;
                aimWorldDirection = Vector3.zero;
                primaryAttackIsPressed = false;
                secondaryAttackIsPressed = false;
                dashIsPressed = false;
                grappleIsPressed = false;
            }

            public void Reset()
            {
                ResetPhysicalState();
                prevPrimaryAttackIsPressed = prevSecondaryAttackIsPressed = prevDashIsPressed = prevGrappleIsPressed = false;
                eventHead = 0;
                eventCount = 0;

                // 清空内容（防止 Debug/序列化残留影响）
                SetEvent(0, default, default, 0, true);
                SetEvent(1, default, default, 0, true);
                SetEvent(2, default, default, 0, true);
                SetEvent(3, default, default, 0, true);
                SetEvent(4, default, default, 0, true);
                SetEvent(5, default, default, 0, true);
                SetEvent(6, default, default, 0, true);
                SetEvent(7, default, default, 0, true);
            }

            public void Dispose() { }
        }
    }
}
