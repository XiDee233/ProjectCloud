## 统一游戏入口设计

### 1. 创建统一游戏入口
创建`GameManager.cs`作为游戏统一入口，负责初始化所有核心组件，包括NetworkManager和SceneQuadtreeManager。

### 2. 简化SceneQuadtreeManager
移除所有多余的NetworkManager相关代码，只保留核心场景管理功能：
   - 删除`NetworkManager.onNetworkManagerCreated`事件订阅
   - 删除`OnNetworkManagerCreated`方法
   - 删除`GetNetworkReferences`方法
   - 删除`GetScenesModule`方法
   - 删除`_networkManager`字段
   - 简化`LoadScene`和`UnloadScene`方法，直接使用`_scenesModule`

### 3. 统一依赖注入
在GameManager中统一初始化所有组件，包括：
   - 获取NetworkManager实例
   - 获取ScenesModule实例
   - 将ScenesModule注入到SceneQuadtreeManager
   - 初始化PlayerSceneTracker

### 4. 简化初始化流程
SceneQuadtreeManager将只负责：
   - 四叉树初始化和管理
   - 监听PlayerSceneTracker的移动事件
   - 根据玩家位置管理场景加载/卸载
   - 不再处理复杂的NetworkManager初始化

### 5. 保留核心功能
确保所有核心场景管理功能正常工作，包括：
   - 四叉树空间查询
   - 场景动态加载/卸载
   - 与PurrNet ScenesModule的集成
   - Gizmos可视化

这样设计将使代码更加简洁，所有初始化逻辑集中在GameManager中，符合统一入口原则。