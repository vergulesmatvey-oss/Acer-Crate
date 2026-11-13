using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Data;
using AcerCrate.Core.Settings;
using AcerCrate.App.Controls;
using AcerCrate.App.Helpers;
using AcerCrate.App.Services;
using AcerCrate.App.ViewModels;
using AcerCrate.Core.Control;
using AcerCrate.Core.Hardware;
namespace AcerCrate.App.Views;
public sealed partial class WorkspacePage : Page
{
    private readonly StackPanel body = new() { Spacing = 18, Padding = new Thickness(36,24,36,36), MaxWidth = 1600 };
    private Panel? cardHost;
    private string currentPage = "";
    private TextBlock? updated;
    private readonly DeviceSession session = App.Current.Services.GetRequiredService<DeviceSession>();
    private readonly MonitorViewModel monitor = App.Current.Services.GetRequiredService<MonitorViewModel>();
    private readonly List<HistoryChart> charts = [];
    public WorkspacePage() { InitializeComponent(); Content = new ScrollView { Content = body }; Loaded += (_, _) => monitor.PropertyChanged += Redraw; Unloaded += (_, _) => monitor.PropertyChanged -= Redraw; SizeChanged += (_, _) => ResizeMonitoring(); }
    private void Redraw(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName == nameof(MonitorViewModel.Revision)) { foreach (var c in charts) c.Revision = monitor.Revision; UpdateTime(); } }
    private static TextBlock Text(string s, double size = 14) => new() { Text = s, FontSize = size, TextWrapping = TextWrapping.Wrap };
    private void Card(string title, UIElement child)
    {
        var panel = new StackPanel { Spacing = 14 }; panel.Children.Add(Text(title,20)); panel.Children.Add(child);
        (cardHost ?? body).Children.Add(new Border { Style = (Style)Application.Current.Resources["CardStyle"], Child = panel });
    }
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        body.Children.Clear(); charts.Clear(); cardHost = body; updated = null;
        var page = e.Parameter as string ?? "modes";
        currentPage = page;
        var title = page switch { "dashboard" => T("Обзор"), "fans" => T("Охлаждение"), "system" => T("Производительность и питание"), "lighting" => T("Подсветка"), "settings" or "preferences" => T("Настройки"), "monitoring" => T("Мониторинг"), "automation" => T("Автоматизация"), "diagnostics" => T("Диагностика"), _ => T("Режимы работы") };

        body.Children.Add(new PageHeading { Title = title, Description = currentPage == "monitoring" ? CrateUi.L("Live history and sensor readings", "История и показания датчиков") : CrateUi.L("Device tools and controls", "Инструменты и управление устройством"), Glyph = currentPage == "monitoring" ? "\uE9D9" : "\uE713" });
        if (page is "automation" or "diagnostics")
        {
            var columns = new TileGrid { MaxColumns = 2, MinColumnWidth = 440, Spacing = 20 };
            body.Children.Add(columns); cardHost = columns;
        }
        
        if (page == "dashboard")
        {
            Card(T("Ваш ритм работы"), new ModePanel());
            var grid = new Grid { ColumnSpacing = 14 }; for(int i=0;i<4;i++) grid.ColumnDefinitions.Add(new ColumnDefinition());
            Metric(grid,0,T("ПРОЦЕССОР"),nameof(MonitorViewModel.CpuTemperatureText),nameof(MonitorViewModel.CpuLoadText));
            Metric(grid,1,T("ВИДЕОКАРТА"),nameof(MonitorViewModel.GpuTemperatureText),nameof(MonitorViewModel.GpuLoadText));
            for(int i=0;i<2;i++) { var f=monitor.Fans.ElementAtOrDefault(i); var panel=new StackPanel { Spacing=12 }; panel.Children.Add(Text(i==0?T("ВЕНТИЛЯТОР CPU"):T("ВЕНТИЛЯТОР GPU"),10)); var value=Text("—",30); if(f is not null) Bind(value,TextBlock.TextProperty,f,"RpmText",false); panel.Children.Add(value);panel.Children.Add(Text(T("об/мин · по данным контроллера"),10)); var border=new Border { Style=(Style)Application.Current.Resources["CardStyle"], Child=panel };Grid.SetColumn(border,i+2);grid.Children.Add(border); }
            body.Children.Add(grid);
            Chart(T("Температура процессора · последние показания"),monitor.CpuHistory,25,100);
            var power = Text(T("Питание: ожидание данных")); if(session.Latest is {} t) power.Text=t.OnAcPower?T("Питание от сети"):T("Работа от батареи"); Card(T("Состояние системы"),power);
        }
        else if (page == "fans") Cooling();
        else if (page == "system") Performance();
        else if (page == "lighting") Lighting();
        else if (page is "settings" or "preferences") Preferences();
        else
        if (page == "modes")
        {
            Card(T("Баланс под вашу задачу"), new ModePanel());
            Card(T("Как работают профили"), Text(T("Тихий: только штатное автоматическое охлаждение, без принудительного минимума оборотов. Сбалансированный: штатный режим с автоматическим или ручным охлаждением. Производительность: доступный штатный профиль, ручное охлаждение на поддерживаемых моделях. Разгон и неподтверждённые лимиты мощности не применяются.")));
        }
        else if (page == "monitoring")
        {
            BuildMonitoring();
        }
        else if (page == "automation")
        {
            Card(T("Сеть и батарея"),Text(T("Без явного правила сохраняется выбор пользователя; ограничения прошивки могут временно снизить доступный режим. Настройка ниже задаёт отдельный штатный режим на батарее. Профиль шума и запрет ручного охлаждения сохраняются.")));
            var combo = new ComboBox { Header = T("Штатный режим на батарее"), HorizontalAlignment = HorizontalAlignment.Stretch, IsEnabled = !App.Current.Options.Preview };
            var modes = session.Capabilities.OperatingModes.Where(m => m is OperatingMode.Quiet or OperatingMode.Eco or OperatingMode.Balanced).ToList();
            combo.Items.Add(T("Сохранять выбранный режим")); foreach(var m in modes) combo.Items.Add(AcerCrate.App.Localization.Names.OperatingMode(m));
            combo.SelectedIndex = session.Settings.Profile.BatteryOperatingMode is { } b ? modes.IndexOf(b)+1 : 0;
            combo.SelectionChanged += async (_,_) => { await session.SetProfileAsync(session.Settings.Profile with { BatteryOperatingMode = combo.SelectedIndex > 0 ? modes[combo.SelectedIndex-1] : null }); };
            Card(T("Правило питания"),combo);
            var button = new Button { Content = T("Настроить горячую клавишу открытия") }; button.Click += (_,_) => App.Current.Services.GetRequiredService<NavigationService>().Navigate("settings"); Card(T("Быстрый доступ"),button);
            var hotkeys=new StackPanel();var prefs=App.Current.Services.GetRequiredService<SettingsViewModel>();prefs.Attach();Toggle(hotkeys,T("Сочетания профилей"),T("Ctrl+Alt+1 — тихий; Ctrl+Alt+2 — сбалансированный; Ctrl+Alt+3 — производительность"),prefs,"ProfileHotkeys");Card(T("Горячие клавиши профилей"),hotkeys);
        }
        else
        {
            Card(T("Устройство"),Text(F("Модель: {0}\nCPU: {1}\nGPU: {2}\nBIOS: {3}\nСоединение: {4}\nСлужба: {5}\nМощность GPU и версия EC: требуется отдельная проверка", session.DeviceName ?? T("нет данных"), session.CpuName ?? T("нет данных"), session.GpuName ?? T("нет данных"), session.BiosVersion ?? T("нет данных"), ConnectionStatus(session.State), T(session.UsesService ? "Да" : "Нет"))));
            Card(T("Поддержка"),Text(F("Штатные режимы: {0}\nУправляемых вентиляторов: {1}\nCoolBoost: {2}\nПодсветка определяется исходными EC/HID-механизмами. Отключённые возможности не включаются принудительно.", string.Join(", ", session.Capabilities.OperatingModes.Select(AcerCrate.App.Localization.Names.OperatingMode)), session.Capabilities.ControllableFans.Count, T(session.Capabilities.CoolBoost ? "Да" : "Нет"))));
            var details = Text(T("Чтение диагностики…"),12); details.IsTextSelectionEnabled = true; Card(T("Журнал обнаружения"),new Expander { Header = T("Ответы встроенного ПО"), IsExpanded = false, Content = new ScrollView { Height = 240, Content = details } });
            try { details.Text = await session.GetDiagnosticsAsync(); } catch (Exception error) { details.Text = error.Message; }
            var result = Text(""); var export = new Button { Content = T("Экспортировать диагностику") };
            export.Click += async (_,_) => { try { var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AcerCrate","exports"); Directory.CreateDirectory(folder); var file = Path.Combine(folder,$"diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.txt"); await File.WriteAllTextAsync(file, await session.GetDiagnosticsAsync()); result.Text = file; } catch(Exception ex) { result.Text = T(ex.Message); } };
            body.Children.Add(export); body.Children.Add(result);
            Card(T("Проверки"),Text(T("Программные тесты и измерения ноутбука фиксируются раздельно в отчёте поставки. Показания температуры, оборотов и уровень шума не подменяются демонстрационными значениями.")));
        }
    }

    private void BuildMonitoring()
    {
        body.Children.Add(Text(T("Последние 5 минут · шкалы подписаны в °C, %, об/мин · пропуски означают отсутствие показаний"), 12));
        var grid = new TileGrid { MinColumnWidth = 300, MaxColumns = 3, Spacing = 16 };
        void Add(string title, object source, string property, HistoryBuffer history, double max, string unit, bool temperature)
        {
            var panel = new StackPanel { Spacing = 6 };
            var header = new Grid(); header.ColumnDefinitions.Add(new ColumnDefinition()); header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.Children.Add(new TextBlock { Text = title, FontSize = 14, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });
            var value = new TextBlock { FontSize = 22, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
            Bind(value, TextBlock.TextProperty, source, property, false); Grid.SetColumn(value, 1); header.Children.Add(value); panel.Children.Add(header);
            var chart = new HistoryChart { Height = 180, Series = history, Minimum = 0, Maximum = max, AxisUnit = unit, ShowAxes = true, UseTemperatureColors = temperature, Revision = monitor.Revision };
            charts.Add(chart); panel.Children.Add(chart);
            grid.Children.Add(new Border { Style = (Style)Application.Current.Resources["CardStyle"], Padding = new Thickness(16), Child = panel });
        }
        Add(T("CPU · температура"), monitor, nameof(MonitorViewModel.CpuTemperatureText), monitor.CpuHistory, 100, "°C", true);
        Add(T("GPU · температура"), monitor, nameof(MonitorViewModel.GpuTemperatureText), monitor.GpuHistory, 100, "°C", true);
        Add(T("CPU · загрузка"), monitor, nameof(MonitorViewModel.CpuLoadText), monitor.CpuLoadHistory, 100, "%", false);
        Add(T("GPU · загрузка"), monitor, nameof(MonitorViewModel.GpuLoadText), monitor.GpuLoadHistory, 100, "%", false);
        foreach (var fan in monitor.Fans) Add(fan.Name, fan, nameof(FanReadingViewModel.RpmText), fan.RpmHistory, 8000, T("об/мин"), false);
        body.Children.Add(grid);
        updated = Text("", 12); updated.Opacity = .65; body.Children.Add(updated);
        ResizeMonitoring(); UpdateTime();
    }

    private void ResizeMonitoring()
    {
        if (currentPage != "monitoring") return;
        var height = Math.Clamp((ActualHeight - 360) / 2, 140, 330);
        foreach (var chart in charts) chart.Height = height;
    }

    private void UpdateTime()
    {
        if (updated is not null) updated.Text = session.Latest is {} sample ? F("Обновлено {0:HH:mm:ss} · {1} · температура спящего GPU может не передаваться", sample.Timestamp.ToLocalTime(), (DateTime.UtcNow - sample.Timestamp.ToUniversalTime() < TimeSpan.FromSeconds(6) ? T("данные устройства") : T("ожидание новых показаний"))) : T("Ожидание показаний устройства");
    }

    private static void Bind(FrameworkElement target, DependencyProperty property, object source, string path, bool twoWay=true)
    { target.SetBinding(property,new Binding { Source=source,Path=new PropertyPath(path),Mode=twoWay?BindingMode.TwoWay:BindingMode.OneWay }); }
    private void Metric(Grid grid,int col,string label,string valuePath,string detailPath)
    {
        var panel=new StackPanel { Spacing=12 }; panel.Children.Add(Text(label,10)); var value=Text("—",36); Bind(value,TextBlock.TextProperty,monitor,valuePath,false);panel.Children.Add(value);var detail=Text("—",12);Bind(detail,TextBlock.TextProperty,monitor,detailPath,false);panel.Children.Add(detail);var b=new Border { Style=(Style)Application.Current.Resources["CardStyle"],Child=panel };Grid.SetColumn(b,col);grid.Children.Add(b);
    }
    private static void Toggle(StackPanel panel,string title,string note,object vm,string property,bool enabled=true)
    {
        var row=new Grid { Padding=new Thickness(0,14,0,14),ColumnSpacing=24 };row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });
        var text=new StackPanel { Spacing=4 };text.Children.Add(Text(title,15));var caption=Text(note,12);caption.Opacity=.6;text.Children.Add(caption);row.Children.Add(text);
        var toggle=new ToggleSwitch { IsEnabled=enabled && !App.Current.Options.Preview,OffContent="",OnContent="",VerticalAlignment=VerticalAlignment.Center }; Bind(toggle,ToggleSwitch.IsOnProperty,vm,property);Grid.SetColumn(toggle,1);row.Children.Add(toggle);panel.Children.Add(row);
    }
    private static ComboBox Combo(string header,object items,object vm,string path,string? display=null)
    { var combo=new ComboBox { Header=header,ItemsSource=(System.Collections.IEnumerable)items,HorizontalAlignment=HorizontalAlignment.Stretch,DisplayMemberPath=display??"",IsEnabled=header==T("Тема оформления") || !App.Current.Options.Preview };Bind(combo,ComboBox.SelectedIndexProperty,vm,path);return combo; }
    private void Cooling()
    {
        var vm=App.Current.Services.GetRequiredService<FanControlViewModel>();
        Card(T("Политика охлаждения"),new ModePanel());
        var panel=new StackPanel { Spacing=14 };var reason=Text(vm.ManualAllowed?T("Ручное управление доступно"):T("Ручное управление доступно в сбалансированном и производительном режимах. Тихий профиль всегда использует автоматику."),13);panel.Children.Add(reason);
        var combo=Combo(T("Управление вентиляторами"),FanControlViewModel.ModeNames,vm,"ModeIndex");Bind(combo,IsEnabledProperty,vm,"ManualAllowed",false);panel.Children.Add(combo);
        foreach(var f in vm.ManualFans) { var slider=new Slider { Header=f.Name+T(" · добавка к автоматике, %"),Minimum=0,Maximum=100,StepFrequency=1 };Bind(slider,Slider.ValueProperty,f,"Percent");Bind(slider,IsEnabledProperty,vm,"ManualAllowed",false);panel.Children.Add(slider);
            var kind=Combo(T("Способ управления · ")+f.Name,new[]{T("Постоянная добавка"),T("Температурная кривая")},f,"KindIndex");Bind(kind,IsEnabledProperty,vm,"ManualAllowed",false);panel.Children.Add(kind);
            var curve=new FanCurveEditor { Height=240 };Bind(curve,FanCurveEditor.CurveProperty,f.Curve,"Curve");var expander=new Expander { Header=T("Кривая · ")+f.Name,Content=curve,HorizontalAlignment=HorizontalAlignment.Stretch };Bind(expander,IsEnabledProperty,vm,"ManualAllowed",false);panel.Children.Add(expander); }
        if(vm.ManualFans.Count==0) panel.Children.Add(Text(T("Управляемые вентиляторы не обнаружены.")));
        Toggle(panel,T("Температурная помощь"),T("Дополнительные обороты при приближении к температурному пределу"),vm,"AutoBoostEnabled",vm.ManualAllowed);
        Card(T("Вентиляторы"),panel);
        var protection=new StackPanel();Toggle(protection,T("Возвращать штатное управление при выходе"),T("Прошивка продолжит управлять охлаждением"),vm,"RestoreAutoOnExit");if(vm.CoolBoostAvailable) Toggle(protection,"CoolBoost",T("Штатное усиление охлаждения Acer"),vm,"CoolBoost",vm.ManualAllowed);Card(T("Защита и штатные функции"),protection);
    }
    private void Performance()
    {
        var vm=App.Current.Services.GetRequiredService<SystemViewModel>(); Card(T("Профиль устройства"),new ModePanel());
        var panel=new StackPanel { Spacing=14 };panel.Children.Add(Text($"CPU · {session.CpuName??T("нет показаний")}\nGPU · {session.GpuName??T("нет показаний")}"));
        panel.Children.Add(Text(T("PL1 / PL2, TGP, андервольтинг: настройка не подтверждена для этой конфигурации. Числовые лимиты мощности не записываются."),13));
        var combo=new ComboBox { Header=T("План электропитания Windows"),ItemsSource=vm.PowerPlans,DisplayMemberPath="Name",HorizontalAlignment=HorizontalAlignment.Stretch,IsEnabled=vm.PowerPlansAvailable&&!App.Current.Options.Preview };Bind(combo,ComboBox.SelectedItemProperty,vm,"SelectedPowerPlan");panel.Children.Add(combo);Card(T("Питание"),panel);
        var keyboard=new StackPanel();Toggle(keyboard,T("Клавиша Windows"),T("Разрешить системные сочетания"),vm,"WindowsKey",vm.WindowsKeyAvailable);Toggle(keyboard,"Fn Lock",T("Поведение функционального ряда"),vm,"FnLock",vm.FnLockAvailable);Card(T("Клавиатура"),keyboard);
        var battery=App.Current.Services.GetRequiredService<BatteryViewModel>();var power=new StackPanel();Toggle(power,T("Ограничить заряд до 80 %"),T("Штатный лимит Acer при подтверждённой поддержке"),battery,"ChargeLimit",battery.ChargeLimitAvailable);Toggle(power,T("Зарядка USB при выключении"),T("Штатное питание USB-порта"),battery,"UsbCharging",battery.UsbChargingAvailable);Card(T("Аккумулятор и USB"),power);
    }
    private void Lighting()
    {
        var vm=App.Current.Services.GetRequiredService<LightingViewModel>();var panel=new StackPanel { Spacing=18 };
        if(!vm.Available) {Card(T("Подсветка не обнаружена"),Text(T("На этом устройстве управляющий компонент ещё не подтвердил доступный EC/HID-интерфейс. Набор цветов и эффектов определяется аппаратной модификацией.")));return;}
        panel.Children.Add(Combo(T("Устройство"),vm.Devices,vm,"DeviceIndex","Name"));panel.Children.Add(Combo(T("Яркость"),vm.BrightnessNames,vm,"BrightnessIndex"));
        if(vm.HasEffects) { var mode=new ComboBox { Header=T("Режим"),ItemsSource=new[]{T("Постоянный цвет"),T("Эффект")},HorizontalAlignment=HorizontalAlignment.Stretch };Bind(mode,ComboBox.SelectedIndexProperty,vm,"ModeIndex");panel.Children.Add(mode);panel.Children.Add(Combo(T("Эффект"),vm.Effects,vm,"EffectIndex","Name"));
        var speed=new Slider { Header=T("Скорость эффекта"),Minimum=vm.SpeedMinimum,Maximum=vm.SpeedMaximum,StepFrequency=1 };Bind(speed,Slider.ValueProperty,vm,"Speed");panel.Children.Add(speed);
        if(vm.Directions.Count>0)panel.Children.Add(Combo(T("Направление"),vm.Directions,vm,"DirectionIndex"));
        var effectColor=new ColorPicker { IsAlphaEnabled=false,IsColorSliderVisible=false };Bind(effectColor,ColorPicker.ColorProperty,vm,"EffectColor");var effectExpander=new Expander { Header=T("Цвет эффекта"),Content=effectColor };panel.Children.Add(effectExpander); }
        var color=new ColorPicker { IsAlphaEnabled=false,IsColorSliderVisible=false,IsHexInputVisible=true };Bind(color,ColorPicker.ColorProperty,vm,"AllZonesColor");panel.Children.Add(Text(T("Общий цвет"),16));panel.Children.Add(color);
        foreach(var zone in vm.Zones) {var button=new Button { Content=zone.Name+T(" · цвет зоны") };var picker=new ColorPicker { IsAlphaEnabled=false };Bind(picker,ColorPicker.ColorProperty,zone,"Color");button.Flyout=new Flyout { Content=picker };panel.Children.Add(button);}
        if(vm.HasKeys) {var keyboard=new PerKeyKeyboard { Source=vm,Height=220 };Bind(keyboard,PerKeyKeyboard.RevisionProperty,vm,"PreviewRevision",false);panel.Children.Add(keyboard);}
        Card(T("Свет, который вам подходит"),panel);
        Card(T("Совместимость"),Text(T("Сохранены исходные драйверы подсветки, поддерживаемые эффекты, зоны и восстановление настроек. Цветовой редактор показывает цвет клавиатуры, оформление приложения остаётся нейтральным.")));
    }
    private void Preferences()
    {
        var vm=App.Current.Services.GetRequiredService<SettingsViewModel>();vm.Attach();
        var startup=new StackPanel();Toggle(startup,T("Запускать вместе с Windows"),T("Текущее состояние проверяется в Windows"),vm,"StartWithWindows");Toggle(startup,T("Запускать свёрнутым в трей"),T("При автоматическом запуске после входа"),vm,"StartMinimized");Toggle(startup,T("Закрывать окно в трей"),T("Управление продолжает работать в фоне"),vm,"CloseToTray");Toggle(startup,T("Открывать кнопкой NitroSense"),T("Показывает новое окно из трея; приложение должно быть запущено"),vm,"OpenWithNitroSenseKey");Toggle(startup,T("Обычные уведомления"),T("Сообщения об ошибках управления остаются видимыми"),vm,"Notifications");Card(T("Запуск и быстрый доступ"),startup);
        var polling = new ComboBox { Header=T("Период опроса датчиков"), ItemsSource=new[]{T("1 секунда"),T("2 секунды"),T("3 секунды")}, SelectedIndex=Math.Clamp(session.Settings.Profile.SensorIntervalSeconds,1,3)-1, IsEnabled=!App.Current.Options.Preview };
        polling.SelectionChanged+=async(_,_)=>await session.SetProfileAsync(session.Settings.Profile with { SensorIntervalSeconds=polling.SelectedIndex+1 });Card(T("Фоновая работа"),polling);
        var look=new StackPanel { Spacing=18 };look.Children.Add(Combo(T("Тема оформления"),SettingsViewModel.ThemeNames,vm,"ThemeIndex"));Card(T("Оформление"),look);
        var backup=new StackPanel { Spacing=12 };var path=new TextBox { Header=T("Файл резервной копии"),Text=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AcerCrate","configuration.json") };backup.Children.Add(path);var result=Text("");
        var save=new Button { Content=T("Сохранить конфигурацию"),IsEnabled=!App.Current.Options.Preview };save.Click+=async(_,_)=>{try {Directory.CreateDirectory(Path.GetDirectoryName(path.Text)!);var data=new ConfigBackup(App.Current.Services.GetRequiredService<SettingsService>().Current,session.Settings.Profile,session.Settings.Lighting);await File.WriteAllTextAsync(path.Text,JsonSerializer.Serialize(data,SettingsJson.Options));result.Text=T("Копия сохранена");}catch(Exception ex){result.Text=T(ex.Message);}};backup.Children.Add(save);
        var restore=new Button { Content=T("Восстановить конфигурацию"),IsEnabled=!App.Current.Options.Preview };restore.Click+=async(_,_)=>{try {var data=JsonSerializer.Deserialize<ConfigBackup>(await File.ReadAllTextAsync(path.Text),SettingsJson.Options)??throw new InvalidDataException(T("Пустая конфигурация"));if(data.Profile.LaptopMode is not {} mode) throw new InvalidDataException(T("Выберите один из трёх профилей перед сохранением копии"));await session.SetProfileAsync(LaptopModePolicy.Select(data.Profile,mode,session.Capabilities));await session.SetLightingAsync(data.Lighting);App.Current.Services.GetRequiredService<SettingsService>().Update(_=>data.User);vm.Attach();result.Text=T("Настройки загружены. Состояние оборудования проверяется отдельно; оформление обновится при перезапуске.");}catch(Exception ex){result.Text=T(ex.Message);}};backup.Children.Add(restore);backup.Children.Add(result);Card(T("Резервная копия"),backup);
        Card(T("О приложении"),Text(T("Acer Crate · новая оболочка на основе OpenSense 0.3.6 · GPL-3.0. Аппаратные проверки и ограничения — в отчёте поставки.")));
    }
    private sealed record ConfigBackup(UserSettings User, ControlProfile Profile, AcerCrate.Core.Lighting.LightingConfig Lighting);
    private void Chart(string title, AcerCrate.App.Helpers.HistoryBuffer buffer, double min, double max)
    { var chart = new HistoryChart { Height = 180, Series = buffer, Minimum = min, Maximum = max }; charts.Add(chart); Card(title,chart); }
}



