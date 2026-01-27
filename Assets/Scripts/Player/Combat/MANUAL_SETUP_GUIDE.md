# Player Combat System 手动配置指南 (Manual SOP)

由于我们移除了自动化的 `PlayerSetup` 脚本以换取更高的架构透明度，现在需要手动在 Unity Inspector 中配置 Player 预制体。请按照以下步骤操作：

## 1. 节点结构建议
在 Player 预制体下，建议保持以下层级结构：
- **Player (Root)**
    - **Logic** (存放状态机节点)
        - `PredictedStateMachine`
        - `MovementStateNode`
        - `MeleeStateNode`
        - `RangedStateNode`
    - **Systems** (存放底层系统)
        - `MeleeCombatSystem`
        - `RangedCombatSystem`
        - `CombatPlayableGraph` (核心动画驱动)

## 2. 组件挂载与引用设置

### A. CombatPlayableGraph (核心)
- **位置**: 挂载在 `Systems` 节点上。
- **说明**: 该组件现在统一负责近战和远程的动画播放及事件分发。
- **引用设置**:
    - `Animator`: 拖入角色模型上的 `Animator` 组件。如果不拖，它会尝试在父级寻找。

### B. MeleeCombatSystem
- **位置**: 挂载在 `Systems` 节点上。
- **引用设置**:
    - `Combat Playable Graph`: 拖入同级节点的 `CombatPlayableGraph` 实例。

### C. RangedCombatSystem
- **位置**: 挂载在 `Systems` 节点上。
- **引用设置**:
    - `Combat Playable Graph`: 拖入同级节点的 `CombatPlayableGraph` 实例。
    - `Weapon Data`: 拖入对应的 `RangedWeaponData` 资产。

### D. MeleeStateNode (状态机)
- **位置**: 挂载在 `Logic` 节点下。
- **引用设置**:
    - `Movement Core`: 拖入 Root 节点的 `PlayerMovementCore`。
    - `Melee Combat System`: 拖入 `Systems/MeleeCombatSystem`。
    - `Control Authority`: 拖入 Root 节点的 `ControlAuthority`。

### E. RangedStateNode (状态机)
- **位置**: 挂载在 `Logic` 节点下。
- **引用设置**:
    - `Movement Core`: 拖入 Root 节点的 `PlayerMovementCore`。
    - `Ranged Combat System`: 拖入 `Systems/RangedCombatSystem`。
    - `Control Authority`: 拖入 Root 节点的 `ControlAuthority`。

## 3. Root Motion 与 Timeline 设置 (高级)

为了确保动作手感精准且兼容网络预测，请遵循以下设置：

### A. FBX 导入设置
1. 选中 FBX 模型，进入 `Animation` 标签页。
2. 找到 `Root Transform Position (Y)` 和 `Root Transform Position (XZ)`。
3. 勾选 `Bake Into Pose`。
4. **注意**: 尽管勾选了 Bake，Unity 仍会生成 `deltaPosition` 数据，`CombatPlayableGraph` 会手动提取这些数据并应用到逻辑状态中。

### B. Timeline 轨道配置
- `CombatPlayableGraph` 已在代码中强制将所有 `Animation Track` 的偏移模式设为 `Apply Scene Offsets`。
- 这意味着动画将始终从角色当前位置开始播放，不会跳回世界原点。
- **预览注意**: 在编辑器中预览 Timeline 时，如果发现位置不对，请确保场景中有一个临时的 `PlayableDirector` 并将 `Track Offsets` 设为 `Apply Scene Offsets`。

## 4. 战斗事件监听 (Important)
如果你需要某个脚本监听 Timeline 中的战斗事件（如攻击判定开始、特效触发）：
1. 确保该脚本实现了 `ICombatEventListener` 接口。
2. 将该脚本挂载在与 `CombatPlayableGraph` 相同的 GameObject 上，或者其子物体上。
3. `CombatPlayableGraph` 在初始化时会自动扫描并向这些监听器转发事件。

## 4. 常见问题排查
- **动画不播放**: 检查 `MeleeAttackData` 或 `RangedWeaponData` 中的 `TimelineAsset` 是否已分配。
- **连招无效**: 检查 Timeline 中是否存在 `ComboWindowTrack`，并且该轨道上是否有 Clip。
- **引用丢失**: 由于删除了 `PlayerSetup`，打开预制体时如果看到 "Missing Script"，请手动移除旧的脚本残留并重新按上述步骤挂载。
