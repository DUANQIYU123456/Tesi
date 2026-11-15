# ROOM1 广告牌交互

2026-11-15，保留用户在 ROOM1 中新摆放的 `WoodenBoard` 及海报位置。

## 试玩

按住鼠标右键显示脚印。在原路线 (43, 0, -9) 附近新增一条 12 个脚印的分支，通向木制广告牌。靠近并看向广告牌，按 E 打开广告；左侧保留用户提供的完整英文，右侧显示项目中与图三对应的镭水宣传图。可按 E、ESC 或点击 CLOSE 关闭。

关闭后，广告牌上的三张主广告及后方的一张旧纸片开始溶解，最后露出木底板。溶解约 3.8 秒，采用绿色细边；减弱动态模式降低边缘亮度并缩短过程。暂停会冻结溶解。木架、支柱和碰撞保持原状。重开游戏会恢复海报、交互和脚印。

广告是独立叙事交互，不会提前发放 XII，也不会触发传送门。原来的化妆盒/画笔解密流程继续保留。

## 使用的现有素材

- 广告图：`Assets/Scenes/aef600aa6ba69aa0a33e388f607d4c44.png`，沿用用户已有素材；在界面中按原比例显示。
- 溶解 Shader：`Assets/小资产们/溶解特效/URP/ShaderGraph/DoubleSide/Dissolve_Metallic_DoubleSide.shadergraph`。针对纸张把金属度设为 0、光滑度设为 0.08，并降低溶解边缘宽度；没有修改包内原 Shader Graph。
- 每张海报有独立的溶解材质。纸张纹理由原图、旧纸色和现有破损遮罩生成，存入 `Assets/Room1/Advertising/Textures`，不覆盖源图。阅读前继续使用用户原有的海报材质，关闭后才切到溶解材质。
- 溶解参数采用每个 Renderer 的 MaterialPropertyBlock，不更改其他场景或其他海报的共享材质参数。
- Editor 中预编译此效果的 Forward Pass，防止第一次触发时出现青色异步编译占位图。Player 不包含 Editor 预编译调用。

## 文案说明

用户提供的文案使用 Radithor，图片上的商品写的是 Radium Emanation Water / Rayode。文案按要求完整保留，界面底部注明它是体验中的解释性广告文案，不是图片广告的逐字转录；没有把两者当成同一份原始史料。

## 代码与对象

- `Assets/Room1/Advertising/TownAdvertisement.cs`：保存海报、宣传图、脚印分支引用，处理关闭后的溶解。
- `Assets/UI/Survey/Scripts/SurveyHUD.cs`：复用原有左文右图阅读面板和输入锁定；增加独立广告阅读模式。
- `WoodenBoard` 上的 `NarrativeInspectable` 提供实际靠近、朝向和遮挡检查。
- `Assets/Room1/Advertising/AdvertisementValidation.cs`：仅 Editor 使用的实机输入和步行检查。

## 验证

`Normal/advertisement-validation.json` 和 `Accessible/advertisement-validation.json` 记录普通模式及大字号/减弱动态模式各项检查，包括脚印显隐、物理步行、E 交互、文案精确匹配、图片比例、文字溢出、F 不会误发碎片、ESC/按钮关闭、溶解中段、暂停、木架保留、操作恢复和重新开始。截图保存在对应文件夹。

最终结果：普通模式 32 项通过，大字号/减弱动态模式 32 项通过。`MainRoute/town-validation.json` 的原有日记、传送门与 ROOM2 转场回归测试 38 项通过。

首次复查发现了青色编译占位图，已通过针对性预编译解决；随后又确认普通模式截图使用了用户当前的减弱动态选项，测试脚本已改为显式设置两种模式并在结束后恢复用户原选项。

修改前已保存用户当前场景，并备份到 `Library/SurveyUIBackups/Advertisement_20261115`。当前工作仅更新 Unity 工程，旧的独立导出包没有重打。
