using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KanbanApp.Views;

/// Renders a semi-transparent snapshot of the dragged card that follows the
/// cursor during a drag-and-drop operation. WPF's DragDrop has no built-in
/// drag-image support, so this is the standard Adorner-based way to get one.
public class DragAdorner : Adorner
{
    private readonly Rectangle _visual;
    private Point _offset;

    public DragAdorner(UIElement adornedElement, UIElement dragged, double opacity) : base(adornedElement)
    {
        IsHitTestVisible = false;
        _visual = new Rectangle
        {
            Width = dragged.RenderSize.Width,
            Height = dragged.RenderSize.Height,
            Fill = new VisualBrush(dragged),
            Opacity = opacity
        };
    }

    public void UpdatePosition(double x, double y)
    {
        _offset = new Point(x, y);
        InvalidateArrange();
    }

    protected override Size MeasureOverride(Size constraint)
    {
        _visual.Measure(new Size(_visual.Width, _visual.Height));
        return _visual.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _visual.Arrange(new Rect(new Point(0, 0), new Size(_visual.Width, _visual.Height)));
        return finalSize;
    }

    public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
    {
        var group = new GeneralTransformGroup();
        group.Children.Add(base.GetDesiredTransform(transform));
        group.Children.Add(new TranslateTransform(_offset.X, _offset.Y));
        return group;
    }

    protected override int VisualChildrenCount => 1;

    protected override Visual GetVisualChild(int index) => _visual;
}
