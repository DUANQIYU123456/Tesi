# ROOM3 Rocketbox 人物与悬浮构图改造

本轮已写入并保存 Unity 主工程的 ROOM3。参考用户两张图的悬浮身体、建筑尺度、顶部红光与逆光层次。

## 已完成

- 使用 Microsoft Rocketbox 的 Female_Adult_11、Female_Adult_15、Female_Adult_02，替换五名宣誓者与三名玻璃后工作残影；增加六名高处记忆剪影。模型保持原始蒙皮骨架，每人有81个蒙皮骨骼槽；不是历史原告真实肖像。
- 原简化人物父对象 Anatomical witnesses / Anatomical archive workers 禁用保留，便于恢复。没有修改原 Room3StoryFlow、EmbodiedExpansion、声音脚本。
- 肤色和衣着贴图做降饱和、1024px派生副本；保留法线与透明头发。衣服仍以原现代服装为基础，没有宣称完成1920年代服装复原。
- 新 RocketboxWitness 适配原骨架，区分静止姿势、工作动作、五人宣誓抬手和高处悬浮姿态；高处六名残影不跟随宣誓。支持原减少动态设置；阅读、暂停、决策、结局时停止人物更新。
- 宣誓区增加两侧竖向槽柱、顶部遮光横梁与有渐变/细颗粒的红光面。局部冷灰人物主光和暗红上层光区分前后景；绿光保留在宣誓反馈。末次审视撤掉胸前树枝图标式静态线，保留原瞬时粒子与灯光。
- RocketboxAtmosphere 接入终局：格式化悬停熄灭顶部红光，封存悬停转绿，正式结局时熄灭。新装饰没有Collider，不新增通路障碍。

## 验证范围与结果

实机游戏持续推进，状态工具确认 playerLoopTicking=true，反复读到增长的帧数。

- 定点准备剧情前置状态后，通过Input System实际长按E完成宣誓。
- 最终版本通过Input System持续左键+向上鼠标delta完成宣誓。
- 结果：5/5前景人物AppliedEffort=1；5处绿光启用；门两侧到达x=-4.3/4.3；oathBarrier禁用；最后光桥可见。玻璃后3名工作人物启用，合计14名活动人物，骨骼空引用0、缺失材质0、新装饰Collider0。
- 调用原结局生产方法验证：封存实际显示纪念页；格式化实际切到ROOM1并显示结局面板。两个悬停分支的新光源颜色已读取核验。
- 最终编辑状态：ROOM3已保存、dirty=false、playing=false；新着色器编译错误0；末轮运行当前Console错误0、警告0。历史缓冲仍有之前导入与旧素材警告，不等于整个工程零警告。

这些是编辑器定点功能和画面检查，不是陌生玩家试玩、全三关重新验收或硬件性能测试。前置条件使用直接赋值；结局使用原生产方法触发。

## 文件与来源

主目录：D:/NABA/Tesi/My project/Assets/Room3/Rocketbox/

- RocketboxWitness.cs、RocketboxAtmosphere.cs、ArchiveCeiling.shader
- Editor/RocketboxSetup.cs、Models/、Materials/
- Models/SOURCES.json 保存固定提交、每个原文件URL、大小和SHA256；Models/LICENSE.Microsoft.txt 保留MIT许可。

原作者仓库：https://github.com/microsoft/Microsoft-Rocketbox
固定提交：0943055db6ec570bcef9f2c8b41c9e5467c808f9
授权：https://github.com/microsoft/Microsoft-Rocketbox/blob/0943055db6ec570bcef9f2c8b41c9e5467c808f9/LICENSE.md

工作源码：D:/CODEX/2026-11-17/new-chat-2/work/RocketboxPass/。polish.cs是Roslyn编辑器脚本，保存着最终精修；RocketboxSetup.Apply只负责首次安装，不能单独重复覆盖最终场景。

改动前场景备份：当前工作区 work/RocketboxPass/Backup/ROOM3-before.unity。恢复时同时恢复该场景；新增Rocketbox资源可留在工程中，不影响旧场景。

截图10-final-rest.png为最终整体构图；06-portraits.png和08-oath-success.png为近景检查。11-final-oath.png是胸前线条收掉之前的中间画面，不作为最终光效示例。所有截图均为原生Game窗口捕获，拍摄用了固定玩家位置和视角。

本轮没有重建exe或提交/推送Git。旧导出包不包含本轮修改。

