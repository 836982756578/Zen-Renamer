// Zen Renamer - Akıllı Toplu Dosya Yeniden Adlandırıcı
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using ComboBox = System.Windows.Controls.ComboBox;
using DragDropEffects = System.Windows.DragDropEffects;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Orientation = System.Windows.Controls.Orientation;
using TextBox = System.Windows.Controls.TextBox;

namespace ZenRenamer
{
    public class MainWindow : Window
    {
        private List<FileRenameItem> _items = new List<FileRenameItem>();
        private List<Tuple<string, string>> _undoHistory = new List<Tuple<string, string>>();

        private TextBox _txtFind;
        private TextBox _txtReplace;
        private TextBox _txtPrefix;
        private TextBox _txtSuffix;

        private CheckBox _chkNumbering;
        private TextBox _txtStartNum;
        private ComboBox _cbDigits;
        private ComboBox _cbNumPos;

        private ComboBox _cbCase;
        private CheckBox _chkReplaceSpaces;

        private StackPanel _tableContainer;
        private Border _dropPromptBorder;
        private TextBlock _txtSummary;
        private Button _btnApply;
        private Button _btnUndo;
        private Button _btnClear;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Title = "Zen Renamer";
            Width = 800;
            Height = 740;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            AllowDrop = true;

            DragOver += (s, e) =>
            {
                if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                    e.Effects = DragDropEffects.Copy;
                else
                    e.Effects = DragDropEffects.None;
                e.Handled = true;
            };

            Drop += (s, e) =>
            {
                if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                {
                    string[] paths = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                    AddPaths(paths);
                }
            };

            var outerBorder = new Border
            {
                Background = new SolidColorBrush(ColorFromHex("#121217")),
                BorderBrush = new SolidColorBrush(ColorFromHex("#2B2B3C")),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(14)
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Rules Panel
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Table
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Action Bar
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // --- 1. HEADER ---
            var header = new Border
            {
                Background = new SolidColorBrush(ColorFromHex("#181822")),
                CornerRadius = new CornerRadius(14, 14, 0, 0),
                Padding = new Thickness(18, 12, 14, 12),
                BorderBrush = new SolidColorBrush(ColorFromHex("#262638")),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            header.MouseLeftButtonDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var iconText = new TextBlock
            {
                Text = "☯️",
                FontSize = 22,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            var titleInfo = new StackPanel();
            var titleText = new TextBlock
            {
                Text = "Zen Renamer",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                FontFamily = new FontFamily("Segoe UI")
            };
            var subTitleText = new TextBlock
            {
                Text = "Akıllı Toplu Dosya Yeniden Adlandırıcı",
                Foreground = new SolidColorBrush(ColorFromHex("#8E8EA8")),
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI")
            };
            titleInfo.Children.Add(titleText);
            titleInfo.Children.Add(subTitleText);
            titleStack.Children.Add(iconText);
            titleStack.Children.Add(titleInfo);

            var controlStack = new StackPanel { Orientation = Orientation.Horizontal };
            var btnMin = CreateTitleButton("—", (s, e) => WindowState = WindowState.Minimized);
            var btnClose = CreateTitleButton("✕", (s, e) => Close());
            controlStack.Children.Add(btnMin);
            controlStack.Children.Add(btnClose);

            Grid.SetColumn(titleStack, 0);
            Grid.SetColumn(controlStack, 1);
            headerGrid.Children.Add(titleStack);
            headerGrid.Children.Add(controlStack);
            header.Child = headerGrid;
            Grid.SetRow(header, 0);
            mainGrid.Children.Add(header);

            // --- 2. RULES DASHBOARD ---
            var rulesBorder = new Border
            {
                Margin = new Thickness(16, 12, 16, 10),
                Padding = new Thickness(16, 12, 16, 14),
                Background = new SolidColorBrush(ColorFromHex("#191924")),
                BorderBrush = new SolidColorBrush(ColorFromHex("#2C2C3F")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10)
            };

            var rulesGrid = new Grid();
            rulesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rulesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rulesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Row 0: Find & Replace + Prefix & Suffix
            var r0 = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            r0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            r0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            r0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            r0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            r0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            r0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            r0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var pFind = CreateLabeledInput("🔍 Bul (Find):", out _txtFind);
            var pRep = CreateLabeledInput("✏️ Değiştir (Replace):", out _txtReplace);
            var pPre = CreateLabeledInput("➕ Ön Ek (Prefix):", out _txtPrefix);
            var pSuf = CreateLabeledInput("➕ Son Ek (Suffix):", out _txtSuffix);

            Grid.SetColumn(pFind, 0);
            Grid.SetColumn(pRep, 2);
            Grid.SetColumn(pPre, 4);
            Grid.SetColumn(pSuf, 6);
            r0.Children.Add(pFind);
            r0.Children.Add(pRep);
            r0.Children.Add(pPre);
            r0.Children.Add(pSuf);
            Grid.SetRow(r0, 0);
            rulesGrid.Children.Add(r0);

            // Row 1: Numbering Controls
            var r1 = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80, GridUnitType.Pixel) });
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120, GridUnitType.Pixel) });
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _chkNumbering = new CheckBox
            {
                Content = "🔢 Numaralandır",
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 14, 0)
            };
            _chkNumbering.Checked += (s, e) => TriggerPreview();
            _chkNumbering.Unchecked += (s, e) => TriggerPreview();

            var pStart = CreateLabeledInput("Başlangıç:", out _txtStartNum, "1");
            _txtStartNum.Width = 60;

            var pDigits = new StackPanel();
            pDigits.Children.Add(new TextBlock { Text = "Basamak:", Foreground = new SolidColorBrush(ColorFromHex("#9E9EBA")), FontSize = 11, Margin = new Thickness(0, 0, 0, 4) });
            _cbDigits = new ComboBox { Height = 28, Background = new SolidColorBrush(ColorFromHex("#222230")), Foreground = Brushes.White };
            _cbDigits.Items.Add("1 (1, 2, 3...)");
            _cbDigits.Items.Add("2 (01, 02...)");
            _cbDigits.Items.Add("3 (001, 002...)");
            _cbDigits.SelectedIndex = 1;
            _cbDigits.SelectionChanged += (s, e) => TriggerPreview();
            pDigits.Children.Add(_cbDigits);

            var pPos = new StackPanel();
            pPos.Children.Add(new TextBlock { Text = "Numara Konumu:", Foreground = new SolidColorBrush(ColorFromHex("#9E9EBA")), FontSize = 11, Margin = new Thickness(0, 0, 0, 4) });
            _cbNumPos = new ComboBox { Height = 28, Background = new SolidColorBrush(ColorFromHex("#222230")), Foreground = Brushes.White };
            _cbNumPos.Items.Add("Son Ek Olarak Ekle (İsim_01)");
            _cbNumPos.Items.Add("Ön Ek Olarak Ekle (01_İsim)");
            _cbNumPos.Items.Add("Tüm İsmin Yerine Koy (01)");
            _cbNumPos.SelectedIndex = 0;
            _cbNumPos.SelectionChanged += (s, e) => TriggerPreview();
            pPos.Children.Add(_cbNumPos);

            Grid.SetColumn(_chkNumbering, 0);
            Grid.SetColumn(pStart, 1);
            Grid.SetColumn(pDigits, 3);
            Grid.SetColumn(pPos, 5);
            r1.Children.Add(_chkNumbering);
            r1.Children.Add(pStart);
            r1.Children.Add(pDigits);
            r1.Children.Add(pPos);
            Grid.SetRow(r1, 1);
            rulesGrid.Children.Add(r1);

            // Row 2: Case Conversion & Spaces
            var r2 = new Grid();
            r2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            r2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16, GridUnitType.Pixel) });
            r2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var pCase = new StackPanel();
            pCase.Children.Add(new TextBlock { Text = "🔤 Harf Dönüşümü:", Foreground = new SolidColorBrush(ColorFromHex("#9E9EBA")), FontSize = 11, Margin = new Thickness(0, 0, 0, 4) });
            _cbCase = new ComboBox { Height = 28, Background = new SolidColorBrush(ColorFromHex("#222230")), Foreground = Brushes.White };
            _cbCase.Items.Add("Değişiklik Yapma");
            _cbCase.Items.Add("küçük harf yap");
            _cbCase.Items.Add("BÜYÜK HARF YAP");
            _cbCase.Items.Add("İlk Harfleri Büyük Yap");
            _cbCase.SelectedIndex = 0;
            _cbCase.SelectionChanged += (s, e) => TriggerPreview();
            pCase.Children.Add(_cbCase);

            _chkReplaceSpaces = new CheckBox
            {
                Content = "Boşlukları alt çizgi (_) yap",
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 16, 0, 0)
            };
            _chkReplaceSpaces.Checked += (s, e) => TriggerPreview();
            _chkReplaceSpaces.Unchecked += (s, e) => TriggerPreview();

            Grid.SetColumn(pCase, 0);
            Grid.SetColumn(_chkReplaceSpaces, 2);
            r2.Children.Add(pCase);
            r2.Children.Add(_chkReplaceSpaces);
            Grid.SetRow(r2, 2);
            rulesGrid.Children.Add(r2);

            rulesBorder.Child = rulesGrid;
            Grid.SetRow(rulesBorder, 1);
            mainGrid.Children.Add(rulesBorder);

            // --- 3. TABLE / LIST ---
            var tableOuter = new Grid { Margin = new Thickness(16, 0, 16, 10) };

            // Column Header
            var headerRow = new Border
            {
                Background = new SolidColorBrush(ColorFromHex("#161622")),
                BorderBrush = new SolidColorBrush(ColorFromHex("#272738")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(12, 8, 12, 8),
                Height = 32,
                VerticalAlignment = VerticalAlignment.Top
            };
            var hGrid = new Grid();
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28, GridUnitType.Pixel) });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110, GridUnitType.Pixel) });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28, GridUnitType.Pixel) });

            hGrid.Children.Add(CreateColumnHeader("Mevcut Dosya Adı", 0));
            hGrid.Children.Add(CreateColumnHeader("", 1));
            hGrid.Children.Add(CreateColumnHeader("Yeni Dosya Adı (Önizleme)", 2));
            hGrid.Children.Add(CreateColumnHeader("Durum", 3));
            headerRow.Child = hGrid;

            var scroll = new ScrollViewer
            {
                Margin = new Thickness(0, 34, 0, 0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            _tableContainer = new StackPanel();
            scroll.Content = _tableContainer;

            tableOuter.Children.Add(headerRow);
            tableOuter.Children.Add(scroll);

            // Drop Prompt when empty
            _dropPromptBorder = new Border
            {
                BorderBrush = new SolidColorBrush(ColorFromHex("#35354A")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(ColorFromHex("#14141E")),
                Padding = new Thickness(24),
                Margin = new Thickness(0, 34, 0, 0)
            };
            var dropStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            var dropIcon = new TextBlock { Text = "🏷️", FontSize = 42, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };
            var dropTitle = new TextBlock { Text = "Dosyaları veya Klasörü Buraya Bırakın", Foreground = Brushes.White, FontSize = 15, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center };
            var dropSub = new TextBlock { Text = "Fotoğraflar, belgeler, müzikler, videolar...", Foreground = new SolidColorBrush(ColorFromHex("#7E7E98")), FontSize = 12, Margin = new Thickness(0, 4, 0, 14), HorizontalAlignment = HorizontalAlignment.Center };

            var btnBrowse = new Button
            {
                Content = "📁 Dosyaları Seç",
                Padding = new Thickness(16, 8, 16, 8),
                Background = new SolidColorBrush(ColorFromHex("#2C2C3D")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            btnBrowse.Click += (s, e) => BrowseFiles();

            dropStack.Children.Add(dropIcon);
            dropStack.Children.Add(dropTitle);
            dropStack.Children.Add(dropSub);
            dropStack.Children.Add(btnBrowse);
            _dropPromptBorder.Child = dropStack;
            tableOuter.Children.Add(_dropPromptBorder);

            Grid.SetRow(tableOuter, 2);
            mainGrid.Children.Add(tableOuter);

            // --- 4. ACTION BAR ---
            var actionBorder = new Border
            {
                Margin = new Thickness(16, 0, 16, 12),
                Background = Brushes.Transparent
            };
            var barGrid = new Grid();
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Clear
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Undo
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Summary
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Apply Button

            _btnClear = new Button
            {
                Content = "🗑️ Temizle",
                Height = 38,
                Padding = new Thickness(12, 0, 12, 0),
                Foreground = new SolidColorBrush(ColorFromHex("#A0A0B8")),
                Background = new SolidColorBrush(ColorFromHex("#222230")),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 8, 0)
            };
            _btnClear.Click += (s, e) => ClearList();

            _btnUndo = new Button
            {
                Content = "↩️ Geri Al (Undo)",
                Height = 38,
                Padding = new Thickness(12, 0, 12, 0),
                Foreground = new SolidColorBrush(ColorFromHex("#A0A0B8")),
                Background = new SolidColorBrush(ColorFromHex("#222230")),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                IsEnabled = false
            };
            _btnUndo.Click += (s, e) => ExecuteUndo();

            _txtSummary = new TextBlock
            {
                Text = "0 dosya yüklendi",
                Foreground = new SolidColorBrush(ColorFromHex("#8E8EA8")),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 12, 0)
            };

            _btnApply = new Button
            {
                Content = "✨ Yeniden Adlandır",
                Height = 38,
                Padding = new Thickness(22, 0, 22, 0),
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(ColorFromHex("#F59E0B")),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            _btnApply.Click += (s, e) => ExecuteRename();

            Grid.SetColumn(_btnClear, 0);
            Grid.SetColumn(_btnUndo, 1);
            Grid.SetColumn(_txtSummary, 2);
            Grid.SetColumn(_btnApply, 3);

            barGrid.Children.Add(_btnClear);
            barGrid.Children.Add(_btnUndo);
            barGrid.Children.Add(_txtSummary);
            barGrid.Children.Add(_btnApply);
            actionBorder.Child = barGrid;

            Grid.SetRow(actionBorder, 3);
            mainGrid.Children.Add(actionBorder);

            // --- 5. FOOTER ---
            var footer = new Border
            {
                Background = new SolidColorBrush(ColorFromHex("#14141B")),
                BorderBrush = new SolidColorBrush(ColorFromHex("#232332")),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(18, 10, 18, 10),
                CornerRadius = new CornerRadius(0, 0, 14, 14)
            };
            var footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var authorText = new TextBlock
            {
                Text = "Geliştirici: Serkan ŞENTÜRK | © 2026 Tüm Hakları Saklıdır.",
                Foreground = new SolidColorBrush(ColorFromHex("#6E6E82")),
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI"),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            authorText.MouseLeftButtonDown += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://github.com/836982756578") { UseShellExecute = true }); } catch { }
            };

            var verText = new TextBlock
            {
                Text = "Zen Renamer v1.0.0",
                Foreground = new SolidColorBrush(ColorFromHex("#6E6E82")),
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI"),
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(authorText, 0);
            Grid.SetColumn(verText, 1);
            footerGrid.Children.Add(authorText);
            footerGrid.Children.Add(verText);
            footer.Child = footerGrid;

            Grid.SetRow(footer, 4);
            mainGrid.Children.Add(footer);

            outerBorder.Child = mainGrid;
            Content = outerBorder;
        }

        private TextBlock CreateColumnHeader(string text, int col)
        {
            var tb = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(ColorFromHex("#7E7E94")),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(tb, col);
            return tb;
        }

        private StackPanel CreateLabeledInput(string label, out TextBox tb, string defaultValue = "")
        {
            var p = new StackPanel();
            p.Children.Add(new TextBlock { Text = label, Foreground = new SolidColorBrush(ColorFromHex("#9E9EBA")), FontSize = 11, Margin = new Thickness(0, 0, 0, 4) });
            tb = new TextBox
            {
                Text = defaultValue,
                Height = 28,
                Background = new SolidColorBrush(ColorFromHex("#222230")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(ColorFromHex("#353548")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 4, 6, 4)
            };
            tb.TextChanged += (s, e) => TriggerPreview();
            p.Children.Add(tb);
            return p;
        }

        private void BrowseFiles()
        {
            var dlg = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Yeniden Adlandırılacak Dosyaları Seçin"
            };
            if (dlg.ShowDialog() == true)
            {
                AddPaths(dlg.FileNames);
            }
        }

        private void AddPaths(string[] paths)
        {
            if (paths == null) return;
            foreach (var p in paths)
            {
                if (Directory.Exists(p))
                {
                    try
                    {
                        string[] files = Directory.GetFiles(p);
                        AddPaths(files);
                    }
                    catch { }
                }
                else if (File.Exists(p))
                {
                    bool exists = false;
                    foreach (var item in _items)
                    {
                        if (string.Equals(item.FullPath, p, StringComparison.OrdinalIgnoreCase))
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        _items.Add(new FileRenameItem(p));
                    }
                }
            }
            TriggerPreview();
        }

        private RenameRules GetCurrentRules()
        {
            var r = new RenameRules
            {
                FindText = _txtFind.Text,
                ReplaceText = _txtReplace.Text,
                Prefix = _txtPrefix.Text,
                Suffix = _txtSuffix.Text,
                EnableNumbering = _chkNumbering.IsChecked == true,
                ReplaceSpaces = _chkReplaceSpaces.IsChecked == true
            };

            int start;
            if (int.TryParse(_txtStartNum.Text, out start))
                r.StartNumber = start;

            r.NumberDigits = _cbDigits.SelectedIndex + 1;

            switch (_cbNumPos.SelectedIndex)
            {
                case 1: r.NumPosition = NumberPosition.Prefix; break;
                case 2: r.NumPosition = NumberPosition.ReplaceName; break;
                default: r.NumPosition = NumberPosition.Suffix; break;
            }

            switch (_cbCase.SelectedIndex)
            {
                case 1: r.CaseOption = CaseMode.LowerCase; break;
                case 2: r.CaseOption = CaseMode.UpperCase; break;
                case 3: r.CaseOption = CaseMode.TitleCase; break;
                default: r.CaseOption = CaseMode.NoChange; break;
            }

            return r;
        }

        private void TriggerPreview()
        {
            if (_tableContainer == null) return;

            var rules = GetCurrentRules();
            RenamerEngine.PreviewRules(_items, rules);

            _tableContainer.Children.Clear();
            _dropPromptBorder.Visibility = _items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            _txtSummary.Text = string.Format("{0} dosya yüklendi", _items.Count);
            _btnApply.IsEnabled = _items.Count > 0;
            _btnClear.IsEnabled = _items.Count > 0;

            foreach (var item in _items)
            {
                var rowBorder = new Border
                {
                    Background = new SolidColorBrush(ColorFromHex("#181824")),
                    BorderBrush = new SolidColorBrush(ColorFromHex("#262638")),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(12, 6, 12, 6)
                };

                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28, GridUnitType.Pixel) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110, GridUnitType.Pixel) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28, GridUnitType.Pixel) });

                var txtOld = new TextBlock
                {
                    Text = item.GetOriginalFullName(),
                    Foreground = Brushes.White,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };

                var txtArrow = new TextBlock
                {
                    Text = "➔",
                    Foreground = new SolidColorBrush(ColorFromHex("#6E6E85")),
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var txtNew = new TextBlock
                {
                    Text = item.GetNewFullName(),
                    Foreground = new SolidColorBrush(item.OriginalFileName == item.NewFileName ? ColorFromHex("#8E8EA8") : ColorFromHex("#38BDF8")),
                    FontWeight = item.OriginalFileName == item.NewFileName ? FontWeights.Normal : FontWeights.SemiBold,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };

                var txtStatus = new TextBlock
                {
                    Text = item.Status,
                    Foreground = new SolidColorBrush(item.HasConflict ? ColorFromHex("#EF4444") : ColorFromHex("#10B981")),
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var btnDel = new Button
                {
                    Content = "✕",
                    Width = 20,
                    Height = 20,
                    Foreground = new SolidColorBrush(ColorFromHex("#7E7E94")),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var captured = item;
                btnDel.Click += (s, e) =>
                {
                    _items.Remove(captured);
                    TriggerPreview();
                };

                Grid.SetColumn(txtOld, 0);
                Grid.SetColumn(txtArrow, 1);
                Grid.SetColumn(txtNew, 2);
                Grid.SetColumn(txtStatus, 3);
                Grid.SetColumn(btnDel, 4);

                row.Children.Add(txtOld);
                row.Children.Add(txtArrow);
                row.Children.Add(txtNew);
                row.Children.Add(txtStatus);
                row.Children.Add(btnDel);

                rowBorder.Child = row;
                _tableContainer.Children.Add(rowBorder);
            }
        }

        private void ExecuteRename()
        {
            if (_items.Count == 0) return;
            var history = RenamerEngine.ApplyRename(_items);
            _undoHistory.AddRange(history);
            _btnUndo.IsEnabled = _undoHistory.Count > 0;
            TriggerPreview();
        }

        private void ExecuteUndo()
        {
            if (_undoHistory.Count == 0) return;
            RenamerEngine.UndoRename(_undoHistory, _items);
            _btnUndo.IsEnabled = false;
            TriggerPreview();
        }

        private void ClearList()
        {
            _items.Clear();
            _undoHistory.Clear();
            _btnUndo.IsEnabled = false;
            TriggerPreview();
        }

        private Button CreateTitleButton(string text, RoutedEventHandler onClick)
        {
            var btn = new Button
            {
                Content = text,
                Width = 32,
                Height = 28,
                Foreground = new SolidColorBrush(ColorFromHex("#A0A0BA")),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(4, 0, 0, 0)
            };
            btn.Click += onClick;
            return btn;
        }

        private static Color ColorFromHex(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
    }
}
