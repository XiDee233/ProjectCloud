# Player角色控制系统

## 概述

这是一个完整的45度视角角色控制系统，基于Unity CharacterController和PurrNet网络同步，使用Feel包实现丰富的game feel效果。

## 核心组件

### 1. PlayerMovementCore
- **继承**: `PredictedIdentity<MovementCoreInput, MovementCoreState>`
- **功能**: 只负责预测移动、重力、朝向、外部锁定；所有输入由状态节点统一喂入

### 2. PredictedStateMachine
- **基础**: 使用 PurrNet 原生的 `PredictedStateMachine`
- **切换**: 状态节点在 `StateSimulate` 中通过 `machine.states` 查找目标类型并执行 `machine.SetState()`，实现零耦合切换
- **状态**: `MovementStateNode`、`DashStateNode`、`MeleeStateNode`、`RangedStateNode`
- **职责**: 状态机由策划在 Inspector 中手动配置状态列表，实现零耦合切换，支持客户端预测和回滚

### 3. 近战战斗系统（MeleeCombatSystem + Combo）
- **数据驱动**: `MeleeAttackData`（攻击帧、取消窗口、判定）
- **连招控制**: `MeleeComboStateMachine` 管理 Start/Active/Recovery
- **状态绑定**: `MeleeStateNode` 在攻击期间锁定方向、在 combo 完成后回到移动

### 4. 远程战斗系统（RangedCombatSystem）
- **武器配置**: `RangedWeaponData`（弓箭蓄力、枪械射速）
- **输入响应**: 蓄力/发射、后坐力、冷却控制
- **状态绑定**: `RangedStateNode` 在射击完成后回到移动状态

### 5. Feel 反馈
- `MovementFeelController` 仍然负责调用 MMFeedbacks 增强 feel（落地、冲刺、脚步等）

## 快速开始

### 1. 自动设置
```csharp
// 添加PlayerSetup组件，它会自动配置所有必需组件
gameObject.AddComponent<PlayerSetup>();
```

### 2. 手动设置
在Unity中：
1. 创建一个GameObject
2. 添加`PlayerSetup`组件，它会自动填充：
   - `PlayerInputHandler`
   - `PlayerMovementCore`
   - `PredictedStateMachine`（及 Movement/Dash/Melee/Ranged 状态节点）
   - `MeleeCombatSystem` + `MeleeComboStateMachine`
   - `RangedCombatSystem`
3. 添加`CharacterController`和`PredictedTransform`（`PlayerSetup`会自动添加）
4. 配置 `MovementFeelController` 可选地增强反馈

### 3. 配置Feel效果
1. 在`MovementFeelController`中创建MMFeedbacks资产
2. 配置各个反馈效果（相机震动、音效、粒子等）

## 输入控制

| 输入 | 键盘 | 游戏手柄 | 功能 |
|------|------|----------|------|
| 移动 | WASD | 左摇杆 | 基础移动 |
| 冲刺 | 空格 | A键 | 闪避冲刺 |
## 网络同步

- 使用PurrDiction的`PredictedIdentity`实现客户端预测
- 输入通过`GetFinalInput()`同步
- 状态包含位置、速度、朝向等关键数据
- 支持回滚和插值

## Game Feel配置

### 相机震动（MMF_CameraShake）
```csharp
// 在MMFeedbacks中添加Camera Shake反馈
- Duration: 0.3s
- Amplitude: 0.5f
- Frequency: 10f
```

### 时间缩放（MMF_TimescaleModifier）
```csharp
// 冲刺时的慢动作效果
- TimeScale: 0.7f
- Duration: 0.2s
```

### 音效（MMF_AudioSource）
```csharp
// 脚步声、落地音效等
- AudioClip: 选择相应音效
- Volume: 0.8f
```

## 自定义扩展

### 添加新能力
```csharp
public interface ICustomAbility : IAbilityProvider
{
    bool CanPerform { get; }
    void Perform();
}

public class CustomAbility : MonoBehaviour, ICustomAbility
{
    // 实现能力逻辑
}
```

### 自定义反馈
```csharp
// 在MovementFeelController中添加新的MMFeedbacks
[SerializeField] private MMFeedbacks customFeedbacks;

// 在适当的时机调用
customFeedbacks?.PlayFeedbacks();
```

## 性能优化

- MMFeedbacks已优化，支持对象池
- 预测系统减少网络延迟影响
- 组件化设计，支持按需启用/禁用

## 调试

### 调试选项
- `PlayerSetup`组件提供验证和重置功能
- 各组件都有详细的Gizmos显示
- 控制台输出重要的状态变化

### 常见问题

1. **移动不流畅**: 检查加速度曲线设置
2. **网络不同步**: 确认PredictedTransform正确配置
3. **反馈不播放**: 检查MMFeedbacks资产是否正确设置

## 架构遵循

✅ **组件化设计**: 每个能力独立组件
✅ **接口多态**: 使用接口而非继承
✅ **契约式设计**: 使用[RequireComponent]保证依赖
✅ **权责分离**: 移动逻辑与视觉效果分离
✅ **Feel包集成**: 充分利用MMFeedbacks系统

## 文件结构

```
Scripts/Player/
├── Core/
│   ├── PlayerInputHandler.cs
│   ├── PlayerMovementCore.cs
│   └── StateMachineExtensions.cs
├── States/
│   ├── MovementStateNode.cs
│   ├── MeleeStateNode.cs
│   └── RangedStateNode.cs
├── Combat/
│   ├── Melee/
│   │   ├── MeleeAttackData.cs
│   │   ├── MeleeComboStateMachine.cs
│   │   └── MeleeCombatSystem.cs
│   └── Ranged/
│       ├── RangedWeaponData.cs
│       └── RangedCombatSystem.cs
├── Feel/
│   └── MovementFeelController.cs
├── PlayerSetup.cs
└── README.md
```