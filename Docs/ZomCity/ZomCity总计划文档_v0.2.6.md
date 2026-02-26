# ZomCity 项目总计划文档（2.5D 实时像素化｜含镜头拉远与偏移）

> 项目代号：**《R市：信号弹》（暂定） / ZomCity**  
> 类型：**2.5D 横版平台跳跃射击**（3D 场景 + 角色/怪物实时 3D 渲染后像素化）/ “搜打撤”×“魂”融合  
> 版本：v0.2.6（补齐：Input System 更新模式/采样口径｜UI Overlay 工程口径｜Gameplay Plane 查询规则｜M0 DoD 可测阈值）  
> 更新时间：2026-02-04  
> 依据文档：  
> - `TempLogs/codexDoc/ZomCity初版游戏设计文档(GDD)-2.5D像素化-含镜头拉远与偏移.md`  
> - `TempLogs/codexDoc/R市_信号弹_总体系统需求清单_执行规划.md`（系统落地顺序参考）

## 变更说明（v0.1 → v0.2）
- 新增：2.5D 实时像素化 Render Stack（World RT + UI Overlay + 坐标换算口径）
- 新增：镜头规则（瞄准拉远 + 水平偏移 + Clamp + 平滑）落地口径补全
- 新增：Gameplay Plane 约束（2.5D 平面锁定与深度/排序规则）
- 明确：2D vs 3D 物理的默认推荐与冻结约束（影响单向平台与碰撞矩阵）

## 变更说明（v0.2 → v0.2.1）
- 新增：PPU/OrthographicSize 的世界单位↔像素网格契约（避免抖动/命中偏差返工）
- 新增：RT 整数倍缩放（Integer Scale）+ DisplayRect（鼠标换算唯一来源）
- 新增：Camera Pixel Snap 可执行规则（镜头平滑/偏移下避免 pixel jitter）
- 新增：Physics Plane 与 Visual Plane 解耦（排序不影响碰撞）
- 更新：M0 DoD 与回归路线补齐 16:9/16:10、窗口缩放/全屏切换的坐标一致性检查点

## 变更说明（v0.2.1 → v0.2.2）
- 新增：Integer Scale 的最小窗口尺寸与降级策略（避免 scale=0 与坐标崩坏）
- 新增：DisplayRect 偏移整数化（避免 0.5 像素游走导致的轻微抖动）
- 更新：Camera Pixel Snap 执行顺序（Desired → Smooth → Clamp → Snap → Apply/LateUpdate）
- 新增：3D 物理下角色 Motor 默认推荐与决策截止点（平台手感/单向平台/硬直可靠性）

## 变更说明（v0.2.2 → v0.2.3）
- 明确：RT/PPU/最小窗口默认值（并给出升级 RT 的唯一流程）
- 明确：最小窗口降级策略只保留“强制回弹”（避免实现分叉）
- 新增：权威组件 `PixelViewportManager`（DisplayRect/Scale/坐标换算禁止多实现）
- 新增：Tick/更新顺序（Input/Motor/Camera/AimPoint/Fire）写死，避免“偶发差一帧”

## 变更说明（v0.2.3 → v0.2.4）
- 新增：物理姿态插值（PhysicsPose→RenderPose），解决“偶发抖动/差一帧命中偏差”
- 新增：Time & Frame Pacing 冻结口径（fixedDeltaTime/maximumDeltaTime/VSync/targetFrameRate）
- 新增：URP 光照/阴影稳定性口径（抑制像素化后的 shadow shimmering）

## 变更说明（v0.2.4 → v0.2.5）
- 新增：M0 产出 5.1（Player Motor + 基础移动 + 翻滚/受击占位 + 最小射击），用于测试场回归与底座封板
- 强化：M0 DoD 与 EventHub/Viewer 咬合（Fire→NoiseEvent；命中 Dummy→DamageEvent，禁止只用 Debug.Log）
- 补充：武器测试场加入受击回归入口（Hazard/EnemyStub）以验证 HitReact 与 Z 锁定

## 变更说明（v0.2.5 → v0.2.6）
- 新增：Input System 更新模式与采样口径（Update 采样/Fixed 物理/Late 消费 FireRequested），避免偶发错帧
- 新增：UI Overlay 工程口径（Canvas 模式与 World→Screen 映射口径），避免 UI 被像素化/黑边错位
- 新增：Gameplay Plane “查询规则”（厚度/LayerMask/QueryTriggerInteraction）与 `NoGameplayHit/Deco` 约束，避免误命中装饰碰撞体
- 新增：M0 DoD 可测阈值（AimPoint 误差/Pixel Jitter/GC/帧时间），提升可验收与可回归性

---

## 0. 目标、非目标与验收指标

### 0.1 MVP 目标（必须跑通）
- 在 **1 个安全屋 + 2 个区域**内跑通闭环：准备 → 出征 → 搜刮 → 压力（死亡/环境/夜晚/噪音）→ 回家结算 → 永久成长 → 更强再出征 → 终局撤离
- 任一里程碑都能打包出 **Playable Build**；改数值/改内容不推翻系统

### 0.2 MVP 非目标（不做或不追求）
- 大规模演出与复杂过场（MVP 仅终端文本/图标投放）
- 多平台与复杂分辨率适配（MVP 先锁定 PC：窗口/全屏；移动端后置）
- 联机、排行榜、云存档等在线能力

### 0.3 体验与技术指标（建议写死）
- 帧率目标：PC（中档机）60 FPS（最低不低于 50 FPS）
- 单局目标：一趟出征 8–12 分钟（MVP 调参基线）
- GC/抖动目标：Run 场景尽量每帧 GC≈0；武器测试场持续射击 60 秒无明显 GC spike/帧率抖动
- 画面目标：2.5D 实时像素化输出稳定（无明显像素抖动），鼠标世界坐标与准星/弹道/命中一致

### 0.4 目标机型（MVP 性能基线，建议写死一条）
- CPU：4C/8T（例如 i5-8xxx / R5 3600 同级）
- GPU：GTX 1060 / RX 580 同级（或更低一档你也可自行定义）
- 内存：16GB
- 测试口径：1080p 窗口模式；`vSyncCount=0` + `targetFrameRate=60`；Integer Scale 生效；RT=320×180

---

## 1. MVP 验收边界（交付口径）

MVP 需要满足：
- 1 个安全屋：制作台 + 改造台 + 信息终端（含“数据录入”）
- 2 个区域：居民区下层 + 商业街天桥
- 3 类敌人：行尸走肉 / 夜魔 / 听觉型盲猎者（噪音驱动）
- 2 把枪：手枪 + 霰弹枪
- 5 种资源 + 3 种消耗品（建议：医疗包/止血剂、氧气罐、诱饵）
- 死亡掉落与可取回遗留物（未录入数据）
- 1 条捷径（魂系结构感）
- 终点事件：红色信号弹 + 30 秒守点撤离

---

## 2. 项目现状盘点（截至 2026-02-03）

### 2.1 工程现状
- Unity：`6000.3.6f1`；URP/Input System 已安装
- 当前内容：模板工程（SampleScene + URP Settings）；无正式玩法代码
- 输入：已有 `InputActions`（Move/Look/Attack/Interact/Crouch/Jump/Prev/Next/Sprint 等），但与 GDD 要求仍有缺口（Roll、Reload、Backpack、QuickSlot1-4、Aim 模式等）

### 2.2 结论
当前处于“可玩性为 0”的模板阶段；必须先把 **2.5D 像素化渲染链路 + 摄像机规则 + 输入/状态机底座**立起来，再做战斗与闭环，避免后期推翻返工。

---

## 3. 关键路径与系统依赖（先对齐，避免返工）

### 3.1 必须前置（不做会返工）
- **2.5D 实时像素化渲染链路（M0）**：RenderTexture 分辨率、Nearest 放大、UI 分层、坐标换算口径；否则后期“准星/弹道对不齐、像素抖动、UI 变糊”必返工
- **Gameplay Plane 平面约束（M0）**：玩法对象 Z 漂移会导致命中判定偏移、排序错乱、导航/掉落异常；必须早期写死规则并做回归用例
- **镜头规则（M1）**：右键瞄准触发“镜头拉远 + 水平偏移 + Clamp + 平滑”；这是操控与战斗体验的核心
- **输入 + UI 模态分层（M0/M1）**：Backpack/MagSelect 时误触射击/翻滚是高频大雷
- **Physics Layer Matrix + 单向平台规则（M0）**：平台/触发器/拾取/伤害碰撞规则不钉死，后续内容会互相打架
- **角色运动控制器（Motor）（M0 结束前必须定）**：CharacterController vs Rigidbody（Dynamic/Kinematic）会直接影响单向平台、翻滚、硬直、斜坡、触发器可靠性与手感；必须早期定口径
- **伤害事件口径（M1）**：DamageEvent 统一收口，弱点/浮字/VFX/SFX/硬直都从事件派生，避免散落导致后期数值与表现返工
- **平台导航方案（M5 前必须确定）**：NavGraph + Jump/Fall Link，否则“听觉型怪物寻声而来”会失真或卡死
- **事件总线 + 事件查看器（M0）**：噪音/伤害/拾取/结算/死亡等系统咬合强，没有可视化会拖慢开发与定位
- **对象池 + 性能预算（M1 起约束）**：Projectile/VFX/浮字/掉落/噪音指示器等短生命周期对象必须池化
- **Validator（M6 前必须有）**：内容堆起来后缺锚点/层级错/引用空会变成“偶发 Bug”，必须用工具提前挡住

### 3.2 推荐依赖链（简化）
Input/Settings
→ UI Framework（Screen 栈 + Modal）
→ 2.5D Pixel Render Pipeline（World RT + UI Overlay + 坐标换算）
→ Gameplay Plane（平面锁定 + 深度/排序口径）
→ Gameplay State（MainMenu/SafeHouse/Run/Result）
→ Physics Layer Matrix（碰撞矩阵/单向平台）
→ Character Motor（Rigidbody Kinematic + 自定义 Motor）
→ Player FSM（Locomotion × Weapon × UI Modal）
→ Weapon & Camera Rule（Spread/Recovery/ShotKick + 镜头拉远/偏移）
→ Damage Pipeline（DamageEvent）& EventHub
→ Loot/Inventory（Interact + Backpack + QuickSlot）
→ SafeHouse（Craft/Mod/Terminal + Meta Progress）
→ Death & Retrieval（Souls）
→ Platform Navigation（NavGraph + Jump/Fall Link + Gizmos）
→ Threat Systems（Noise/AI/昼夜/O2感染）
→ Content Pipeline（房间模块/预算/Validator/终局事件）

---

## 4. 里程碑（M0 → M7）

> 每个里程碑都必须产出一个可运行 Build；每次迭代都必须能按固定回归路线复测（见 6.1）。

| 里程碑 | 目标一句话 | 关键交付物（可验收） |
|---|---|---|
| M0 工程底座就绪 | 架子搭好，2.5D 像素化链路与基础 UI/输入稳定 | Boot/状态机/基础UI/2.5D像素化渲染链路/Physics Layer/PlayerMotor+基础移动+翻滚/受击占位+最小射击（回归用）/数据&存档骨架/事件总线+事件查看器/调试入口/测试场景骨架 |
| M1 Combat Prototype | 打起来“像个游戏”，并把镜头规则与伤害管线钉死 | 360°自由瞄准（3D骨骼/IK）/镜头拉远与偏移/精度模型/伤害管线（DamageEvent）/弱点&浮字/换弹&选弹匣UI/武器测试场/性能回归 |
| M2 Loot & Inventory Loop | “搜—捡—装包—取舍”成立 | 交互系统/掉落系统/背包&容器&快捷栏UI/基础消耗品/拾取测试区 |
| M3 Safehouse Meta Loop | “带回才算赚到”兑现为永久成长 | 安全屋流程/制作&改造&终端UI/数据录入&解锁/弹匣&弹种约束/结算入库 |
| M4 Souls 骨架 | 死亡可逆损失 + 捷径结构成立 | 遗留物回收/二次死亡惩罚/捷径解锁/死亡&结算UI/回归稳定 |
| M5 Threat Systems | 噪音/昼夜/O2感染驱动战术，AI 可调可测 | 三类 AI/噪音事件总线/平台导航+Gizmos/昼夜/O2感染/压力HUD/怪物测试场回归 |
| M6 MVP Content | 2 区域内容可不依赖 Debug 打通一趟 | 房间模块化/内容预算/Validator/两区域纵向路线/终端投放/出征回家完整流程 |
| M7 Endgame & Polish | 终局事件完成并抛光调参 | 信号弹守点撤离+UI/精英/参数化/表现与平衡/可重复回归 |

---

## 5. 落地执行拆解（按“先跑起来→再闭环→再压力→再内容”）

### M0：工程底座就绪（2.5D 像素化项目的“地基”）
**产出 1：工程结构与规范**
- 建议目录：`Assets/ZomCity/*`（Scenes/Scripts/Prefabs/Configs/UI/Art）
- asmdef 拆分：Core / Gameplay / UI / Tools / Tests

**产出 2：Boot + 游戏状态机**
- 状态：MainMenu / SafeHouse / Run / Result
- 切场：最小 Loading/淡入淡出（避免 RT 链路切换时闪屏）

**产出 3：UI 框架（最小可用）**
- UGUI + Input System UI 输入
- Screen 栈：MainMenu / Settings / Pause / Result
- Modal 层：后续 Backpack/MagSelect 复用同一套“模态屏蔽输入”规则

**产出 4：2.5D 实时像素化渲染链路（M0 必须锁定）**
- Render Stack（执行口径）：WorldCamera → 低分辨率 RenderTexture（默认：320×180；升级规则见 7.关键决策）
  → Output（全屏 Quad/RawImage/Blit）以 Nearest 放大输出
  → UI Overlay（原生分辨率渲染，不参与像素化）
- Quality 开关（M0 必须统一）：关闭 MSAA/任何 AA（FXAA/SMAA/TAA）、动态分辨率；RT 与显示材质采样必须 Point/Nearest
- RT 整数倍缩放（Integer Scale）+ DisplayRect（Letterbox/Pillarbox）：由 `PixelViewportManager` 统一计算（禁止多实现），作为鼠标→RT→World 的唯一映射来源（Debug 面板可显示）
- 世界单位↔像素网格契约：锁定 PPU（默认 16），并据此计算 OrthographicSize；相机输出启用 Pixel Snap（见 6.2）
- 可选：调色板限制/抖动（Dithering）作为后续扩展（先留插槽）
- UI 分层：UI 在原生分辨率单独渲染（不参与像素化），避免文字/图标模糊
- 坐标口径：鼠标屏幕坐标 → DisplayRect 本地坐标（整数倍缩放后的 RT 显示区）→ RT 像素坐标 → WorldCamera 正交范围 → 与 Gameplay Plane 交点 = AimPoint；准星/弹道/命中严格一致（回归覆盖多分辨率 + 窗口缩放/全屏切换）

**产出 5：输入系统 + 设置项骨架**
- Actions 补齐：Roll/Reload/Backpack/QuickSlot1-4/Aim（按住/切换）等
- Settings：右键瞄准“按住/点按切换”；键位重绑入口（最小版：存/读）
- Input System 口径（M0 写死）：Project Settings > Input System Package，`Update Mode = Process Events In Dynamic Update`

**产出 5.1：Player Motor + 人物基础移动/控制 + 最小射击（M0 必做，用于测试场回归）**
- 目标：让 `Run_WeaponTest` / `Run_MonsterTest` 可控可测；同时验证 Plane 约束、Tick 顺序、AimPoint 换算、黑边 clamp、Pose 插值与事件查看器
- 覆盖动作（M0 最小闭环）：正常移动 / 跑 / 跳 / 蹲 / 蹲下移动 / **翻滚（最小占位）** / **受击硬直（最小占位）** / 射击（不做 Spread/换弹/弱点等，留到 M1）
- 实现步骤（口径写死，与 6.2 Tick 顺序一致）：
  1) Update：采样 Move/Sprint/Jump/Crouch/Fire，写入 `PlayerInputIntent`；Fire 只置位 `FireRequested`（不在 Update 直接发射），并携带 `requestFrame = Time.frameCount`（LateUpdate 仅消费一次，防重复/错帧）
  2) FixedUpdate：`Rigidbody(Kinematic)` + 自定义 Motor（Sweep/Probe）；处理 Walk/Run、GroundCheck、Jump、Crouch（CapsuleHeight/Center 与速度修正），并强制 PhysicsRoot Z=0
     - Roll（最小占位）：由 `RollRequested` 触发；Motor 在固定时长内使用固定速度倍率（仅 X 轴）；结束后回到 Locomotion；期间强制 Z=0
     - HitReact（最小占位）：由 `DamageEvent` 或 `Hazard/EnemyStub` 触发；进入短硬直并屏蔽移动输入（可选轻击退，仅 X 轴）；期间强制 Z=0
  3) Update/LateUpdate：按 RenderPose 插值口径产出 `renderPose`；视觉/相机跟随/瞄准读取 renderPose（避免“差一帧”）
  4) LateUpdate：通过 `PixelViewportManager.ScreenToWorldOnPlane` 计算 AimPoint（黑边按 DisplayRect clamp；需要 UI 表现则用 `TryScreenToRT` 驱动准星吸附边缘）
  5) LateUpdate：消费 `FireRequested` 执行最小射击验证（Raycast/SphereCast 或单发 Projectile）
     - Fire 必须走 `GameplayEventHub`：发布 `NoiseEvent(pos,intensity,radius)`（字段占位即可），并在事件查看器可见（禁止只写 Debug.Log）
     - 命中 `DamageDummy` 必须走 `GameplayEventHub`：发布 `DamageEvent(attackerId,targetId,damage,damageType,isWeakpoint,hitPos)`（字段占位即可），并在事件查看器可见（禁止只写 Debug.Log）
  6) 回归：WeaponTest 中验证走/跑/跳/蹲走+射击对齐；MonsterTest 中验证跨层移动与 Z 不漂移
  - 验收/自测（M0 产出 5.1）：
    - Roll：持续时间/速度倍率/输入屏蔽规则明确；触发期间 Z=0；与单向平台交互不穿透
    - HitReact：触发条件与硬直时长明确；硬直期间输入屏蔽规则明确；Z=0；事件查看器可见 DamageEvent（占位）

**产出 6：Physics Layer Matrix & 单向平台规则（M0 必须钉死）**
- Layer：Player / Enemy / Projectile / Ground / OneWayPlatform / Interactable / Trigger / Hazard / NoGameplayHit（或 Deco） / UI
- 碰撞矩阵：明确哪些碰撞、哪些忽略，并固化为项目约定
- 地面检测/落地事件稳定（用于噪音、翻滚、跳跃联动）

**产出 7：数据层与存档骨架（Meta/Run + SaveVersion）**
- SO：武器/弹种/弹匣/配件/物品/掉落表/配方/敌人/房间点位配置（统一稳定 ID）
- Save：Meta（永久）与 Run（临时）拆分；带 `SaveVersion` 与最小兼容策略

**产出 8：GameplayEventHub + 事件查看器（M0 必交付）**
- EventHub：Noise/Damage/Pickup/Drop/Death/Save/Craft/Mod/Result 统一发事件
- 事件查看器：最近 N 条、可过滤、可暂停滚动（用于复现与定位）

**产出 9：调试入口（M0 必需）**
- Debug 面板：刷物品/刷怪/切时间/注入 O2&感染/快速到终局入口（后续 M7 用）；显示 RT/PPU/DisplayRect/IntegerScale/PixelSnap 状态（用于回归）

**产出 10：两张长期测试场景（骨架先搭好）**
- 武器测试场：假人（Damage Dummy）+ 弱点/伤害数字/准星与弹道对齐检查点 + `Hazard/EnemyStub`（最小版：可对玩家造成一次 Damage，用于触发 HitReact 回归；Viewer 可见 DamageEvent，占位字段即可）
- 怪物测试场：刷三类怪 + 跨层/跳跃/下落导航用例（为 M5 铺路）

**M0 DoD**
- 2.5D 像素化链路稳定：无明显像素抖动；UI 清晰；准星/弹道/命中一致
- Gameplay Plane 稳定：持续跑跳/翻滚/受击/掉落 60 秒后，玩法对象 Z 不漂移
- Integer Scale 与 DisplayRect 生效：窗口/全屏、16:9/16:10 下鼠标→AimPoint 一致
- 基础移动与最小射击回归：走/跑/跳/蹲/蹲走/翻滚/受击硬直稳定（Z=0 不漂移）；鼠标在黑边时 AimPoint clamp 不跳变；射击命中与准星一致（最小验证）；事件查看器可见 DamageEvent/NoiseEvent（占位）
- Pixel Snap 生效：开启镜头平滑与瞄准偏移时无明显 pixel jitter
- 插值回归：启用 RenderPose 插值后，角色与镜头在波动帧率下无明显抖动
- 小窗口回归：窗口缩放到最小尺寸时 DisplayRect 仍有效（scale>=1），鼠标→AimPoint 不崩坏
- DisplayRect 偏移整数化：在奇数分辨率/窗口尺寸下无 0.5 像素游走
- 阴影回归：镜头平滑+偏移情况下移动 30 秒，阴影不出现明显 shimmering
- 事件查看器可用：能看到 Noise/Damage/Pickup/Death/Save 等关键事件
- Layer Matrix 固化：地面检测/落地事件稳定；单向平台规则明确
- M0 DoD 可测阈值（建议写死，便于回归与自动化）：
  - AimPoint 误差：同一屏幕点在窗口缩放/全屏切换后，命中点偏差 ≤ **1 RT 像素**
  - Pixel Jitter：角色静止、镜头平滑开启、鼠标左右缓慢摆动 30 秒，轮廓漂移 ≤ **1 RT 像素**
  - GC：武器测试场持续射击 60 秒，单帧 GC Alloc 峰值 ≤ **1KB** 且无周期性尖刺（Profiler 留档）
  - 帧时间：目标机型（见 0.4）下 95th percentile 帧时间 ≤ **20ms**（≈50FPS）

---

### M1：Combat Prototype（战斗 + 镜头规则 + 伤害管线一次钉死）
**产出 1：角色分层状态机（Locomotion × Weapon × UI Modal）**
- Locomotion：Idle/Walk/Run/Crouch/Roll + Jump/Fall（抓边/攀爬可后补）
- Weapon：Hip/ADS + Ready/Firing/Reloading/MagSelect
- UI Modal：Normal/Backpack/MagSelect（模态下屏蔽冲突输入）

**产出 2：360°自由瞄准（3D 骨骼/IK 驱动，像素化呈现）**
- 鼠标控制准星；根节点按鼠标左右侧 Yaw 180° 转身
- 上半身/手臂骨骼旋转或 IK 指向准星（MVP 可先用简化 IK/LookAt）

**产出 3：镜头规则（GDD 5.2.3：瞄准拉远与偏移）**
- 非瞄准：基础距离跟随（角色居中）
- 瞄准：镜头拉远 + X 轴偏移（鼠标在右偏右、在左偏左）
- 约束：偏移平滑、Clamp、与场景/房间边界共同约束；避免鼠标抖动导致镜头抖动

**产出 4：射击/弹道/精度模型**
- Spread/Recovery/ShotKick 四态（Hip/ADS × 站立/移动）；准星可读且与弹道一致
- 手枪 + 霰弹枪（节奏明显不同）

**产出 5：战斗 HUD/UI（最小可用）**
- 准星扩散/闭合可视化必须与弹道一致
- 弹药 UI：当前弹匣/备用弹药（或备用弹匣）/当前弹种；换弹与选弹匣提示

**产出 6：战斗伤害管线与碰撞体约定（M1 必须钉死）**
- Hurtbox/Weakpoint 命名、Layer、Collider 规则统一
- 统一事件：`DamageEvent(attackerId, targetId, damage, damageType, isWeakpoint, hitPos)`
- VFX/SFX/浮字/硬直从 DamageEvent 订阅派生（不要散落在脚本里）

**产出 7：换弹 + 弹匣选择 UI（R / 长按 R）**
- R：换弹；空仓自动换弹
- 长按 R：弹匣选择 UI（进入后暂停射击逻辑；避免输入穿透）

**M1 DoD**
- 右键瞄准触发镜头拉远与偏移，且不抖、不穿墙、不把角色推到屏幕外
- 武器测试场可重复回归：弱点/浮字/准星弹道一致；DamageEvent 可在事件查看器中观察
- 性能回归：持续射击 60 秒无明显 GC spike（Projectile/VFX/浮字/掉落/噪音指示器走对象池）

---

### M2：Loot & Inventory Loop（把“搜与撤”做出来）
**产出**
- 交互系统（F）：门/拾取/终端/制作台/改造台/容器
- 掉落系统：敌人/容器/拾取点；权重/稀有度数据驱动
- 背包 + 容器 + 快捷栏 UI：B/1-4
- MVP 三消耗品落地：医疗包/止血剂、氧气罐、诱饵（诱饵与噪音系统联动）

**M2 DoD**
- 拾取→进包→快捷栏使用→丢弃/转移链路稳定；关键事件可在事件查看器中复现（Pickup/Drop/Use）

---

### M3：Safehouse Meta Loop（把成长兑现）
**产出**
- 安全屋三台：终端/制作/改造（UI 可用、流程可跑通）
- 数据录入：未录入升级数据带回终端录入 → 固化为永久成长/解锁项
- 弹匣/弹种约束：一个弹匣只装一种弹种；不同弹匣支持弹种不同
- 结算：Run → SafeHouse 入库清单可见；未带回清空规则明确

**M3 DoD**
- 改造台能看到至少 2–3 个核心参数的变化对比（噪音/散布闭合/换弹窗口等）

---

### M4：Souls 骨架（死亡可逆损失 + 捷径）
**产出**
- 死亡遗留物：未录入数据掉落 → 下次回收；回收前二次死亡清空
- 捷径解锁：门禁/破拆通道/单向梯等，明确降低回撤成本
- UI：死亡提示/遗留物标记/取回反馈/Result 结算提示

**M4 DoD**
- 玩家不看文档也能从 UI 理解“带回/录入/回收”规则；遗留物引导在 2 区域可用

---

### M5：Threat Systems（AI/噪音/昼夜/O2感染）
**产出**
- 三类 AI：行尸走肉/夜魔/听觉型盲猎者
- 噪音系统：动作/武器/陷阱生成声源；诱饵可重定向
- 平台导航：NavGraph + Jump/Fall Link；Gizmos 可视化（节点/Link/路径/声源点）
- 昼夜：夜魔增强、视野受限、照明与暴露风险（最小版即可）
- O2/感染：O2=0 后感染累积；污染雾加速；O2>0 可被动恢复感染（如做成长线）
- 压力 HUD：噪音/昼夜/O2/感染/照明状态可读

**M5 DoD**
- 盲猎者能在怪物测试场“跨层寻声到达”稳定回归；Gizmos 可开关用于调试

---

### M6：MVP Content（两区域打通）
**产出**
- 房间模块化（Prefab 锚点规范）+ 内容预算落地（主路/支路/风险点/捷径间隔）
- 出征流程完整化：安全屋出发 → 区域实例 → 回家结算
- 终端投放：任务/日志/地图碎片（最小可用）
- Validator：Build 前/Editor 校验房间锚点、Layer、Collider、关键引用

**M6 DoD**
- 两区域内容不依赖 Debug 面板也能打通；Validator 对两区域房间校验无阻塞性错误

---

### M7：Endgame & Polish（终局与抛光）
**产出**
- 信号弹事件：触发不可逆、30 秒守点、撤离窗口与撤离区指引（UI 齐全）
- 至少 1 个精英/变异体：有可学习前摇/硬直窗口与弱点收益
- 参数化与抛光：精度模型、噪音半径、掉落/消耗/成长曲线围绕“单局 8–12 分钟”调参
- 快速到终局的调试入口（回归用）

**M7 DoD**
- 终局事件可重复回归，玩家不看攻略也能靠 UI 完成撤离

---

## 6. 横向保障（必须长期维护）

### 6.1 固定回归路线（任何人照着测）
1. Boot → MainMenu → Settings（切换“右键瞄准按住/切换”）→ Start（打开事件查看器）
2. Run：武器测试场（射击/换弹/选弹匣/弱点/浮字/准星弹道一致；切换 2–3 个常用分辨率 + 16:9/16:10 + 窗口缩放到最小尺寸/全屏（含奇数窗口宽高）：观察 Debug 面板 DisplayRect/IntegerScale，并检查命中点一致性）
3. Run：怪物测试场（刷三类怪：噪音驱动、昼夜、O2/感染；跨层寻路）
4. Run：拾取测试区（拾取/背包/容器/快捷栏）
5. SafeHouse：制作/改造/终端录入（入库结算）
6. Result：带回清单与提示

回归要求：以上流程需用事件查看器确认关键事件可见（Noise / Damage / Pickup / Drop / Death / Save / Craft / Mod / Result）。

### 6.2 技术规范（写死，减少扯皮）
**2.5D 像素化 Render Stack（执行口径）**
- Camera：WorldCamera → RenderTexture（低分辨率）
- Output：将 RT 以 Nearest 放大输出（全屏 Quad/RawImage/Blit 均可，但采样必须 Point）
- UI：UICamera/UI Overlay → 原生分辨率渲染（不参与像素化）
- 画面稳定性：必要时对齐像素网格（Camera Snap），避免 pixel jitter（尤其是镜头平滑/偏移时）

**世界单位 ↔ 像素网格契约（必须锁定）**
- 定义：PPU（Pixels Per Unit）= 角色与关卡的“像素网格尺度”
- 默认（MVP）：PPU=16（即 1 world unit ≈ 16 px），与平台关卡 tile/grid 更易对齐
- 公式：`OrthographicSize = (RT_Height / PPU) / 2`
  - 例：RT=320×180，PPU=16 → OrthographicSize = (180/16)/2 = 5.625
- 约束：
  - PPU 一旦锁定，关卡模块尺寸（tile/平台高度/跳跃高度）全部基于该网格
  - 禁止随意改 OrthographicSize（需要改必须同步改 PPU 与回归用例）
- 回归用例：同一目标点在不同分辨率/窗口缩放下，AimPoint 与命中保持一致

**Camera Pixel Snap（可执行规则）**
- 原则：WorldCamera 的投影应使“世界坐标 → RT 像素坐标”尽量落到整数像素网格
- 建议实现：
  - 计算每像素世界单位：`pixelWorld = 1 / PPU`
  - 将相机位置 (camX, camY) 量化到 `pixelWorld` 的整数倍（或在输出矩阵层做 snap）

**Pixel Snap 执行顺序（必须统一）**
1) 计算 DesiredPos（跟随 + 瞄准偏移）
2) Smooth/Damp（平滑）
3) Clamp（相机边界/房间边界）
4) Pixel Snap（最后一步，对齐到 1/PPU 的网格）
5) Apply（建议在 LateUpdate 执行，避免与角色移动不同步）
- 回归用例：持续瞄准移动鼠标 30 秒无明显 pixel jitter；角色边缘与平台边缘不“游走”

**Quality 开关清单（M0 必须统一）**
- 关闭：MSAA、FXAA/SMAA/TAA（任何抗锯齿都会破坏像素边缘）
- 谨慎：HDR、Bloom、DoF、MotionBlur（MVP 默认先关闭，后续逐项验证）
- 禁止：动态分辨率、RT 采样非 Nearest、双线性过滤导致的模糊

**Time & Frame Pacing（M0 冻结口径）**
- `Time.fixedDeltaTime`：默认 1/60（MVP）
- `Time.maximumDeltaTime`：建议 clamp（例如 ≤ 1/15），避免掉帧时一帧跑过多物理步导致跳动
- 帧率策略（MVP 默认写死）：`QualitySettings.vSyncCount = 0`，`Application.targetFrameRate = 60`
  - 备注：如果后续提供“开启 VSync”的设置项，则切换为 `vSyncCount = 1` 且不再额外限制 `targetFrameRate`
- 回归用例：低帧/卡顿模拟时，DisplayRect/AimPoint 不崩坏；镜头与角色不“跳帧”

**URP 光照/阴影稳定性（像素化项目的 MVP 口径）**
- MVP 默认：减少/关闭高频闪烁来源（过强 Bloom、实时 AO、高频细碎高光）
- 阴影策略：优先稳定硬边；控制级联/分辨率在可回归范围；避免因相机平滑导致的 shadow shimmering
- 回归用例：镜头平滑+偏移下移动 30 秒，阴影与高光不出现明显闪烁/游走

**Gameplay Plane（2.5D 平面约束）**
- 坐标约定：X=水平，Y=垂直，Z=深度（仅用于渲染排序/轻微分层）
- 约束规则：所有“玩法对象”（Player/Enemy/Projectile/Loot/Interactable）运行时 Z 锁定到 0（或固定小范围），禁止自由漂移
- 有效厚度（封板）：`PlaneThickness = 0.5`（world unit），所有玩法 PhysicsRoot 的 |Z| 必须 ≤ `PlaneThickness/2`（超阈值视为错误）
- 排序策略：Z 只用于渲染层级（例如 0、±0.01），不参与玩法位移
- 回归用例：持续翻滚/跳跃/受击/掉落 60 秒后，Z 不漂移；命中点与准星一致

**Physics Plane 与 Visual Plane 解耦（强制约束）**
- 物理（PhysicsRoot）：Player/Enemy/Projectile/Loot 的 Rigidbody/Collider 永远锁定 Z=0；禁止通过修改 Root 的 Z 做排序
- 渲染（VisualRoot 子节点）：允许用微小 Z 偏移（如 0、±0.01）或 Renderer SortingOrder 做视觉层级；任何视觉偏移不得影响 Collider
- 回归用例：高频翻滚/跳跃/掉落/近距离射击下不出现“穿平台/错过触发器/子弹贴边穿透”的偶发问题

**Physics Query 口径（必须统一）**
- GroundCheck / AimRay / BulletCast 等所有查询必须使用明确 LayerMask（禁止默认“全打”）
- `NoGameplayHit/Deco` 层：装饰与非玩法碰撞体专用；Projectile/AimQuery 必须排除命中
- 所有查询必须明确 `QueryTriggerInteraction`（默认忽略 Trigger，除非特定系统需要）

**RT 整数倍缩放（Integer Scale）与 DisplayRect（唯一口径）**
- 计算：
  - `scale = floor(min(ScreenW/RTW, ScreenH/RTH))`；并 clamp 到 `scale >= 1`
  - `displayW = RTW * scale`；`displayH = RTH * scale`
  - `offsetX = (ScreenW - displayW)/2`；`offsetY = (ScreenH - displayH)/2`
  - 得到 `DisplayRect = [offsetX, offsetY, displayW, displayH]`
- 规则：
  - 只允许使用整数 scale（避免非整数缩放引入采样误差与像素抖动）
  - 所有“鼠标→RT→World”的映射必须依赖 DisplayRect（禁止各自实现）

**权威组件：PixelViewportManager（禁止多实现）**
- 唯一职责：计算并缓存 IntegerScale、DisplayRect、RT 参数，并提供 Screen→RT→World 的统一转换 API
- 规则：准星、弹道、UI 点击、Debug 面板只能读取该组件的数据/接口（禁止各自实现一套 DisplayRect/换算）

**最小窗口尺寸与降级策略（必须写死）**
- 最小窗口：`MinWindowW = RTW * 2`；`MinWindowH = RTH * 2`（默认至少×2，保证可读性）
- 若窗口小于最小值：强制回弹到最小窗口尺寸（MVP 固定）
- 约束：scale 计算后必须 clamp 到 `[1, +∞)`，并确保 DisplayRect 始终有效

**DisplayRect 像素对齐**
- offsetX/offsetY 必须整数化（round 或 floor，二选一并写死）
- 默认：`offsetX = floor(offsetX)`、`offsetY = floor(offsetY)`，避免 0.5 像素偏移导致的轻微抖动
- 回归用例：16:9、16:10、窗口缩放、全屏切换、奇数窗口尺寸下 DisplayRect 与 AimPoint 一致

**鼠标/准星坐标换算（唯一口径）**
- 先将鼠标屏幕坐标映射到 DisplayRect（RT 显示区域）的本地坐标（Letterbox/Pillarbox 与整数倍缩放）
- 再将 RT 像素坐标映射到 WorldCamera 的视锥/正交范围（正交时可线性映射）
- 最终得到 World 平面上的 AimPoint（与 Gameplay Plane 交点）
- 回归用例：多分辨率（16:9/16:10）、窗口缩放、全屏切换下 AimPoint 一致

**Input System 口径（必须写死）**
- Project Settings > Input System：Update Mode 固定为【Process Events In Dynamic Update】（禁止随意改）
- Update 中仅采样输入并写入 `PlayerInputIntent`；不得在 Update 直接发射/直接改 Rigidbody
- `FireRequested` 必须带 `requestFrame = Time.frameCount`（或时间戳），并在 LateUpdate 仅消费一次（避免重复与错帧）
- 回归：VSync 开关/帧率波动下连续点射不出现偶发偏差

**Tick/更新顺序（必须统一，防止偶发对不齐）**
- Update：采样输入，记录意图（Move/Aim/FireRequested+requestFrame），不直接发射
- FixedUpdate：Motor 移动与碰撞求解（Kinematic + Sweep/Probe），产出 PlayerPose
- LateUpdate：Camera Desired→Smooth→Clamp→PixelSnap（最后一步）
- LateUpdate：用本帧 Camera + DisplayRect 计算 AimPoint；消费 FireRequested（仅消费一次）并执行射击（避免一帧偏差）

**Script Execution Order / Update Pipeline（建议写死）**
- 建议使用 Script Execution Order（或单点 GameLoop 驱动）固定：InputSampler → Motor(Fixed) → Camera(Late) → AimPoint/Fire(Late)
- 回归：多人协作时不因脚本执行顺序改变而出现“偶发一帧不对齐”

**物理姿态插值（Physics Pose Interpolation）（必须统一，解决抖动/差一帧）**
- FixedUpdate：Motor 计算并缓存 prevPose/currPose（PhysicsPose）
- RenderPose：每帧用 `alpha = (Time.time - Time.fixedTime) / Time.fixedDeltaTime`（clamp 到 0..1）对 prev/curr 插值得到
- Camera/AimPoint/枪口点/IK 目标必须读取 RenderPose（禁止直接用 Rigidbody 的未插值 pose）
- 回归用例：60FPS 与波动帧率下，角色与镜头无明显抖动；连续点射命中点无“偶发偏差”

**渲染分层规则（必须固定）**
- 进入 World RT（像素化）：角色/敌人/武器/子弹/命中特效/希望像素风一致的场景/VFX
- 进入 UI Overlay（不参与像素化）：HUD、文字、图标、背包/弹匣 UI、可选的 FloatingText（确保可读）
- FloatingText 若走 UI Overlay：必须使用统一的 World→Screen 映射，并进行像素网格对齐（避免抖动）

**UI Overlay 工程口径（建议写死）**
- UI Canvas：推荐 `Screen Space - Overlay`（最稳）；若用 `Screen Space - Camera` 必须写死 UI Camera 与 DisplayRect 的关系（否则黑边区域错位）
- FloatingText（走 UI Overlay，推荐）：位置 = `World → RT 像素 → DisplayRect → Screen`，并做像素对齐（避免飘）
- 规则：任何 UI 位置映射禁止直接用 `Camera.main.WorldToScreenPoint` 走捷径（必须考虑 DisplayRect/IntegerScale）

**贴图导入与采样规则（最小）**
- UI/像素素材：Filter=Point；MipMap=Off（按需）；Compression=None/Low
- RT/显示材质：采样必须 Point；禁止 Bilinear/Trilinear

**性能与对象池（Pooling）**
- 必池化：Projectile、Impact/VFX、FloatingText、Loot Drop、Noise Indicator
- 武器测试场 60 秒持续射击回归：无明显 GC spike

**事件总线（GameplayEventHub）**
- 噪音、伤害、拾取、死亡、制作、改造、结算全部走事件
- 事件查看器可过滤/暂停滚动（用于复现）

### 6.3 内容生产规范（让 M6 不失控）
**房间 Prefab 锚点**
- PlayerSpawn / EnemySpawn[] / LootPoint[] / ContainerPoint[] / ShortcutGate / ExitGate

**Validator（内容护栏）**
- Editor/Build 前自动校验：锚点完整、Layer/Collider 合规、关键引用不为空

---

## 7. 关键决策（默认推荐 + 截止点）

1. 像素化 RT 分辨率：默认 320×180  
   - 同步锁定：Integer Scale + DisplayRect（Letterbox/Pillarbox）+ 最小窗口尺寸 + DisplayRect 偏移整数化（见 6.2）
   - 截止点：M0 结束前必须定（影响所有画面/坐标/相机）
2. 摄像机类型：默认正交（需要景深/透视感再评估低视角透视）  
   - 同步锁定：PPU/OrthographicSize 的世界单位↔像素网格契约 + Camera Pixel Snap 规则（见 6.2）
   - 截止点：M0 结束前必须定（影响像素抖动与关卡尺度）

**默认基准（v0.2.3 起写死）**
- RT：320×180
- PPU：16
- 最小窗口：640×360（=RT×2）
- 若需要更清晰：允许升级 RT（如 400×225 或 480×270），但必须升级文档版本并重跑回归路线

3. 物理：默认推荐 **3D 物理（Physics）+ Gameplay Plane 平面约束**（更贴合 3D 场景，避免双套 Collider 成本）  
   - 约束：场景几何用 3D Collider；玩法对象 PhysicsRoot 锁定 Z=0 并冻结不必要旋转；渲染排序走 VisualRoot/SortingOrder（见 6.2）；单向平台用 Trigger + 自定义穿透规则（上行穿透、下行站立），并在 Layer Matrix 中固化  
   - 截止点：M0 结束前必须定（影响 Layer Matrix、单向平台与角色控制）
4. 角色运动控制器（Motor）口径  
   - 默认推荐：Rigidbody（Kinematic）+ 自定义 Motor（MovePosition + Sweep/Probe）
   - 目标：稳定平台手感、易实现单向平台与翻滚/硬直、减少物理抖动
   - 截止点：M0 结束前必须定（影响碰撞矩阵、单向平台、Roll/Jump 手感与回归用例）
5. UI 技术：默认 UGUI；并锁定基准分辨率与缩放策略  
   - 截止点：M0 结束前必须定
6. 数值基准：默认单局 8–12 分钟为调参基线  
   - 截止点：M1 结束前必须定
7. 美术管线：3D→实时像素化（本项目默认）；角色/怪物 3D 动作集合与命名规则需尽早锁定  
   - 截止点：M2 结束前必须定（否则动画/IK/武器绑点会返工）
