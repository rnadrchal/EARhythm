using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
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