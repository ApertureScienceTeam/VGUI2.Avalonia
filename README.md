# VGUI2.Avalonia

复刻 Valve VGUI2（Source 引擎经典橄榄绿 UI）的观感，移植为
**Avalonia 12 控件库**。每个 VGUI2 部件对应一个独立的控件类
（`VguiPanel`、`VguiButton`、`VguiSelect`…），控件类 + 默认 `ControlTheme`
+ 模板全部包含在 `VGUI2.Avalonia` 程序集内，主题自包含，不依赖
FluentTheme / SimpleTheme。

```
VGUI2.Avalonia.sln
├─ src/VGUI2.Avalonia/              控件库（net8.0，Avalonia 12.1.4）
│  ├─ Controls/                     控件类（公共 API）
│  └─ Themes/                       调色板 + 各控件 ControlTheme
├─ src/VGUI2.Avalonia.Demo/         Demo：复刻参考界面
└─ tools/VGUI2.Avalonia.RenderTool/ 无头渲染截图工具（视觉回归用）
```

## 控件清单（对应关系）

| VGUI2.js       | Avalonia 控件                          | 说明 |
|----------------|----------------------------------------|------|
| `Panel()`      | `VguiPanel : ContentControl`           | 基础面板，承载文字/子控件 |
| `Button()`     | `VguiButton : Button`                  | 浮雕按钮，按下时翻转为凹陷 + 文字缩进（CSS `:active`） |
| `Text/Password/Number()` | `VguiTextBox : TextBox`      | 内凹输入框；密码模式用 `PasswordChar` |
| `CheckBox()`   | `VguiCheckBox : CheckBox`              | 14px 统一灰橄榄边框方块，选中显示浅灰粗对勾，标签选中变金；焦点黑色点线框 |
| `Radio()`      | `VguiRadioButton : RadioButton`        | 圆形内凹井，选中显示金色 • |
| `Select()`     | `VguiComboBox : SelectingItemsControl` | 内凹头部 + `›` 箭头 + 浮雕下拉列表（`VguiComboBoxItem` 24px 行，悬停/选中金色）。容器由 `CreateContainerForItemOverride` 创建为 `VguiComboBoxItem`，点击即选中并收起列表 |
| `Slider()`     | `VguiSlider : RangeBase`               | 内凹滑轨 + 6px 浮雕滑块 + 13px 刻度 + 最小/最大标注 |
| `ProgressBar()`| `VguiProgressBar : RangeBase`          | 8px 金色块 / 12px 周期的分段进度条 |
| `Tab()`        | `VguiTabControl : TabControl` + `VguiTabItem : TabItem` | 选中页签增高并与内容面板融合（底边省略） |
| `Table()`      | `VguiTable : TemplatedControl`         | 按钮表头（点击排序）、列宽拖拽、18px 行、行选中高亮、`RowClick` / `RowDoubleClicked` |
| `Dialog()`     | `VguiWindow : Window`                  | 无系统装饰的自绘窗口：30px 标题栏（Steam 图标/标题/参考同款红 X 关闭钮）、可选 30px 底栏、四周 5px 缩放握边，`BeginMoveDrag` / `BeginResizeDrag` 拖动缩放；直角（DWM DONOTROUND） |

辅助类型：`VguiFrame`（Raised/Inset 两种浮雕边框的装饰器，对应 CSS
`.vgui-theme` / `.vgui-theme-inner`，模板内部使用）、`VguiConverters`。

滚动条（`ScrollBar` / `ScrollViewer` / `Thumb` / `RepeatButton`）作为库内部
的绘制原语随主题一并提供——18px 橄榄绿滚动条、两端箭头按钮、浮雕滑块，
与参考实现的 `::-webkit-scrollbar` 一致。

## 使用

```xml
<!-- App.axaml -->
<Application.Styles>
    <StyleInclude Source="avares://VGUI2.Avalonia/Themes/VGUI2Theme.axaml" />
</Application.Styles>
```

> **中文字体**：Avalonia 的 `FontFamily` 逗号列表只有第一个字体参与逐字
> 回退，中文会落到系统默认（雅黑）而不是参考的宋体。要在 AppBuilder 上
> 配置 `FontManagerOptions.FontFallbacks` 把 CJK 区段路由到宋体，
> 见 `src/VGUI2.Avalonia.Demo/Program.cs`。

```xml
xmlns:controls="using:VGUI2.Avalonia.Controls"

<controls:VguiWindow Title="测试窗口" Width="515" Height="415">
    <controls:VguiTabControl Width="500" Height="330">
        <controls:VguiTabItem Header="多人游戏">
            <Canvas>
                <!-- VGUI2 的绝对定位：Canvas.Left / Canvas.Top -->
                <controls:VguiPanel Content="文本框" Width="60" Height="20"
                                    Canvas.Top="10" Canvas.Left="10" />
                <controls:VguiTextBox Width="400" Height="20"
                                      Canvas.Top="10" Canvas.Left="80" />
            </Canvas>
        </controls:VguiTabItem>
    </controls:VguiTabControl>
</controls:VguiWindow>
```

表格：

```csharp
table.Columns.Add(new VguiTableColumn { Key = "Server", Header = "服务器", Width = 100 });
table.ItemsSource = servers;          // 按 Key 反射属性 / 字段 / IDictionary 取值
table.RowClick += (s, e) => { /* e.Item, e.Index */ };
```

下拉框：`ItemsSource` + `DisplayMemberBinding="{Binding Label}"` +
`SelectedIndex` / `SelectedItem`，与参考的 `setDatas / setSelected` 对应。

## 设计要点（符合 Avalonia 12 规范）

- 全部公共控件为 `Vgui*` 类，模板部件遵循 `PART_*` 命名契约
  （已对照 Avalonia 12.1.4 源码核对 `TextBox` / `TabControl` / `ScrollBar` 等的
  `TemplatePart` 约定）。
- 状态用伪类表达：`:pressed`、`:checked`、`:selected`、`:pointerover`、
  `:vertical` / `:horizontal`。
- 可换肤：颜色 / 字体 / 尺寸全部集中在 `Themes/Palette.axaml`
  （`VguiBrushBg`、`VguiBrushAccent`…，取自参考 CSS 的 `:root` 变量），
  通过 `DynamicResource` 引用，可整包替换资源实现换色。
- `VguiWindow` 通过 `StyleKeyOverride` 指回自身类型，确保隐式主题命中
  自定义模板。

## Demo

`src/VGUI2.Avalonia.Demo` 复刻参考界面：主窗口（页签内文本框、密码框、
数字框、单选、多选、下拉、滑块联动数字框、分段进度条 + / − 按钮）与
CS 1.6 服务器列表表格窗口（可排序、可拖拽列宽、行选择）。

```bash
dotnet run --project src/VGUI2.Avalonia.Demo
```

`tools/VGUI2.Avalonia.RenderTool` 用 Avalonia Headless 渲染两张截图，
可用于视觉回归：

```bash
dotnet run --project tools/VGUI2.Avalonia.RenderTool -- shots/
```

