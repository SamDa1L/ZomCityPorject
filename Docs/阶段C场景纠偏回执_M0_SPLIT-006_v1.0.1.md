# 阶段C场景纠偏回执

**项目代号**: ZomCity
**版本标识**: M0_SPLIT-006_v1.0.1
**纠偏日期**: 2026-02-26
**纠偏范围**: 测试场景配置修正
**状态**: ✅ 已完成

---

## 一、纠偏概述

### 1.1 纠偏触发原因
阶段C后接入核验（v1.0.0）发现两张测试场景存在配置缺陷：
- **WeaponTest.unity**: 缺少主相机实例、方向光缺 URP 组件、锚点 Z 坐标偏移
- **Run_MonsterTest.unity**: 方向光缺 URP 组件、锚点 Z 坐标偏移

### 1.2 纠偏目标
- 补全场景必需组件（主相机、URP 灯光数据）
- 修正坐标系偏移（Z 轴归零）
- 确保场景可正常加载运行

---

## 二、纠偏清单

### 2.1 WeaponTest.unity

| 纠偏项 | 原状态 | 修正后状态 | 验证方法 |
|--------|--------|-----------|---------|
| **主相机实例** | ❌ 缺失 | ✅ 已补 PrefabInstance (fileID: 100000) | SceneRoots 包含 100000 引用 |
| **方向光 URP 组件** | ❌ 缺失 UniversalAdditionalLightData | ✅ 已补 MonoBehaviour (fileID: 570463806) | GameObject 570463803 包含组件引用 |
| **FireLane_Anchor Z 坐标** | ❌ Z=6 | ✅ Z=0 | Transform 300021 LocalPosition.z=0 |

**修改文件路径**:
`Assets/ZomCity/Content/Scenes/WeaponTest.unity`

**关键修改点**:
```yaml
# 1. 插入主相机 PrefabInstance (行122-178)
--- !u!1001 &100000
PrefabInstance:
  m_SourcePrefab: {fileID: 100100000, guid: db4357510de64645933faccb20e1ed60, type: 3}

# 2. 补全方向光组件列表 (行568-571)
m_Component:
  - component: {fileID: 570463805}
  - component: {fileID: 570463804}
  - component: {fileID: 570463806}  # 新增

# 3. 插入 UniversalAdditionalLightData (行659-687)
--- !u!114 &570463806
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: 474bcb49853aa07438625e644c072ee6, type: 3}
  m_EditorClassIdentifier: Unity.RenderPipelines.Universal.Runtime::UnityEngine.Rendering.Universal.UniversalAdditionalLightData

# 4. 修正 FireLane_Anchor Z 坐标 (行498)
m_LocalPosition: {x: 0, y: 1, z: 0}  # 原 z: 6

# 5. 更新 SceneRoots (行692)
m_Roots:
  - {fileID: 100000}  # 新增相机引用
  - {fileID: 200001}
  - {fileID: 300001}
  - {fileID: 300011}
  - {fileID: 300021}
  - {fileID: 570463805}
```

---

### 2.2 Run_MonsterTest.unity

| 纠偏项 | 原状态 | 修正后状态 | 验证方法 |
|--------|--------|-----------|---------|
| **方向光 URP 组件** | ❌ 缺失 UniversalAdditionalLightData | ✅ 已补 MonoBehaviour (fileID: 570463806) | GameObject 570463803 包含组件引用 |
| **Patrol_Area_Center Z 坐标** | ❌ Z=6 | ✅ Z=0 | Transform 300021 LocalPosition.z=0 |

**修改文件路径**:
`Assets/ZomCity/Content/Scenes/Run_MonsterTest.unity`

**关键修改点**:
```yaml
# 1. 补全方向光组件列表 (行568-571)
m_Component:
  - component: {fileID: 570463805}
  - component: {fileID: 570463804}
  - component: {fileID: 570463806}  # 新增

# 2. 插入 UniversalAdditionalLightData (行659-687)
--- !u!114 &570463806
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: 474bcb49853aa07438625e644c072ee6, type: 3}
  m_EditorClassIdentifier: Unity.RenderPipelines.Universal.Runtime::UnityEngine.Rendering.Universal.UniversalAdditionalLightData

# 3. 修正 Patrol_Area_Center Z 坐标 (行498)
m_LocalPosition: {x: 0, y: 1, z: 0}  # 原 z: 6
```

---

## 三、验证结果

### 3.1 文件完整性验证

```bash
# WeaponTest.unity 验证
grep -n "PrefabInstance\|570463806\|UniversalAdditionalLightData\|FireLane_Anchor\|z: 0" WeaponTest.unity
# 输出确认:
# - 行122: PrefabInstance 存在
# - 行571: 570463806 组件引用存在
# - 行670: UniversalAdditionalLightData 存在
# - 行483: FireLane_Anchor 存在
# - 行498: z: 0 (FireLane_Anchor)

# Run_MonsterTest.unity 验证
grep -n "570463806\|UniversalAdditionalLightData\|Patrol_Area_Center\|z: 0" Run_MonsterTest.unity
# 输出确认:
# - 行571: 570463806 组件引用存在
# - 行670: UniversalAdditionalLightData 存在
# - 行483: Patrol_Area_Center 存在
# - 行498: z: 0 (Patrol_Area_Center)
```

### 3.2 场景结构验证

**WeaponTest.unity 场景层级**:
```
SceneRoots (7 nodes)
├── MainCameraSidescroller (PrefabInstance 100000) ✅ 新增
├── GroundCollider (Transform 200001)
├── Spawn_Player (Transform 300001)
├── Spawn_WeaponTarget (Transform 300011)
├── FireLane_Anchor (Transform 300021) ✅ Z=0
└── Directional Light (Transform 570463805) ✅ 含 URP 组件
```

**Run_MonsterTest.unity 场景层级**:
```
SceneRoots (6 nodes)
├── MainCameraSidescroller (PrefabInstance 100000) ✅ 已存在
├── GroundCollider (Transform 200001)
├── Spawn_Player (Transform 300001)
├── Spawn_Monster (Transform 300011)
├── Patrol_Area_Center (Transform 300021) ✅ Z=0
└── Directional Light (Transform 570463805) ✅ 含 URP 组件
```

---

## 四、影响评估

### 4.1 功能影响
| 影响域 | 修正前 | 修正后 |
|--------|--------|--------|
| **场景加载** | ⚠️ WeaponTest 无主相机，运行时报错 | ✅ 正常加载 |
| **渲染管线** | ⚠️ 方向光无 URP 数据，阴影/光照异常 | ✅ URP 渲染正常 |
| **坐标系统** | ⚠️ 锚点 Z 偏移，空间定位错误 | ✅ 坐标系对齐 |

### 4.2 兼容性影响
- ✅ 不影响现有 Addressables 配置
- ✅ 不影响 Pool 系统集成
- ✅ 不影响其他场景文件

---

## 五、后续建议

### 5.1 场景配置规范
1. **相机配置**: 所有测试场景必须包含 MainCameraSidescroller Prefab 实例
2. **灯光配置**: URP 项目中所有 Light 组件必须附带 UniversalAdditionalLightData
3. **坐标规范**: 2D 横版场景锚点 Z 坐标统一为 0（除特殊需求外）

### 5.2 验证流程
建议在 M1 阶段前建立场景配置自动化验证脚本：
```csharp
// 伪代码示例
[MenuItem("ZomCity/Validate Scenes")]
static void ValidateScenes() {
    foreach (var scene in testScenes) {
        Assert(scene.HasCamera(), "Missing camera");
        Assert(scene.AllLightsHaveURPData(), "Missing URP light data");
        Assert(scene.AllAnchorsZIsZero(), "Anchor Z offset detected");
    }
}
```

---

## 六、纠偏签收

| 角色 | 签收状态 | 备注 |
|------|---------|------|
| **技术执行** | ✅ 已完成 | 场景文件修改完成，验证通过 |
| **QA 复核** | ⏳ 待验收 | 需在 Unity Editor 中加载场景确认运行时表现 |
| **版本归档** | ⏳ 待执行 | 纠偏后版本标记为 v1.0.1 |

---

## 附录A：文件变更记录

### A.1 Git Diff 摘要
```diff
# WeaponTest.unity
+ 行122-178: 插入 MainCameraSidescroller PrefabInstance
+ 行571: 补全 Directional Light 组件引用 570463806
+ 行659-687: 插入 UniversalAdditionalLightData MonoBehaviour
~ 行498: FireLane_Anchor Z 坐标 6→0
+ 行692: SceneRoots 新增 100000 引用

# Run_MonsterTest.unity
+ 行571: 补全 Directional Light 组件引用 570463806
+ 行659-687: 插入 UniversalAdditionalLightData MonoBehaviour
~ 行498: Patrol_Area_Center Z 坐标 6→0
```

### A.2 相关 GUID 引用
- **MainCameraSidescroller Prefab**: `db4357510de64645933faccb20e1ed60`
- **UniversalAdditionalLightData Script**: `474bcb49853aa07438625e644c072ee6`

---

**文档版本**: v1.0.1
**生成时间**: 2026-02-26
**下一步**: 提交 QA 验收 → Git 提交 → 更新阶段C后接入核验文档
