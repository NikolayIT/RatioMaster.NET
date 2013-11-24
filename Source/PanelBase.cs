using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace RatioMaster_source
{
    [DefaultEvent("Expanded"), DefaultProperty("Text")]
    public abstract class PanelBase : Panel
    {
        [Category("Property Changed"), Description("Occurs AllowCollapse property changed")]
        public event EventHandler AllowCollapseChanged;
        [Category("Property Changed"), Description("Occurs BevelStyle property changed")]
        public event EventHandler BevelStyleChanged;
        [Description("Occurs when the panel collapsed"), Category("Action")]
        public event EventHandler Collapsed;
        [Description("Occurs when the panel expanded"), Category("Action")]
        public event EventHandler Expanded;
        [Description("Occurs IsCollapse property changed"), Category("Property Changed")]
        public event EventHandler IsCollapseChanged;

        public PanelBase()
        {
            this.bevelStyle = BevelStyles.Flat;
            this.isCollapsed = false;
            this.fullHeight = 0;
            this.allowCollapse = true;
            base.SetStyle(ControlStyles.ContainerControl, true);
            base.SetStyle(ControlStyles.ResizeRedraw, true);
            base.SetStyle(ControlStyles.Selectable, true);
            base.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        }

        public void Collapse()
        {
            this.fullHeight = base.Height;
            base.Height = this.GetCaptionHeight();
            this.isCollapsed = true;
            base.SetStyle(ControlStyles.FixedHeight, true);
            base.Invalidate();
            this.OnCollapsed(new EventArgs());
            this.OnIsCollapseChanged(new EventArgs());
        }

        public void Expand()
        {
            this.isCollapsed = false;
            base.Height = this.fullHeight;
            base.SetStyle(ControlStyles.FixedHeight, false);
            base.Invalidate();
            this.OnExpanded(new EventArgs());
            this.OnIsCollapseChanged(new EventArgs());
        }

        protected int GetCaptionHeight()
        {
            return (this.Font.Height + 4);
        }

        protected virtual void OnAllowCollapseChanged(EventArgs e)
        {
            if (this.AllowCollapseChanged != null)
            {
                this.AllowCollapseChanged(this, e);
            }
        }

        protected virtual void OnBevelStyleChanged(EventArgs e)
        {
            if (this.BevelStyleChanged != null)
            {
                this.BevelStyleChanged(this, e);
            }
        }

        protected virtual void OnCollapsed(EventArgs e)
        {
            if (this.Collapsed != null)
            {
                this.Collapsed(this, e);
            }
        }

        protected virtual void OnExpanded(EventArgs e)
        {
            if (this.Expanded != null)
            {
                this.Expanded(this, e);
            }
        }

        protected virtual void OnIsCollapseChanged(EventArgs e)
        {
            if (this.IsCollapseChanged != null)
            {
                this.IsCollapseChanged(this, e);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (this.allowCollapse && (e.Y <= this.GetCaptionHeight()))
            {
                this.IsCollapsed = !this.IsCollapsed;
            }
            base.OnMouseDown(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.isCollapsed)
            {
                base.Height = this.GetCaptionHeight();
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.Invalidate();
            base.OnTextChanged(e);
        }

        public bool ShouldSerializeSize()
        {
            return false;
        }

        [DefaultValue(true), Category("Behavior"), Description("Indicate whether the user can collapse/expand panel by clicking on caption")]
        public bool AllowCollapse
        {
            get
            {
                return this.allowCollapse;
            }
            set
            {
                if (value != this.allowCollapse)
                {
                    this.allowCollapse = value;
                    base.Invalidate();
                    this.OnAllowCollapseChanged(new EventArgs());
                }
            }
        }

        [Description("Style of panel's bevel"), Category("Appearance")]
        public BevelStyles BevelStyle
        {
            get
            {
                return this.bevelStyle;
            }
            set
            {
                if (this.bevelStyle != value)
                {
                    this.bevelStyle = value;
                    base.Invalidate();
                    this.OnBevelStyleChanged(new EventArgs());
                }
            }
        }

        [Category("Layout"), Description("Control size in the expanded state")]
        public Size ExpandSize
        {
            get
            {
                if (!this.isCollapsed)
                {
                    return base.Size;
                }
                return new Size(base.Width, this.fullHeight);
            }
            set
            {
                if (!this.isCollapsed)
                {
                    base.Size = value;
                }
                else if (value != base.Size)
                {
                    this.fullHeight = value.Height;
                    base.Width = value.Width;
                }
            }
        }

        [DefaultValue(false), Description("Indicate whether the panel is collapsed now"), Category("Behavior")]
        public bool IsCollapsed
        {
            get
            {
                return this.isCollapsed;
            }
            set
            {
                if (this.isCollapsed != value)
                {
                    if (value)
                    {
                        this.Collapse();
                    }
                    else
                    {
                        this.Expand();
                    }
                }
            }
        }

        [Browsable(true)]
        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                if (base.Text != value)
                {
                    base.Text = value;
                }
            }
        }


        private bool allowCollapse;
        private BevelStyles bevelStyle;
        private int fullHeight;
        private bool isCollapsed;
    }
    public enum BevelStyles
    {
        None,
        Flat,
        Single,
        Double,
        Raised,
        Lowered,
        DoubleRaised,
        DoubleLowered,
        FrameRaised,
        FrameLowered
    }
    public enum PanelMarkerStyle
    {
        None,
        Arrow,
        PlusMinus
    }
    [ToolboxBitmap(typeof(MagneticPanel))]
    public class MagneticPanel : PanelBase
    {
        [Description("Occurs when the CaptionEndColor property changed"), Category("Property Changed")]
        public event EventHandler CaptionEndColorChanged;
        [Description("Occurs when the CaptionForeColor property changed"), Category("Property Changed")]
        public event EventHandler CaptionForeColorChanged;
        [Category("Property Changed"), Description("Occurs when the CaptionStartColor property changed")]
        public event EventHandler CaptionStartColorChanged;
        [Description("Occurs when the Marker property changed"), Category("Property Changed")]
        public event EventHandler MarkerChanged;

        public MagneticPanel()
        {
            this.marker = PanelMarkerStyle.Arrow;
            this.captionStartColor = Color.SaddleBrown;
            this.captionEndColor = Color.AntiqueWhite;
            this.captionForeColor = Color.White;
        }

        private void DisposeGraphics()
        {
        }

        private void DrawBevel(Graphics g)
        {
            int num1 = base.GetCaptionHeight();
            Rectangle rectangle1 = this.DisplayRectangle;
            rectangle1.Size -= new Size(1, 1);
            if (base.BevelStyle != BevelStyles.None)
            {
                Point point1 = rectangle1.Location;
                Point point2 = rectangle1.Location + rectangle1.Size;
                Point point3 = rectangle1.Location + rectangle1.Size;
                point3.Y = (point1.Y + num1) - 1;
                Pen pen1 = SystemPens.ControlLightLight;
                Pen pen2 = SystemPens.ControlDark;
                Pen pen3 = Pens.Black;
                Pen pen4 = Pens.Black;
                switch (base.BevelStyle)
                {
                    case BevelStyles.Flat:
                        pen3 = pen2;
                        pen4 = pen2;
                        break;

                    case BevelStyles.Single:
                        pen3 = Pens.Black;
                        pen4 = Pens.Black;
                        break;

                    case BevelStyles.Double:
                        pen3 = Pens.Black;
                        pen4 = Pens.Black;
                        break;

                    case BevelStyles.Raised:
                        pen3 = pen1;
                        pen4 = pen2;
                        break;

                    case BevelStyles.Lowered:
                        pen3 = pen2;
                        pen4 = pen1;
                        break;

                    case BevelStyles.DoubleRaised:
                        pen3 = pen1;
                        pen4 = pen2;
                        break;

                    case BevelStyles.DoubleLowered:
                        pen3 = pen2;
                        pen4 = pen1;
                        break;

                    case BevelStyles.FrameRaised:
                        pen3 = pen1;
                        pen4 = pen2;
                        break;

                    case BevelStyles.FrameLowered:
                        pen3 = pen2;
                        pen4 = pen1;
                        break;
                }
                g.DrawLine(pen3, point1.X, point1.Y, point1.X, point2.Y);
                g.DrawLine(pen3, point1.X, point1.Y, point2.X, point1.Y);
                g.DrawLine(pen4, point1.X, point2.Y, point2.X, point2.Y);
                g.DrawLine(pen4, point2.X, point1.Y, point2.X, point2.Y);
                g.DrawLine(pen4, point1.X + 1, point3.Y, point3.X - 1, point3.Y);
                if (((base.BevelStyle == BevelStyles.Double) || (base.BevelStyle == BevelStyles.DoubleRaised)) || (((base.BevelStyle == BevelStyles.DoubleLowered) || (base.BevelStyle == BevelStyles.FrameLowered)) || (base.BevelStyle == BevelStyles.FrameRaised)))
                {
                    if ((base.BevelStyle == BevelStyles.FrameLowered) || (base.BevelStyle == BevelStyles.FrameRaised))
                    {
                        Pen pen5 = pen3;
                        pen3 = pen4;
                        pen4 = pen5;
                    }
                    point1.X++;
                    point1.Y++;
                    point2.X--;
                    point2.Y--;
                    point3.X--;
                    point3.Y--;
                    g.DrawLine(pen3, point1.X, point1.Y, point1.X, point2.Y);
                    g.DrawLine(pen3, point1.X, point1.Y, point2.X, point1.Y);
                    g.DrawLine(pen4, point1.X, point2.Y, point2.X, point2.Y);
                    g.DrawLine(pen4, point2.X, point1.Y, point2.X, point2.Y);
                    g.DrawLine(pen4, point1.X + 1, point3.Y, point3.X - 1, point3.Y);
                }
            }
        }

        private void DrawCaption(Graphics g)
        {
            Color color1 = this.captionStartColor;
            Color color2 = this.captionEndColor;
            Color color3 = this.captionForeColor;
            Font font1 = this.Font;
            int num1 = base.GetCaptionHeight();
            Rectangle rectangle1 = this.DisplayRectangle;
            rectangle1.Height = num1;
            if ((rectangle1.Height > 0) && (rectangle1.Width > 0))
            {
                LinearGradientBrush brush1 = new LinearGradientBrush(rectangle1, color1, color2, LinearGradientMode.Horizontal);
                g.FillRectangle(brush1, rectangle1);
            }
            int num2 = 20;
            if (this.Marker == PanelMarkerStyle.None)
            {
                num2 = 5;
            }
            Point point1 = new Point(num1 / 2, num1 / 2);
            point1.X += 3;
            if (this.Marker == PanelMarkerStyle.Arrow)
            {
                GraphicsPath path1 = new GraphicsPath();
                path1.StartFigure();
                if (base.IsCollapsed)
                {
                    Point[] pointArray1 = new Point[] { new Point(point1.X - 3, point1.Y - 6), new Point(point1.X - 3, point1.Y + 6), new Point(point1.X + 3, point1.Y) };
                    path1.AddLines(pointArray1);
                }
                else
                {
                    point1.Y += 2;
                    Point[] pointArray2 = new Point[] { new Point(point1.X - 6, point1.Y - 3), new Point(point1.X + 6, point1.Y - 3), new Point(point1.X, point1.Y + 3) };
                    path1.AddLines(pointArray2);
                }
                path1.CloseFigure();
                g.FillPath(new SolidBrush(color3), path1);
            }
            if (this.Marker == PanelMarkerStyle.PlusMinus)
            {
                Rectangle rectangle2 = new Rectangle(point1.X - 4, point1.Y - 1, 9, 3);
                Rectangle rectangle3 = new Rectangle(point1.X - 1, point1.Y - 4, 3, 9);
                g.FillRectangle(new SolidBrush(color3), rectangle2);
                if (base.IsCollapsed)
                {
                    g.FillRectangle(new SolidBrush(color3), rectangle3);
                }
            }
            g.DrawString(this.Text, font1, new SolidBrush(color3), (float)num2, 2f);
        }

        protected virtual void OnCaptionEndColorChanged(EventArgs e)
        {
            if (this.CaptionEndColorChanged != null)
            {
                this.CaptionEndColorChanged(this, e);
            }
        }

        protected virtual void OnCaptionForeColorChanged(EventArgs e)
        {
            if (this.CaptionForeColorChanged != null)
            {
                this.CaptionForeColorChanged(this, e);
            }
        }

        protected virtual void OnCaptionStartColorChanged(EventArgs e)
        {
            if (this.CaptionStartColorChanged != null)
            {
                this.CaptionStartColorChanged(this, e);
            }
        }

        protected virtual void OnMarkerChanged(EventArgs e)
        {
            if (this.MarkerChanged != null)
            {
                this.MarkerChanged(this, e);
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
        }

        protected override void OnPaintBackground(PaintEventArgs pe)
        {
            int num1 = base.GetCaptionHeight();
            Rectangle rectangle1 = new Rectangle(0, num1, this.DisplayRectangle.Width, this.DisplayRectangle.Height - num1);
            if ((rectangle1.Height >= 0) && (rectangle1.Width >= 0))
            {
                GraphicsContainer container1 = pe.Graphics.BeginContainer();
                pe.Graphics.IntersectClip(rectangle1);
                base.OnPaintBackground(pe);
                pe.Graphics.EndContainer(container1);
            }
            this.DrawCaption(pe.Graphics);
            this.DrawBevel(pe.Graphics);
        }

        public bool ShouldSerializeCaptionEndColor()
        {
            return (this.captionEndColor != Color.AntiqueWhite);
        }

        public bool ShouldSerializeCaptionForeColor()
        {
            return (this.captionForeColor != Color.White);
        }

        public bool ShouldSerializeCaptionStartColor()
        {
            return (this.captionStartColor != Color.SaddleBrown);
        }


        [Description("Start color of the caption gradient"), Category("Appearance")]
        public Color CaptionEndColor
        {
            get
            {
                return this.captionEndColor;
            }
            set
            {
                if (this.captionEndColor != value)
                {
                    this.captionEndColor = value;
                    base.Invalidate();
                    this.OnCaptionEndColorChanged(new EventArgs());
                }
            }
        }

        [Description("Start color of the caption gradient"), Category("Appearance")]
        public Color CaptionForeColor
        {
            get
            {
                return this.captionForeColor;
            }
            set
            {
                if (this.captionForeColor != value)
                {
                    this.captionForeColor = value;
                    base.Invalidate();
                    this.OnCaptionForeColorChanged(new EventArgs());
                }
            }
        }

        [Description("Start color of the caption gradient"), Category("Appearance")]
        public Color CaptionStartColor
        {
            get
            {
                return this.captionStartColor;
            }
            set
            {
                if (this.captionStartColor != value)
                {
                    this.captionStartColor = value;
                    base.Invalidate();
                    this.OnCaptionStartColorChanged(new EventArgs());
                }
            }
        }

        [DefaultValue(1), Category("Appearance"), Description("Marker shape style")]
        public PanelMarkerStyle Marker
        {
            get
            {
                return this.marker;
            }
            set
            {
                if (this.marker != value)
                {
                    this.marker = value;
                    base.Invalidate();
                    this.OnMarkerChanged(new EventArgs());
                }
            }
        }


        private Color captionEndColor;
        private Color captionForeColor;
        private Color captionStartColor;
        private PanelMarkerStyle marker;
    }
}
