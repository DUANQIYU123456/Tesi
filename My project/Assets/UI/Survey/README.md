# Luce Verde nelle Ossa · 三关叙事 UI

已将 Bloodlines UI 接入原有 uGUI / TextMeshPro 框架，采用冷白、深灰与荧光绿。素材包原始文件与字体许可证保留。正文沿用易读的无衬线字体，意大利语用于身份、菜单和结局标签，主要说明为英语。

2026-11-12 更新：正文使用 IBM Plex Sans Regular，标题使用 Sans SemiBold，仪表使用 Plex Mono Medium。新增 ESC 暂停菜单，可调整音量、观察灵敏度、减少抖动及大字号阅读。偏好设置会保留到下次运行，表盘刻度只记录当前一轮。

## 从哪里开始

打开 `Assets/Scenes/ROOM1.unity`，点击 Play。选择 **AVVIA ISPEZIONE / BEGIN INSPECTION**。

三个场景的 Hierarchy 都有 **System Survey UI / Bloodlines bootstrap**，其 Inspector 指向共用界面预制体：

`Assets/UI/Survey/Resources/SurveyInterface.prefab`

运行时只保留一套 `System Survey Interface / Bloodlines`，随玩家通过传送门。可以在预制体里调整字体、位置、图片、按钮与音效引用；无需修改商店包原件。

字体由预制体上的 `SurveyTypography` 三个字体引用统一应用；档案版面的尺寸和阅读字号也由这个脚本控制。调整档案布局时应同步修改该脚本，避免运行时覆盖编辑器中的位置微调。

## 操作与路线

| 区域 | 操作 | 结果 |
|---|---|---|
| ROOM1 | WASD 移动，鼠标观察；按住右键 | 显示绿色残留脚印与取景边框，表盘按方向和距离反馈 |
| 小镇传送门 | E | 显示“执行数据清理”，随后提示连接异常并进入 ROOM2 |
| ROOM2 绿瓶 | E 检视，按住 F 或 Space 两秒 | 对焦圈稳定、官方层淡出、历史层与车间照片出现，点亮 XII |
| 大厅左侧钟表牢房 | 对准声源，按住右键两秒 | 声波锁定、出现劳动记忆字幕 |
| 大厅右侧冲压牢房 | 同上 | 另一段字幕；两边完成后点亮 III |
| 扫描大厅左侧后段 | 等待绿色扫描波显示病历 | 文件在第一次扫描后保持可见，便于再次寻找 |
| 病历 | E，按住左键拖动三笔，覆盖至少约 22%；或按住 F 三秒 | 擦除涂层、显示历史层与研究图片，点亮 VI |
| 大厅出口 | 完成 XII、III、VI 后 E | 通往 ROOM3 的原传送门解锁 |
| ROOM3 | 原来的玻璃夹层、右键校准拼桥、法庭档案和宣誓 | 保留原玩法；法庭档案按住 F 或 Space 三秒，点亮 IX |
| 宣誓 | 按住左键并上推鼠标；也可按住 E 四秒 | 右侧竖向条反馈进度，原有大门开启 |
| 核心控制台 | E，然后选择按钮 | 两种结局均保留；悬停有颜色、说明与音效预览 |
| 任意区域 | M | 切换减少抖动；ROOM2 摄像机扫描冲击也随之关闭 |

检视界面用 **ESC、E 或 CLOSE** 关闭。结局用 **RESTART / R** 开始新一轮，清空表盘收集，恢复小镇传送门。

在普通探索时按 **ESC** 暂停，世界时间、声音及人物输入会暂停。再按 ESC 或 RESUME 返回；暂停界面的 RESTART FROM THE TOWN 会从小镇重新开始。档案内的 ESC 只关闭档案。

罗盘现在按当前可完成的步骤引导。尚未解锁的大厅出口会显示缺少哪些刻度；依照表盘返回下一条线索即可。

## 四个常驻组件

1. 左上：场景、任务、身份与完整度。完整度是虚构系统权限的叙事状态；每个真相刻度改变一次，边框随之生长。
2. 右下：XII / III / VI / IX 表盘、方向指针与模拟读数。它根据游戏线索位置反馈，不是辐射测量模型。
3. 下方：声波、两秒锁定与字幕；保留原来的 E 交互目标选择。
4. 中央：双层档案，对焦 / 刮涂层，并保留来源说明。

两间声音牢房实际位于大厅入口门后，因此入口仍自动开启；解码条件放在通往 ROOM3 的出口，避免把玩家锁在声源外。

## 资源与史料

- UI：Alebardium / Bloodlines UI。使用灰色按钮、框架及圆形进度装饰；通过单独的 UI 材质转为白绿，未覆盖原图。
- 原有 NarrativeInterface 和 ROOM3 档案 / 双结局界面保留脚本引用并换肤。
- 新 UI 滴答、启动、钟鸣、八音盒和对焦声音为程序生成的音效原型；按钮点击使用 Bloodlines 提供的音频。可在共用预制体中换成最终录音。
- 车间、病变照片、报纸与人物照片来自已有论文研究材料。病变照片的人物身份尚未确认，界面没有将其标成 Mollie。
- 官方层是游戏重构；历史层是研究摘要，不冒充原始病历或逐字证词。未采用“每日 250 次 / 12000 次”等尚未核实的数字，也未将放射性半衰期等同于骨骼持续发绿光。

史料依据延续 `Assets/Room3/STORY_SOURCES.md`：NPS / HAER NJ-121、Library of Congress 的 Radium Girls 研究、2017 年 La Medicina del Lavoro 研究（PMC7682169）。现代屏幕劳动与表盘劳动作为批判性比较呈现，并非同一种医学伤害。

## 文件和备份

- 运行与编辑代码：`Assets/UI/Survey/Scripts/`
- 共用预制体：`Assets/UI/Survey/Resources/SurveyInterface.prefab`
- 新材质与着色器：`Assets/UI/Survey/Materials/`、`Shaders/`
- 新音效：`Assets/UI/Survey/Audio/`
- 字体、许可证与固定版本记录：`Assets/UI/Survey/Fonts/`
- 字体布局：`SurveyTypography`；暂停设置：`SurveyPause`，均挂在共用界面预制体上。
- 界面替换前备份：`Library/SurveyUIBackups/BeforeBloodlines_20261112/`
- 本轮可读性检查前备份：`Library/SurveyUIBackups/BeforeReadabilityAudit_20261112/`

所有进度只在这一轮运行中保存；停止 Play 或重新启动程序会重置。本次没有增加磁盘存档系统。
