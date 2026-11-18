**2026-11-18 更新：** ROOM2 已完成整体表现精修。当前行为以 Assets/Room2/Presentation/README.md 和主工程为准；下方历史参数、语言及效果描述有已被替换的部分。

# ROOM2 · 绿光捕影

**最新中央轮换布置（2026-11-17）**：三个主画面统一至 (-5.55,39,-141)，等比放大20%，每次扫描轮换一张。临床图后旧工厂叠层收起备用；图二导入溶解材质用于出现/消失与捕获，细光边过渡0.5秒，恢复原全息材质。约16秒一轮、5秒捕获窗，捕获后跳过、漏拍之后补拍。源比例和原扫描基础保留。最终58项检查通过，见工作区 outputs/CentralProjectionFinal/实现与检查记录.md。当前工作源码 work/CentralProjection/。下方记录描述各历史阶段，后续操作以此最新布置与主工程为准。


**最新比例与体验修正（2026-11-17）**：已关闭照片 NPOT 自动重整，所有全息按源比例重建，临床人物恢复为 1.9604×2.600 米。人物区域瞄准、等待倒计时、瞄准时扫描减弱、关停文案、进度承接和运行时材质实例已更新。档案桌临床照片、手写图片和提示亦按源比例修正。26 项档案桌检查及 50 项大厅/手印检查通过；完整记录在工作区 outputs/Room2Polish/修改与检查记录.md。


**最新素材替换（2026-11-17）**：第二处原 AI 捂下颌人物已按用户新要求换为用户图一，`Textures/ClinicalPortrait_UserSupplied.png`。图二因与第一处画表盘照片重复未采用。场景中已移除原生成形象，第二处标签改为 SUPPLIED REFERENCE；工厂照片背景保留。下方 AI 生成说明为旧版本来源记录，旧素材仅供备份，不代表当前场景仍使用。新图没有重新绘制，也没有在本轮核实人物身份或进行医学诊断。定点替图验证见工作区 outputs/PortraitReplacement/。


本轮实现“第三幕：中央扫描大厅”。保留既有 SpatialScanEvent 和深度扫描效果，在其上叠加项目已导入的 Dissolve_Unlit_DoubleSide 材质驱动的开放式雷达网格。每轮扫描约 22 秒，间隔 5 秒；扫描尾迹停留约五秒，与三处残影的捕获窗口同步。进入大厅后不再提示站着不动，扫描本身不会伤害玩家或触发相机震动。

三个目标依次为画表盘、工厂劳动与下颌疼痛、舔笔动作。用户原图原样复制为 Memory_0/1/2.png，使用已导入的 Basic_Scanline_Hologram 材质，并做浅浮雕网格与几何轮廓裁切；没有把用户照片重新绘制成伪史料。第二处保留工厂照片背景，前方加入 AI 生成的捂下颌劳工形象，游戏中明确标注 ARTISTIC RECONSTRUCTION。第三张剧照式参考标注 SCENE RECONSTRUCTION。原图年代、人物身份和来源未在本轮重新考证，不将此组图宣称为真实医疗或历史取证。

操作：趁扫描尾迹照亮残影，靠近至 5.5 米内，对准目标按 E。正常状态不可见，尾迹离开立即隐藏；错过可等下一轮，无永久失败。每个目标只能捕获一次。拍摄时暂停世界 0.5 秒，锁住其他互动和暂停入口；随后绿色粒子团沿弧线进入右下表盘，依次点亮 XII、III、VI，左上报错加深。减弱动态模式保留交互，减少粒子和界面扰动。

收齐三份后，扫描与叠加网格熄灭，粒子炸裂、灯光压暗、门扉移开，移除出口阻挡和原膜面，开启原有通往 ROOM3 的 E 传送。旧医疗页和旧收集位不能绕过此门锁；原烛光桌、原桌面陈设、48 处右键手印及声音牢房保留。没有重建独立 exe。

## 实现位置

- Assets/Room2/Panopticon/PanopticonHall.cs：窗口、瞄准距离与视线遮挡、捕获、时间锁、粒子吸收、崩溃与开门。
- Assets/Room2/Panopticon/Editor/PanopticonInstaller.cs、PanopticonArtPass.cs：主动场景安装与构图，不自动运行，不覆盖手动摆放。
- Assets/UI/Survey/Scripts/SurveyHUD.cs：沿用已有界面的任务、雷达与扫描通知。
- Assets/UI/Survey/Scripts/SurveyPortalGate.cs：存在大厅控制器时使用三份记忆的新门锁条件。
- 原场景与 HUD/门锁代码备份：Library/PanopticonBackup/。初始场景备份 ROOM2_20261117_013643.unity。

## 字体与工程修复

中文提示在现有拉丁字体后接 Windows 系统 Microsoft YaHei 的运行时回退，1024 动态字集，仅在本场景存在时挂接并清理；不复制或分发系统字体文件。当前目标是用户的 Windows 原型，其他系统需配置相应中文字体回退。

本次刷新时再次暴露截图中的旧 URP 编译错误：PathComponentEditor.cs 缺失、仅 meta 存在。从另一份本地工程的相同包哈希 com.unity.render-pipelines.universal@6c02063a0a76 恢复该文件，先核对 package.json、PathEditorTool.cs 和 Light2DEditor.cs 的 SHA256 完全一致，再重新导入虚拟 Packages 路径并清理编译缓存。没有升级 URP 或改动包依赖版本。

## 生成素材说明

捂下颌人物为 AI 生成的虚构人物艺术重构，不是 Mollie 或其他真实人物照片。使用内置 image_gen 工具，transparent_background=true，保留生成 RGBA；这里只按 alpha 生成场景网格，没有在 Python 中编辑图片。工作区源图：work/Panopticon/JawPain_ArtisticReconstruction.png；工程消费路径：Assets/Room2/Panopticon/Textures/JawPain_ArtisticReconstruction.png。

最终提示词（原文）：

Use case: historical-scene. Asset type: transparent cutout for a Unity historical labor-memory hologram; fictional artistic reconstruction, not an archival photograph. Create a single adult female dial-painting factory worker circa 1922, seated, visible from head to mid-thigh including both arms, three-quarter front view. She wears a modest worn off-white cotton blouse with rolled sleeves and a plain work apron; early 1920s short softly waved dark hair. She is suffering deep jaw pain: one hand firmly cups the underside of her lower jaw, fingers anatomically natural along the cheek; the other hand rests limply in her lap holding a fine camel-hair paintbrush. Head slightly bowed, eyes half closed, restrained believable exhaustion, no theatrical screaming. Monochrome grayscale, realistic fine fabric and facial detail, soft photographic studio lighting without dramatic highlights. Complete silhouette with comfortable empty transparent margin around head, elbows and lap. Genuine transparent alpha background. No chair visible, no desk, no environment, no floating props, no text, no neon color, no baked hologram effect, no medical wounds, no blood. Do not depict or reproduce the identity of a specific real person. The green hologram shader will be added in Unity later.

## 验证范围

第一轮 outputs/PanopticonReview/ 已完成 36 项实测检查，通过实际 Input System E 输入，验证限时窗口、窗口过期不可拍、下一轮重试、不能重复计数、半秒恢复与 ESC 冲突、三份崩溃、开门、旧进度不能绕过，以及实际 E 进入 ROOM3 和 UI 清理。补充捂下颌前景后，最终重跑结果见 outputs/PanopticonFinal/validation.json。验证用了已有场景入口和固定观察位置，不宣称陌生玩家体验验收或完整独立版通关。

最终出口回归：outputs/PanopticonFinalGate/validation.json，38 项检查通过。该轮先验证旧进度位不能绕过门锁，再清掉旧声音进度，只凭三份全息记忆完成开门、验证不再显示旧锁定提示，并实际按 E 进入 ROOM3。Console 运行后未出现新的错误；工程原有素材/渲染警告仍可能存在。

替图专项验证 outputs/PortraitReplacement/validation.json：11 项通过，涵盖引用正确、原生成形象移除、显影窗口、新图 E 捕获与时间和控制恢复。此轮不重复此前的 38 项整段流程验证。
