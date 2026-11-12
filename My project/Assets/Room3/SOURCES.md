# ROOM3 开源与原创内容记录

研究日期：2026-11-11。

## 实际采用

Cristian Qiu Félez — Unity-URP-Volumetric-Light

- 仓库：https://github.com/CristianQiu/Unity-URP-Volumetric-Light
- 文档：https://github.com/CristianQiu/Unity-URP-Volumetric-Light/blob/main/README.md
- 许可：https://github.com/CristianQiu/Unity-URP-Volumetric-Light/blob/main/LICENSE.md
- 下载提交：`354ebc5b6ed3c2c40dc9797c813b56d5da213c4e`
- 包：`com.cqf.urpvolumetricfog`，版本 `0.5.91`。
- 许可证：MIT，原始 LICENSE.md 保留在包目录内。
- 本地位置：`D:/NABA/Tesi/My project/Packages/com.cqf.urpvolumetricfog`。

阅读了 README、包信息、Volume、Renderer Feature 与额外光源接口，核实 Unity 6 / Render Graph 支持和 MIT 许可后采用。

Unity 编辑器无法调用 Git，所以通过本机 Git 下载，再作为嵌入包导入。未手动替换 UPM manifest 的依赖关系。

只给 ROOM3 的独立 Renderer 加入该 Feature。原先 Renderer 的扫描 Feature 保留。ROOM3 相机离开时恢复默认 Renderer。

体积雾参数：40 步，1 次模糊，150m 距离，只计算主光贡献，以控制额外成本。
包本身存在透明物体与雾混合的限制；本场景的玻璃和传送门为手工透明效果，不声称完整物理模拟。

## 浏览但未导入

- 搜索了 floating islands/procedural architecture 与 URP volumetric light 相关仓库。
- 浮岛方向的搜索结果以风格化自然环境为主，不适合本项目的混凝土档案空间，未使用其代码或资产。
- 没有找到或导入《Control》的商业游戏模型、贴图或完整关卡。

## 本轮原创与项目已有资源

- ROOM3 的建筑摆放、倒角基础网格、表盘布局、漂移脚本、场景连接和虚构档案文案为本轮编写。
- 三个 ROOM3 着色器为本轮编写。
- 环境声由确定性合成生成，无第三方采样；生成脚本保留于本聊天工作区 `work/room3/generate_ambience.py`。
- 混凝土基础色贴图复用了项目已有 LeartesStudios 资产 `T_Wall_Concrete_BC.png`；该资产继续遵循原资产许可证。
- 第一人称角色沿用项目已使用的 Starter Assets 及实际相机配置。
- 叙事界面沿用项目现有 Narrative uGUI/TMP 系统；本轮未导入 Bloodlines UI 包。

《Control》截图用于空间尺度、构图、悬浮建筑和逆光雾层的视觉参考。ROOM3 是新建关卡，没有复制其关卡文件。
