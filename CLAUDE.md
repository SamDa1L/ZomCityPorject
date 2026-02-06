# ZomCity 项目配置

## 项目概述
**项目名称：** ZomCity（R市：信号弹）
**类型：** 2.5D 横版平台跳跃射击 + Roguelike 元素
**游戏类型：** "搜打撤" × 魂系融合
**版本：** v0.1.3（开发计划）/ v0.2.6（总计划文档）
**更新时间：** 2026-02-06

## 权威文档（按优先级）
- `Docs/ZomCity/ZomCity总计划文档_v0.2.6.md`（产品标准/验收口径）
- `Docs/ZomCity/JU工程结构分析汇总_2026-02-05.md`（可复用架构/资产/实现参考）
- `Docs/ZomCity/ZomCity项目开发计划规划.md`（本开发计划）

**归档规则：** 权威文档统一归档到 `Docs/ZomCity/`（文件名带版本号/日期）；TempLogs 仅允许生成物/草稿，不得作为引用入口。

## 技术栈
- **引擎：** Unity 6000.3.6f1
- **渲染管线：** URP（通用渲染管线）
- **输入系统：** Unity Input System（Update Mode: `Process Events In Dynamic Update`）
- **物理系统：** 3D 物理 + Gameplay Plane 平面约束（Z 轴锁定）
- **渲染风格：** 实时 3D 渲染后像素化为 2.5D 输出
- **数据主权：** CastleDB（唯一事实源）+ Addressables（运行时资源加载）
- **平台：** PC（窗口/全屏），60FPS 目标，最低不低于 50FPS

## 当前项目状态
- **阶段：** 模板/启动阶段（可玩性 = 0）
- **当前内容：** 参考套件资源 + URP Settings，无正式玩法代码
- **下一步优先级：** M0 - 工程底座就绪（2.5D 像素化渲染链路 + 摄像机规则 + 输入/状态机底座）
- **复用策略：** 最大化复用参考套件现成系统与资产，但通过系统性重命名/封装/替换确保最终工程不包含参考套件命名痕迹

## 常量清单（单一事实源｜写死）
- **DocsRoot：** `Docs/ZomCity/`
- **ReportsOutDir：** `TempLogs/ZomCityReports/`
- **ReportFileName：** `<ReportName>_<yyyyMMdd_HHmmss>.json`
- **ReportRetention：** 仅保留最近 20 次；CI 将最新一次作为 Artifact 存档
- **AddressDomainWhitelist：** `Enemies/Rooms/Containers/Items/UI/VFX/Audio`
- **TagWhitelist（Unity Tag）：** `Player/Enemy/Interactable/Container`
- **StableIdPrefixWhitelist（MVP）：** `MAT_/CON_/WPN_/ATT_/DATA_/LT_/CT_/EN_/RM_/ZN_/RC_/UP_`
- **DesignRoot：** `Assets/ZomCity/Data/Design/`
- **GeneratedRoot：** `Assets/ZomCity/Data/Generated/`
- **ContentRoot：** `Assets/ZomCity/Content/`

## 核心技术规范

### 硬约束（必须先封板）
> 这些约束一旦松动，后续系统（射击、UI、命中、相机平滑、像素化稳定性）会出现"偶发差一帧/抖动/命中偏差"，返工成本极高。

**用语分级：** 本项目只使用三档——**写死（必须）/强烈建议（默认执行）/可选（后续）**

### Tick 顺序（写死）
1. **Update：** InputSampler（采样输入意图）
2. **FixedUpdate：** Motor（移动与碰撞）
3. **LateUpdate：** Camera（Desired→Smooth→Clamp→PixelSnap）
4. **LateUpdate：** AimPoint/Fire（消费 FireRequested）

### Time/FramePacing 主权（TimeSettings Ownership｜写死）
- **禁止：** 任意系统在运行时修改 `Time.fixedDeltaTime/maximumDeltaTime/vSync/targetFrameRate`
- **仅允许：** Boot/Settings 单点入口设置并记录日志
- **Development Build：** TimeSettingsGuard 监测篡改并回写（Viewer 可见）
- **事件命名：** `TimeSettingsTamperedEvent`（发布到 GameplayEventHub）

### 2.5D 像素化渲染链路（封板）
- **WorldCamera：** 渲染到低分辨率 RT（默认 320×180）
- **放大方式：** Nearest 过滤放大输出
- **UI 渲染：** Overlay 层，原生分辨率渲染（不参与像素化）
- **RenderTexture 分辨率：** 320×180（默认，M0 锁定）
- **PPU（Pixels Per Unit）：** 16（写死）
- **OrthographicSize 公式：** `(RT_Height / PPU) / 2` = `(180/16)/2 = 5.625`
- **最小窗口尺寸：** 640×360（RT×2）
- **缩放方式：** 整数倍缩放（Integer Scale，禁止非整数缩放）
- **采样方式：** Point/Nearest 过滤（禁用 AA、双线性过滤）

### PPU/OrthographicSize 契约（写死）
- **默认 PPU：** 16
- **OrthographicSize：** `(RT_Height / PPU)/2`
- **PixelSnap 执行顺序：** Desired→Smooth→Clamp→Snap→Apply（LateUpdate，Snap 最后一步）
- **Debug 面板必须显示：** PPU、OrthoSize、IntegerScale、DisplayRect、PixelSnap 状态

### 权威组件（写死）
- **PixelViewportManager：** IntegerScale/DisplayRect/Screen↔RT↔World 换算禁止多实现
- **禁止：** 任何脚本使用 `Camera.main.WorldToScreenPoint` 作为 UI 映射捷径（必须考虑 DisplayRect/IntegerScale）

### Gameplay Plane 平面约束（写死）
- **坐标系统：** X=水平，Y=垂直，Z=深度（仅用于渲染排序）
- **物理约束：** 所有玩法对象（Player/Enemy/Projectile/Loot）PhysicsRoot 的 Z 强制锁定
- **PlaneThickness：** 0.5，|Z|≤0.25
- **Physics 与 Visual 解耦：** PhysicsRoot 在 Z=0，VisualRoot 子节点可有 Z 偏移
- **渲染排序：** 使用 Z 偏移（±0.01）或 SortingOrder 做视觉层级
- **Room Prefab 锚点约束：** RoomRoot 与所有锚点（PlayerSpawn/EnemySpawn/LootPoint/ContainerPoint/ShortcutGate/ExitGate/NavGraphRoot）Z 必须为 0（或 |Z|≤0.01）

### Layer/Tag 主权收口（写死）
- **LayerCatalog/TagCatalog：** 建立常量表，禁止硬编码 layer index/tag 字符串散落
- **Validator/CI 校验：** 必需 Layer/Tag 存在与关键 Prefab 合规
- **Tag Policy：** 玩法判定优先用 LayerMask + 组件接口；如需使用 Tag，仅允许白名单（见常量清单 `TagWhitelist`）

### 数据与资源主权（写死）
- **配置数据唯一事实源：** CastleDB（确定性导出 + DataValidator，Build 前执行）
- **运行时资源加载唯一口径：** Addressables
  - PrefabKey = Address
  - Address 命名合同：`ZC/<Domain>/<StableID>`
  - 至少覆盖：Rooms/Enemies/Containers/VFX 等运行时资源
- **截止点：** M0 结束前必须封板

### Motor（运动控制器）
- **默认口径：** Rigidbody(Kinematic) + 自定义 Sweep/Probe
- **优势：** 平台手感、单向平台、翻滚/硬直稳定

### 事件总线（写死）
- **GameplayEventHub：** 统一发布/订阅（Noise/Damage/Pickup/Drop/Death/Save/Craft/Mod/Result…）
- **禁止：** 只用 Debug.Log（事件为唯一事实源）
- **Event 命名规范（写死）：**
  - 事件名统一前缀：`EVT_<Domain>_<Verb>`（例如 `EVT_Loot_Pickup`、`EVT_Save_WriteMeta`、`EVT_Combat_Damage`）
  - 事件 payload 必须包含：`Frame`、`Scene`、`ActorID`（若有）、`ItemID`/`WeaponID`（若有）
  - Debug.Log 只能作为补充，不作为系统事实源

### 性能底线（写死）
- **对象池化：** Projectile/VFX/FloatingText/LootDrop/NoiseIndicator 必须对象池化
- **武器测试场：** 持续射击 60 秒无明显 GC spike

### VFXPoolBudget（写死｜MVP）
- **覆盖范围（最小集）：** Projectile / HitVFX / MuzzleFlash / FloatingText / LootDrop / NoiseIndicator
- **每类必须配置：** `PoolSize` + `MaxAlivePerRoom`
  - 例如：`ProjectilePoolSize/HitVFXPoolSize/MuzzleFlashPoolSize/FloatingTextPoolSize/...`
- **超出上限：** 必须降级（丢弃/复用/降低粒子数），禁止无上限增长

### URP/后处理策略
- **参考套件的 PostProcessing v2：** 仅用于对照，不进入主线依赖
- **需要后处理：** 统一用 URP Volume 体系实现
- **MVP 建议：** 先关闭，优先确保像素化链路稳定

### 镜头规则（M1 优先级）
- **基础模式：** 跟随玩家居中
- **瞄准模式：** 镜头拉远 + 根据鼠标位置水平偏移
- **约束：** 平滑插值、房间边界 Clamp、像素对齐
- **Pixel Snap 顺序：** Desired → Smooth → Clamp → PixelSnap → Apply（LateUpdate）

## 资源/数据/遗留物：术语与规则（必须写死）

### 术语定义
- **资源（Material）：** 用于制作/改造/升级的材料类物品（可叠加）。回家后走"入库结算"进入 **Meta 仓库**
- **消耗品（Consumable）：** 出征中直接使用（医疗/O2/诱饵等）。可带入/带出；默认可入库
- **装备（Gear）：** 武器/护甲/弹匣等。回家后可入库；死亡按规则掉落（是否掉落由难度/模式决定）
- **未录入数据（UnregisteredData）：** 只能在 Run 中产出（探索/击杀/事件），用于永久成长/解锁，但**不录入则不生效**
- **数据录入（Record）：** 仅在安全屋终端执行。录入会把 UnregisteredData 转化为 **Meta 解锁/成长**，并触发一次存档
- **遗留物（Legacy/CorpseCache）：** 玩家死亡时在死亡点生成的可回收容器，包含：未录入数据（必掉）+（可选）部分物资/装备

### 规则（写死）
1. **入库结算（Run → Meta）**
   - 资源/装备/未使用消耗品：结算进入 Meta 仓库（可在 Result/终端查看清单）
   - 未带回（未撤离/未触发结算）：该趟 Run 所有"可入库物品"清空（除遗留物规则外）

2. **未录入数据**
   - UnregisteredData **不会自动入库**，必须在安全屋终端执行"录入"才会转化为永久成长/解锁
   - UnregisteredData 在死亡时必进入遗留物；若在回收遗留物前再次死亡，则上一份遗留物及其中 UnregisteredData 清空（魂系口径）

3. **遗留物回收**
   - 回收遗留物会把其中物品转移到背包/结算清单；其中的 UnregisteredData 仍需回到终端执行"录入"才生效

4. **存档分层（写死｜MVP）**
   - **MetaSave（永久，最小字段边界）：**
     - `MetaInventory`
     - `UnlockedUpgrades`
     - `TerminalLogs`
     - `Settings`（可选）
   - **RunSave（临时，最小字段边界）：**
     - `RunSeed` / `ZoneID` / `RoomProgress`
     - `Backpack`
     - `PlayerState`（位置/血量可选）
     - `WorldPickupsState`（可选）
   - **写入点（写死）：**
     - 撤离结算、终端录入、遗留物回收：均触发 MetaSave
     - Run 内增量保存频率/触发点由 `DOC-04-SAVE` 细化（**M3 开始前必须封板**；实现可在 M3 阶段落地）

### 仓库事实源（写死）
- **MVP：** `Meta 仓库（MetaInventory）` 为唯一事实源（唯一持久化仓库数据）
- **"安全屋仓库/箱子/储物柜"：** 仅是对 MetaInventory 的 UI/交互呈现（可做多柜标签/分类/搜索，但底层不另存第二套容器存档）
- **若未来需要"物理箱子独立存储"：** 必须提出设计变更并升级 SaveVersion（否则禁止引入第二事实源）

## MVP 五资源 + 三消耗品（写死：名称 / ItemID / 主要用途 / 主要掉落）

> 这 5+3 个 ItemID 必须稳定（掉落表/配方/图标/存档都以 ItemID 为主键），后续允许"换名字/换图标/调权重"，但禁止改 ID。

### 五资源
1. **金属废料（ItemID: MAT_SCRAP）**
   - 用途：基础制作、维修、低阶改造
   - 掉落：居民区容器/拆解家具/常见敌

2. **机械零件（ItemID: MAT_PARTS）**
   - 用途：武器改造、装置修理、机关捷径
   - 掉落：商业街店铺/机械敌/工具箱容器

3. **电子元件（ItemID: MAT_ELECTRONICS）**
   - 用途：终端升级、瞄具/灯具/传感器类改造
   - 掉落：配电箱/电子柜/商业街高价值容器

4. **化学试剂（ItemID: MAT_CHEM）**
   - 用途：医疗/止血强化、诱饵/陷阱配方、感染相关
   - 掉落：药房/实验柜/污染区容器

5. **织物纤维（ItemID: MAT_FABRIC）**
   - 用途：护甲/背包/绑带类制作与升级
   - 掉落：民居衣柜/箱包店/尸体掉落（低价值叠加）

### 三消耗品
1. **止血绷带（ItemID: CON_BANDAGE）**
2. **氧气罐（ItemID: CON_O2）**
3. **诱饵（ItemID: CON_DECOY）**

### 约束
- 五资源 + 三消耗品默认可叠加、可入库（Meta 仓库）
- 掉落权重需支持"区域倾向"（居民区偏 MAT_SCRAP/FABRIC，商业街偏 MAT_PARTS/MAT_ELECTRONICS/MAT_CHEM）

## CastleDB 数据配置主权（Design DB｜写死）

### 核心原则
- **所有"可配置数据"统一使用 CastleDB 作为权威来源**
- **运行时不允许散落多套重复配置**（ScriptableObject/Json/硬编码）互相打架
- **SO（可选）：** 仅允许作为**运行时资产容器/Addressables 引用壳/缓存**；禁止作为权威数值源

### 单库 vs 多库策略
- **MVP 推荐：单库单文件**（1 个 `.cdb`）：`DesignRoot/ZomCityDesign.cdb`
  - 好处：表间引用（ref）与一致性校验更容易收口；早期迭代快
  - 风险：多人同时编辑易冲突；用"数据负责人/时间片"或拆 sheet 负责人降低冲突
- **后续扩展：按域拆分多库**（多个 `.cdb`）
  - 触发条件：多人频繁冲突、或内容量大到单库难维护
  - 约束：拆库后**跨库引用不作为依赖**；跨库关联统一改用"稳定 ID（string）+ Validator 校验"

### 导出与运行时加载口径（写死｜MVP）
- **DesignRoot：** 存放源文件（`.cdb`）
- **GeneratedRoot：** 存放导出产物（**deterministic JSON** + 可选 Generated C# + `DataVersionManifest.json`），**禁止手改**
- **Player（运行时）：** 只读取 `GeneratedRoot`，不解析 `.cdb`；`.cdb` 仅用于 Editor 配表与导出
- **Unity Editor 导入器职责：**
  - 检测 `.cdb` 变更 → 导出 deterministic JSON → `NormalizeExport()` →（可选）生成强类型 C# → 刷新 `DataRegistry`
  - 运行 `DataValidator`：重复 ID、坏引用、缺必填字段、枚举非法、权重异常、跨表不变量、PrefabKeyValidator 等 → 失败则阻断打包/合并门槛
  - 输出：`ReportsOutDir/DataValidatorReport_<yyyyMMdd_HHmmss>.json`

### 运行时数据失败策略（写死）
- **任何 `DataRegistry.Get*(ID)` 若 ID 不存在：**
  - Development：抛出可定位异常 + 事件上报（`DataMissingEvent`），并在屏幕提示当前缺失 ID
  - Release：进入"可见错误页"（含缺失 ID、当前 DataVersion、SaveVersion、场景名），禁止继续在坏状态下运行
- **任何 `PrefabKey` 加载失败：**
  - Development：日志 + 事件上报（`PrefabLoadFailedEvent`）+（可选）允许用 `DebugFallbackPrefab` 占位
  - Release：同上进入"可见错误页"（避免空引用连锁崩溃）

### CastleDB Sheet（表）清单与关系（MVP 必需）

**核心主表（必需）：**
- `Settings`（主键：`SettingsID`，单行）：全局常量/地址/开关（例如 `GenericPickupPrefabKey`）。约束：必须且只能有 1 行；`SettingsID` 固定为 `SETTINGS_MAIN`
- `Items`（主键：`ItemID`）：全物品总表（Material/Consumable/Gear/Weapon/Attachment/UnregisteredData…）
- `RarityTiers`（主键：`RarityID`）：稀有度分级（颜色、默认权重倍率、最小/最大数量修正等）
- `LootTables`（主键：`LootTableID`）：掉落表
- `Containers`（主键：`ContainerID`）：可搜刮容器定义（箱子/柜子/尸体/遗留物）
- `Weapons`（主键：`WeaponID`，建议与 `Items.ItemID` 同键）：武器参数（射速/散布/后坐/噪音/弹匣等）
- `Attachments`（主键：`AttachmentID`，建议与 `Items.ItemID` 同键）：配件参数（槽位/允许武器/属性差值）
- `Enemies`（主键：`EnemyID`）：敌人定义（PrefabKey/Stats/AI/掉落）
- `Rooms`（主键：`RoomID`）：房间定义（RoomPrefabKey、刷怪点方案、容器点方案）
- `Zones`（主键：`ZoneID`）：区域/章节（RoomPool、掉落倾向、敌人池）

**SafeHouse/Meta 扩展表（M3 起用）：**
- `CraftRecipes`（主键：`RecipeID`）：制作配方（输入/输出/工作台类型/解锁条件）
- `MetaUpgrades`（主键：`UpgradeID`）：终端升级/永久解锁（配方解锁、仓库容量、改造槽位开放等）
- `RecordRules`（主键：`RecordID`）：数据录入口径（UnregisteredData 录入→转化为 MetaUpgrades/成长点/解锁项）

### 稳定主键（ID）命名规范（必须写死）
- **ID 为 ASCII：** `A-Z`、`0-9`、`_`；禁止空格/中文；禁止大小写混用
- **正则建议：** `^[A-Z][A-Z0-9_]{2,63}$`
- **禁止改 ID：** 允许改显示名/图标/数值/权重，但改 ID 必须走：变更评审 → DataVersion/SaveVersion 升级 → 迁移映射 → 回归路线补齐
- **前缀白名单：** 见常量清单（`StableIdPrefixWhitelist`）

### 引用策略（ref / string）与资源引用（PrefabKey）
- **表间引用口径：** CastleDB 内部允许使用 `ref` 便于编辑，但导出后/运行时 **一律以 string ID 作为引用主干**
- **Prefab/资源引用：** CastleDB 数据中禁止直接存 UnityEngine.Object 引用
- **统一使用 `PrefabKey`（string）：** MVP 采用 Addressables，`PrefabKey` = Addressables Address

### Addressables 主线最小闭环（MVP 必须封板｜M0）
- **统一约束：** `PrefabKey` = Addressables Address（string），禁止混用 ResourcesPath
- **目录与组策略：** `ContentRoot` 下的运行时资源必须进入 Addressables Group（按域分组：见常量清单 `AddressDomainWhitelist`）
- **Runtime 初始化口径：** Boot 时 `Addressables.InitializeAsync()`；失败则进入可见报错页
- **Address 命名合同（写死｜MVP）：** `ZC/<Domain>/<StableID>`
  - Domain 取值白名单：见常量清单（`AddressDomainWhitelist`）
  - StableID 取对应表主键：如 `EN_*`、`RM_*`、`CT_*`、`MAT_*/CON_*/...`
  - 自动对齐规则（可被 DataValidator 校验）：
    - `Enemies.PrefabKey == "ZC/Enemies/" + EnemyID`
    - `Rooms.RoomPrefabKey == "ZC/Rooms/" + RoomID`
    - `Containers.PrefabKey == "ZC/Containers/" + ContainerID`

### 世界拾取表现（WorldPickup）统一口径（写死｜MVP）
- **MVP 采用"通用拾取 Prefab + 图标渲染"作为默认：**
  - 所有可掉落 Item 都允许没有 `WorldPickupPrefabKey`，此时使用 `GenericPickupPrefabKey`（单一 Addressables Address）
  - `GenericPickupPrefab` 运行时根据 `ItemID/IconKey/Rarity` 渲染外观（Sprite/颜色/描边等）
- **只有在"需要特殊体积/特殊动画/特殊交互"的物品，才填写 `Items.WorldPickupPrefabKey` 覆盖默认表现**
- **`GenericPickupPrefabKey` 定义位置：** CastleDB `Settings` 表（导出到 `GeneratedRoot`；运行时从 `DataRegistry.Settings.GenericPickupPrefabKey` 读取）

### DataVersion（设计数据版本）与 SaveVersion（存档版本）
- **DataVersion：** 随 CastleDB 表结构/语义变更而递增
- **SaveVersion：** 随存档结构变更而递增
- **联动规则（写死）：**
  - 仅改数值/权重/显示名：通常不升 SaveVersion；但可升 DataVersion（可选）
  - 改"解释口径"：必须升 DataVersion，并评估是否需要 SaveVersion
  - 改 ID 或引用方式：必须升 DataVersion + SaveVersion，并提供迁移映射（MigrationMap）

### 存档必须携带版本号与迁移入口（落地条款｜写死）
- **MetaSave/RunSave 顶层字段（写死）：**
  - `int SaveVersion`
  - `int DataVersionAtSave`（保存时的设计数据版本）
- **启动加载流程（写死）：**
  - 读取 Save → 若 `SaveVersion < CurrentSaveVersion`：执行 `SaveMigrator.Migrate(...)` → 写回 → 再加载
  - 若 `DataVersionAtSave < CurrentDataVersion`：按需要触发 `DataCompatPass`

### DataValidator 必需覆盖的跨表不变量（写死最小集）
- `Weapons.WeaponID` 必须对应 `Items.Category=Weapon`
- `Attachments.AttachmentID` 必须对应 `Items.Category=Attachment`
- `LootTables.entries[]`：`Weight>0`，`MinQty<=MaxQty`，引用 `Items` 存在
- `Settings`：必须且只能有 1 行，且 `SettingsID == SETTINGS_MAIN`；否则记为 Error（阻断）
- `Rooms.RoomPrefabKey` / `Enemies.PrefabKey` / `Containers.PrefabKey`：在 Editor 下可加载（Addressables 校验）
- Address 对齐：校验上述 `PrefabKey` 匹配 `ZC/<Domain>/<StableID>`（MVP 先记 Warning，M2 后升级为 Error）
- ID 前缀：所有稳定主键必须匹配常量清单 `StableIdPrefixWhitelist`（非白名单记为 Warning（MVP），M2 后可升级为 Error）
- `Items.NameKey/DescKey/IconKey`：在 Editor 下校验键存在（StringTable/AtlasKeyMap）；缺失记为 Warning（M2 结束前可升级为 Error）
- WorldPickup：若 `Items` 被 `LootTables.entries[]` 引用（或 `Items.CanDropInWorld==true`），则必须满足：
  - `Items.WorldPickupPrefabKey` 存在且可加载，或
  - `DataRegistry.Settings.GenericPickupPrefabKey` 非空且可加载（作为 fallback）
- `RecordRules.consume[]`：必须非空；所有 `consume[].Item` 必须为 `Category=UnregisteredData`，且 `Qty>0`

## 内容生产合同：Room Prefab 最低合同 + 预算 + Validator 分级

### 房间 Prefab 最低合同（必须写死）
每个 Room Prefab 必须包含：
- **RoomRoot**（Transform，Z=0）
- **RoomBounds**（BoxCollider/BoundsData，用于房间相机 Clamp/区域拼接边界）
- **PlayerSpawn**（Transform）
- **EnemySpawn[]**（Transform，可为空）
- **LootPoint[] / ContainerPoint[]**（Transform，可为空）
- **ShortcutGate**（Transform，可为空）
- **ExitGate**（Transform，可为空）
- **可选：NavGraphRoot**（跨层节点与 Jump/Fall Link 的父节点，用于 Gizmos/调试）

### 命名约束
- 锚点命名必须精确匹配（区分大小写），否则 Validator 视为缺失

### 尺寸/网格口径（写死｜MVP）
- **基准：** PPU=16；约定 **1 tile = 1 world unit**（=16 px）
- **RoomBounds/平台边缘/关键锚点位置：** 应尽量对齐 tile 网格（便于相机 Clamp、跳跃高度与内容拼接一致性）

### Gameplay Plane 与锚点 Z 约束（写死｜MVP）
- **RoomRoot 与所有锚点 Z 必须为 0**（或 |Z|≤0.01），不得用锚点 Z 做渲染分层
- **如需视觉分层：** 只允许在 VisualRoot 侧用 SortingOrder/微小 Z；不得影响 PhysicsRoot 与碰撞

### 入口/出口/捷径方向约定（写死｜MVP）
- **`ExitGate`/`ShortcutGate`：** `Transform.position` 为交互中心；`Transform.right` 指向"离开本房间的方向"，且必须为 ±X 轴对齐

### 碰撞层口径（写死｜MVP）
- **可站立/可命中的平台碰撞体：** 必须使用 LayerMatrix 规定的 Ground/OneWayPlatform 等层
- **装饰/无命中碰撞体：** 必须在 NoGameplayHit/Deco，并在 Gameplay 查询中被排除
- **RoomID/PrefabKey/Address：** Room Prefab 必须进入 Addressables；Address 必须满足 `ZC/Rooms/<RoomID>`

### 内容预算（MVP 最小口径）

**预算常量化（写死｜MVP）：**
- `RoomBudget.DynamicPeakMax = 80`（Enemy+Projectile+Loot 峰值）
- `RoomBudget.InteractableMax = 20`
- `RoomBudget.ColliderMax = 200`（非 NoGameplayHit/Deco）
- `RoomBudget.RealtimeLightMax = 8`（启用的实时 Light 组件）
- `RoomBudget.ParticleSystemMax = 32`（启用的 ParticleSystem；不含运行时池化的 combat VFX）

**预算统计口径（写死｜MVP）：**
- **静态 Validator（Editor/CI）：** Prefab 静态遍历计数
- **运行时 Telemetry（PlayMode/可选 nightly）：** 每房间进入后统计实际峰值，落盘到 `ReportsOutDir/RoomBudgetRuntimeReport_<yyyyMMdd_HHmmss>.json`；超标记 Warning（MVP），M6 前升级为 Error 门槛

**例外声明机制（写死｜MVP）：**
- 允许少数特殊房间（Boss房/事件房）突破预算，但必须在 RoomRoot 显式声明：
  - `AllowBudgetOverride=true` + `OverrideReason`（非空）
- 未声明却超预算：按预算规则处理（MVP 先 Warning，M6 前升级为 Error）

### Validator 分级（写死）

**Error（阻塞打包/合并）：**
- 缺少 RoomRoot/RoomBounds/PlayerSpawn
- 关键锚点存在但引用为空（例如 ExitGate 指向缺失）
- 玩法对象 Layer 不合规（Player/Enemy/Projectile/Loot/Interactable）
- Gameplay 查询层混入 NoGameplayHit/Deco 碰撞体
- 房间内存在 Z 漂移（玩法对象 Z 非 0）

**Warning（不阻塞但必须记录）：**
- EnemySpawn/LootPoint/ContainerPoint 为空
- 预算超标但仍可运行（用于内容迭代期）
- NavGraph/JumpLink 缺失（若该房间设计需要跨层）

## 文档封板节点（Doc Gates｜必须标明"最晚在什么阶段前补齐"）

> 目的：把"先写死、再实现"的依赖关系提前，避免到了 M2–M6 才发现口径不清导致 UI/存档/掉落/内容生产返工。

**封板规则：** "封板"= 文档写明 **规则 + 数据主键（ID）+ 变更影响面**；后续若要改，必须走：变更评审 → 版本号/SaveVersion 更新 → 数据迁移/回归路线补齐。

**建议把下列文档放入工程内 `Docs/ZomCity/` 目录，作为合并/打包前的硬门槛项：**

| DocID | 文档 | 必须写死的内容（摘要） | 最晚封板（开始该阶段前） |
| --- | --- | --- | --- |
| DOC-00-TECH | 技术口径封板 | 2.5D 像素化 Render Stack、PPU/OrthoSize 契约、Tick 顺序、PixelViewportManager 唯一入口、Layer/Tag 主权、TimeSettings Ownership | **M0** |
| DOC-08-CDB | CastleDB 数据库与导出规范 | `.cdb` 文件策略、Sheet/子表与关系、稳定主键命名规范、引用策略、导出产物、DataValidator 最小规则集 | **M0** |
| DOC-01-RES | 资源与经济文档 | 玩家可获取的资源/消耗品/装备清单；来源与权重倾向；消耗去向；稀有度分级与叠加/入库规则 | **M2** |
| DOC-02-ITEM | 物品分类与稳定 ID 规范 | ItemID 命名规则；分类；堆叠/重量/格子口径；Icon/本地化键命名；SaveVersion 规则 | **M2** |
| DOC-09-LOC | 本地化与图标键规范 | NameKey/DescKey/IconKey 的命名规则；字符串表位置；IconKey→Sprite 映射 | **M2** |
| DOC-03-LOOT | 掉落与容器规则文档 | 容器类型与可掉落池；区域倾向；稀有度→权重映射；搜刮动画→UI 模态→转移状态机 | **M2** |
| DOC-04-SAVE | 存档分层与字段口径 | MetaSave/RunSave 的字段边界；录入/入库结算/遗留物对存档的写入点；版本升级策略 | **M3** |
| DOC-05-MOD | 武器改造系统规则文档 | 槽位模型、配件类型、属性差值口径、装卸规则、持久化结构与兼容策略 | **M3** |
| DOC-06-DEATH | 死亡/遗留物/回收规则文档 | 魂系口径；二次死亡清空规则；回收 UI/提示；与入库结算/录入的关系 | **M4** |
| DOC-07-ROOM | 房间 Prefab 合同与 Validator 规则 | 最低合同/预算/分级；例外声明机制；内容生产工作流与合并策略 | **M6**（建议 M0 先出草案） |

## 复用策略：把参考套件当"现成子系统库"，但工程主权归 ZomCity

### 复用分级（A/B/C 三档）

**A｜直接复用（优先级最高，几乎零风险）：**
- 模型、动画、音频、粒子、材质、通用 UI 贴图/控件
- 武器/命中特效 prefab（枪口火焰、弹壳、命中火花、血雾）作为占位资源

**B｜复用并改造（主要节省开发量的来源）：**
- 人物/敌人：Humanoid 动画控制器、基础状态参数、上半身瞄准/IK 思路
- 武器系统：射击逻辑、弹丸/命中、枪口/弹壳触发、表面命中特效映射
- 背包与 UI：背包格子 UI、拖拽丢弃、基础拾取/掉落链路（先跑通再重构数据驱动）
- AI：视野/FOV、简单追击/攻击节奏（作为 M0/M1 测试场与 M5 前的占位）

**C｜不复用（必须由 ZomCity 自己实现并掌控）：**
- 像素化渲染链路（Render Stack）、`PixelViewportManager`、DisplayRect/坐标换算
- 游戏状态机（MainMenu/SafeHouse/Run/Result）与 Boot 流程
- 事件总线（GameplayEventHub）+ 事件查看器（可过滤/暂停滚动）
- ZomCity 的 Motor（Kinematic + Sweep/Probe）与 Gameplay Plane 约束组件
- 数据驱动（掉落表、容器、制作/改造、终端录入、永久成长、弹匣/弹种约束）
- Validator（内容护栏）与内容生产规范工具
- **禁用实现：** 禁止采用"角色身上预放置所有 Item GameObject 并解锁"的库存路线；Inventory/Loot 必须以 **Definition + 稳定 ItemID + 数据驱动掉落表** 为核心

### 输入系统适配策略（强烈建议｜默认执行）
- ZomCity 的 InputActions 只保留当前 DoD 需要的 action；其余 action 保留占位但默认不启用
- **M0 阶段默认禁用 Gamepad（仅键鼠）**，后续需要再按里程碑开启

### 命名与"去痕"策略（最终工程禁止出现参考套件字样）

**目标：** 最终产物中不出现参考套件的：
- 目录名、Prefab 名、Scene 名、UI 文案
- C# namespace / class / menu path（AddComponentMenu/CreateAssetMenu）
- Debug/Profiler 里可见的系统名（事件名、Log tag 等）

**推荐做法：**
1. **保持 GUID 不变**进行"原地重命名"（移动/改名脚本文件与 meta 一起移动，避免 prefab 丢脚本）
2. 先做"编译通过 + 场景可跑"，再做"全量去痕扫描"，最后再做"菜单/Prefab/UI 文案细节清理"
3. 建立 `BannedTokenScanner`（Editor/CI）：扫描工程内是否出现禁用词（包括脚本类名、AddComponentMenu、资源名）

## 项目结构（建议落地到 Assets/ZomCity/*）

> 用 asmdef 把"核心底座"和"复用改造层"隔离，避免后续替换时牵一发动全身。

### 建议目录
```
Assets/ZomCity/
├── Core/            # Boot、GameLoop、Time/FramePacing、Save、EventHub、Pooling、Config
├── Data/            # CastleDB：Design 源文件、GeneratedRoot（导出物）、Runtime DataRegistry、DataValidator
├── Rendering/       # PixelViewportManager、WorldRT、UIOverlay、CameraPixelSnap、World↔UI 映射
├── Gameplay/        # Motor、Character、Combat、Inventory、Interact、AI、ThreatSystems
├── UI/              # Screen 栈：MainMenu/Settings/Pause/Result/Backpack/MagSelect
├── Content/         # Scenes、Prefabs、Art、Audio、VFX、Materials
├── Tools/           # Validator、Debug 面板、Spawner、Gizmos、数据校验
└── ThirdPartyRef/   # 仅保留必要第三方依赖；名称不带参考套件痕迹
```

### 建议 asmdef
- `ZomCity.Core`
- `ZomCity.Data`
- `ZomCity.Rendering`
- `ZomCity.Gameplay`
- `ZomCity.UI`
- `ZomCity.Tools`（Editor only）
- `ZomCity.Tests`（可选）

## 物理配置
- **物理类型：** 3D 物理（Physics，非 Physics2D）
- **角色运动控制器：** Rigidbody Kinematic + 自定义 Motor（MovePosition + Sweep/Probe）
- **Fixed Delta Time：** 1/60（60 FPS 目标）
- **Maximum Delta Time：** ≤1/15（限制防止物理跳跃）
- **帧率策略：** VSync=0，targetFrameRate=60（MVP 默认）

### 物理层级（M0 必须定义）
- Player（玩家）
- Enemy（敌人）
- Projectile（子弹）
- Ground（地面）
- OneWayPlatform（单向平台）
- Interactable（可交互物）
- Trigger（触发器）
- Hazard（危险区）
- NoGameplayHit/Deco（装饰碰撞体，禁止进入 Gameplay 查询）
- UI（界面）

### 物理姿态插值（必须实现）
- **FixedUpdate：** 缓存 prevPose/currPose（PhysicsPose）
- **RenderPose：** 用 `alpha = (Time.time - Time.fixedTime) / Time.fixedDeltaTime` 插值
- **使用规则：** Camera/AimPoint/枪口点/IK 目标必须读取 RenderPose（不直接用 Rigidbody）

## 里程碑计划（M0→M7）

> 下列 DoD 以《总计划文档》为准；本计划只补充"如何复用参考套件来减少工作量"。

### M0：工程底座就绪（2.5D 像素化地基封板）

**M0 的定位：** 让武器测试场/怪物测试场/拾取测试区可控可测可回归

**为避免 M0 被工具工作"拖死"，建议在执行层面拆成两档：**
- **M0-Min（最小可运行子集，必须先过）：** Render Stack + PixelViewportManager + Tick/Time/Layer 主权 + EventHub/Viewer + CastleDB 最小链路（Items/Weapons/LootTables/Containers）+ Addressables 初始化/Loader + 3 个测试场可进入
- **M0-Plus（不阻塞，可在 M0 后半或 M1 补齐）：** 一键建场/向导、禁用词扫描、Room/Zone 更完整的 Validator 规则、nightly BuildPlayerContent、RoomBudget 运行时 Telemetry、更多 Editor 辅助工具
- **约束：** 任何"向导/一键生成/编辑效率工具"默认归入 M0-Plus，不阻塞 M0-Min 的 DoD 验收

**新增（本次补充需求，M0 就要落地）：**
- **Git 分支策略：** 从 `main` 切出分支 `2nd`；后续所有开发只在 `2nd` 上进行；稳定后再合并回 `main`（禁止直接提交到 `main`）
  - 合并门槛建议：可打包 Build + 固定回归路线全绿 + 事件查看器关键事件齐全 + BuildSanityCheck/禁用词扫描通过
- **输入：** 暂时禁用手柄，只保留键鼠（Keyboard&Mouse）
  - 实现口径：InputActions 只保留 Keyboard&Mouse bindings（或运行时过滤 Gamepad 设备）；保留一个未来可开启的开关/编译宏以便后续恢复
- **测试场景快速搭建（落地方案）：**
  - 目标：任何人 5 分钟内能新建/重建三大测试场，并一键得到：像素化链路、事件查看器、玩家/相机/UI、基础刷怪刷物品入口、PPU/Ortho/DisplayRect 状态面板
  - 场景清单（建议固定命名）：`Run_WeaponTest`、`Run_MonsterTest`、`Run_PickupTest`、`SafeHouse_Test`
  - 工程化落地：
    1. 制作 `TestSceneBootstrap`（或 SceneTemplate）Prefab：自动创建/引用 PixelViewportManager、WorldRT 输出、UI Overlay、GameplayEventHub + Viewer、TimeSettingsGuard、基础灯光与地面
    2. 提供 Editor 菜单：`Tools/ZomCity/Create Test Scenes`，一键生成上述场景并放置 Bootstrap 与默认测试物体（靶子/刷怪点/箱子）
    3. 运行时 Debug 面板：一键刷武器/刷弹药/刷敌人/切时间/注入噪音事件，所有操作都走 EventHub 以便回放/复现

**自研（必须本阶段完成）：**
- Render Stack（World RT + UI Overlay）与 `PixelViewportManager`
- PPU/OrthographicSize 契约冻结：默认 PPU=16；按公式固定 WorldCamera 的 OrthoSize；Debug 面板显示 PPU/OrthoSize/PixelSnap/DisplayRect/IntegerScale
- InputSampler（Update 采样意图）+ FireRequested（LateUpdate 消费一次）
- Gameplay Plane 约束组件（PhysicsRoot Z 锁、超阈值报错、VisualRoot 排序）
- Layer/Tag 主权收口：LayerCatalog/TagCatalog + 统一 QueryMask（并为 Validator/工具暴露一份"必需清单"）
- Addressables 主线最小闭环：Group/目录规范、Boot 初始化、`AddressablesLoader` 门面、PrefabKeyValidator（Editor/CI）、可选 `BuildPlayerContent`（nightly）
- CastleDB 数据链路：`DesignRoot/ZomCityDesign.cdb` →（deterministic JSON + 可选 Generated C#）→ `DataRegistry`；导入器内置 `NormalizeExport()` 与 `DataValidator`，并写入 `DataVersion`
- `GameplayEventHub` + 事件查看器（Noise/Damage 至少先通）
- Time/FramePacing 冻结口径 + TimeSettingsGuard（持续监测并上报 TamperedEvent）
- BuildSanityCheck（Editor/CI）：扫描 Runtime asmdef 是否引用 UnityEditor、禁用词、Layer/Tag 清单；并运行 CastleDB `DataValidator` + "导出确定性比对"；一键输出报告
  - Doc Gates 自动校验：对 M0 必需文档（DOC-00-TECH、DOC-08-CDB）做"文件存在性 + 关键段落标记"校验
  - VFXPoolBudget 报表：输出到 `ReportsOutDir/VFXPoolBudgetReport_<yyyyMMdd_HHmmss>.json`
- 最小场景：Boot/MainMenu/Run（WeaponTest/MonsterTest/PickupTest）/Result/SafeHouse 空壳

**复用（用于快速起跑与对照验证）：**
- 直接用参考套件的横版 Demo 资源作为"测试场占位"：玩家/敌人/武器/子弹/命中特效/UI 占位
- 复用其输入 actions 的结构（Move/Look/Fire/Aim/Run/Crouch/Reload/Interact/Inventory/Slots），但以 ZomCity 的命名体系重新整理

**本阶段必须做的"改造点"（否则必返工）：**
- 替换所有 Screen→World/World→Screen 换算：统一走 `PixelViewportManager`（UI、准星、浮字、交互提示）
- 禁止任何脚本在 Start/Awake 偷偷改 `Time.fixedDeltaTime`（迁移参考套件相机/慢动作等脚本时必须删除/改造相关逻辑）
- 去除/隔离 Editor-only 代码（运行时代码禁止依赖 UnityEditor）；并用 BuildSanityCheck 强制体检通过
- 后处理：参考套件的 PPv2 仅对照，不进入主线依赖；主线统一 URP Volume（或 MVP 先关闭）

**M0 DoD（关键可测阈值）：**
- AimPoint：窗口缩放/全屏切换后，命中点偏差 ≤ 1 个 RT 像素
- Pixel Jitter：静止+镜头平滑+鼠标缓摆 30 秒，轮廓漂移 ≤ 1 个 RT 像素
- 性能：武器测试场持续射击 60 秒，无明显 GC spike（Profiler 留档）
- 2.5D 像素化链路稳定（无像素抖动，UI 清晰，准星/弹道/命中对齐）
- Gameplay Plane 稳定（持续跑跳 60 秒后 Z 不漂移）
- Integer Scale + DisplayRect 生效（多分辨率下鼠标→AimPoint 一致）
- Pixel Snap 生效（镜头平滑时无明显 pixel jitter）
- 插值生效（波动帧率下角色与镜头无明显抖动）
- 小窗口回归（缩放到最小尺寸时 scale≥1，鼠标→AimPoint 不崩坏）
- DisplayRect 偏移整数化（奇数分辨率下无 0.5 像素游走）
- 阴影回归（镜头平滑+偏移下移动 30 秒无明显 shimmering）
- 事件查看器可用（能看到 Noise/Damage/Pickup/Death/Save 等事件）
- Layer Matrix 固化（地面检测/落地事件稳定）

### M1：Combat Prototype（战斗原型）

**目标：** 战斗"像个游戏"，钉死镜头规则与伤害管线

**自研（ZomCity 标准件）：**
- 三层状态机：Locomotion × Weapon × UI Modal（Backpack/MagSelect 模态屏蔽输入）
- 镜头规则：瞄准触发"拉远 + 水平偏移 + Clamp + 平滑 + PixelSnap(最后一步)"
- Damage Pipeline：`DamageEvent` 成为唯一入口；弱点/浮字/VFX/SFX/硬直从事件派生
- 数据口径：武器/弹匣/配件/基础物品参数统一来自 CastleDB（`Items/Weapons/Attachments`）；Combat 逻辑只依赖稳定 ID + `DataRegistry`
- 精度模型：Spread/Recovery/ShotKick（Hip/ADS × 站立/移动）
- 换弹与弹匣选择 UI（R / 长按 R）

**复用（减少开发量的核心）：**
- 复用参考套件的：
  - 武器脚本整体结构（射击→弹丸→命中→特效/音效→命中反馈）
  - Humanoid 上半身瞄准/LookAt/IK 的实现思路
  - 命中材质/表面特效映射（按 tag/材质类型触发不同 ImpactFX）
- 但必须改造为：
  - Fire 在 LateUpdate 消费（Tick 口径）
  - 命中伤害改为发布 `DamageEvent`（而不是脚本里直接扣血）
  - 子弹/特效走对象池（而不是 Instantiate/Destroy）

**关键交付物：**
- 角色分层状态机（Locomotion × Weapon × UI Modal）
- 360°自由瞄准（3D 骨骼/IK 驱动，像素化呈现）
- 镜头规则（瞄准拉远 + 水平偏移）
- 射击/弹道/精度模型（Spread/Recovery/ShotKick）
- 2 把枪：手枪 + 霰弹枪
- 战斗 HUD/UI（准星扩散可视化、弹药显示）
- 战斗伤害管线（DamageEvent 统一）
- 弱点 & 浮字
- 换弹 + 弹匣选择 UI

**M1 DoD 关键点：**
- 镜头不抖、不穿墙、不把角色推到屏幕外
- 武器测试场：弱点/浮字/准星弹道一致；DamageEvent 在事件查看器中可见
- 持续射击 60 秒无明显 GC spike
- 性能回归用例必须可自动跑（Editor/CI 或 nightly），并输出对比用报告/证据（至少包含帧时间与 GC 指标）

### M2：Loot & Inventory Loop（搜刮与背包循环）

**目标：** "搜—捡—装包—取舍"成立

**自研：**
- 交互系统（F）：门/拾取/终端/制作台/改造台/容器
- 掉落系统数据驱动（CastleDB）：`LootTables/Containers/Zones.lootBias` 统一配置；运行时只读 `DataRegistry`，禁止多套数据源并存
- 资源表封板：按 MVP 五资源 + 三消耗品（ItemID/用途/主要掉落/区域倾向），并落到 CastleDB `Items`；掉落表与 UI 图标以 ItemID 为唯一主键
- **搜刮（Search/Loot）流程（新增需求，建议放在 M2 交付）：**
  - 交互对象：敌人尸体容器（EnemyDeath→生成 LootContainer）/ 箱子容器（预配置或随机生成）
  - 流程：F 交互 → 进入 Searching 状态 → 播放搜刮动画 → 动画事件/计时完成后打开 Loot UI（进入 UI Modal，屏蔽移动/射击/翻滚等冲突输入）
  - UI：Loot UI 双栏（容器/背包）+ 拾取/转移/拆分/丢弃；可选"全部拾取"与基础过滤（按类型）+ 文本搜索
  - 事件：SearchStart/SearchComplete、Pickup/Drop/Transfer 都走 GameplayEventHub，便于回放与回归

**复用并改造：**
- 先复用参考套件"背包 UI + 拖拽丢弃 + 拾取/掉落"链路，快速跑通闭环
- 随后做一次关键重构：从"角色身上预放所有 item 物体并解锁" → 迁移到"数据驱动 + 运行时生成/池化 + 统一物品定义"
- 背包/容器/快捷栏 UI：复用其 Slot UI 结构，但整体 UI 外观、命名、交互提示全部换成 ZomCity 风格
- 先复用其"Loot View"思路（扫描附近容器/尸体并展示），但坐标换算/输入屏蔽必须按 ZomCity 口径重做

**DoD：**
- 尸体/箱子：交互→动画→Loot UI→选择拾取→进入背包链路稳定；Pickup/Drop/Transfer 事件可在事件查看器中复现
- UI 模态不误触射击/翻滚；关闭 Loot UI 后输入状态恢复一致
- 物品系统重构达标：物品定义稳定 ID + SaveVersion；世界掉落/容器掉落池化；不再依赖"角色预置 all-items"才能拾取
- 物品/掉落/浮字/VFX 对象池开始生效（至少覆盖拾取提示、掉落物、基础 VFX）

### M3：Safehouse Meta Loop（安全屋元循环）

**目标：** "带回才算赚到"兑现为永久成长

**自研：**
- 安全屋流程与三台 UI（终端/制作/改造）
- 配置数据统一（CastleDB）：终端升级/制作配方/数据录入规则/改造参数全部从 `DesignRoot/ZomCityDesign.cdb` 读取（`CraftRecipes/MetaUpgrades/RecordRules/Weapons/Attachments`），UI 只展示与触发，不在 Prefab/脚本里写死数值
- 数据录入与永久成长（未录入的数据带回终端才固化）
- 入库结算（Run → Meta）：Result/终端可查看本次结算清单；结算后写入 Meta 仓库并触发 MetaSave
- 存档分层口径：MetaSave（永久）/RunSave（临时）；"录入"与"入库结算"必须触发 MetaSave
- **安全屋仓库（新增需求，建议放在 M3 交付）：**
  - 仓库事实源：MVP 仅持久化 `MetaInventory`，安全屋"箱子/储物柜"只是 UI/交互外壳（可做多柜标签/分类/搜索）
  - 存取 UI：背包↔仓库双栏；支持搜索/过滤/快速存放；所有操作走 GameplayEventHub（Deposit/Withdraw）
  - 规则对齐：Run 里拾取到的物品需要"带回安全屋"并存入仓库才算永久获得（与 Result 结算/丢失规则一致）
- **武器改造系统（新增需求，建议放在 M3 交付；M2 先投放配件与存取链路）：**
  - 配件作为可拾取物品（AttachmentItem）进入背包/仓库；按枪械类型/槽位限制可安装
  - 改造台 UI：选择武器 → 选择槽位 → 安装/拆卸配件 → 展示属性差值（后坐/散布闭合/换弹窗口/噪音等）
  - 数据结构：WeaponDefinition + AttachmentDefinition + WeaponBuild（持久化）；改造结果影响战斗手感与事件（例如 Noise 半径）
  - 限制（MVP 建议）：改造仅在安全屋进行，避免 Run 中引入高复杂度 UI 状态机
- 弹匣/弹种约束（一个弹匣只装一种弹种；改造影响噪音/散布/换弹窗口等）

**可复用点：**
- UI 基础组件（Tabs/OptionContainer/按钮等）作为占位，但需要换皮与重命名
- 物品/武器脚本中的参数字段与展示逻辑可复用（作为仓库 UI/改造台参数展示的第一批字段来源）

### M4：Souls 骨架（魂系骨架）

**目标：** 死亡可逆损失 + 捷径结构成立

**自研：**
- 遗留物规则：死亡生成 Legacy 容器（必含 UnregisteredData）；回收前二次死亡清空上一份遗留物（魂系口径）
- 捷径解锁系统（降低回撤成本）

**复用点：**
- 掉落/拾取链路（M2 已改造后可直接复用到遗留物）

### M5：Threat Systems（威胁系统）

**目标：** 噪音/昼夜/O2感染驱动战术，AI 可调可测

**自研（重点）：**
- 噪音系统：动作/武器/陷阱生成声源 → 事件总线；诱饵重定向
- 平台导航：NavGraph + Jump/Fall Link + Gizmos；保证"跨层寻声到达"可回归
- 昼夜/O2/感染/压力 HUD

**复用点：**
- 视野/FOV 与简单追击逻辑作为占位（用于快速验证"能感知玩家/能追/能打"）
- 但最终寻路必须替换为平台导航（NavMesh 不能覆盖横版平台跳跃的真实可达性）

### M6：MVP Content（MVP 内容）

**目标：** 2 区域内容可不依赖 Debug 打通一趟

**自研：**
- 房间 Prefab 合同（RoomRoot/RoomBounds/PlayerSpawn…）+ 命名约束 + 预算阈值
- Validator 分级：Error 阻塞打包/合并；Warning 仅记录
- 内容数据（CastleDB）：`Zones/Rooms/Enemies/Containers/LootTables` 成为关卡内容的唯一数据源；Validator 同时校验"RoomID→RoomPrefabKey 可加载/锚点命名组一致/引用不为空"
- 出征流程：安全屋出发 → 区域实例 → 回家结算

**复用点：**
- 参考套件的 Gizmo/调试绘制思路可复用到 Validator/关卡工具

### M7：Endgame & Polish（终局与抛光）

**目标：** 终局事件完成并抛光调参

**自研：**
- 信号弹事件：30 秒守点 + 撤离窗口 + 指引 UI
- 精英/变异体：弱点收益与硬直窗口明确

**复用点：**
- 武器/命中反馈/特效资源继续复用；最终以 ZomCity 的数值/事件管线为准

## "参考套件 → ZomCity" 差异修改清单（必须明确，否则复用会变成负债）

### 4.1 输入与 Tick 口径
- **禁止：** 角色脚本在 Update 里直接发射/直接改 Rigidbody
- **必做：** InputSampler(Update) 只写 `PlayerInputIntent`；FireRequested 带 requestFrame；LateUpdate 消费一次
- **复用改造：** 把参考套件的输入门面/ActionMap 结构迁移为 ZomCity 的 `InputActions` 与 `InputIntent`

### 4.2 相机与像素化
- **必做：** 新增 `PixelViewportManager`，所有坐标换算统一入口
- **必做：** 相机规则（Desired→Smooth→Clamp→Snap→Apply）必须在 LateUpdate 执行且 Snap 最后一步
- **禁止：** 任何脚本使用 `Camera.main.WorldToScreenPoint` 作为 UI 映射捷径（必须考虑 DisplayRect/IntegerScale）

### 4.3 Gameplay Plane 与物理根节点
- **必做：** 所有玩法对象（Player/Enemy/Projectile/Loot/Interactable）必须拆分 PhysicsRoot 与 VisualRoot
- **必做：** PhysicsRoot 的 Z 锁 0，VisualRoot 用 SortingOrder 或微小 Z 做渲染分层

### 4.4 战斗与伤害管线
- **必做：** 伤害唯一入口为 `DamageEvent`，表现从订阅派生
- **复用改造：** 把参考套件"直接扣血/直接刷特效"的路径，替换为：
  - Weapon/Bullet 命中 → 发布 DamageEvent
  - DamageSystem 订阅 DamageEvent → 计算弱点/护甲/硬直 → 再发布 SecondaryEvents（HitReact/FloatingText/VFX/SFX）

### 4.5 对象池与性能
- **必做：** Projectile/VFX/FloatingText/LootDrop/NoiseIndicator 池化
- **复用改造：** 将参考套件中广泛存在的 Instantiate/Destroy 改为 Pool.Spawn/Despawn

### 4.6 Editor-only 代码隔离
- **必做：** 任何仅用于 Gizmo/Inspector 的代码必须放到 `Editor/` 或 `#if UNITY_EDITOR`
- **原因：** PC Player Build 时 UnityEditor 程序集不可用，会导致构建失败

### 4.7 全局时间设置主权
- **禁止：** 任意模块私自改 `Time.fixedDeltaTime`（以及 maximumDeltaTime/vSync/targetFrameRate 等时间与节奏参数）
- **必做：** 由 ZomCity 的 TimeSettings/Boot 统一设置（并在 Debug 面板显示当前值）
- **TimeSettings Ownership：** 仅允许在 Boot/Settings 的单点入口设置，并记录到日志
- **TimeSettingsGuard：** Development Build 下持续监测是否被改写；发现改写则立即回写并输出调用栈，同时发布 `TimeSettingsTamperedEvent` 到 EventHub
- **复用改造：** 迁移参考套件的相机/慢动作等脚本时，必须删除或改造其 fixedDeltaTime 改写逻辑

### 4.8 Layer/Tag 主权收口（迁移护栏）
- **必做：** 建立 `LayerCatalog` / `TagCatalog`（常量表或 ScriptableObject），所有代码通过 Catalog 获取 Layer/Mask/Tag，禁止硬编码 layer index/tag 字符串散落
- **复用改造：** 把参考套件中硬编码的 layer index（例如 2/9/14/15）替换为 `NameToLayer("...")` 或 Catalog
- **Validator/CI：** Build 前校验必需 Layer/Tag 存在、关键 Prefab 的 Layer/Tag/Mask 合规；缺失直接阻断 Build
- **Tag Policy：** 玩法判定优先用 LayerMask + 组件接口；如需使用 Tag，仅允许白名单（见常量清单 `TagWhitelist`）

## 去痕执行清单（让最终工程"看起来就是 ZomCity"）

> 这是"复用但不露痕"的关键工作包。建议在 M0 完成后立刻开工，否则后面改名会撞上更多内容资产。

### 目录清理
- 所有可复用资产统一迁入 `Assets/ZomCity/*`，并按 Content/Gameplay/UI/Rendering 分类

### C# 命名体系
- 所有脚本 namespace 统一为 `ZomCity.*`
- 所有脚本类名、AddComponentMenu、CreateAssetMenu、事件名统一为 ZomCity 术语

### 资源命名
- Prefab/Scene/UI 文案/AnimatorController/参数名按 ZomCity 命名规范统一（禁止出现参考套件字样）

### 自动化扫描
- **新增 Editor 工具：** BannedTokenScanner，一键扫描脚本/资源名/菜单路径/Prefab/Scene/UI 文本（含本地化表/字符串表）
- **新增 Editor/CI：** BuildSanityCheck，扫描 Runtime asmdef 是否引用 UnityEditor、禁用词、Layer/Tag 清单合规，并输出报告（作为合并回 main 的门槛）

## 风险与应对（复用常见大坑）

### 风险 1：大规模重命名导致 Prefab/Scene 丢脚本
- **应对：** 坚持"原地改名 + 保持 GUID"原则；移动/重命名尽量在 Unity Editor 内完成；每一步都以可编译/可进入测试场为回归点

### 风险 2：坐标换算没统一，导致"准星/弹道/命中/UI"对不齐
- **应对：** PixelViewportManager 作为唯一入口；禁用直接 WorldToScreenPoint；建立回归路线（多分辨率/窗口缩放/全屏切换）

### 风险 3：时间/步进被第三方脚本篡改，导致"手感漂移/偶发错帧"
- **应对：** TimeSettings + TimeSettingsGuard 统一主权；发现篡改立即回写并发布 `TimeSettingsTamperedEvent`；迁移相机/慢动作脚本必须移除改写逻辑

### 风险 4：Layer/Tag 不一致导致命中/拾取/表面特效/AI 判定异常（"能跑但判定全错"）
- **应对：** LayerCatalog/TagCatalog 收口 + Validator/CI 审计（必需清单、关键 Prefab 合规、QueryMask 合规）

### 风险 5：把 PostProcessing v2 当依赖带进主线，导致 URP/像素化链路维护复杂
- **应对：** PPv2 仅对照；主线统一 URP Volume（或 MVP 先关闭）；后处理只允许在单一入口管线中启用

### 风险 6：Runtime 代码误引用 UnityEditor，导致 Windows Player Build 编译失败
- **应对：** Editor-only 隔离 + BuildSanityCheck 扫描 Runtime asmdef 引用并阻断合并/打包

### 风险 7：先堆内容再补 Validator，导致 M6 失控
- **应对：** M0 就建 Tools 骨架；M2 结束前至少挡住"锚点缺失/Layer 错/引用空"

## 建议的落地顺序（最省时间的施工路径）

1. 先把 M0 地基做对：PixelViewportManager + Render Stack + Tick 顺序 + EventHub + 测试场
2. 用参考套件快速跑通"最小射击/命中/反馈"，但立刻把命中/伤害收口到 EventHub
3. M1 把镜头规则与伤害管线钉死，再扩展武器与 UI
4. M2 跑通搜撤闭环后，立即把"背包/掉落/物品"重构成数据驱动，避免 SafeHouse/改造系统后期推翻
5. M5 之前务必落地平台导航（NavGraph + Jump/Fall Link）

## MVP 范围
- **1 个安全屋：** 制作台 + 改造台 + 信息终端（含"数据录入"）
- **2 个区域：** 居民区下层 + 商业街天桥
- **3 类敌人：** 行尸走肉 / 夜魔 / 听觉型盲猎者（噪音驱动）
- **2 把枪：** 手枪 + 霰弹枪
- **5 种资源：** 金属废料(MAT_SCRAP) / 机械零件(MAT_PARTS) / 电子元件(MAT_ELECTRONICS) / 化学试剂(MAT_CHEM) / 织物纤维(MAT_FABRIC)
- **3 种消耗品：** 止血绷带(CON_BANDAGE) / 氧气罐(CON_O2) / 诱饵(CON_DECOY)
- **死亡与取回：** 死亡掉落未录入数据，可取回（魂系口径）
- **1 条捷径：** 魂系结构感
- **终局事件：** 红色信号弹 + 30 秒守点撤离

## 性能目标
- **帧率目标：** PC（中档机）60 FPS（最低不低于 50 FPS）
- **单局目标：** 一趟出征 8–12 分钟（MVP 调参基线）
- **GC 目标：** Run 场景尽量每帧 GC≈0；武器测试场持续射击 60 秒无明显 GC spike
- **画面目标：** 2.5D 实时像素化输出稳定（无明显像素抖动），鼠标世界坐标与准星/弹道/命中一致

## 关键技术约束

### 必须前置（M0 锁定）
- 2.5D 像素化渲染链路（RT 分辨率、Nearest 放大、UI 分层、坐标换算口径）
- Gameplay Plane 平面约束（Z 漂移会导致命中判定偏移、排序错乱、导航/掉落异常）
- 镜头规则（右键瞄准触发"镜头拉远 + 水平偏移 + Clamp + 平滑"）
- 输入 + UI 模态分层（Backpack/MagSelect 时误触射击/翻滚是高频大雷）
- Physics Layer Matrix + 单向平台规则
- 角色运动控制器（Motor）（CharacterController vs Rigidbody 会直接影响单向平台、翻滚、硬直、斜坡、触发器可靠性与手感）
- 伤害事件口径（DamageEvent 统一收口，弱点/浮字/VFX/SFX/硬直都从事件派生）

### Quality 设置（M0 必须统一）
- **关闭：** MSAA、FXAA/SMAA/TAA（任何抗锯齿都会破坏像素边缘）
- **谨慎：** HDR、Bloom、DoF、MotionBlur（MVP 默认先关闭，后续逐项验证）
- **禁止：** 动态分辨率、RT 采样非 Nearest、双线性过滤导致的模糊

## 输入动作（M0 必须补齐）
当前缺口：
- Roll（翻滚）
- Reload（换弹）
- Backpack（背包）
- QuickSlot1-4（快捷栏 1-4）
- Aim（瞄准，按住/切换模式）

**M0 阶段输入约束：**
- 暂时禁用 Gamepad（仅键鼠）
- InputActions 只保留 Keyboard&Mouse bindings
- 保留一个未来可开启的开关/编译宏以便后续恢复

## 编码规范
- **避免过度工程：** 只做直接请求或明确必要的改动
- **不做过早抽象：** 不为一次性操作创建辅助函数/工具类
- **安全性：** 注意命令注入、XSS、SQL 注入、OWASP top 10
- **注释：** 只在逻辑不明显时添加
- **错误处理：** 只在系统边界（用户输入、外部 API）验证
- **命名空间：** 所有脚本统一使用 `ZomCity.*` namespace
- **禁止参考套件痕迹：** 最终工程不得出现参考套件的目录名、类名、菜单路径、UI 文案

## 测试与回归

### 固定回归路线（任何人照着测）
1. Boot → MainMenu → Settings（切换"右键瞄准按住/切换"）→ Start（打开事件查看器）
2. Run：武器测试场（射击/换弹/选弹匣/弱点/浮字/准星弹道一致；切换 2–3 个常用分辨率 + 16:9/16:10 + 窗口缩放到最小尺寸/全屏（含奇数窗口宽高）：观察 Debug 面板 DisplayRect/IntegerScale，并检查命中点一致性）
3. Run：怪物测试场（刷三类怪：噪音驱动、昼夜、O2/感染；跨层寻路）
4. Run：拾取测试区（拾取/背包/容器/快捷栏）
5. SafeHouse：制作/改造/终端录入（入库结算）
6. Result：带回清单与提示

**回归要求：** 以上流程需用事件查看器确认关键事件可见（Noise / Damage / Pickup / Drop / Death / Save / Craft / Mod / Result）

### 测试场景快速搭建（M0 落地）
- **场景清单：** `Run_WeaponTest`、`Run_MonsterTest`、`Run_PickupTest`、`SafeHouse_Test`
- **工程化落地：**
  1. 制作 `TestSceneBootstrap` Prefab：自动创建 PixelViewportManager、WorldRT、UI Overlay、GameplayEventHub + Viewer、TimeSettingsGuard、基础灯光与地面
  2. Editor 菜单：`Tools/ZomCity/Create Test Scenes`，一键生成测试场景
  3. 运行时 Debug 面板：一键刷武器/刷弹药/刷敌人/切时间/注入噪音事件

## 快速自检清单（对应总计划审阅建议）

- [ ] 0.1 已写死：术语定义 + 规则闭环 + MetaSave/RunSave 分层口径 + 仓库唯一事实源（MetaInventory）
- [ ] 0.2 已写死：五资源 + 三消耗品的名称/ItemID/用途/掉落倾向（掉落表/配方/图标/存档统一以 ItemID 为主键）
- [ ] 0.3 已写死：Room Prefab 合同 + 预算 + Validator 分级（Error 阻断 / Warning 记录）
- [ ] 0.5 已写死：CastleDB 数据主权（单库/多库策略、Sheet/子表关系、ID 命名规范、PrefabKey/Addressables 主线闭环、导出确定性、DataVersion/SaveVersion + 迁移入口、DataValidator 接入 BuildSanityCheck）
- [ ] Tag Policy + TimeSettings Ownership 已写死并被工具链（Validator/Guard/BuildSanityCheck）覆盖
- [ ] Git 分支策略：从 `main` 切出 `2nd`；后续开发只在 `2nd` 上进行
- [ ] M0-Min 可运行：Render Stack + PixelViewportManager + Tick/Time/Layer 主权 + EventHub/Viewer + CastleDB 最小链路 + Addressables 初始化 + 3 个测试场可进入
- [ ] BuildSanityCheck 通过：Runtime asmdef 不引用 UnityEditor、禁用词扫描通过、Layer/Tag 清单合规、CastleDB DataValidator 通过、导出确定性比对通过
- [ ] Doc Gates 校验通过：DOC-00-TECH、DOC-08-CDB 文件存在于 `Docs/ZomCity/`，包含版本号/最后修改日期，验收产物齐全

## Validator 分级升级点（Warning→Error 升级计划｜写死）

| 规则 | MVP 级别 | 升级为 Error 的最晚阶段 |
|---|---|---|
| Address 对齐（`ZC/<Domain>/<StableID>`） | Warning | M2 |
| 非白名单 ID 前缀（见 `StableIdPrefixWhitelist`） | Warning | M2 |
| LOC/IconKey 缺失（NameKey/DescKey/IconKey） | Warning | M2 结束 |
| WorldPickup 无 fallback | Warning | M2 结束 |
| RoomBudget 超标（无 Override 声明） | Warning | M6 前 |

## 重要提示
- **当前阶段：** M0 底座必须稳固后再进入 M1 战斗
- **不做过早内容：** 除非明确要求，否则不创建文档/README 文件
- **像素完美优先：** 所有坐标转换、镜头移动、渲染必须保持像素稳定性
- **事件驱动架构：** 所有主要系统使用 GameplayEventHub 保持松耦合
- **测试场景纪律：** 在整个开发过程中维护武器测试场和怪物测试场用于回归测试
- **数据主权：** CastleDB 为唯一配置数据源，禁止散落多套重复配置
- **复用但不露痕：** 最大化复用参考套件资源，但最终工程不得出现参考套件命名痕迹
- **M0 拆分执行：** M0-Min（最小可运行）必须先过，M0-Plus（工具/向导）可后置，避免被工具工作拖死

## 关键文件路径速查

### 权威文档
- 总计划文档：`Docs/ZomCity/ZomCity总计划文档_v0.2.6.md`
- 工程结构分析：`Docs/ZomCity/JU工程结构分析汇总_2026-02-05.md`
- 开发计划：`Docs/ZomCity/ZomCity项目开发计划规划.md`

### 数据与配置
- CastleDB 源文件：`Assets/ZomCity/Data/Design/ZomCityDesign.cdb`
- 导出产物：`Assets/ZomCity/Data/Generated/*.json`
- 数据版本清单：`Assets/ZomCity/Data/Generated/DataVersionManifest.json`

### 报告输出
- 报告目录：`TempLogs/ZomCityReports/`
- DataValidator 报告：`TempLogs/ZomCityReports/DataValidatorReport_<yyyyMMdd_HHmmss>.json`
- RoomBudget 报告：`TempLogs/ZomCityReports/RoomBudgetRuntimeReport_<yyyyMMdd_HHmmss>.json`
- VFXPoolBudget 报告：`TempLogs/ZomCityReports/VFXPoolBudgetReport_<yyyyMMdd_HHmmss>.json`

### 测试场景
- 武器测试场：`Assets/ZomCity/Content/Scenes/Run_WeaponTest.unity`
- 怪物测试场：`Assets/ZomCity/Content/Scenes/Run_MonsterTest.unity`
- 拾取测试区：`Assets/ZomCity/Content/Scenes/Run_PickupTest.unity`
- 安全屋测试：`Assets/ZomCity/Content/Scenes/SafeHouse_Test.unity`
