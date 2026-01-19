# 战斗连招系统编辑指南

## 系统架构概览

本战斗系统采用 **Timeline 表现时轴** + **输入缓冲** + **数据驱动逻辑** 的三层架构：

- **表现层**：Unity Timeline 控制动画、特效、音效的精确时间点
- **输入层**：`PlayerInputHandler` 已实现 **Capcom/Nintendo 风格**的 150ms 输入缓冲（`bufferWindow = 0.15f`）
- **逻辑层**：`MeleeAttackData` 通过委托函数定义连招规则，`MeleeCombatSystem` 处理连招窗口与输入缓冲的配合

---

## 第一步：创建基础资源

### 1.1 创建 CombatTimelineData 资产

1. 在 Project 窗口右键：`Create > Player > Combat > Combat Timeline Data`
2. 创建三个资产：
   - `LightAttack_1_Timeline.asset`
   - `LightAttack_2_Timeline.asset`
   - `LightAttack_3_Timeline.asset`

### 1.2 创建 Timeline 资源

1. 在 Project 窗口右键：`Create > Timeline`
2. 创建三个 `.playable` 文件：
   - `LightAttack_1.playable`
   - `LightAttack_2.playable`
   - `LightAttack_3.playable`

### 1.3 关联 Timeline 到数据

1. 打开 `Window > Player > Combat Timeline Editor`
2. 选择 `LightAttack_1_Timeline.asset`
3. 将 `LightAttack_1.playable` 拖入 `Timeline 资源` 字段
4. 点击 `应用 Timeline 到数据`
5. 重复步骤 2-4 处理另外两个攻击

### 1.4 创建 MeleeAttackData 资产

1. 在 Project 窗口右键：`Create > Player > Melee Attack Data`
2. 创建三个资产：
   - `LightAttack_1_Data.asset`
   - `LightAttack_2_Data.asset`
   - `LightAttack_3_Data.asset`
3. 为每个资产配置：
   - `attackName`: "LightAttack_1" / "LightAttack_2" / "LightAttack_3"
   - `animationData`: 关联对应的 `CombatTimelineData`

---

## 第二步：Timeline 轨道配置（典型的动作结构）

打开 `LightAttack_1.playable`，在 Timeline 窗口中建立以下轨道：

### 2.1 Animation Track（动画轨道）

1. 右键轨道区域：`Add > Animation Track`
2. 将角色的攻击动画剪辑拖入轨道
3. **关键技巧**：
   - **动作融合**：第一击的收招帧可以与第二击的起手帧在 Timeline 上重叠 5-10 帧，实现无缝衔接
   - **速度调整**：选中 Clip，在 Inspector 中调整 `Speed Multiplier`（例如 1.2x 让动作更快更凌厉）

### 2.2 Combat Event Track（战斗事件轨道）

1. 右键轨道区域：`Add > Combat Event Track`（自定义轨道）
2. 在轨道上右键：`Add > Combat Event Clip`

#### A. 伤害判定窗口（Hitbox Window）

在武器挥动到最快速度的瞬间，添加两个 Event Clip：

- **开启判定**（例如在动画 30% 处）：
  - `eventName`: `"HitboxActive"`
  - `stringParam`: `"LightAttack_1_Hitbox"`（用于标识，可选）
  
- **关闭判定**（例如在动画 50% 处）：
  - `eventName`: `"HitboxDeactive"`

#### B. 连招输入窗口（Combo Window）

这是实现连招手感的核心：

- **开启连招窗口**（例如在动画 60% 处，判定结束后）：
  - `eventName`: `"ComboWindow"`
  - `floatParam`: `1.0`（1.0 表示开启，0.0 表示关闭）
  
- **关闭连招窗口**（例如在动画 90% 处，进入后摇）：
  - `eventName`: `"ComboWindow"`
  - `floatParam`: `0.0`

**窗口时长建议**：0.2s - 0.4s，给玩家足够的反应时间

#### C. 打击感增强（Hit-stop）

在 `HitboxActive` 事件触发时，如果检测到命中敌人，可以触发：

- `eventName`: `"HitStop"`
- `floatParam`: `0.08`（顿帧时长，单位：秒）

### 2.3 音效与特效（可选）

可以在 `CombatEventTrack` 中添加：

- **音效触发**：
  - `eventName`: `"PlaySound"`
  - `stringParam`: `"Sword_Whoosh"`（音效资源名称）
  
- **特效触发**：
  - `eventName`: `"PlayEffect"`
  - `stringParam`: `"Slash_VFX"`（特效资源名称）

---

## 第三步：实现三连击逻辑

### 3.1 配置连招数据

在 `MeleeAttackData` 中，你需要通过**委托函数**定义连招规则。

**方式一：在代码中初始化（推荐）**

创建一个初始化脚本或在 `PlayerSetup` 中：

```csharp
// 在 Awake 或 Start 中
var attack1 = Resources.Load<MeleeAttackData>("LightAttack_1_Data");
var attack2 = Resources.Load<MeleeAttackData>("LightAttack_2_Data");
var attack3 = Resources.Load<MeleeAttackData>("LightAttack_3_Data");

// 定义连招规则：第一击可以连到第二击
attack1.canComboTo = (next) => next == attack2;

// 第二击可以连到第三击
attack2.canComboTo = (next) => next == attack3;

// 第三击是终结技，不能连招
attack3.canComboTo = (next) => false;
```

**方式二：通过 ScriptableObject 扩展（高级）**

可以在 `MeleeAttackData` 中添加 `MeleeAttackData[] nextPossibleAttacks` 字段，然后在运行时转换为委托。

### 3.2 状态机集成

`MeleeStateNode` 已经正确使用了输入缓冲系统：

```csharp
// 在 GetFinalInput 中
input.primaryAttack = p.PrimaryAttack;
if (input.primaryAttack.wasPressed) p.ConsumeInput(InputActionType.PrimaryAttack);
```

`wasPressed` 会在 150ms 缓冲窗口内保持为 `true`，即使玩家提前按键也能被捕获。

### 3.3 连招触发流程

1. **玩家按下攻击键**：
   - `MovementStateNode` 检测到 `input.primaryAttack.wasPressed`
   - 切换到 `MeleeStateNode`

2. **第一击播放**：
   - `MeleeStateNode.Enter()` 调用 `meleeCombatSystem.TryAttack(attack1)`
   - Timeline 开始播放

3. **Timeline 到达 60%**：
   - `ComboWindow(1.0)` 事件触发
   - `MeleeCombatSystem.IsInComboWindow = true`

4. **玩家在窗口内再次按键**：
   - `MeleeStateNode.StateSimulate()` 检测到 `input.primaryAttack.wasPressed`
   - 调用 `meleeCombatSystem.RequestCombo(attack2)`
   - 系统检测到 `IsInComboWindow == true`，立即执行 `TryAttack(attack2)`
   - 第一击的 Timeline 被中断，第二击开始播放

5. **重复步骤 3-4** 直到第三击完成

6. **第三击结束**：
   - `MeleeCombatSystem.OnAttackComplete` 触发
   - `MeleeStateNode` 检测到 `!meleeCombatSystem.IsAttacking`
   - 自动切回 `MovementStateNode`

---

## 第四步：进阶细节

### 4.1 输入缓冲与连招窗口的配合

**关键理解**：
- **输入缓冲**（150ms）：玩家可以提前按键，系统会记住这个输入
- **连招窗口**（200-400ms）：只有在窗口开启时，缓存的输入才会被消费

**最佳实践**：
- 连招窗口应该在判定结束后立即开启，持续到后摇开始前
- 窗口时长应该**大于**输入缓冲时长，确保玩家有足够时间反应

### 4.2 取消窗口（Cancel Window）

可以在 Timeline 中添加 `CancelWindow` 事件，允许玩家在特定时机通过闪避取消后摇：

```csharp
case "CancelWindow":
    // 允许闪避取消
    if (input.dash.wasPressed) {
        // 切换到 DashStateNode
    }
    break;
```

### 4.3 位移修正（Warping）

如果需要实现攻击时的“滑步”效果，可以在 Timeline 中使用 **Transform Track** 或自定义的 **Movement Track**：

1. 添加 `Transform Track`
2. 在攻击起手阶段（0-20%），设置角色的 `Position` 曲线
3. 让角色向前产生 0.5-1.0 单位的位移

---

## 第五步：调试与优化

### 5.1 检查清单

- [ ] Timeline 动画剪辑的节奏是否紧凑？（避免拖沓）
- [ ] `HitboxActive` 事件是否精确对准了武器挥动的视觉位置？
- [ ] `ComboWindow` 的开启时机是否在判定结束后？（避免判定未结束就能连招）
- [ ] `ComboWindow` 的时长是否足够？（建议 0.2s-0.4s）
- [ ] `MeleeAttackData.canComboTo` 委托是否正确配置？

### 5.2 常见问题

**Q: 连招无法触发**
- 检查 `ComboWindow` 事件是否正确触发（查看 Console 日志）
- 检查 `MeleeAttackData.canComboTo` 委托是否返回 `true`
- 检查输入缓冲是否被正确消费（`ConsumeInput` 调用时机）

**Q: 连招手感生涩**
- 增加 `ComboWindow` 的时长
- 检查 Timeline 中动画剪辑之间是否有重叠融合
- 调整 `PlayerInputHandler.bufferWindow`（默认 0.15s，可以增加到 0.2s）

**Q: 攻击判定不准确**
- 在 Timeline 中精确调整 `HitboxActive` 和 `HitboxDeactive` 的时间点
- 使用 Unity 的 `Animation Events` 作为参考点

---

## 总结

这套系统的核心优势：

1. **输入缓冲**：玩家不需要在精确的一帧按键，提前 150ms 按键都能被系统识别
2. **连招窗口**：通过 Timeline 事件精确控制连招时机，实现类似《鬼泣》的手感
3. **数据驱动**：所有时间、判定、连招规则都通过 Timeline 和 ScriptableObject 配置，无需修改代码
4. **网络兼容**：Timeline 的播放时间与 PurrNet 的 Tick 系统绑定，支持客户端预测和回滚

现在你可以在 Unity 编辑器中开始配置你的第一个三连击了！
