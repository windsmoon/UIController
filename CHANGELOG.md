# Changelog

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
