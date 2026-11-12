# ROOM3 空间回环与劳动档案

本轮在现有三个场景中继续完善，没有增加第四个场景。新内容位于 ROOM3 玻璃夹层之后、校准断桥之前。

## 新路线

1. 沿原路线穿过观察玻璃缺口，阅读医疗档案。
2. 回到中央通路，走向新的矩形门框。可以透过开口看到另一侧的走廊；直接步行穿过，不用额外按键。
3. 走廊尽头通回走廊起点。默认重复两次，门上的计数变化，墙面逐渐孔蚀、露出内部绿色电路，钟表声与鼠标点击声的比例变化。
4. 第二次回环后，右侧工作台出现可检视的劳动记录。对准工作台按 E，再按住 F 或 Space 约三秒，保留被系统省略的劳动记录。
5. 记录保留后，尽头的门改为通往原来的断桥校准平台。之后的校准、法庭剪报、宣誓和双结局继续使用现有流程。

可以从起点门折返原走廊，再次进入。暂停时停止穿门检测与视窗更新。新档案不额外占用 XII / III / VI / IX 四枚刻度。

## 表达与边界

空间回环把“不断重复而输出不变”的劳动节奏变成可走过的路线。工作台将夜光表盘、手工动作、数据标注与信息检查放在同一份对照记录里，让现代劳动不只通过电路或点击声暗示。

白字层是虚构系统界面，绿字层是研究摘要。依据包括 Gray 与 Suri 的 Ghost Work、Crawford 的 Atlas of AI 及原有镭女郎历史研究；没有将两类劳动写成相同疾病，也没有制作冒充真实档案或工人证词的内容。

这是通过局部坐标映射、可穿越视窗和有限递归画面构成的空间回环。没有改变整个世界的几何度量，也没有实现任意重力方向。

## 视觉与阅读

- 冷白门边、混凝土表皮与低位工作光延续现有档案空间。
- 表皮从完整变为孔蚀，背后的线路是独立的内部结构；稳定的结构碰撞用于保持通路可走。
- 工作台收在侧边，中央通路保留通行净宽。
- 档案打开的同一帧隐藏探索 HUD 与警报横幅，阅读底板改为不透明深色，避免高亮墙纹穿过正文。
- 使用原有 IBM Plex 字体与大字号选项，没有另换 UI 框架。

## GitHub 取舍

实际参考并适配的是 Zbajnek/urp-portals，固定版本 13dcdb21d972d5f1f6df4a9506d198524bf044c2，MIT 许可。适配了投影视窗 shader、坐标映射、斜裁剪和平面附近屏幕厚度处理，并使用当前 URP SingleCameraRequest 完成离屏渲染。

没有导入开源项目的演示场景、项目设置或整套包。视窗宽度上限 768 像素、30 Hz 更新、最多一层额外递归，每帧最多四次视图请求；这些是开销限制，不是最终性能测量结果。

另外审阅了 Infinite-Room-Unity 和 Daniel Ilett 的 dissolve-urp。前者作为空间重复参考，后者 README 面向 Unity 2019.3 / URP 7，均未直接导入文件。

完整来源与修改记录在项目的 Assets/Room3/SpatialMemory/SOURCES.md，MIT 文本保留在 ThirdParty/LICENSE.Zbajnek.txt。

## 编辑位置

- 场景根对象：ROOM3 - Repeated labor archive。
- 逻辑组件：Room3RecursiveArchive；四个门框分别挂有 ResidualPortal。
- 劳动工作台：Labor comparison workstation，使用现有 Room3StoryNode / Room3StoryFlow / Room3StoryUI。
- 资源目录：Assets/Room3/SpatialMemory/。
- 修改前备份：Library/Room3Backups/BeforeRecursiveArchive_20261112_191047/。

## 验证记录

实际运行的自动检查覆盖新回环与原有三关流程。完整报告保留在本次工作区 outputs：Memory-full-route-validation.json、Memory-three-scene-UI-format-validation.json、Memory-mouse-oath-validation.json。

- 完整 ROOM3 路线与两种结局：58 项通过。
- 最后一轮三关 UI、回环、反向穿门、阅读切换与格式化结局：81 项通过。
- 普通鼠标宣誓与纪念结局复核通过，俯仰限制恢复。
- 返回小镇后检查：新增传送视窗纹理为 0、辅助相机为 0，保留一个玩家与一个启用的 AudioListener。
- 最后一轮没有游戏脚本错误或编译错误。

自动检查验证功能与状态，不替代真人试玩对叙事理解和体验节奏的评价。
