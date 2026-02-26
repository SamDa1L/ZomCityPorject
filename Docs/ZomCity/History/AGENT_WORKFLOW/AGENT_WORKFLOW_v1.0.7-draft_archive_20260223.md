# ZomCity 多 Agent 协作工作规范

DocID：`ZC-WF-AGENT-001`  
文件：`AgentDoc/AGENT_WORKFLOW_v1.0.md`（历史文件名保留）  
版本：`v1.0.7-draft`  
状态：`Draft`  
Owner：项目负责人（你）  
生效日期：2026-02-23  
Supersedes：`v1.0.6-draft`

## 0. 文档定位

- 本文用于规范 5 个 Agent（总控 / 程序 / 美术 / 策划 / QA）的协作流程、权限边界、门禁规则与交付要求。
- 本文优先于口头约定；若与 TaskCard 冲突，以本文为准。
- 功能与系统细节仍以 `Docs/ZomCity/` 权威文档为准；本文只定义协作治理、执行闭环与审计口径。
- 单文件单口径：本文件只允许保留一套生效规则，不得混入旧草案片段。历史草案应迁移到 `Docs/ZomCity/History/AGENT_WORKFLOW/`。

## 1. 项目架构与事实源

### 1.1 当前工程目录职责

- `Assets/ZomCity/Core/`：跨系统合同、事件、时间与 Layer/Tag 守卫。
- `Assets/ZomCity/Data/Design/`：CastleDB 源数据（`ZomCityDesign.cdb`）。
- `Assets/ZomCity/Data/Generated/`：导出产物（JSON + Manifest）。
- `Assets/ZomCity/Data/Runtime/`：运行时数据加载、注册、键校验。
- `Assets/ZomCity/Rendering/`：相机与像素渲染链路。
- `Assets/ZomCity/Gameplay/`：玩法系统层。
- `Assets/ZomCity/UI/`：UI 系统层。
- `Assets/ZomCity/Tools/Editor/`：编辑器工具链（导出/校验/测试入口）。
- `Assets/ZomCity/Tests/`：测试与验收。
- `Assets/ZomCity/Content/`：美术资源与内容资源。

### 1.2 单一事实源（硬约束）

以 `Assets/ZomCity/Core/ZomCityProjectConstants.cs` 为准：

- `DocsRoot = Docs/ZomCity/`
- `ReportsOutDir = TempLogs/ZomCityReports/`
- `DesignRoot = Assets/ZomCity/Data/Design/`
- `GeneratedRoot = Assets/ZomCity/Data/Generated/`
- `ContentRoot = Assets/ZomCity/Content/`

禁止新增第二套真相（平行 ScriptableObject 数值源、散落 Json 配置、运行时临时硬编码覆盖）。

### 1.3 路径大小写与根路径一致性（硬约束）

- 路径写法必须与事实源完全一致，尤其是：`Docs/ZomCity/`、`TempLogs/ZomCityReports/`。
- 禁止混用 `docs/`、`tempLogs/` 等大小写或拼写变体。
- DocGate 最小检查必须包含“`DocsRoot` 路径存在性”；不存在直接判定 Error。

### 1.4 里程碑锚点（M0-M6）

- TaskCard 的 `MilestoneImpact` 必须引用同一份权威里程碑定义。
- 权威里程碑文档约定：`Docs/ZomCity/ZomCity里程碑定义_v1.0.0.md`（DocID：`ZC-DOC-MILE-001`，版本：`v1.0.0`）。
- 在 `ZC-DOC-MILE-001` 发布前，允许临时引用 `AGENTS.md` 的里程碑定义。
- 临时引用退役条件：`ZC-DOC-MILE-001` 发布后，下一版协作规范删除对 `AGENTS.md` 的临时引用。

### 1.5 StableID 大小写决议（与现有架构对齐）

- 当前项目执行口径：StableID 全大写，沿用现有合同与校验规则。
- 当前正则：`^[A-Z][A-Z0-9_]{2,63}$`（与 `ZomCityProjectConstants` / `PrefabKeyValidator` 一致）。
- 约束：禁止在 StableID 中通过大小写区分语义；展示名请使用显示字段，不通过 ID 表达。
- 若未来要迁移为全小写，必须走合同变更评审，并同步升级校验器、数据导出与回归。

## 2. 治理、语言与权限

### 2.1 决策层级

1. 项目负责人（你）：需求立项批准、Git 授权、最终取舍。
2. 总控 Agent：任务拆解、依赖调度、冲突仲裁、集成审批。
3. 专业 Agent（程序/美术/策划/QA）：在授权边界内执行并交付。

### 2.2 语言与注释规则（5 Agent 全员生效，硬约束）

- 所有 Agent 的任何文档类输出必须使用中文。
- 文档类输出包括但不限于：TaskCard、验收报告、风险清单、复盘记录、设计说明、变更说明、门禁报告解读。
- 程序代码中的注释必须使用中文。
- 禁止新增英文注释作为主注释；仅允许保留第三方源码原注释，或协议字段名（ID、路径、命令、接口名）本身的英文。

## 3. 角色职责、禁止路径与 Owner

### 3.1 总控 Agent（Control）

- 职责：里程碑排期、任务分解、阻塞调度、冲突仲裁、集成窗口审批。
- 允许修改：`Docs/ZomCity/`、任务看板与排期文档。
- 默认禁止修改：业务代码与生产资源路径（除紧急止血且经批准）。
- 语言要求：任务说明、调度记录、决策记录必须为中文。

### 3.2 程序 Agent（Engineering）

- 职责：Core/Rendering/Data Runtime/工具链/性能池化与必要 Gameplay 代码实现。
- 允许修改：`Assets/ZomCity/Core/`、`Assets/ZomCity/Rendering/`、`Assets/ZomCity/Data/Runtime/`、`Assets/ZomCity/Tools/`、必要时 `Assets/ZomCity/Gameplay/`。
- 禁止修改：`Assets/ZomCity/Data/Design/`（策划权威数据）、纯美术调优文件主版本。
- 语言要求：程序说明文档必须中文；新增或修改的代码注释必须中文。

### 3.3 美术 Agent（Art/TA）

- 职责：面数/材质/贴图/LOD 优化，角色动作导入与 Animator 配置。
- 允许修改：`Assets/ZomCity/Content/` 及相关 Prefab/Material/Animation。
- 禁止修改：`Assets/ZomCity/Core/`、`Assets/ZomCity/Rendering/`、`Assets/ZomCity/Data/Runtime/`、`Assets/ZomCity/Data/Design/`。
- 语言要求：美术规范、资源说明、优化记录、动作配置说明必须中文。

### 3.4 策划 Agent（Design）

- 职责：CastleDB 规则数据（掉落/经济/配方/成长/录入等）与设计文档维护。
- 允许修改：`Assets/ZomCity/Data/Design/`、`Docs/ZomCity/`。
- 禁止修改（硬约束）：`Assets/ZomCity/Core/`、`Assets/ZomCity/Rendering/`、`Assets/ZomCity/Data/Runtime/`、`Assets/ZomCity/Tools/`、`Assets/ZomCity/Tests/`。
- 语言要求：规则说明、数值变更说明、文档修订必须中文。

### 3.5 QA Agent（Quality）

- 职责：测试方案、回归执行、风险评估、验收结论。
- 允许修改：`Assets/ZomCity/Tests/`。
- 允许读取：`TempLogs/ZomCityReports/*`。
- 允许新增：`TempLogs/ZomCityReports/*_QA_SUMMARY.md`（仅解读与结论）。
- 禁止修改：工具生成的证据文件（`*.json` / `*.log` / `*.txt`），以及 `Assets/ZomCity/Core/`、`Assets/ZomCity/Rendering/`、`Assets/ZomCity/Data/Runtime/`、`Assets/ZomCity/Data/Design/`。
- 语言要求：测试报告、缺陷记录、验收结论必须中文。

### 3.6 路径 Owner 表（越权审批依据）

- `Assets/ZomCity/Core/` -> Owner：Engineering
- `Assets/ZomCity/Rendering/` -> Owner：Engineering
- `Assets/ZomCity/Data/Design/` -> Owner：Design
- `Assets/ZomCity/Data/Generated/` -> Owner：Design（生成工具实现 Owner：Engineering）
- `Assets/ZomCity/Data/Runtime/` -> Owner：Engineering
- `Assets/ZomCity/Gameplay/` -> Owner：Engineering
- `Assets/ZomCity/UI/` -> Owner：Engineering
- `Assets/ZomCity/Content/` -> Owner：Art
- `Assets/ZomCity/Tests/` -> Owner：QA
- `Docs/ZomCity/` -> Owner：Control（最终批准权仍归项目负责人）

### 3.7 越权写入例外流程（硬约束）

跨角色修改“禁止路径”时，以下条件必须全部满足：

1. TaskCard 标注 `CrossRoleWrite=true` 与 `CrossRolePaths`。
2. 双批准：总控 Agent + 目标路径 Owner。
3. 授权仅对当前任务窗口有效，不可复用。
4. 合并前由目标路径 Owner 完成复核，并在 `ApprovalRef` 留痕。

## 4. 新需求与变更立项

### 4.1 权限规则（写死）

- 所有角色都可提出需求建议。
- 仅项目负责人（你）有权批准需求进入开发。
- 新需求默认状态为 `Proposed`，未批准不得进入 `InProgress`。

### 4.2 唯一状态机（硬约束）

- 主链路：`Proposed -> Assessing -> Approved -> InProgress -> ReadyForQA -> ReadyForMerge -> Merged -> Done`
- 拒绝支路：`Assessing -> Rejected`
- 禁止在文档其他章节使用未定义状态名。

### 4.3 总控评估最小输出

总控在 `Assessing` 阶段必须提交：

- 范围与目标
- `MilestoneImpact`（引用权威里程碑文档）
- DocGate 影响面（涉及 DocID）
- 风险与回滚策略
- 优先级与资源占用评估

## 5. Git 与集成规则（硬约束）

### 5.1 Git 默认禁用

- 所有 Agent 禁止擅自执行任何 Git 操作。
- 仅当你在当次会话明确授权“可以提交 Git”后，才允许执行最小必要 Git 命令。

### 5.2 集成执行者（Integrator）

- 默认 Integrator = 项目负责人（你），或你明确指定的单一角色。
- Integrator 负责：合并候选评审、冲突处理、回滚、最终 push 到 `agent`。
- 非 Integrator 的 Agent 只交付“变更包”（文件清单/报告/风险），不执行集成分支操作。

### 5.3 授权后允许范围

- 允许：`git add`、`git commit`、`git push`，以及本次提交所需的最小查询命令。
- 仍然禁止：删分支、重写历史、强推等高风险操作（除非你再次明确授权）。
- 审计要求：提交前复述授权内容；提交后回报分支、摘要、哈希。

## 6. 执行工作流

### 6.1 分支模型

- 稳定分支：`main`
- 集成分支：`agent`
- 任务分支：`agent/<role>/<task-id>-<short-name>`

### 6.2 并行规则与锁定机制

- 无依赖任务可并行执行。
- 有依赖任务置为 `Blocked`，由总控重排。
- 同一关键文件/Prefab 同时仅允许一个 Agent 主改。
- TaskCard 新增字段：`ExclusiveLock`（`true/false`）。
- 当 `ExclusiveLock=true` 时，总控必须在任务开始时公告锁定范围。
- TaskCard 新增字段：`LockedAssets`（锁定文件/Prefab 列表）、`LockReleaseCondition`（通常为“合并或撤销后释放”）。

### 6.3 每日节奏与必须产出

- 10:00 计划同步（15 分钟）后必须产出：
  - 更新任务状态机
  - 更新 `Blocked` 清单（含责任人和解除条件）
- 18:00 集成同步（30 分钟）后必须产出：
  - 合并候选清单
  - 门禁报告文件名清单
  - 回滚/延期决策记录

### 6.4 阻塞升级时限

- 阻塞 > 2 小时：Owner 主动上报总控。
- 阻塞 > 4 小时：总控必须给出处理路径。
- 阻塞 > 1 个工作日：升级为里程碑风险并全员同步。

## 7. 合并门禁（可执行定义）

### 7.1 合并硬门槛

变更合并到 `agent` 前必须全部满足：

1. DocGate 通过（至少不逆行）。
2. DataValidator 与 PrefabKey 校验无 Error。
3. 对应测试通过（EditMode/PlayMode/Smoke）。
4. QA 给出通过或附条件通过结论（Conditional Pass 规则见 7.5）。

### 7.2 工具入口与输出

> 说明：`Tools/ZomCity/M0.5/*` 中的 `M0.5` 表示“工具链版本号（ToolsPack v0.5）”，不是项目里程碑编号。项目里程碑仍按 `M0..M6`。

- 迁移策略：工具链稳定后，统一迁移到 `Tools/ZomCity/Validate/*` 与 `Tools/ZomCity/Tests/*`。
- 兼容策略：旧的 `Tools/ZomCity/M0.5/*` 保留 1 个版本作为 alias（仅转发到新入口），随后移除。

#### DocGate

- 来源：`Docs/ZomCity/DocGate_Index.md`（若暂缺，临时使用 `AGENTS.md` 的 DocGate 表人工核对）。
- 输出：`TempLogs/ZomCityReports/DocGate_<TaskID>_<yyyyMMdd_HHmmss>.md`
- 最小判定：
  1) 涉及 DocID 存在且非 Deprecated
  2) 合同变更必须同步更新文档并记录版本
  3) 不移除既有回归条目
  4) `DocsRoot` 路径存在性检查通过
  5) 里程碑权威文档（DocID/路径/版本）存在且可读取

- 里程碑锚点校验策略：
  - 在 2026-03-02（含）之前：若里程碑权威文档仍未落盘，DocGate 记 Warning，但 TaskCard 必须填写迁移计划与截止日期。
  - 自 2026-03-03 起：里程碑权威文档缺失一律 Error 阻断合并。

#### DataValidator

- 入口：`Tools/ZomCity/M0.5/Run DataValidator`
- 组合入口：`Tools/ZomCity/M0.5/Export And Validate`
- 实现：`Assets/ZomCity/Tools/Editor/Data/ZomCityDataBuildTools.cs`
- 输出：`DataValidatorReport_<yyyyMMdd_HHmmss>.json`（目录：`TempLogs/ZomCityReports/`）
- 规则：Error 阻断合并；Warning 必须写入 TaskCard 的 `RiskFollowups`。

#### PrefabKey 校验

- 规则实现：`Assets/ZomCity/Data/Runtime/PrefabKeyValidator.cs`
- 当前执行方式：由 DataValidator 过程内调用（暂无独立菜单入口）。
- 输出：并入 DataValidator 报告。

#### 测试回归

- 入口：`Tools/ZomCity/TestRunner/*`（当前工具标签包含 `M0.5/M0.6/M0.7`，为工具分类，不等于里程碑编号）。
- 实现：`Assets/ZomCity/Tests/Editor/TestRunner/TestRunner.cs`
- 输出前缀：`AcceptanceReport_<Milestone>_<yyyyMMdd_HHmmss>.json`
- `<Milestone>` 取值必须是 `M0..M6`（或权威里程碑文档定义的枚举），禁止写成 `M0.5`。
- 说明：`AcceptanceReport_<Milestone>` 中的 `<Milestone>` 仅指项目里程碑，不指工具链版本号。

### 7.3 证据文件不可篡改与留存策略

- 工具生成的报告文件（`*.json` / `*.log` / `*.txt`）属于证据文件，禁止手工编辑。
- QA 可新增解读文件（`*_QA_SUMMARY.md`），不得修改原始证据文件。
- 触发条件：任何进入 `ReadyForMerge` 状态的 TaskCard，必须完成门禁报告归档。
- 归档责任：Integrator 负责归档，不由 QA 负责。
- 每次进入集成候选时，Integrator 必须归档关键门禁证据到：
  - `Docs/ZomCity/Reports/<TaskID>/`，或
  - 项目使用的任务系统附件（需可追溯）
- 归档最小集：DocGate、DataValidator、PrefabKey 校验结果、Smoke/PlayMode 结果。
- 原始临时目录 `TempLogs/ZomCityReports/` 仅作为运行目录，不作为长期唯一存证位置。

### 7.4 门禁报告命名规范

- 输出目录：`TempLogs/ZomCityReports/`
- 命名格式：`<Tool>_<TaskID>_<yyyyMMdd_HHmmss>.<ext>`
- Tool 建议取值：`DocGate`、`DataValidator`、`PrefabKeyValidator`、`Smoke`、`PlayMode`、`Editor`、`AcceptanceReport`
- 对于当前工具固定命名（无 TaskID）的报告，Integrator 归档时必须补充任务映射说明。

### 7.5 Conditional Pass（附条件通过）

- 允许范围：仅限 Warning 级问题，且不影响主线可玩与数据一致性。
- 禁止范围：任何 Error、崩溃、数据损坏、引用断裂、Addressables Key 冲突、回归项失败。
- 合并策略：允许进入 `ReadyForMerge` 并由 Integrator 合并，但必须满足落盘要求。
- 落盘要求（必须全部满足）：
  1) TaskCard 标注 `ConditionalPass=true`
  2) TaskCard 填写 `Conditions`（条件列表）
  3) TaskCard 填写 `Deadline`（截止日期，最长不超过 7 天）
  4) TaskCard 填写 `ConditionOwnerRole`
  5) QA 输出 `_QA_SUMMARY.md` 说明风险与复现
  6) Integrator 在合并说明中记录该 Conditional Pass

## 8. 数据与 Addressables 合同（协作视角）

### 8.1 Generated 目录责任与刷新时机

- Design 负责 `*.cdb` 数据正确性。
- Engineering 负责导出工具和校验工具可运行。
- 每次修改 `Assets/ZomCity/Data/Design/ZomCityDesign.cdb` 后必须执行：
  1) 导出
  2) 校验
  3) 报告挂载到 TaskCard
- `Generated/` 是否提交由你当次授权决定；最终集成仅由 Integrator 执行。

### 8.2 Addressables 与 StableID

- 地址格式：`ZC/<Domain>/<StableID>`
- 当前 Domain 白名单：`Enemies`、`Rooms`、`Containers`、`Items`、`UI`、`VFX`、`Audio`
- StableID 正则（当前实现）：`^[A-Z][A-Z0-9_]{2,63}$`
- StableID 来源：CastleDB 主键（当前项目采用大写命名合同）
- 禁止随意重命名 Addressables key；若必须改名，需提供迁移映射与回归证据。
- 若未来需要改为全小写，必须走合同变更评审（可选方案：放宽正则，或新增 `stable_id` 字段并由 DataValidator 校验唯一性）。

### 8.3 新增 Domain 流程

- 新增 Addressables Domain 必须走合同变更流程。
- 必须同步更新：
  1) 规范文档中的 Domain 白名单
  2) `ZomCityProjectConstants` 白名单常量
  3) DataValidator / PrefabKey 校验逻辑与回归用例

## 9. TaskCard 标准

### 9.1 必填字段

- `TaskID`
- `OwnerRole`
- `Objective`
- `Inputs`
- `Outputs`
- `AffectedPaths`
- `MilestoneImpact`
- `Constraints`
- `DoD`
- `Validation`
- `RiskLevel`
- `RiskFollowups`
- `Rollback`
- `Status`（`Proposed/Assessing/Approved/Rejected/InProgress/ReadyForQA/ReadyForMerge/Merged/Done`）
- `ChangeSource`
- `ApprovalBy`
- `ApprovalRef`
- `CrossRoleWrite`
- `CrossRolePaths`
- `ExclusiveLock`
- `LockedAssets`
- `LockReleaseCondition`
- `ConditionalPass`
- `Conditions`
- `Deadline`
- `ConditionOwnerRole`

### 9.2 示例模板（可直接复制）

```markdown
# TaskCard: ZC-0123

## 基本信息
- TaskID: ZC-0123
- OwnerRole: Engineering
- Objective: 实现 Door 交互（读取字段 -> 条件检查 -> 切房）
- Status: ReadyForMerge
- ChangeSource: Control
- ApprovalBy: 项目负责人
- ApprovalRef: 2026-02-23 / SESSION-20260223 / 批准进入开发
- AffectedPaths:
  - Assets/ZomCity/Core/Systems/LevelLoader.cs
  - Assets/ZomCity/Content/Prefabs/Doors/
- MilestoneImpact: M1
- RiskLevel: P1
- ExclusiveLock: true
- LockedAssets:
  - Assets/ZomCity/Core/Systems/LevelLoader.cs
  - Assets/ZomCity/Content/Prefabs/Doors/MainDoor.prefab
- LockReleaseCondition: 合并到 agent 或任务撤销后释放
- ConditionalPass: false
- Conditions: N/A
- Deadline: N/A
- ConditionOwnerRole: N/A

## 输入
- Docs/ZomCity/Levels/Door_Contract.md (DocID: ZC-DOC-LVL-DOOR-001)

## 输出
- 代码与 Prefab 变更（见 AffectedPaths）
- 报告：
  - TempLogs/ZomCityReports/DocGate_ZC-0123_20260223_180000.md
  - TempLogs/ZomCityReports/DataValidatorReport_20260223_180000.json
  - TempLogs/ZomCityReports/DocGate_ZC-0123_20260223_180000_QA_SUMMARY.md

## 约束
- Addressables key 必须符合 `ZC/<Domain>/<StableID>`
- 禁止新增第二套数值源

## DoD
- Door 交互链路通过 PlayMode Smoke
- 门禁报告无 Error

## 验证
- TestScope: PlayMode Smoke - Door

## 回滚
- 回滚当前任务改动并恢复到 `agent` 最近稳定点

## 跨角色
- CrossRoleWrite: false
- CrossRolePaths: N/A
```

## 10. 紧急止血协议（Emergency Patch Protocol）

### 10.1 触发条件

- 主线构建不可运行
- 主流程阻断级崩溃
- 数据损坏导致无法继续联调

### 10.2 允许改动范围

- 仅允许解除阻断所需的最小路径改动。
- 禁止借机夹带非紧急功能。

### 10.3 24 小时内补齐要求

- 补齐 TaskCard 与 `ApprovalRef`
- 补齐门禁报告与回归记录
- 由总控输出事故复盘（原因、影响、预防）

## 11. 修订机制

- 本文为 `v1.0.7-draft`，试运行 1 周。
- 试运行结束后由总控组织复盘，升级为 Active 版本。
- 重大调整必须附带变更记录（原因、影响范围、回归更新点）。

## 12. 本版验收清单

- [ ] 文档内无旧版本草案片段残留
- [ ] QA 权限口径唯一且与“证据文件不可篡改”一致
- [ ] 状态机节点唯一且与门禁触发规则一致
- [ ] Conditional Pass 具备可追责字段与截止日期
- [ ] 门禁报告归档触发条件、责任人、输出路径均可执行
