**2026-11-18 更新：** ROOM2 已完成整体表现精修。当前行为以 Assets/Room2/Presentation/README.md 和主工程为准；下方历史参数、语言及效果描述有已被替换的部分。

# ROOM2 荧光手印

2026-11-17。使用用户提供的第二、第三张黑白掌纹，原 JPG 不改写，通过 Unity 材质将白纸部分变为透明，将掌纹显示为荧光绿。

共 35 处：18 处铁门/栏杆残留、17 处墙面掌印，分散在大厅两侧的上下层。尺寸、高度和倾斜角度有变化，保留成对和零散的节奏。

正常探索时隐藏；按住鼠标右键显影，松开渐隐。屏幕绿边直接复用 ROOM1 的 Q Vignette 预制体与颜色参数。阅读档案、暂停及切换场景时按原流程隐藏和清理。

铁栏上的贴图由实体表面三角形投影生成，栏杆缝隙不绘制平面贴纸；门上的手印作为门模型的子对象，随门开启而移动。手印没有新增碰撞体。

工程目录：`Assets/Room2/Handprints/`。

- `Textures/`：两张原始掌纹 JPG。
- `Materials/`：两种荧光掌纹材质。
- `Prefabs/`：可复用的两种墙面手印预制体。
- `Meshes/`：本场景已贴合表面的手印网格。
- `PrisonHandprints.cs`：右键显影与 ROOM1 同款绿边。

原有烛光档案桌、桌面陈设、声音牢房与扫描功能保留。独立 exe 没有重新构建。

修改前场景备份在工程的 `Library/HandprintBackups/`。本轮实际输入检查报告和截图保存在当前聊天工作区 `outputs/HandprintFinal/`；首次检查在 `HandprintReview/`，用于保留时序修正前的记录。

最终 22 项检查通过，包括右键显隐、与 ROOM1 同款全屏绿边、铁门开启跟随、阅读/暂停隐藏、转入 ROOM3 后清理，以及重开恢复唯一 ROOM1 绿边。验证通过 Input System 实际右键输入进行；为聚焦此功能，使用既有场景传送器并设置固定观察位置，不宣称完整独立版通关。

## 构图精修 2026-11-17

收到“手印错落有致一点，现在有点死板”的反馈后，重排 35 处手印，打散逐门重复配对。单印、双印、三印小组穿插，门墙有意留白，两侧和上下层不对称；高度、倾角和大小不同，加入左右手翻转与浓淡两档共八种组合材质。旧网格资产保留，方便场景备份恢复。

编辑器脚本 `Editor/HandprintCompositionPass.cs` 可在 ROOM2 Edit 模式主动调用 Apply()；不会自动运行或覆盖以后手动摆放。运行控制器和 ROOM1 绿边未改。定点实际 RMB 检查通过，报告位于聊天工作区 `outputs/HandprintComposition/validation.json`；验证源码留在聊天工作区 work/HandprintCompositionValidation.cs。最初从 ROOM1 自动启动时的验证脚本空引用已定位为启动场景假设，改在传送到 ROOM2 后完成验证。

## 大小与质感二次精修 2026-11-17（当前最新）

总数增加为 48 处：25 栏杆/铁门、23 墙面。大小跨度明显拉开，少量大掌印与中、小印相互呼应；两侧不对称，保留单印、密集组和留白。`Materials/Patina*` 的 12 种变体组合原始掌纹、左右翻转和三档残留；shader 在原图上叠加双尺度颗粒侵蚀与细微亮度变化，降低实心发光感，保留边缘、掌纹和缺损。

主动重排脚本 `Editor/HandprintPatinaPass.cs`；投影资产在 `Meshes/Patina/`，旧资产保留以便旧场景备份恢复。原 shader 备份为 `Library/HandprintBackups/HandprintReveal_before_patina.shader`，进入此轮前的 35 印场景备份 `ROOM2_before_patina_20261117_005321.unity`。

工作区 `outputs/HandprintPatina/validation.json` 是本轮实际输入检查：48 处网格、12 材质、三档磨损、shader 编译、透明度保持、RMB 显隐、同款绿边和开门跟随通过。运行检查后仅第一扇门一处印迹微调轮廓位置并保存。临时验证源码留在工作区，已从工程移除。此轮未重跑三关全套，未重建 exe。
