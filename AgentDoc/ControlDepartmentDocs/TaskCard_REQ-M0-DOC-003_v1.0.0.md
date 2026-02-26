# TaskCard: ZC-M0-DOC-003

## 基本信息
- TaskID: ZC-M0-DOC-003
- OwnerRole: Control
- Objective: 完成 `REQ-M0-DOC-003`（口径冲突冻结结论单）产出、审批通过与留痕回填
- Status: Done
- ChangeSource: Control
- ApprovalBy: 项目负责人
- ApprovalRef: APR-M0-FREEZE-20260225-141238
- AffectedPaths:
  - AgentDoc/ControlDepartmentDocs/口径冲突冻结结论单_M0_v1.0.0.md
- MilestoneImpact: M0
- RequirementDocRef: AgentDoc/DesignerDepartmentDocs/M0阶段/里程碑拆分需求_REQ-M0-SPLIT-001_v1.0.0.md
- CollaborationDepartments: Control, Design, Engineering
- MilestoneWorkflowState: ControlReviewed
- RiskLevel: P1
- ExclusiveLock: false
- LockedAssets: N/A
- LockReleaseCondition: N/A
- ConditionalPass: false
- Conditions: N/A
- Deadline: 2026-03-05 18:00
- ConditionOwnerRole: N/A
- PlanDocRef: AgentDoc/DesignerDepartmentDocs/M0阶段/里程碑拆分需求_REQ-M0-SPLIT-001_v1.0.0.md
- CompletionReportRef: AgentDoc/ControlDepartmentDocs/口径冲突冻结结论单_M0_v1.0.0.md
- Gate03Status: TaskCardGate03Ready

## 输入
- AgentDoc/DesignerDepartmentDocs/M0阶段/里程碑总需求分析_M0_v1.0.2.md
- AgentDoc/DesignerDepartmentDocs/M0阶段/里程碑拆分需求_REQ-M0-SPLIT-001_v1.0.0.md
- AgentDoc/AGENT_WORKFLOW_v1.0.md

## 输出
- AgentDoc/ControlDepartmentDocs/口径冲突冻结结论单_M0_v1.0.0.md
- 冻结单审批留痕回填（与 TaskCard `ApprovalRef` 一致）

## 约束
- 冻结范围仅限：RT 基线、输出策略、相机合同、文档权威入口。
- 进入 `RequirementSplit` 前必须满足冻结单“通过”且迁移门槛达成。

## DoD
- 冻结单状态为 `Approved`。
- 冻结单审批留痕包含：审批结论、审批人、审批时间、ApprovalRef、生效时间。
- 本 TaskCard 的 `ApprovalRef` 与冻结单保持一致。

## 验证
- 验证文档：`AgentDoc/ControlDepartmentDocs/口径冲突冻结结论单_M0_v1.0.0.md`
- 验证点：
  - `状态：Approved`
  - `ApprovalRef：APR-M0-FREEZE-20260225-141238`

## 风险跟踪
- RiskFollowups: 文档入口迁移（TempLogs -> Docs/ZomCity）未完成前不得放行 `RequirementSplit`。
- RiskFollowups: RF-002｜Player Placeholder Domain 迁移（Owner=Engineering，Deadline=2026-03-05 18:00，验收入口=`AgentDoc/EngineDepartmentDocs/M0_SPLIT-006_风险清单_v2.md`）。
- RiskFollowups: 审批结论（2026-02-26）｜`Characters` Domain + `PC_` 前缀已批准；Round2 期间不改线上基线，M1 窗口执行迁移。

## 回滚
- 若审批结论撤销，冻结单状态回退为 `PendingApproval`，并撤销本 TaskCard 的 `Done` 状态。

## 跨角色
- CrossRoleWrite: false
- CrossRolePaths: N/A
