# ROOM3 艺术精修：来源与实际使用

核查日期：2026-11-17。用户明确要求检查 GitHub 中适合 ROOM3 的资源。

## 实际接入：keijiro/NoiseShader

- 仓库：https://github.com/keijiro/NoiseShader
- 固定提交：`550100d4a74de1ba90eb1b8e90f25f9dbeec28d2`
- 原路径：`Packages/jp.keijiro.noiseshader/Shader/SimplexNoise3D.hlsl`、`Common.hlsl`。
- 工程位置：`Assets/Room3/ArtDirection/ThirdParty/NoiseShader/`。
- MIT 许可证全文保留为 `LICENSE.NoiseShader.txt`。原著作权归 Ashima Arts、Stefan Gustavson；HLSL 改写说明保留。
- 唯一改动：SimplexNoise3D 中的 Common include 路径改为相邻文件 `Common.hlsl`，适应局部导入。
- 实际调用：混凝土的细微法线扰动、非均匀沉积色差；同位素残影、光幕与光雾的不规则密度。不是仅下载后未使用。
- 未安装注册表、未改 Packages/manifest.json，也未导入示例或整套包。

## 已审查、仅参考：MirzaBeig/Post-Processing-Scan

- 仓库：https://github.com/MirzaBeig/Post-Processing-Scan
- 审查分支：`urp`，提交 `6cf0ced0afb0c94b67e9aa6d99cf1e7013cd4644`。
- 阅读 README、PostProcessingScan.cs 和 Includes/Common.hlsl。
- 此 URP 分支 README 标注 Unity 2022.3 LTS、Shader Graph、Fullscreen Pass Renderer Feature；主分支的旧说明曾标 2019.4，不能混用两个分支的兼容性说明。
- 仓库许可为 Unlicense。
- 本轮仅参考“以世界坐标位置、半径控制球面扫描壳”的方式。没有复制其 Shader Graph、C# 或渲染器配置。ROOM3 现有几何显影继续工作，混凝土增加薄球面扫描带；这部分代码为项目本地实现，不宣称已经安装该后处理插件。

## 项目已有素材

- 混凝土色图、法线及 ORM 源自工程已导入的 LeartesStudios 资源。新法线和 ORM 使用工程内副本，分别按 NormalMap / Linear 导入；没有修改原素材导入设置。
- 阀门、压力表、表盘、显卡、弯曲提取线及姿态差异由本项目的 Unity 原生网格生成。没有下载第三方模型并冒充自制。
- 原门窗回环继续使用此前已有的 urp-portals 适配；许可仍在 SpatialMemory/ThirdParty，未替换。
- 新增的合成音效沿用上一轮原型，没有以 GitHub 音效或历史录音名义重新标注。
