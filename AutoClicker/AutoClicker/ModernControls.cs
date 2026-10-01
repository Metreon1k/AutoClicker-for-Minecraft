using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using Button = System.Windows.Forms.Button;
using Color = System.Drawing.Color;
using Control = System.Windows.Forms.Control;
using Font = System.Drawing.Font;
using FontStyle = System.Drawing.FontStyle;
using Graphics = System.Drawing.Graphics;
using GraphicsPath = System.Drawing.Drawing2D.GraphicsPath;
using LineCap = System.Drawing.Drawing2D.LineCap;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using Panel = System.Windows.Forms.Panel;
using Pen = System.Drawing.Pen;
using Point = System.Drawing.Point;
using PointF = System.Drawing.PointF;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Drawing.Size;
using SmoothingMode = System.Drawing.Drawing2D.SmoothingMode;
using SolidBrush = System.Drawing.SolidBrush;

namespace AutoClicker;

public class RoundButton : Button
{
    public int CornerRadius { get; set; } = 12;
    public Color HoverColor { get; set; } = Color.FromArgb(255, 90, 120);
    public Color NormalColor { get; set; } = Color.FromArgb(230, 60, 90);

    private bool _hover;

    public RoundButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        ForeColor = Color.White;
        Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
        Cursor = Cursors.Hand;

        BackColor = Color.Transparent;

        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var g = pevent.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var bg = new SolidBrush(Parent?.BackColor ?? Color.FromArgb(18, 18, 26)))
            g.FillRectangle(bg, ClientRectangle);

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedRect(rect, CornerRadius);

        using var brush = new SolidBrush(_hover ? HoverColor : NormalColor);
        g.FillPath(brush, path);

        TextRenderer.DrawText(g, Text, Font, rect, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

public class CardPanel : Panel
{
    public int CornerRadius { get; set; } = 16;
    public Color FillColor { get; set; } = Color.FromArgb(30, 30, 42);

    public CardPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = new GraphicsPath();
        int d = CornerRadius * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();

        using var brush = new SolidBrush(FillColor);
        e.Graphics.FillPath(brush, path);
    }
}

public class ModernSlider : Control
{
    public int Minimum { get; set; } = 1;
    public int Maximum { get; set; } = 200;
    public event EventHandler? ValueChanged;

    private int _value = 30;
    public int Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, Minimum, Maximum);
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public Color TrackColor { get; set; } = Color.FromArgb(55, 55, 70);
    public Color AccentColor { get; set; } = Color.FromArgb(120, 140, 255);

    private bool _dragging;

    public ModernSlider()
    {
        Height = 24;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseDown(MouseEventArgs e) { _dragging = true; UpdateFromX(e.X); }
    protected override void OnMouseMove(MouseEventArgs e) { if (_dragging) UpdateFromX(e.X); }
    protected override void OnMouseUp(MouseEventArgs e) { _dragging = false; }

    private void UpdateFromX(int x)
    {
        double ratio = (double)(x - 10) / Math.Max(1, Width - 20);
        ratio = Math.Clamp(ratio, 0, 1);
        Value = Minimum + (int)Math.Round(ratio * (Maximum - Minimum));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        int cy = Height / 2;
        var trackRect = new Rectangle(10, cy - 3, Width - 20, 6);

        using (var brush = new SolidBrush(TrackColor))
            e.Graphics.FillRectangle(brush, trackRect);

        double ratio = (double)(Value - Minimum) / (Maximum - Minimum);
        int filledW = (int)(trackRect.Width * ratio);
        using (var brush = new SolidBrush(AccentColor))
            e.Graphics.FillRectangle(brush, new Rectangle(trackRect.X, trackRect.Y, filledW, trackRect.Height));

        int knobX = trackRect.X + filledW;
        using (var brush = new SolidBrush(Color.White))
            e.Graphics.FillEllipse(brush, knobX - 8, cy - 8, 16, 16);
        using (var pen = new Pen(AccentColor, 2))
            e.Graphics.DrawEllipse(pen, knobX - 8, cy - 8, 16, 16);
    }
}

public class ModernCheckBox : Control
{
    private bool _checked;
    public bool Checked
    {
        get => _checked;
        set { _checked = value; Invalidate(); CheckedChanged?.Invoke(this, EventArgs.Empty); }
    }

    public event EventHandler? CheckedChanged;

    public string LabelText { get; set; } = "Кликер активен";
    public Color BoxBgColor { get; set; } = Color.White;
    public Color BoxBorderColor { get; set; } = Color.FromArgb(200, 200, 210);
    public Color CheckColor { get; set; } = Color.Black;
    public Color TextColor { get; set; } = Color.White;
    public Color HoverBoxBg { get; set; } = Color.FromArgb(240, 240, 250);

    private bool _hover;

    public ModernCheckBox()
    {
        Height = 28;
        Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
        Cursor = Cursors.Hand;

        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;

        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        int boxSize = 22;
        int boxY = (Height - boxSize) / 2;
        var boxRect = new Rectangle(0, boxY, boxSize - 1, boxSize - 1);

        using (var path = RoundedRect(boxRect, 6))
        {
            Color fill = _checked ? BoxBgColor : (_hover ? HoverBoxBg : Color.FromArgb(230, 230, 235));
            using var bgBrush = new SolidBrush(fill);
            e.Graphics.FillPath(bgBrush, path);

            using var borderPen = new Pen(_checked ? Color.White : BoxBorderColor, 1.5f);
            e.Graphics.DrawPath(borderPen, path);
        }

        if (_checked)
        {
            using var pen = new Pen(CheckColor, 2.6f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            var p1 = new PointF(boxRect.X + 5f, boxRect.Y + boxSize / 2f);
            var p2 = new PointF(boxRect.X + boxSize / 2f - 1f, boxRect.Bottom - 5f);
            var p3 = new PointF(boxRect.Right - 5f, boxRect.Y + 5f);
            e.Graphics.DrawLines(pen, new[] { p1, p2, p3 });
        }

        var textRect = new Rectangle(boxSize + 10, 0, Width - boxSize - 10, Height);
        TextRenderer.DrawText(e.Graphics, LabelText, Font, textRect, TextColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}