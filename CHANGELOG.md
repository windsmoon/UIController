# Changelog

## 0.5.1

### English

- Fixed controller state animation cleanup so runtime state switching only stops an existing tween when both the target and property match the new animated property.
- Kept full tween cleanup for panel destruction and edit-mode preview state changes.
- Updated edit-mode preview switching so previous preview tweens are moved to their final values before being cleared.

### 中文

- 修复 controller state 动画清理逻辑：运行时切换状态时，只有 target 和 property 都相同的旧 tween 才会被停止。
- 保留 panel 销毁和编辑器 edit mode 预览切换状态时的全量 tween 清理。
- 调整编辑器 edit mode 预览切换逻辑，旧预览 tween 会先应用到最终值再被清理。

## 0.5.0

### English

- Added per-property animation delay settings and runtime DOTween delay playback.
- Added `GetStateCount` and `GetStateAnimationDuration` runtime APIs for querying controller state data and the longest animated state duration.
- Added editor and runtime animation support for `int` properties.
- Added built-in properties for `CanvasGroupInteractable`, `LocalRotation`, `ImageFillAmount`, `TextMeshFontSize`, `Pivot`, `AnchorMin`, and `AnchorMax`.
- Changed `UIControllerProperty<T>.GetTargetValue` to provide a default implementation that returns the stored value.
- Marked `GetValueText` obsolete because it is no longer used by the editor workflow.

### 中文

- 新增每个属性单独配置动画延迟，并在运行时通过 DOTween 播放延迟。
- 新增 `GetStateCount` 和 `GetStateAnimationDuration` 运行时 API，用于查询 controller 的 state 数量和某个 state 的最长动画时长。
- 新增 `int` 属性的编辑器编辑和运行时动画支持。
- 新增内置属性：`CanvasGroupInteractable`、`LocalRotation`、`ImageFillAmount`、`TextMeshFontSize`、`Pivot`、`AnchorMin`、`AnchorMax`。
- `UIControllerProperty<T>.GetTargetValue` 改为提供默认实现，默认返回已保存的属性值。
- 将 `GetValueText` 标记为过时，因为当前编辑器工作流不再使用它。

## 0.4.0

### English

- Changed the serialized data layout so each controller owns its target list and controlled property names, while each state stores values by matching target and property order.
- Added `UIControllerTargetData` and data version tracking for the new controller target structure.
- Kept legacy target binding, state index, and target name data under editor-only legacy fields for manual migration.
- Updated runtime state application to use controller target and property indexes instead of legacy target-name dictionaries.
- Reworked the UIController Inspector and panel editor window around controller-level targets, property lists, synchronized state data, capture, preview, and target/property editing.
- Added single-panel and selected-folder prefab migration tools for upgrading legacy state-level target/property data to the new controller target structure.
- Added migration warnings, prefab modification recording, scene dirty handling, and README documentation for the v0.4.0 data migration workflow.
- Added a release zip creation tool that can package UIController with or without bundled DOTween.
- Updated the demo scene, sample hover controller wiring, screenshots, and WebGL release build for the new data structure.
- Updated the GitHub Pages deployment workflow to support Brotli-compressed Unity WebGL build outputs by preparing decompressed Pages artifacts before upload.
- Updated `.gitignore` to ignore local Codex agent instruction files.

### 中文

- 调整序列化数据结构：每个 controller 自己保存 target 列表和受控 property 名称，每个 state 按对应的 target/property 顺序保存状态值。
- 新增 `UIControllerTargetData` 和数据版本记录，用于新的 controller target 结构。
- 将旧版 target binding、state index、target name 数据保留为 editor-only 旧字段，仅用于手动迁移。
- 运行时状态应用改为按 controller target/property 索引匹配数据，不再依赖旧版 target 名称字典。
- 重做 UIController Inspector 和面板编辑窗口，围绕 controller 级 target、property 列表、状态同步、捕获、预览和 target/property 编辑工作流组织。
- 新增单个 panel 和选中文件夹 prefab 的旧数据迁移工具，可把旧版 state-level target/property 数据升级到新的 controller target 结构。
- 新增迁移警告、prefab 修改记录、scene dirty 标记，并在 README 中补充 v0.4.0 数据迁移说明。
- 新增 release zip 创建工具，可分别打包带 DOTween 和不带 DOTween 的 UIController 包。
- 更新演示场景、示例 hover controller 绑定、截图和 WebGL Release 构建，以适配新的数据结构。
- 更新 GitHub Pages 部署工作流，支持 Brotli 压缩的 Unity WebGL 构建产物，并在上传前准备解压后的 Pages artifact。
- 更新 `.gitignore`，忽略本地 Codex agent 指令文件。

## 0.3.0

### English

- Added a live WebGL demo published through GitHub Pages.
- Added a GitHub Actions workflow for deploying the Unity WebGL release build to GitHub Pages.
- Added Unity WebGL release build files and a Web Desktop Release build profile for the demo project.
- Added per-property animation ease and duration settings for animatable properties.
- Added editor UI for editing each animated property's Ease type and Duration directly in the UIController panel window.
- Updated runtime animation playback to use each property's own animation settings instead of a single fixed duration and easing.
- Properties now apply immediately when animation is disabled, forced off, or when animation duration is set to `0`.
- Replaced the demo video with a GIF so the demo can be previewed directly in README.
- Updated README and screenshots with the live demo link and animation configuration notes.

### 中文

- 新增通过 GitHub Pages 发布的在线 WebGL 演示页面。
- 新增 GitHub Actions 工作流，用于把 Unity WebGL Release 构建自动部署到 GitHub Pages。
- 新增 Unity WebGL Release 构建文件，以及演示工程使用的 Web Desktop Release 构建配置。
- 支持为每个可动画属性单独配置动画 Ease 类型和动画时长。
- 编辑器 UIController 面板窗口新增动画参数编辑入口，可直接编辑属性的 Ease 类型和 Duration。
- 运行时动画改为使用每个属性自己的动画配置，不再固定使用统一的动画时长和缓动类型。
- 当动画被禁用、强制关闭，或动画时长为 `0` 时，属性会立即应用目标值。
- 将演示视频替换为 GIF，方便在 README 中直接预览。
- 更新 README 和截图，补充在线演示入口与动画配置说明。

## 0.2.0

### English

- Bundled DOTween runtime and editor files with the package release.
- Added `HasController` and `HasControllerState` runtime APIs for checking whether a controller or state exists before switching UI state.
- Added demo media and screenshots for the UIController panel inspector and editor window.
- Expanded README with bilingual documentation, demo media, screenshots, runtime API notes, and installation guidance.
- Improved documentation around reusable control states, DOTween usage, and duplicate DOTween reference handling.

### 中文

- 在发布包中内置 DOTween 运行时和编辑器文件。
- 新增 `HasController` 和 `HasControllerState` 运行时 API，便于在切换 UI 状态前判断 Controller 或 State 是否存在。
- 新增 UIController 面板 Inspector 和编辑器窗口的演示素材与截图。
- 扩充 README，补充中英文文档、演示素材、截图、运行时 API 说明和安装指引。
- 完善可复用控件状态、DOTween 使用方式，以及重复 DOTween 引用处理的说明。

## 0.1.0

### English

- Initial Unity Package Manager package structure.
- Added runtime data models and `UIControllerPanel` for named UI controllers, state lists, target bindings, and property values.
- Added built-in UI properties for active state, anchored position, local scale, size delta, canvas group alpha, image color, TextMeshPro text, and TextMeshPro color.
- Added editor tooling for configuring controllers, targets, states, and properties in the Inspector and dedicated UIController panel window.
- Added editor capture, value editing, and preview workflows for UI state setup.
- Added sample, documentation, tests, package metadata, license, and a local Unity development project.

### 中文

- 初始化 Unity Package Manager 包结构。
- 新增运行时数据模型和 `UIControllerPanel`，支持命名 UI Controller、状态列表、目标绑定和属性值配置。
- 新增内置 UI 属性，支持激活状态、锚点位置、本地缩放、尺寸、CanvasGroup 透明度、图片颜色、TextMeshPro 文本和 TextMeshPro 颜色。
- 新增编辑器工具，可在 Inspector 和独立 UIController 面板窗口中配置 Controller、Target、State 和 Property。
- 新增编辑器内捕获、数值编辑和状态预览流程，便于配置 UI 状态。
- 新增示例、文档、测试目录、包元数据、许可证和本地 Unity 开发工程。
