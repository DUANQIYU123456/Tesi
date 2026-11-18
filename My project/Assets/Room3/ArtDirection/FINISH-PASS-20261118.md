# ROOM3 美术精修与全英文 UI · 2026-11-18

已保存主工程 `D:/NABA/Tesi/My project`。本轮延续绿→黑→冷白→骨白金，重点解决上轮逆光下构筑物遮挡人体、终端文字受背景干扰、字体和语言混杂的问题。

## 实际改动

- 五名前景女工横向位置调整为-1.90、-0.85、0.35、1.15、1.95，保留个体身高、动作差异和宣誓逻辑；右侧人物完整露出，不再被门框吞掉。
- 骨架双螺旋整体抬高8m，各段尺寸收至(.82,.78,.82)，保留双螺旋意象，减少实体密度。最低的工业齿环禁用保留，另两层缩至86%与78%。
- 中央悬浮石碑抬至y18.5、收窄为(2.4,10,2.8)，发光接缝随主体校正。巨大表盘中心抬至y29，指针枢轴保持表盘局部原点。让人物所在高度有连续的亮背景，而不是碎块相互叠压。
- 原有混凝土材质的法线强度统一降至.32，减轻皱箔般的凹凸。保留既有倒角、金属门板、缝隙与紧固件；试验中重复的细节覆盖层已撤掉。
- GitHub官方Inter v4.1字体导入：正文Inter Regular，标题Inter SemiBold，仪表继续用IBM Plex Mono。移除共享UI中哥特式展示字体的使用，统一标题、正文和仪表的分工。
- 共用UI和ROOM3用户可见文字统一英文：ROLE、INTEGRITY、SIMULATED CPM、BEAR WITNESS、ERASE / PRESERVE，以及RESTORE ORDER / PRESERVE THE RUINS。菜单显示GREEN LIGHT IN THE BONES；作品与论文的原意大利语题名仍可用于论文署名和项目元数据。历史人名、书刊题名和原始档案图像不是重新编造或翻译的界面说明。
- 终端使用不透明深色阅读底，保留外围金光；悬浮碎块不再透过正文和按钮。图片按钮改为CLINICAL IMAGE，仪表文本框增至170×64，解决英文变长后的溢出。
- 更新运行时脚本和相应场景生成器的文字，避免只改画面截图、运行后又被意大利语覆盖。

没有新增运行包、改写剧情分支、改变桥梁碰撞或重做人物模型。原有Rocketbox模型与许可沿用前轮。

## GitHub筛选与来源

1. [rsms/inter](https://github.com/rsms/inter)，采用官方[v4.1发布版](https://github.com/rsms/inter/releases/tag/v4.1)中的Inter-Regular.ttf与Inter-SemiBold.ttf；SIL Open Font License 1.1。原字体、完整LICENSE、来源与逐文件SHA256已保存在 `Assets/UI/Survey/Fonts/Inter/`，并烘焙成TMP SDF资产。
2. [brandf/SliceyMesh](https://github.com/brandf/SliceyMesh)：检查其MIT授权与参数化倒角网格能力。工程已有ChamferedBlock及世界坐标材质，本轮使用现有网格，不引入整包或第二套运行时几何系统。

Inter发布包SHA256：9883fdd4a49d4fb66bd8177ba6625ef9a64aa45899767dde3d36aa425756b11e。

## 实机与排版验证

- 实机帧数持续增长；一次跨场景加载期间状态短暂显示playerLoopTicking=false，加载完成后继续推进，ROOM2场景与HUD标识均正确。
- 定点准备前置条件后通过Input System实际E键完成宣誓：5/5努力值1、五处反馈光、门打开、屏障关闭、最终桥可见；活动人物14，无缺失骨骼或材质。最后的表盘枢轴/接缝修正另作定点画面核验。
- 原ERASE按钮onClick进入格式化结局并返回ROOM1；原PRESERVE按钮onClick进入纪念页。英文结局标题分别为RESTORE ORDER与PRESERVE THE RUINS。原格式化结局会在稍后继续显示纪念名单，这一既有行为保留。
- 终端决策页、主菜单、暂停页、ROOM1/ROOM2初始共享HUD、两种结局页检查：指定的意大利语残留/中文UI检查为空，可见文字溢出为空。
- ROOM3三份档案，分别在普通与大字模式检查，共6种布局，文字溢出均为空。测试后恢复原大字设置。
- 主菜单项目标题、身份栏、仪表、宣誓标牌与按钮已保存英文。正文Inter、标题Inter SemiBold、仪表IBM Plex Mono跨场景保持。
- 当前Console错误0、警告0；原有历史警告缓冲仍在。混凝土与三个色彩Shader编译消息为空。
- 最终ROOM3编辑模式保存，dirty=false，下一次Play设为ROOM3预览。

这些是编辑器定点操作、原按钮回调与排版检查，不是三关完整重新通关、陌生玩家试玩或硬件性能测试。没有重新打包exe、提交或推送GitHub。新字体在ROOM2的所有深层交互状态尚未逐一人工检查。

## 仍需提高的完成度

人物的现代服装与程序化动作仍有原型痕迹；后续最有价值的是衣着轮廓与手臂/肩部动作质量。建筑虽然更清楚，部分重复方块、表盘与骨架仍过于规则。黑区可读性与金白亮度需要在实际显示器和完整行走过程中评估。这些限制不能用“加一盏强光就达到3A”来掩盖。

本轮构图更适合论文中“从被审查的身体转为承担见证的主体”的论述；没有直接修改论文，也没有把创作解释当作用户研究结论。

## 文件与审计

工作脚本 `work/FinishPass/`：patch_sources.py、install.cs、final-polish.cs、save-final.cs与final-audit.cs保存本轮实施过程。install.cs是中间安装过程，包含后来撤掉的试验层；不能单独重跑它覆盖保存的最终场景。

运行时改动：SurveyHUD.cs、SurveyTypography.cs、Room3EmbodiedExpansion.cs、Room3StoryUI.cs、Room3ChromaticSequence.cs。共用SurveyInterface.prefab、新Inter字体与SDF、ROOM3场景及相关混凝土材质已保存。

备份 `work/FinishPass/Backup/` 包含改前ROOM3场景、SurveyInterface.prefab与被改脚本。材质法线强度的调整是共享资产修改，单独恢复场景不会恢复这一参数；不是完整工程快照。

截图均来自原生Game窗口。精修前后-实机对照.jpg只缩放拼版，未后期调色；动态构筑物相位与截图时刻不同。01/02/03/04分别为冷白、金白、终端和档案；05为ROOM1状态栏，06为主菜单。JSON保存宣誓、跨场景UI、6种阅读布局及最终编辑状态的审计结果。
