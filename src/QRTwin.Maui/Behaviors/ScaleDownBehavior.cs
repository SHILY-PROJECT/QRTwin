namespace QRTwin.Maui.Behaviors;

public sealed class ScaleDownBehavior : Behavior<View>
{
    public static readonly BindableProperty ScaleProperty =
        BindableProperty.Create(nameof(Scale), typeof(double), typeof(ScaleDownBehavior), 0.92);

    public double Scale
    {
        get => (double)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    private TapGestureRecognizer? _tapGestureRecognizer;
    private View? _attachedView;

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        _attachedView = bindable;
        _tapGestureRecognizer = new TapGestureRecognizer();
        _tapGestureRecognizer.Tapped += OnTapped;
        bindable.GestureRecognizers.Add(_tapGestureRecognizer);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        if (_tapGestureRecognizer is { } recognizer)
        {
            recognizer.Tapped -= OnTapped;
            bindable.GestureRecognizers.Remove(recognizer);
        }

        _attachedView = null;
        base.OnDetachingFrom(bindable);
    }

    private async void OnTapped(object? sender, TappedEventArgs e)
    {
        if (_attachedView is not { } view)
        {
            return;
        }

        await view.ScaleToAsync(Scale, 80, Easing.CubicOut);
        await view.ScaleToAsync(1, 80, Easing.CubicIn);
    }
}
