# ROOM3 四区扩展：设计与史料记录

制作日期：2026-11-12。延续 2026-11-11 已有 ROOM3、相机、体积光和场景切换。

## 四段体验

1. **隔离玻璃长廊**：原观察玻璃改到右侧并加高；走廊塌落部分迫使玩家穿过玻璃缺口，绕到夹层。跨越边界显露工作台上方的三个劳工剪影，钟表、鼠标点击、计数器声发生叠加。
2. **校准断桥**：阅读并显影医疗档案后，站到地面标记，按住鼠标右键，使屏幕中心与空中的校准标记重合。三块倾斜模块在动画中归位；等归位完成才开启桥面碰撞并撤去边界。这里使用视角校准和实际几何移动，没有宣称实现拓扑上的非欧空间。
3. **宣誓门**：阅读法庭剪报后，靠近五个具名的抽象女性剪影。按住左键并持续向上移动鼠标；也可长按 E。约四秒有效输入后剪影内部的象征性绿光亮起，双扇门打开。镜头阻力、干扰均为有限幅度，不降低真实帧率。
4. **硅骨深渊**：穿过门后走上较窄的长梁，进入双螺旋肋骨式巨构、巨大表盘与微芯片核心。在控制台按 E 打开两个有真实后果的结局按钮。

序列没有自动倒计时。预计时长受阅读与探索速度影响，不能将“三分钟”当作已经验证的统一游玩时长。

## 控制与可读性

- WASD 移动，E 检视，F 或 Space 显影；E / ESC 关闭阅读。
- 鼠标右键进行桥梁校准。
- 宣誓门支持左键加向上移动，也支持长按 E。
- M 切换减弱镜头干扰。
- 档案显影解锁后可以再次打开阅读，无需等待场景扫描。
- 剧情进度、双层档案、显示图片、两个结局均使用现有项目的 uGUI / TMP 框架。
- 新 UI 保留英语为主要语言；结局按钮保留用户指定的意大利语标题。
- 窄梁设有较宽的容错边界，意外落下使用分阶段检查点。

## 两个结局的范围

- **RIPRISTINA L'ORDINE**：白色过渡后传回 ROOM1，关闭这一轮中的监狱入口，并显示虚构系统结语。
- **CUSTODISCI LE ROVINE**：停止悬浮建筑和指针，开启绿色纪念粒子，显示人物姓名、两张来自论文的照片与来源说明。
- 重开恢复原小镇与入口。没有永久删除项目、场景或用户文件；没有写入不可逆的存档决定。

## 历史依据与创作区分

### 已核对的时间线

Library of Congress — Arlene Balkansky, “Radium Girls: Living Dead Women,” 19 March 2019：
https://blogs.loc.gov/headlinesandheroes/2019/03/radium-girls-living-dead-women/

支持 1927 年起诉、1928 年一月与四月的作证、延期到九月引发批评，以及 **1928 年 6 月 4 日接受庭外和解** 的时间线。不能写成她们一直等到九月才首次开庭。

剪报：“Five Women Doomed to Die,” *The World* (New York), 10 May 1928, p.14。
实际图像使用用户现有论文 Fig.1.5 的素材，未制作冒充历史原件的新剪报。

### 五位原告与档案保存

National Archives — “The Radium Girls” (2018)：
https://www.archives.gov/files/citizen-archivist/images/08-23-2018-radium-girls.pdf

五位姓名为 Grace Fryer、Katherine Schaub、Edna Hussman、Albina Larice、Quinta McDonald。
该来源还说明公司纸质档案具有放射性，其数字化保存使历史记录仍可访问。这是纪念结局选择“保留记录”的依据之一。

Mollie Maggia 在本场景纪念文字中单独列出，**没有把她误列为 1927 年的五位原告之一**。

### 疾病、误诊与否认

NPS / HAER, U.S. Radium Corporation, NJ-121：
https://tile.loc.gov/storage-services/master/pnp/habshaer/nj/nj1600/nj1643/data/nj1643data.pdf

历史研究：“The contribution of Italian migrant women in the New World to health and safety at work” (2017)：
https://pmc.ncbi.nlm.nih.gov/articles/PMC7682169/

医疗档案白字层被明确标为 **重构的公司界面**；绿字层标为历史研究摘要，不冒充逐字病历或牙医证词。没有使用“公司买通法医”的未核实情节、每日舔笔 240 次或骨密度归零等虚构测量值。

### 视觉隐喻

EPA — Radionuclide Basics: Radium：
https://www.epa.gov/radiation/radionuclide-basics-radium

Ra-226 半衰期约 1,600 年。它不证明人体骸骨在地下持续发出可见绿光。绿色骨骼、电路、双螺旋建筑、宣誓阻力属于艺术构造，界面和结局来源说明有区分。

没有确认“五位女性均无法举手宣誓”这一具体细节，因此宣誓门被标为象征性重构，没有作为史实逐字展示。
当代数据劳动与内容审核的创伤作为论文的批判性比较，不等同于放射性损伤，也不声称所有数据标注员从事相同工作或具有相同诊断。

## 图像与声音来源

图像提取自用户提供并已在项目研究中使用的：
`D:/NABA/论文/tesi/Duan qiyu_Tesi.docx`。

- `image6.png`：论文 Fig.1.2 临床参考图。不能确认患者身份；没有把这张临床图归为 Mollie Maggia 的肖像。界面提供自主显示/隐藏按钮。
- `image7.png`：论文 Fig.1.3，用户标识为 Mollie Maggia。
- `image8.png`：论文 Fig.1.4，用户标识为 Grace Fryer。
- `image9.png`：论文 Fig.1.5，1928 年报纸剪报。

本轮核对了剪报标题、时间线和人物姓名，但没有找到上述两张肖像的原始收藏馆藏记录。最终论文/公开版本仍应补齐原始图像出处。没有为另外三位女性配上生成或猜测的肖像。

七段声音为本轮原创合成原型，生成脚本在聊天工作区 `work/room3-v2/generate_story_audio.py`。
不是历史录音、真实医疗设备测量或真实女性劳工录音。没有加入未经来源确认的人声证词。

原有开源体积光的 MIT 许可证和版本记录继续见 `Assets/Room3/SOURCES.md`。

## 备份

修改前的 ROOM3 场景与整套 Room3 资源复制到：
`D:/NABA/Tesi/My project/Library/Room3Backups/BeforeFourZoneStory_20261112`。

安装前另有场景备份，路径见 `Library/Room3StoryBuild.json`。
原有的 ROOM1、ROOM2 场景文件没有为这两个结局加入永久的删除行为；小镇入口的关闭发生在运行时。
