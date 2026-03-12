using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using Blattwerk.ViewModels;

namespace Blattwerk.Views;

public partial class ImageOverlayView : UserControl
{
    private DependencyPropertyDescriptor? _sourceDescriptor;

    public ImageOverlayView()
    {
        InitializeComponent();
        PART_Image.SizeChanged += (_, _) => UpdateOverlay();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        // Interaktivität: Fokus/Events
        Focusable = true;
        IsManipulationEnabled = true;

        // Sicherstellen, dass Overlay Mausereignisse empfangen kann
        PART_Overlay.IsHitTestVisible = true;
        PART_Overlay.Background ??= Brushes.Transparent;
        PART_Overlay.Focusable = true;

        // Mouse + keyboard + wheel + manipulation
        PART_Overlay.MouseLeftButtonDown += OnOverlayMouseLeftButtonDown;
        this.PreviewMouseWheel += OnOverlayMouseWheel;
        KeyDown += OnKeyDownHandler;
        ManipulationDelta += OnManipulationDelta;
    }

    private void OnLoaded(object? s, RoutedEventArgs e)
    {
        // Watch Source property changes reliably in WPF
        _sourceDescriptor = DependencyPropertyDescriptor.FromProperty(Image.SourceProperty, typeof(Image));
        if (_sourceDescriptor != null)
            _sourceDescriptor.AddValueChanged(PART_Image, OnImageSourceChanged);

        // Ensure overlay is drawn for initial content
        UpdateOverlay();
    }

    private void OnUnloaded(object? s, RoutedEventArgs e)
    {
        if (_sourceDescriptor != null)
        {
            _sourceDescriptor.RemoveValueChanged(PART_Image, OnImageSourceChanged);
            _sourceDescriptor = null;
        }

        // detach handlers to avoid leaks
        PART_Overlay.MouseLeftButtonDown -= OnOverlayMouseLeftButtonDown;
        PART_Overlay.MouseWheel -= OnOverlayMouseWheel;
        KeyDown -= OnKeyDownHandler;
        ManipulationDelta -= OnManipulationDelta;
    }

    private void OnImageSourceChanged(object? sender, EventArgs e) => UpdateOverlay();

    // Bildquelle
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source), typeof(ImageSource), typeof(ImageOverlayView),
            new PropertyMetadata(null, (_, __) => ((ImageOverlayView)_).PART_Image.Source = (ImageSource)((DependencyPropertyChangedEventArgs)__).NewValue));

    public ImageSource? Source
    {
        get => (ImageSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    // Richtung
    public static readonly DependencyProperty DirectionProperty =
        DependencyProperty.Register(nameof(Direction), typeof(ImageDirection), typeof(ImageOverlayView),
            new PropertyMetadata(ImageDirection.Ltr, (_, __) => ((ImageOverlayView)_).UpdateOverlay()));

    public ImageDirection Direction
    {
        get => (ImageDirection)GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    // aktive Spalte/Zeile (0-basiert) — immer Bild-Pixel-Index (0 = erste Zeile/Spalte)
    public static readonly DependencyProperty ColumnOrRowProperty =
        DependencyProperty.Register(nameof(ColumnOrRow), typeof(int), typeof(ImageOverlayView),
            new PropertyMetadata(0, (_, __) => ((ImageOverlayView)_).UpdateOverlay()));

    public int ColumnOrRow
    {
        get => (int)GetValue(ColumnOrRowProperty);
        set => SetValue(ColumnOrRowProperty, value);
    }

    // Anzahl Segmente
    public static readonly DependencyProperty SegmentCountProperty =
        DependencyProperty.Register(nameof(SegmentCount), typeof(int), typeof(ImageOverlayView),
            new PropertyMetadata(1, (_, __) => ((ImageOverlayView)_).UpdateOverlay()));

    public int SegmentCount
    {
        get => (int)GetValue(SegmentCountProperty);
        set => SetValue(SegmentCountProperty, value);
    }

    // ----------------- Interaktionshandler -----------------

    // Mouse click: set ColumnOrRow depending on Direction (row for LTR/RTL, column for TopDown/BottomUp)
    private void OnOverlayMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
    {
        if (PART_Image.Source is not BitmapSource bs) return;

        Point pos = e.GetPosition(PART_Image);
        if (!TryGetImageMetrics(bs, out Rect rect, out int imgW, out int imgH, out double pixelW, out double pixelH))
            return;

        // only react if click within display rect
        if (!rect.Contains(pos)) return;

        if (Direction == ImageDirection.Ltr || Direction == ImageDirection.Rtl)
        {
            // ColumnOrRow is a row index here
            int row = (int)((pos.Y - rect.Y) / pixelH);
            row = Math.Clamp(row, 0, imgH - 1);
            ColumnOrRow = row;
        }
        else
        {
            // ColumnOrRow is a column index here
            int col = (int)((pos.X - rect.X) / pixelW);
            col = Math.Clamp(col, 0, imgW - 1);
            ColumnOrRow = col;
        }

        // ensure control has keyboard focus after click
        Keyboard.Focus(this);
        PART_Overlay.Focus();
        e.Handled = true;
    }

    // Mouse wheel: increment/decrement ColumnOrRow. wheel up -> decrement, down -> increment.
    private void OnOverlayMouseWheel(object? sender, MouseWheelEventArgs e)
    {
        if (PART_Image.Source is not BitmapSource bs) return;
        int delta = e.Delta > 0 ? -1 : 1;
        AdjustIndexBy(delta);
        e.Handled = true;
    }

    // Keyboard: arrows / page / home / end
    private void OnKeyDownHandler(object? sender, KeyEventArgs e)
    {
        if (PART_Image.Source is not BitmapSource bs) return;

        int step = 1;
        if (e.Key == Key.PageUp) step = -10;
        if (e.Key == Key.PageDown) step = 10;

        switch (e.Key)
        {
            case Key.Up:
                AdjustIndexBy(-step);
                e.Handled = true;
                break;
            case Key.Down:
                AdjustIndexBy(step);
                e.Handled = true;
                break;
            case Key.Left:
                AdjustIndexBy(-step);
                e.Handled = true;
                break;
            case Key.Right:
                AdjustIndexBy(step);
                e.Handled = true;
                break;
            case Key.Home:
                SetIndexToBoundary(isStart: true);
                e.Handled = true;
                break;
            case Key.End:
                SetIndexToBoundary(isStart: false);
                e.Handled = true;
                break;
        }
    }

    // Touch / gesture: use ManipulationDelta to swipe
    private void OnManipulationDelta(object? sender, ManipulationDeltaEventArgs e)
    {
        if (PART_Image.Source is not BitmapSource bs) return;

        var delta = e.DeltaManipulation.Translation;
        // Use horizontal swipe to change column, vertical swipe to change row based on Direction semantics
        const double threshold = 20.0; // pixels of movement to trigger
        if (Math.Abs(delta.X) > Math.Abs(delta.Y) && Math.Abs(delta.X) > threshold)
        {
            int steps = (int)(Math.Sign(delta.X) * Math.Ceiling(Math.Abs(delta.X) / 40.0));
            AdjustIndexBy(steps);
            e.Handled = true;
        }
        else if (Math.Abs(delta.Y) > threshold)
        {
            int steps = (int)(Math.Sign(delta.Y) * Math.Ceiling(Math.Abs(delta.Y) / 40.0));
            AdjustIndexBy(steps);
            e.Handled = true;
        }
    }

    // ----------------- Hilfsfunktionen für Index-Anpassung -----------------

    // Bewegt ColumnOrRow um delta (positiv -> weiter, negativ -> zurück)
    private void AdjustIndexBy(int delta)
    {
        if (PART_Image.Source is not BitmapSource bs) return;
        if (!TryGetImageMetrics(bs, out _, out int imgW, out int imgH, out _, out _)) return;

        int maxIndex = (Direction == ImageDirection.Ltr || Direction == ImageDirection.Rtl) ? imgH - 1 : imgW - 1;
        int newIndex = ColumnOrRow + delta;
        newIndex = Math.Clamp(newIndex, 0, Math.Max(0, maxIndex));
        if (newIndex != ColumnOrRow)
            ColumnOrRow = newIndex;
    }

    private void SetIndexToBoundary(bool isStart)
    {
        if (PART_Image.Source is not BitmapSource bs) return;
        if (!TryGetImageMetrics(bs, out _, out int imgW, out int imgH, out _, out _)) return;

        int target = isStart ? 0 : ((Direction == ImageDirection.Ltr || Direction == ImageDirection.Rtl) ? imgH - 1 : imgW - 1);
        ColumnOrRow = target;
    }

    // Liefert rectangle und Bilddimensionen sowie Pixelgrößen; false wenn nicht möglich
    private bool TryGetImageMetrics(BitmapSource bs, out Rect rect, out int imgW, out int imgH, out double pixelW, out double pixelH)
    {
        rect = GetImageDisplayRect(PART_Image, bs);
        imgW = bs.PixelWidth;
        imgH = bs.PixelHeight;
        if (rect.Width <= 0 || rect.Height <= 0 || imgW <= 0 || imgH <= 0)
        {
            pixelW = pixelH = 0;
            return false;
        }

        pixelW = rect.Width / imgW;
        pixelH = rect.Height / imgH;
        return true;
    }

    private void UpdateOverlay()
    {
        PART_Overlay.Children.Clear();

        if (PART_Image.Source is not BitmapSource bs) return;
        if (SegmentCount <= 0) return;

        // berechne Bild-Display-Rect innerhalb des Image-Steuerelements (Stretch=Uniform)
        var rect = GetImageDisplayRect(PART_Image, bs);
        if (rect.Width <= 0 || rect.Height <= 0) return;

        // Bildpixel-Größen in Anzeige-Koordinaten
        int imgW = bs.PixelWidth;
        int imgH = bs.PixelHeight;
        if (imgW <= 0 || imgH <= 0) return;

        double pixelW = rect.Width / imgW;
        double pixelH = rect.Height / imgH;

        // Linienfarbe / Highlight-Farbe
        var separatorBrush = new SolidColorBrush(Color.FromArgb(150, 200, 200, 200));
        separatorBrush.Freeze();
        var activeBrush = new SolidColorBrush(Color.FromArgb(200, 255, 80, 80));
        activeBrush.Freeze();

        // Segment-Grenzen: wenn horizontal segmentation (LTR/RTL) => vertical separators (columns)
        if (Direction == ImageDirection.Ltr || Direction == ImageDirection.Rtl)
        {
            double segW = rect.Width / SegmentCount;
            for (int i = 1; i < SegmentCount; i++)
            {
                double x = rect.X + i * segW;
                var line = new Line
                {
                    X1 = x,
                    Y1 = rect.Y,
                    X2 = x,
                    Y2 = rect.Y + rect.Height,
                    Stroke = separatorBrush,
                    StrokeThickness = 1,
                    SnapsToDevicePixels = true
                };
                PART_Overlay.Children.Add(line);
            }

            // ColumnOrRow ist HIER eine Zeile (row index in image coordinates).
            int row = Math.Clamp(ColumnOrRow, 0, imgH - 1);

            // Anzeige-Y für die obere Kante der Pixel‑Zeile
            double yTop = rect.Y + row * pixelH;

            // Zeichne exakt eine Pixel-Höhe hohe Markierung (im Display-Koordinatensystem).
            var activeRect = new Rectangle
            {
                Width = rect.Width,
                Height = Math.Max(1.0, pixelH), // mindestens 1 device-independent pixel
                Fill = activeBrush,
                SnapsToDevicePixels = true
            };
            Canvas.SetLeft(activeRect, rect.X);
            Canvas.SetTop(activeRect, yTop);
            PART_Overlay.Children.Add(activeRect);

            // ---- neue kleine Dreiecke links und rechts (außerhalb des Bildrechtecks) ----
            double triSize = Math.Max(6.0, Math.Min(16.0, pixelH * 2.0));
            double gap = 4.0; // Abstand außerhalb des Bildes
            double yCenter = yTop + (Math.Max(1.0, pixelH) / 2.0);

            // left triangle (points to the right)
            var left = new Polygon
            {
                Fill = activeBrush,
                Stroke = null,
                Points = new PointCollection
                {
                    new Point(rect.X - gap, yCenter),
                    new Point(rect.X - gap - triSize, yCenter - triSize/2),
                    new Point(rect.X - gap - triSize, yCenter + triSize/2)
                },
                IsHitTestVisible = false
            };
            PART_Overlay.Children.Add(left);

            // right triangle (points to the left)
            var right = new Polygon
            {
                Fill = activeBrush,
                Stroke = null,
                Points = new PointCollection
                {
                    new Point(rect.X + rect.Width + gap, yCenter),
                    new Point(rect.X + rect.Width + gap + triSize, yCenter - triSize/2),
                    new Point(rect.X + rect.Width + gap + triSize, yCenter + triSize/2)
                },
                IsHitTestVisible = false
            };
            PART_Overlay.Children.Add(right);
        }
        else // TopDown / BottomUp: vertical segmentation => horizontal separators; ColumnOrRow ist eine Spalte.
        {
            double segH = rect.Height / SegmentCount;
            for (int i = 1; i < SegmentCount; i++)
            {
                double y = rect.Y + i * segH;
                var line = new Line
                {
                    X1 = rect.X,
                    Y1 = y,
                    X2 = rect.X + rect.Width,
                    Y2 = y,
                    Stroke = separatorBrush,
                    StrokeThickness = 1,
                    SnapsToDevicePixels = true
                };
                PART_Overlay.Children.Add(line);
            }

            // ColumnOrRow ist HIER eine Spalte (column index in image coordinates).
            int col = Math.Clamp(ColumnOrRow, 0, imgW - 1);

            // Anzeige-X für die linke Kante der Pixel-Spalte
            double xLeft = rect.X + col * pixelW;

            // Zeichne exakt eine Pixel-Breite breite Markierung (im Display-Koordinatensystem).
            var activeRect = new Rectangle
            {
                Width = Math.Max(1.0, pixelW),
                Height = rect.Height,
                Fill = activeBrush,
                SnapsToDevicePixels = true
            };
            Canvas.SetLeft(activeRect, xLeft);
            Canvas.SetTop(activeRect, rect.Y);
            PART_Overlay.Children.Add(activeRect);

            // ---- neue kleine Dreiecke oben und unten (außerhalb des Bildrechtecks) ----
            double triSize = Math.Max(6.0, Math.Min(16.0, pixelW * 2.0));
            double gap = 4.0; // Abstand außerhalb des Bildes
            double xCenter = xLeft + (Math.Max(1.0, pixelW) / 2.0);

            // top triangle (points down)
            var top = new Polygon
            {
                Fill = activeBrush,
                Stroke = null,
                Points = new PointCollection
                {
                    new Point(xCenter, rect.Y - gap),
                    new Point(xCenter - triSize/2, rect.Y - gap - triSize),
                    new Point(xCenter + triSize/2, rect.Y - gap - triSize)
                },
                IsHitTestVisible = false
            };
            PART_Overlay.Children.Add(top);

            // bottom triangle (points up)
            var bottom = new Polygon
            {
                Fill = activeBrush,
                Stroke = null,
                Points = new PointCollection
                {
                    new Point(xCenter, rect.Y + rect.Height + gap),
                    new Point(xCenter - triSize/2, rect.Y + rect.Height + gap + triSize),
                    new Point(xCenter + triSize/2, rect.Y + rect.Height + gap + triSize)
                },
                IsHitTestVisible = false
            };
            PART_Overlay.Children.Add(bottom);
        }
    }

    // Berechnet das Rechteck, in dem das Bitmap tatsächlich innerhalb des Image-Steuerelements angezeigt wird
    private static Rect GetImageDisplayRect(Image imageCtrl, BitmapSource bs)
    {
        double imgW = bs.PixelWidth;
        double imgH = bs.PixelHeight;
        double ctrlW = imageCtrl.ActualWidth;
        double ctrlH = imageCtrl.ActualHeight;

        if (ctrlW <= 0 || ctrlH <= 0 || imgW <= 0 || imgH <= 0)
            return Rect.Empty;

        double ratioImg = imgW / imgH;
        double ratioCtrl = ctrlW / ctrlH;

        if (ratioCtrl > ratioImg)
        {
            // control breiter -> höhe füllen
            double scale = ctrlH / imgH;
            double displayH = ctrlH;
            double displayW = imgW * scale;
            double offsetX = (ctrlW - displayW) / 2.0;
            return new Rect(offsetX, 0, displayW, displayH);
        }
        else
        {
            // control höher -> breite füllen
            double scale = ctrlW / imgW;
            double displayW = ctrlW;
            double displayH = imgH * scale;
            double offsetY = (ctrlH - displayH) / 2.0;
            return new Rect(0, offsetY, displayW, displayH);
        }
    }
}