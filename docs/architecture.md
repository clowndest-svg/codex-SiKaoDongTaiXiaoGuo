# 架构与隔离边界

- `App.cs`：独立透明 WPF 窗口、目标切换、点击/拖动、无重叠定时读取、单实例与本组件状态持久化。
- `AutoFollow.cs`：从正在运行的 Codex 安装路径发现宿主；只读解析主窗口日志的 ownerRoutePath。只支持已验证的主窗口及 /local/UUID 路由。读取有大小上限，日志以 ReadWrite/Delete 共享方式短时打开。
- `DesktopLink.cs`：命名管道连接与超时。唯一允许发出的请求是 initialize 和 thread-follower-read-model-settings。其他请求在发送之前抛出异常；不声明任何聊天所有权，也不处理桌面端分发给其他客户端的工作。
- `Catalog.cs`：通过 Windows 自带 SQLite 查询有限聊天标题元数据，SQLITE_OPEN_READONLY 并检查 sqlite3_db_readonly。默认档位仅从 CODEX_HOME 的配置只读继承。
- `AnimatedPortrait.cs`：纹理网格、光照与矢量星光；隐藏时停止动画，最多 30 帧，粒子数量有界。六档共享照明参数以保持银白、淡紫配色一致；Ultra 保留独立特效分支。

不修改桌面端安装文件、不持有其安装文件句柄、不联网，不与其更新器交互。初始化连接本身仍使用内部桌面协议，因此这里保证的是隔离及失败暂停策略，不是未知未来版本的兼容承诺。

设置及诊断只写组件自身 runtime；单实例互斥量避免重复显示。安装脚本只停止目标安装目录下的 DragonDisplay.exe，拒绝覆盖指向其他安装的同名计划任务，更新前备份并保留 runtime。卸载脚本只移除本组件任务和匹配进程，保留文件。
